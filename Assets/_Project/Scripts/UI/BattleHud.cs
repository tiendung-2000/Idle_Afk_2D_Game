using System.Collections;
using PixelImmortal.Combat;
using PixelImmortal.Cultivation;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Equipment;
using PixelImmortal.Gacha;
using PixelImmortal.Localization;
using PixelImmortal.Progression;
using PixelImmortal.Quests;
using PixelImmortal.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Placeholder HUD built in code. Top and bottom bars sit outside the arena viewport.
    /// Every visible string comes from the LocalizationService.
    /// </summary>
    public class BattleHud : MonoBehaviour, IScreenFader
    {
        /// <summary>Arena viewport (screen fractions) inside the safe area, between the top and bottom bars.</summary>
        public static Rect ArenaViewportFor(Rect safe)
            => new Rect(safe.x, safe.y + safe.height * BottomPanelMaxY, safe.width, safe.height * (TopPanelMinY - BottomPanelMaxY));

        const float TopPanelMinY = 0.86f;
        const float BottomPanelMaxY = 0.16f;
        const float RefreshInterval = 0.1f;
        static readonly Color PanelColor = new Color(0.08f, 0.07f, 0.1f, 1f);
        static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.4f, 1f);
        static readonly float[] Speeds = { 1f, 2f, 4f };

        BattleDirector director;
        PlayerUnit player;
        PlayerProgress progress;
        QuestSystem quests;
        LocalizationService loc;

        Text titleText, waveText, linhThachText, linhKhiText, tinhHoaText, playerHpText, bossHpText, stateText, speedText, langText, questsLabel;
        RectTransform playerFill, bossFill;
        GameObject bossRow, bossButton;
        Image fade;
        int speedIndex;
        float nextRefresh;
        UIManager ui;
        Text bannerText;
        Text soundText;
        PixelImmortal.Audio.AudioService audio;

        public PopupPanel Popup { get; private set; }
        /// <summary>What feature screens use; the bootstrap fills in the save hooks once the coordinator exists.</summary>
        public GameContext Game { get; private set; }

        public void Build(UIManager uiManager, BattleDirector d,PlayerUnit p, PlayerProgress prog, UpgradeSystem upgrades, CultivationSystem cultivation,
            QuestSystem questSystem, EquipmentSystem equipment, GachaSystem gacha, LocalizationService localization, ArtCatalog art = null,
            PixelImmortal.Audio.AudioService audioService = null)
        {
            UiKit.Skin = UiSkin.From(art);
            ui = uiManager;
            director = d;
            player = p;
            progress = prog;
            quests = questSystem;
            loc = localization;

            // The UIManager owns the canvas and safe-area layers; the HUD lives on its HUD layer.
            var root = ui.HudLayer;

            // Top panel: title (+ language), wave, resources, player HP, boss HP
            var top = UiKit.Panel("TopPanel", root, new Vector2(0f, TopPanelMinY), Vector2.one, PanelColor, true);
            UiKit.AddFrame(top);
            titleText = UiKit.Label(top, 0.02f, 0.8f, 0.68f, 1f, 36, TextAnchor.MiddleCenter);
            audio = audioService;
            if (audio != null)
                UiKit.TextButton("SoundBtn", top, new Vector2(0.70f, 0.82f), new Vector2(0.83f, 0.98f), ButtonColor, "SFX", 24, () => audio.Toggle(), out soundText);
            UiKit.TextButton("LanguageBtn", top, new Vector2(0.85f, 0.82f), new Vector2(0.98f, 0.98f), ButtonColor, "", 26, () => loc.Toggle(), out langText);
            waveText = UiKit.Label(top, 0f, 0.6f, 1f, 0.8f, 32, TextAnchor.MiddleCenter);
            linhThachText = UiKit.Label(top, 0.08f, 0.4f, 0.34f, 0.6f, 24, TextAnchor.MiddleLeft);
            linhKhiText = UiKit.Label(top, 0.41f, 0.4f, 0.66f, 0.6f, 24, TextAnchor.MiddleLeft);
            tinhHoaText = UiKit.Label(top, 0.74f, 0.4f, 0.99f, 0.6f, 24, TextAnchor.MiddleLeft);
            if (UiKit.Skin != null)
            {
                UiKit.AddIcon(top, UiKit.Skin.iconLinhThach, 0.02f, 0.41f, 0.075f, 0.59f);
                UiKit.AddIcon(top, UiKit.Skin.iconLinhKhi, 0.35f, 0.41f, 0.405f, 0.59f);
                UiKit.AddIcon(top, UiKit.Skin.iconTinhHoa, 0.68f, 0.41f, 0.735f, 0.59f);
            }
            linhThachText.color = new Color(1f, 0.9f, 0.5f);
            linhKhiText.color = new Color(0.6f, 0.9f, 1f);
            tinhHoaText.color = new Color(1f, 0.6f, 0.9f);

            var hpBack = UiKit.Panel("PlayerHpBack", top, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.38f), new Color(0.2f, 0.05f, 0.05f));
            playerFill = UiKit.FillBar(hpBack, new Color(0.3f, 0.85f, 0.4f));
            playerHpText = UiKit.Label(hpBack, 0f, 0f, 1f, 1f, 26, TextAnchor.MiddleCenter);
            var bossBack = UiKit.Panel("BossRow", top, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.19f), new Color(0.2f, 0.05f, 0.05f));
            bossRow = bossBack.gameObject;
            bossFill = UiKit.FillBar(bossBack, new Color(0.85f, 0.2f, 0.2f));
            bossHpText = UiKit.Label(bossBack, 0f, 0f, 1f, 1f, 26, TextAnchor.MiddleCenter);
            bossRow.SetActive(false);

            // Bottom panel: row A menu buttons, row B state + boss button
            var bottom = UiKit.Panel("BottomPanel", root, Vector2.zero, new Vector2(1f, BottomPanelMaxY), PanelColor, true);
            UiKit.AddFrame(bottom);
            Text lbl;
            UiKit.TextButton("Speed", bottom, new Vector2(0.01f, 0.54f), new Vector2(0.10f, 0.96f), ButtonColor, "x1", 26, CycleSpeed, out speedText);

            // Navigation row. Cultivation is reached from the Hero screen; quests, stage and settings from More.
            string[] navIds = { ScreenIds.Character, ScreenIds.Skills, ScreenIds.Equipment, ScreenIds.Inventory, ScreenIds.Summon, ScreenIds.Upgrades, ScreenIds.More };
            string[] navKeys = { "nav.character", "btn.skills", "btn.equipment", "nav.inventory", "btn.gacha", "btn.upgrades", "nav.more" };
            const float navX0 = 0.105f, navW = 0.127f;
            for (int i = 0; i < navIds.Length; i++)
            {
                string id = navIds[i];
                float x0 = navX0 + i * navW;
                UiKit.TextButton("Nav" + id, bottom, new Vector2(x0, 0.54f), new Vector2(x0 + navW - 0.005f, 0.96f), ButtonColor, "", 20, () => ToggleScreen(id), out lbl);
                if (id == ScreenIds.More) questsLabel = lbl; // shows an alert while quest rewards wait
                else LocalizedLabel.Bind(lbl, loc, navKeys[i]);
            }

            stateText = UiKit.Label(bottom, 0.02f, 0.04f, 0.6f, 0.48f, 30, TextAnchor.MiddleLeft);
            var boss = UiKit.TextButton("ChallengeBoss", bottom, new Vector2(0.62f, 0.04f), new Vector2(0.98f, 0.48f), new Color(0.7f, 0.15f, 0.15f), "", 32, director.RequestBoss, out lbl);
            LocalizedLabel.Bind(lbl, loc, "btn.challenge_boss");
            bossButton = boss.gameObject;

            Popup = ui.Confirm;

            Game = new GameContext
            {
                Ui = ui, Loc = loc, Progress = progress, Upgrades = upgrades, Cultivation = cultivation, Equipment = equipment, Gacha = gacha,
                Quests = quests, Director = director, Audio = audio, Stats = () => player.Stats
            };
            var game = Game;
            var nav = ui.Navigator;
            nav.Register(ScreenIds.Character, parent => FrameScreen.Create<CharacterScreen>(parent, game, "nav.character"));
            nav.Register(ScreenIds.Cultivation, parent => FrameScreen.Create<CultivationScreen>(parent, game, "btn.cultivate"));
            nav.Register(ScreenIds.Skills, parent => FrameScreen.Create<SkillsScreen>(parent, game, "btn.skills"));
            nav.Register(ScreenIds.Equipment, parent => FrameScreen.Create<EquipmentScreen>(parent, game, "btn.equipment"));
            nav.Register(ScreenIds.Inventory, parent => FrameScreen.Create<InventoryScreen>(parent, game, "nav.inventory"));
            nav.Register(ScreenIds.Summon, parent => FrameScreen.Create<SummonScreen>(parent, game, "btn.gacha"));
            nav.Register(ScreenIds.Upgrades, parent => FrameScreen.Create<UpgradesScreen>(parent, game, "btn.upgrades"));
            nav.Register(ScreenIds.Quests, parent => FrameScreen.Create<QuestsScreen>(parent, game, "btn.quests"));
            nav.Register(ScreenIds.More, parent => FrameScreen.Create<MoreScreen>(parent, game, "nav.more"));
            nav.Register(ScreenIds.Stage, parent => FrameScreen.Create<StageScreen>(parent, game, "more.stage"));
            nav.Register(ScreenIds.Settings, parent => FrameScreen.Create<SettingsScreen>(parent, game, "more.settings"));

            // Back pops screens (handled by the UIManager); with nothing open it asks before leaving the game.
            ui.BackAtRoot = AskToQuit;

            // Fade overlay (drawn above everything, never blocks input)
            fade = UiKit.Panel("Fade", ui.OverlayLayer, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f)).GetComponent<Image>();
            fade.raycastTarget = false;
            bannerText = UiKit.Label(fade.rectTransform, 0.05f, 0.4f, 0.95f, 0.6f, 72, TextAnchor.MiddleCenter);
            bannerText.color = new Color(1f, 0.9f, 0.6f, 0f);
            loc.LanguageChanged += ForceRefresh;
        }

        /// <summary>Short message over the lower edge of the arena (drops, gacha errors). Auto-hides.</summary>
        public void ShowToast(string text) => ui.ShowToast(text);

        /// <summary>Nav button: closes the screen if it is the one open, otherwise swaps to it.</summary>
        void ToggleScreen(string id)
        {
            var nav = ui.Navigator;
            if (nav.Current != null && nav.Current.Id == id) nav.Pop();
            else if (nav.Depth > 0) nav.Replace(id);
            else nav.Push(id);
        }

        void AskToQuit()
            => Popup.Show(loc.Get("exit.confirm"), loc.Get("btn.quit"), Quit, loc.Get("btn.stay"), null, true);

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void CycleSpeed()
        {
            speedIndex = (speedIndex + 1) % Speeds.Length;
            Time.timeScale = Speeds[speedIndex];
            speedText.text = "x" + Speeds[speedIndex];
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (loc != null) loc.LanguageChanged -= ForceRefresh;
        }

        void ForceRefresh() => nextRefresh = 0f;

        void Update()
        {
            if (director == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshInterval;

            langText.text = loc.Language.ToUpperInvariant();
            if (soundText != null) soundText.color = audio.Muted ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            questsLabel.text = loc.Get(quests.ClaimableCount > 0 ? "nav.more_alert" : "nav.more");

            titleText.text = loc.Format("hud.title", director.Chapter, director.Cycle, loc.Get("map." + director.Map.mapId));
            waveText.text = loc.Format("hud.wave", director.Wave, director.WavesPerMap, director.EnemiesAlive, director.Kills);
            var w = progress.Wallet;
            linhThachText.text = loc.Format("hud.resource", loc.Get("res.linh_thach"), UiKit.Short(w.Get(ResourceType.LinhThach)));
            linhKhiText.text = loc.Format("hud.resource", loc.Get("res.linh_khi"), UiKit.Short(w.Get(ResourceType.LinhKhi)));
            tinhHoaText.text = loc.Format("hud.resource", loc.Get("res.tinh_hoa"), UiKit.Short(w.Get(ResourceType.TinhHoa)));

            var h = player.Health;
            UiKit.SetFill(playerFill, h.Current / h.Max);
            playerHpText.text = player.IsAlive
                ? loc.Format("hud.hp", Mathf.CeilToInt(h.Current), Mathf.CeilToInt(h.Max))
                : loc.Get("hud.defeated");

            var boss = director.Boss;
            bool showBoss = boss != null && director.State == BattleState.BossCombat;
            if (bossRow.activeSelf != showBoss) bossRow.SetActive(showBoss);
            if (showBoss)
            {
                UiKit.SetFill(bossFill, boss.Health.Current / boss.Health.Max);
                bossHpText.text = loc.Format("hud.boss_hp", loc.Get("boss." + director.Map.mapId), Mathf.CeilToInt(boss.Health.Current), Mathf.CeilToInt(boss.Health.Max));
            }

            bool ready = director.State == BattleState.BossReady;
            if (bossButton.activeSelf != ready) bossButton.SetActive(ready);
            stateText.text = director.State switch
            {
                BattleState.NormalWave => loc.Get("state.farming"),
                BattleState.WaveTransition => loc.Get("state.wave_cleared"),
                BattleState.BossReady => loc.Format("state.boss_ready", director.Wave),
                BattleState.BossCombat => loc.Get("state.boss_fight"),
                BattleState.BossResult => loc.Get(player.IsAlive ? "state.boss_win" : "state.boss_lose"),
                _ => loc.Get("state.travel")
            };
        }

        public IEnumerator FadeTo(float alpha, float seconds)
        {
            float start = fade.color.a, t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                SetFadeAlpha(Mathf.Lerp(start, alpha, t / seconds));
                yield return null;
            }
            SetFadeAlpha(alpha);
        }

        void SetFadeAlpha(float a)
        {
            var c = fade.color; c.a = a; fade.color = c;
            var b = bannerText.color; b.a = a; bannerText.color = b; // title is only visible while the screen is dark
        }

        /// <summary>Shows the new map's name on the dark screen during a map transition.</summary>
        public void ShowMapTitle() => bannerText.text = loc.Get("map." + director.Map.mapId);
    }
}
