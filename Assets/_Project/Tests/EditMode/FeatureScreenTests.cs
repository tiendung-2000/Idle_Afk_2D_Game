using NUnit.Framework;
using PixelImmortal.Combat;
using PixelImmortal.Cultivation;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Equipment;
using PixelImmortal.Localization;
using PixelImmortal.UI;
using PixelImmortal.Upgrades;
using UnityEditor;
using UnityEngine;

namespace PixelImmortal.Tests
{
    public class FeatureScreenTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";

        UIManager ui;
        GameContext game;
        PlayerProgress progress;
        PrototypeConfig cfg;
        GameObject audioObject;

        [SetUp]
        public void SetUp()
        {
            cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            progress = new PlayerProgress(cfg.economy);
            audioObject = new GameObject("audio");
            ui = UIManager.Create();
            game = new GameContext
            {
                Ui = ui,
                Loc = new LocalizationService(cfg.localizationCsv.text, "en"),
                Progress = progress,
                Upgrades = new UpgradeSystem(progress),
                Cultivation = new CultivationSystem(progress),
                Equipment = new EquipmentSystem(progress),
                Gacha = new PixelImmortal.Gacha.GachaSystem(progress),
                Quests = new PixelImmortal.Quests.QuestSystem(cfg.economy, progress, () => System.DateTime.Now),
                Audio = audioObject.AddComponent<PixelImmortal.Audio.AudioService>(),
                Stats = () => PlayerStats.Compute(cfg, cfg.economy, progress)
            };
            var nav = ui.Navigator;
            nav.Register(ScreenIds.Character, p => FrameScreen.Create<CharacterScreen>(p, game, "nav.character"));
            nav.Register(ScreenIds.Cultivation, p => FrameScreen.Create<CultivationScreen>(p, game, "btn.cultivate"));
            nav.Register(ScreenIds.Skills, p => FrameScreen.Create<SkillsScreen>(p, game, "btn.skills"));
            nav.Register(ScreenIds.Equipment, p => FrameScreen.Create<EquipmentScreen>(p, game, "btn.equipment"));
            nav.Register(ScreenIds.Inventory, p => FrameScreen.Create<InventoryScreen>(p, game, "nav.inventory"));
            nav.Register(ScreenIds.Summon, p => FrameScreen.Create<SummonScreen>(p, game, "btn.gacha"));
            nav.Register(ScreenIds.Upgrades, p => FrameScreen.Create<UpgradesScreen>(p, game, "btn.upgrades"));
            nav.Register(ScreenIds.Quests, p => FrameScreen.Create<QuestsScreen>(p, game, "btn.quests"));
            nav.Register(ScreenIds.More, p => FrameScreen.Create<MoreScreen>(p, game, "nav.more"));
            nav.Register(ScreenIds.Settings, p => FrameScreen.Create<SettingsScreen>(p, game, "more.settings"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(ui.gameObject);
            Object.DestroyImmediate(audioObject);
        }

        static readonly string[] All =
        {
            ScreenIds.Character, ScreenIds.Cultivation, ScreenIds.Skills, ScreenIds.Equipment, ScreenIds.Inventory, ScreenIds.Summon,
            ScreenIds.Upgrades, ScreenIds.Quests, ScreenIds.More, ScreenIds.Settings
        };

        [Test]
        public void EveryScreenOpensAndRendersWithoutErrorsInBothLanguages()
        {
            foreach (var lang in new[] { "en", "vi" })
            {
                game.Loc.SetLanguage(lang);
                foreach (var id in All)
                {
                    Assert.IsTrue(ui.Navigator.Push(id), id);
                    ui.Navigator.Pop();
                }
            }
        }

        [Test]
        public void ScreensRenderWithOwnedItemsAndMaxedProgress()
        {
            foreach (var item in cfg.economy.items) progress.AddItem(item.id, 3);
            progress.SetRealm(cfg.economy.realms.Length - 1);
            progress.Wallet.Add(ResourceType.TinhHoa, 1e9);
            foreach (var id in All)
            {
                ui.Navigator.Push(id);
                ui.Navigator.Pop();
            }
        }

        [Test]
        public void SkillUpgradeSpendsEssenceThroughTheDomain()
        {
            var skill = cfg.economy.skills[0];
            progress.Wallet.Add(ResourceType.TinhHoa, 1e6);
            int before = progress.GetSkillLevel(skill.id);
            Assert.IsTrue(game.Upgrades.TryUpgradeSkill(skill));
            Assert.AreEqual(before + 1, progress.GetSkillLevel(skill.id));
            Assert.AreEqual(UpgradeSystem.SkillMultiplierAt(skill, before + 1), game.Upgrades.SkillMultiplier(skill), 1e-5f);
        }


        [Test]
        public void SummonFlowShowsRatesAndResultPopups()
        {
            var pool = cfg.economy.gachaPools[0];
            progress.Wallet.Add(pool.costResource, pool.costPerPull * 100);
            var results = new System.Collections.Generic.List<PixelImmortal.Gacha.PullResult>();
            Assert.AreEqual(PixelImmortal.Gacha.PullStatus.Ok, game.Gacha.TryPull(pool, cfg.economy.maxPullCount, results));

            var host = new GameObject("popups").transform;
            var result = new GameObject("r").AddComponent<SummonResultPopup>();
            result.transform.SetParent(host);
            result.Build(ui.PopupLayer, game.Loc, progress);
            ui.RegisterPopup(result);
            result.Show(results);
            Assert.IsTrue(result.IsOpen);
            ui.HandleBack();
            Assert.IsFalse(result.IsOpen, "Back closes the result");

            var rates = new GameObject("rt").AddComponent<RatesPopup>();
            rates.transform.SetParent(host);
            rates.Build(ui.PopupLayer, game.Loc);
            ui.RegisterPopup(rates);
            rates.Show(pool, "pool");
            Assert.IsTrue(rates.IsOpen);
            Object.DestroyImmediate(host.gameObject);
        }
        [Test]
        public void NavTextKeysExistInBothLanguages()
        {
            foreach (var key in new[] { "nav.character", "nav.inventory", "btn.equip_item", "btn.cancel", "inv.empty", "cult.success" })
                foreach (var lang in LocalizationService.Supported)
                    Assert.IsTrue(!game.Loc.Get(key, lang).StartsWith("["), key + " " + lang);
        }
    }
}
