using PixelImmortal.Audio;
using PixelImmortal.Combat;
using PixelImmortal.Cultivation;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Equipment;
using PixelImmortal.Gacha;
using PixelImmortal.Loot;
using System;
using PixelImmortal.Enemies;
using PixelImmortal.Localization;
using PixelImmortal.Offline;
using PixelImmortal.Quests;
using PixelImmortal.Save;
using PixelImmortal.Progression;
using PixelImmortal.Skills;
using PixelImmortal.UI;
using PixelImmortal.Upgrades;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace PixelImmortal.Core
{
    /// <summary>Composes the Phase 1 prototype at startup. The scene only needs a camera and this component.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] PrototypeConfig config;

        public PlayerProgress Progress { get; private set; }
        public UpgradeSystem Upgrades { get; private set; }
        public CultivationSystem Cultivation { get; private set; }
        public BattleDirector Director { get; private set; }
        public PlayerUnit Player { get; private set; }
        public SaveCoordinator Saves { get; private set; }
        public QuestSystem Quests { get; private set; }
        public EquipmentSystem Equipment { get; private set; }
        public GachaSystem Gacha { get; private set; }
        public LootSystem Loot { get; private set; }
        public LocalizationService Loc { get; private set; }

        /// <summary>Tests and tools can redirect saves away from the real save folder.</summary>
        public static string SaveDirectoryOverride;

        // With "Enter Play Mode without domain reload" statics survive between play sessions; a stale
        // override would silently send a later session's saves to a temp folder. Reset it on every play start.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            SaveDirectoryOverride = null;
            UiKit.Skin = null;
            UiKit.ClickSound = null;
        }

        void Start()
        {
            if (config == null || config.scaling == null || config.economy == null || config.localizationCsv == null || config.maps == null || config.maps.Length == 0)
            {
                Debug.LogError("GameBootstrap: PrototypeConfig with scaling, economy, localization CSV and at least one map is required.", this);
                return;
            }

            // Mobile: a steady 60 fps cap, no vsync double-limiting.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            EnsureGlobalLight();
            EnsureEventSystem();

            var arena = new GameObject("Arena").AddComponent<ArenaView>();
            arena.Init(config.arenaSize, config.art);

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("GameBootstrap: no MainCamera in scene.", this);
                return;
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.depth = 0;
            cam.gameObject.AddComponent<ArenaCamera>().Configure(arena.Bounds, () => BattleHud.ArenaViewportFor(SafeAreaFitter.Normalized));

            var backdrop = new GameObject("BackdropCamera").AddComponent<Camera>();
            backdrop.clearFlags = CameraClearFlags.SolidColor;
            backdrop.backgroundColor = new Color(0.08f, 0.07f, 0.1f);
            backdrop.cullingMask = 0;
            backdrop.depth = -10;

            var registry = new EnemyRegistry();
            var progress = new PlayerProgress(config.economy);
            var upgrades = new UpgradeSystem(progress);
            var cultivation = new CultivationSystem(progress);
            var quests = new QuestSystem(config.economy, progress, () => DateTime.Now); // device-local time
            var equipment = new EquipmentSystem(progress);
            var gacha = new GachaSystem(progress);
            var loot = new LootSystem(progress);

            var saveSystem = new SaveSystem(SaveDirectoryOverride ?? SaveFlow.DefaultDirectory);
            var loadResult = SaveFlow.LoadAndApply(saveSystem, progress, quests, out var saveData);

            string language = LocalizationService.IsSupported(saveData.settings.language)
                ? saveData.settings.language
                : LocalizationService.DetectSystemLanguage();
            var loc = new LocalizationService(config.localizationCsv.text, language);

            var player = new GameObject("Player").AddComponent<PlayerUnit>();
            player.Init(config, registry, progress);
            var skills = player.gameObject.AddComponent<SkillRunner>();
            skills.Init(player, registry, progress, upgrades, new SkillVfxPool(new GameObject("Vfx").transform, 24), config.art);

            var ui = UIManager.Create();
            var hud = new GameObject("BattleHud").AddComponent<BattleHud>();
            var director = new GameObject("BattleDirector").AddComponent<BattleDirector>();
            Progress = progress; Upgrades = upgrades; Cultivation = cultivation; Director = director; Player = player;
            Quests = quests; Loc = loc; Equipment = equipment; Gacha = gacha; Loot = loot;
            var audio = new GameObject("Audio").AddComponent<AudioService>();
            audio.Init(saveData.settings.muted);
            var numbers = new GameObject("DamageNumbers").AddComponent<DamageNumbers>();
            numbers.Init(config.art);
            hud.Build(ui, director, player, progress, upgrades, cultivation, quests, equipment, gacha, loc, config.art, audio);
            director.EnemyKilled += e => loot.RollKill(e.IsBoss, e.IsElite);
            loot.Dropped += item => hud.ShowToast(loc.Format("toast.drop", loc.Get("item." + item.id), loc.Get("rarity." + item.rarity)));

            bool usable = loadResult.Status == LoadStatus.Loaded || loadResult.Status == LoadStatus.LoadedFromBackup;
            director.Init(config, player, registry, arena, hud, progress, usable ? saveData.world : null);

            var tracker = new GameObject("QuestTracker").AddComponent<QuestTracker>();
            tracker.Init(quests, director, progress.Wallet, upgrades);

            var coordinator = new GameObject("SaveCoordinator").AddComponent<SaveCoordinator>();
            coordinator.Init(saveSystem, saveData, progress, director, quests, loc);
            coordinator.Audio = audio;
            audio.MutedChanged += coordinator.MarkDirty;
            WireFeedback(audio, numbers, player, skills, director, upgrades, quests, cultivation, loot, gacha);
            Saves = coordinator;
            hud.Game.SaveNow = () => coordinator.SaveNow();
            hud.Game.ResetData = coordinator.ResetAndReload;
            var offlineService = new OfflineService(config);
            coordinator.Resumed += () => SaveFlow.ShowReturnMessage(coordinator, offlineService, progress, hud.Popup, loc);
            SaveFlow.ShowStartupMessages(loadResult, coordinator, offlineService, progress, hud.Popup, loc, DateTime.UtcNow.Ticks);
        }

        /// <summary>Connects gameplay events to sound effects and floating damage numbers. Gameplay never calls these directly.</summary>
        static void WireFeedback(AudioService audio, DamageNumbers numbers, PlayerUnit player, SkillRunner skills, BattleDirector director,
            UpgradeSystem upgrades, QuestSystem quests, CultivationSystem cultivation, LootSystem loot, GachaSystem gacha)
        {
            player.AttackFired += crit => audio.Play(crit ? Sfx.Crit : Sfx.Hit, 0.55f);
            player.Hurt += amount =>
            {
                audio.Play(Sfx.PlayerHurt);
                numbers.Show(player.transform.position + Vector3.up * 0.6f, amount, false, new Color(1f, 0.35f, 0.3f));
            };
            player.Died += () => audio.Play(Sfx.Death);
            player.Revived += () => audio.Play(Sfx.Revive);

            director.EnemyDamaged += (enemy, amount, crit) =>
                numbers.Show(enemy.transform.position + Vector3.up * 0.4f, amount, crit, crit ? new Color(1f, 0.85f, 0.2f) : Color.white);
            director.EnemyKilled += e => audio.Play(Sfx.Kill, e.IsBoss ? 1f : 0.45f);
            director.BossDefeatedEvent += () => audio.Play(Sfx.BossWin);
            director.BossLostEvent += () => audio.Play(Sfx.BossLose);
            director.MapChangedEvent += () => audio.Play(Sfx.MapChange);

            skills.Cast += s => audio.Play(s.kind == SkillKind.SingleTarget ? Sfx.CastSword
                : s.kind == SkillKind.Projectile ? Sfx.CastFire
                : s.kind == SkillKind.AreaAtTarget ? Sfx.CastIce
                : s.kind == SkillKind.SelfShield ? Sfx.CastShield : Sfx.CastBlades);

            upgrades.Purchased += () => audio.Play(Sfx.Buy);
            quests.Claimed += () => audio.Play(Sfx.Claim);
            cultivation.BrokeThrough += () => audio.Play(Sfx.Breakthrough);
            loot.Dropped += _ => audio.Play(Sfx.Drop);
            gacha.Pulled += _ => audio.Play(Sfx.Pull);
            UiKit.ClickSound = () => audio.Play(Sfx.Click, 0.7f);
        }

        static void EnsureGlobalLight()
        {
            if (FindAnyObjectByType<Light2D>() != null) return;
            var go = new GameObject("Global Light 2D");
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Global;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
