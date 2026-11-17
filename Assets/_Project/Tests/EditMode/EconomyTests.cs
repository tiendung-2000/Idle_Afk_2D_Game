using System.Linq;
using NUnit.Framework;
using PixelImmortal.Combat;
using PixelImmortal.Cultivation;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Save;
using PixelImmortal.Upgrades;
using UnityEditor;

namespace PixelImmortal.Tests
{
    public class EconomyTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";

        PrototypeConfig cfg;
        EconomyConfig eco;
        PlayerProgress progress;
        UpgradeSystem upgrades;
        CultivationSystem cultivation;

        [SetUp]
        public void SetUp()
        {
            cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            eco = cfg.economy;
            progress = new PlayerProgress(eco);
            upgrades = new UpgradeSystem(progress);
            cultivation = new CultivationSystem(progress);
        }

        // ---- pure math ------------------------------------------------------

        [Test]
        public void UpgradeCostFollowsCurve()
        {
            Assert.AreEqual(20.0, UpgradeMath.Cost(20, 1.15, 0), 1e-9);
            Assert.AreEqual(20 * 1.15 * 1.15, UpgradeMath.Cost(20, 1.15, 2), 1e-9);
            Assert.AreEqual(20.0, UpgradeMath.Cost(20, 1.15, -4), 1e-9);
        }

        [Test]
        public void UpgradeCostOverflowIsInfinityNotNaN()
        {
            double c = UpgradeMath.Cost(1e10, 10, 100000);
            Assert.IsTrue(double.IsPositiveInfinity(c));
            Assert.IsFalse(new Wallet().CanAfford(ResourceType.LinhThach, c));
        }

        [Test]
        public void DamageFormulaAppliesEachFactorOnce()
            => Assert.AreEqual(10f * 2f * 1f * 1.5f, CombatMath.Damage(10f, 2f, 1f, 1.5f), 1e-4f);

        [Test]
        public void DamageIsSafeAgainstNaNAndOverflow()
        {
            Assert.AreEqual(0f, CombatMath.Damage(float.NaN, 1f, 1f, 1f));
            Assert.LessOrEqual(CombatMath.Damage(float.MaxValue, float.MaxValue, 1f, 1f), CombatMath.MaxDamage);
            Assert.AreEqual(1f, CombatMath.Damage(0.001f, 1f, 1f, 1f));
        }

        // ---- wallet ---------------------------------------------------------

        [Test]
        public void WalletRejectsInvalidAmounts()
        {
            var w = new Wallet();
            w.Add(ResourceType.LinhThach, -5);
            w.Add(ResourceType.LinhThach, double.NaN);
            Assert.AreEqual(0, w.Get(ResourceType.LinhThach));
            w.Add(ResourceType.LinhThach, 10);
            Assert.IsFalse(w.TrySpend(ResourceType.LinhThach, 11));
            Assert.IsFalse(w.TrySpend(ResourceType.LinhThach, -1));
            Assert.AreEqual(10, w.Get(ResourceType.LinhThach));
            Assert.IsTrue(w.TrySpend(ResourceType.LinhThach, 10));
            Assert.AreEqual(0, w.Get(ResourceType.LinhThach));
        }

        [Test]
        public void RewardGrantUsesMultiplier()
        {
            RewardMath.Grant(progress.Wallet, eco.normalKill, 2f);
            Assert.AreEqual(eco.normalKill.linhThach * 2, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);
            RewardMath.Grant(progress.Wallet, eco.normalKill, float.NaN);
            Assert.AreEqual(eco.normalKill.linhThach * 2, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);
        }

        // ---- content shape (GDD requirements) ---------------------------------

        [Test]
        public void ThreeResourcesSixUpgradeGroupsFiveSkillsNineRealms()
        {
            Assert.AreEqual(3, System.Enum.GetValues(typeof(ResourceType)).Length);
            Assert.AreEqual(6, eco.upgrades.Length);
            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(StatType)).Cast<StatType>(), eco.upgrades.Select(u => u.stat));
            Assert.AreEqual(5, eco.skills.Length);
            Assert.AreEqual(9, eco.realms.Length);
            Assert.AreEqual(eco.upgrades.Length, eco.upgrades.Select(u => u.id).Distinct().Count(), "upgrade ids unique");
            Assert.AreEqual(eco.skills.Length, eco.skills.Select(s => s.id).Distinct().Count(), "skill ids unique");
        }

        [Test]
        public void EveryRealmCostIsNonDecreasing()
        {
            for (int i = 2; i < eco.realms.Length; i++)
            {
                Assert.GreaterOrEqual(eco.realms[i].linhKhiCost, eco.realms[i - 1].linhKhiCost);
                Assert.GreaterOrEqual(eco.realms[i].linhThachCost, eco.realms[i - 1].linhThachCost);
            }
        }

        // ---- stats ----------------------------------------------------------

        [Test]
        public void BaselineStatsMatchConfig()
        {
            var s = PlayerStats.Compute(cfg, eco, progress);
            Assert.AreEqual(cfg.playerAttack, s.Attack, 1e-4f);
            Assert.AreEqual(cfg.playerMaxHp, s.MaxHp, 1e-4f);
            Assert.AreEqual(cfg.playerAttackInterval, s.AttackInterval, 1e-4f);
            Assert.AreEqual(eco.baseOfflineEfficiency, s.OfflineEfficiency, 1e-4f);
        }

        [Test]
        public void EachUpgradeAffectsItsStat()
        {
            var baseS = PlayerStats.Compute(cfg, eco, progress);
            foreach (var u in eco.upgrades) progress.SetUpgradeLevel(u.id, 5);
            var s = PlayerStats.Compute(cfg, eco, progress);
            Assert.Greater(s.Attack, baseS.Attack);
            Assert.Greater(s.MaxHp, baseS.MaxHp);
            Assert.Less(s.AttackInterval, baseS.AttackInterval);
            Assert.Greater(s.SkillDamageMultiplier, baseS.SkillDamageMultiplier);
            Assert.Greater(s.CritChance, baseS.CritChance);
            Assert.Greater(s.OfflineEfficiency, baseS.OfflineEfficiency);
        }

        [Test]
        public void StatCapsHoldAtExtremeLevels()
        {
            foreach (var u in eco.upgrades) progress.SetUpgradeLevel(u.id, 100000);
            var s = PlayerStats.Compute(cfg, eco, progress);
            Assert.LessOrEqual(s.CritChance, eco.maxCritChance);
            Assert.LessOrEqual(s.OfflineEfficiency, eco.maxOfflineEfficiency);
            Assert.GreaterOrEqual(s.AttackInterval, eco.minAttackInterval);
            Assert.IsFalse(float.IsNaN(s.Attack));
        }

        [Test]
        public void RealmMultipliersApply()
        {
            var before = PlayerStats.Compute(cfg, eco, progress);
            progress.SetRealm(eco.realms.Length - 1);
            var after = PlayerStats.Compute(cfg, eco, progress);
            Assert.Greater(after.Attack, before.Attack);
            Assert.Greater(after.MaxHp, before.MaxHp);
        }

        // ---- upgrades -------------------------------------------------------

        [Test]
        public void BuyUpgradeSpendsAndLevels()
        {
            var u = eco.upgrades[0];
            progress.Wallet.Add(u.costResource, 1000);
            double cost = upgrades.Cost(u);
            Assert.IsTrue(upgrades.TryBuy(u));
            Assert.AreEqual(1, progress.GetUpgradeLevel(u.id));
            Assert.AreEqual(1000 - cost, progress.Wallet.Get(u.costResource), 1e-9);
            Assert.Greater(upgrades.Cost(u), cost);
        }

        [Test]
        public void BuyFailsWithoutFundsAndChangesNothing()
        {
            var u = eco.upgrades[0];
            Assert.IsFalse(upgrades.TryBuy(u));
            Assert.AreEqual(0, progress.GetUpgradeLevel(u.id));
            Assert.AreEqual(0, progress.Wallet.Get(u.costResource));
        }

        [Test]
        public void MaxLevelBlocksPurchase()
        {
            var u = eco.upgrades.FirstOrDefault(x => x.maxLevel > 0);
            if (u == null) Assert.Pass("no capped upgrade configured");
            progress.SetUpgradeLevel(u.id, u.maxLevel);
            progress.Wallet.Add(u.costResource, 1e12);
            Assert.IsFalse(upgrades.TryBuy(u));
            Assert.AreEqual(u.maxLevel, progress.GetUpgradeLevel(u.id));
        }

        // ---- skills ---------------------------------------------------------

        [Test]
        public void SkillsUnlockByRealmAndUpgradeWithTinhHoa()
        {
            var s = eco.skills.First(x => x.unlockRealmIndex > 0);
            progress.Wallet.Add(ResourceType.TinhHoa, 1000);
            Assert.IsFalse(upgrades.TryUpgradeSkill(s), "locked skill cannot be upgraded");
            progress.SetRealm(s.unlockRealmIndex);
            float m1 = upgrades.SkillMultiplier(s);
            Assert.IsTrue(upgrades.TryUpgradeSkill(s));
            Assert.AreEqual(2, progress.GetSkillLevel(s.id));
            Assert.Greater(upgrades.SkillMultiplier(s), m1);
        }

        // ---- cultivation ----------------------------------------------------

        [Test]
        public void BreakthroughFailsWithoutRequirementsAndSpendsNothing()
        {
            Assert.AreNotEqual(BreakthroughBlock.None, cultivation.Check());
            Assert.IsFalse(cultivation.TryBreakthrough());
            Assert.AreEqual(0, progress.RealmIndex);
        }

        [Test]
        public void BreakthroughSpendsOnceAndAdvances()
        {
            var next = cultivation.Next;
            progress.NoteChapter(next.minChapter);
            progress.Wallet.Add(ResourceType.LinhKhi, next.linhKhiCost);
            progress.Wallet.Add(ResourceType.LinhThach, next.linhThachCost);
            Assert.AreEqual(BreakthroughBlock.None, cultivation.Check());
            Assert.IsTrue(cultivation.TryBreakthrough());
            Assert.AreEqual(1, progress.RealmIndex);
            Assert.AreEqual(0, progress.Wallet.Get(ResourceType.LinhKhi), 1e-9);
            Assert.AreEqual(0, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);
            Assert.IsFalse(cultivation.TryBreakthrough(), "no double breakthrough with empty wallet");
            Assert.AreEqual(1, progress.RealmIndex);
        }

        [Test]
        public void BreakthroughBlockedByChapterEvenWhenRich()
        {
            progress.SetRealm(3);
            var next = cultivation.Next;
            progress.Wallet.Add(ResourceType.LinhKhi, next.linhKhiCost * 10);
            progress.Wallet.Add(ResourceType.LinhThach, next.linhThachCost * 10);
            if (next.minChapter > 1)
            {
                Assert.AreEqual(BreakthroughBlock.NeedChapter, cultivation.Check());
                Assert.IsFalse(cultivation.TryBreakthrough());
                Assert.AreEqual(next.linhKhiCost * 10, progress.Wallet.Get(ResourceType.LinhKhi), 1e-6);
            }
        }

        [Test]
        public void MaxRealmCannotBreakThrough()
        {
            progress.SetRealm(eco.realms.Length - 1);
            Assert.AreEqual(BreakthroughBlock.MaxRealm, cultivation.Check());
            Assert.IsFalse(cultivation.TryBreakthrough());
        }

        // ---- save DTO -------------------------------------------------------

        [Test]
        public void SaveDataRoundTripsThroughJson()
        {
            progress.Wallet.Add(ResourceType.LinhThach, 1234.5);
            progress.Wallet.Add(ResourceType.TinhHoa, 7);
            progress.SetUpgradeLevel(eco.upgrades[1].id, 4);
            progress.SetSkillLevel(eco.skills[0].id, 3);
            progress.SetRealm(2);
            progress.NoteChapter(5);

            string json = UnityEngine.JsonUtility.ToJson(progress.ToData());
            var restored = new PlayerProgress(eco);
            restored.ApplyData(UnityEngine.JsonUtility.FromJson<ProgressData>(json));

            Assert.AreEqual(1234.5, restored.Wallet.Get(ResourceType.LinhThach), 1e-9);
            Assert.AreEqual(7, restored.Wallet.Get(ResourceType.TinhHoa), 1e-9);
            Assert.AreEqual(4, restored.GetUpgradeLevel(eco.upgrades[1].id));
            Assert.AreEqual(3, restored.GetSkillLevel(eco.skills[0].id));
            Assert.AreEqual(2, restored.RealmIndex);
            Assert.AreEqual(5, restored.HighestChapter);
        }

        [Test]
        public void ApplyDataSanitizesCorruptValues()
        {
            var bad = new ProgressData
            {
                linhThach = -50, linhKhi = double.NaN, tinhHoa = 3,
                realmIndex = 999, highestChapter = -4
            };
            bad.upgrades.Add(new LevelEntry { id = "does_not_exist", level = 5 });
            bad.upgrades.Add(new LevelEntry { id = eco.upgrades[0].id, level = -9 });
            bad.skills.Add(new LevelEntry { id = eco.skills[0].id, level = 99999 });
            bad.skills.Add(null);

            progress.ApplyData(bad);

            Assert.AreEqual(0, progress.Wallet.Get(ResourceType.LinhThach));
            Assert.AreEqual(0, progress.Wallet.Get(ResourceType.LinhKhi));
            Assert.AreEqual(3, progress.Wallet.Get(ResourceType.TinhHoa));
            Assert.AreEqual(eco.realms.Length - 1, progress.RealmIndex);
            Assert.AreEqual(1, progress.HighestChapter);
            Assert.AreEqual(0, progress.GetUpgradeLevel(eco.upgrades[0].id));
            Assert.AreEqual(eco.skills[0].maxLevel, progress.GetSkillLevel(eco.skills[0].id));
        }
    }
}
