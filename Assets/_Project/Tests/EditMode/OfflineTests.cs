using System;
using NUnit.Framework;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Offline;
using PixelImmortal.Save;
using UnityEditor;

namespace PixelImmortal.Tests
{
    public class OfflineTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";
        static readonly long Hour = TimeSpan.TicksPerHour;

        PrototypeConfig cfg;
        PlayerProgress progress;
        OfflineService service;
        long now;

        [SetUp]
        public void SetUp()
        {
            cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            progress = new PlayerProgress(cfg.economy);
            service = new OfflineService(cfg);
            now = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc).Ticks;
        }

        SaveData SavedHoursAgo(double hours, int chapter = 1, int wave = 5, bool bossReady = false)
        {
            var d = new SaveData { savedAtUtcTicks = now - (long)(hours * Hour) };
            d.world.currentChapter = chapter;
            d.world.currentWave = wave;
            d.world.bossReady = bossReady;
            return d;
        }

        static string WorldKey(WorldData w) => $"{w.currentChapter}/{w.currentWave}/{w.bossReady}/{w.kills}/{w.bossesDefeated}";

        // ---- elapsed validation ---------------------------------------------

        [Test]
        public void NegativeElapsedIsZeroAndFlagged()
        {
            double s = OfflineMath.ValidElapsedSeconds(now + Hour, now, out bool anomaly);
            Assert.AreEqual(0, s);
            Assert.IsTrue(anomaly);
        }

        [Test]
        public void UnknownTimestampIsZero()
        {
            Assert.AreEqual(0, OfflineMath.ValidElapsedSeconds(0, now, out bool anomaly));
            Assert.IsFalse(anomaly);
        }

        // ---- formula --------------------------------------------------------

        [Test]
        public void RewardIsRateTimesTimeTimesEfficiency()
        {
            var rate = new RewardEntry { linhThach = 2, linhKhi = 1, tinhHoa = 0.5 };
            var r = OfflineMath.Compute(3600, rate, 0.8f, 12 * 3600);
            Assert.AreEqual(2 * 3600 * 0.8, r.linhThach, 1e-3);
            Assert.AreEqual(1 * 3600 * 0.8, r.linhKhi, 1e-3);
            Assert.AreEqual(0.5 * 3600 * 0.8, r.tinhHoa, 1e-3);
            Assert.IsFalse(r.WasCapped);
        }

        [Test]
        public void TwelveHourCapApplies()
        {
            var rate = new RewardEntry { linhThach = 1 };
            var r = OfflineMath.Compute(30 * 3600, rate, 0.8f, 12 * 3600);
            Assert.AreEqual(12 * 3600, r.effectiveSeconds, 1e-6);
            Assert.AreEqual(12 * 3600 * 0.8, r.linhThach, 1e-3);
            Assert.IsTrue(r.WasCapped);
        }

        [Test]
        public void ComputeIsSafeForBadInput()
        {
            var rate = new RewardEntry { linhThach = 1 };
            Assert.AreEqual(0, OfflineMath.Compute(double.NaN, rate, 0.8f, 100).linhThach);
            Assert.AreEqual(0, OfflineMath.Compute(-50, rate, 0.8f, 100).linhThach);
            Assert.AreEqual(0, OfflineMath.Compute(50, rate, float.NaN, 100).linhThach);
        }

        [Test]
        public void ConfiguredBaselineIsTwelveHoursAtEightyPercent()
        {
            Assert.AreEqual(12f, cfg.economy.offlineMaxHours);
            Assert.AreEqual(0.8f, cfg.economy.baseOfflineEfficiency, 1e-6f);
        }

        // ---- rate from wave data --------------------------------------------

        [Test]
        public void RateComesFromSavedWaveAndGrowsWithCycle()
        {
            var c1 = service.RateForSavedPosition(new WorldData { currentChapter = 1, currentWave = 3 });
            var c4 = service.RateForSavedPosition(new WorldData { currentChapter = 4, currentWave = 3 });
            Assert.Greater(c1.linhThach, 0);
            Assert.Greater(c4.linhThach, c1.linhThach, "cycle 2 pays more");
            Assert.Greater(c1.linhKhi, 0);
        }

        [Test]
        public void BossReadySaveFarmsLastNormalWave()
        {
            var boss = service.RateForSavedPosition(new WorldData { currentChapter = 1, currentWave = 10, bossReady = true });
            var w10 = service.RateForSavedPosition(new WorldData { currentChapter = 1, currentWave = 10, bossReady = false });
            Assert.AreEqual(w10.linhThach, boss.linhThach, 1e-9);
        }

        // ---- prepare / claim --------------------------------------------------

        [Test]
        public void ShortAbsenceGivesNothingButConsumesTheGap()
        {
            var d = SavedHoursAgo(0.005); // 18 seconds
            Assert.IsFalse(service.Prepare(d, now, progress));
            Assert.IsFalse(d.offline.has);
            Assert.AreEqual(now, d.savedAtUtcTicks);
        }

        [Test]
        public void PrepareThenClaimGrantsExactlyOnce()
        {
            var d = SavedHoursAgo(2);
            Assert.IsTrue(service.Prepare(d, now, progress));
            double expected = d.offline.linhThach;
            Assert.Greater(expected, 0);

            var g1 = service.Claim(d, progress);
            Assert.AreEqual(expected, g1.linhThach, 1e-9);
            Assert.AreEqual(expected, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);

            var g2 = service.Claim(d, progress);
            Assert.AreEqual(0, g2.linhThach);
            Assert.AreEqual(expected, progress.Wallet.Get(ResourceType.LinhThach), 1e-9, "second claim must not add anything");
            Assert.IsFalse(d.offline.has);
        }

        [Test]
        public void SameGapCannotBeCountedTwice()
        {
            var d = SavedHoursAgo(3);
            service.Prepare(d, now, progress);
            double first = d.offline.effectiveSeconds;
            service.Prepare(d, now, progress); // same moment again, e.g. a double load
            Assert.AreEqual(first, d.offline.effectiveSeconds, 1e-6);
        }

        [Test]
        public void QuittingBeforeClaimMergesWithinTheTwelveHourCap()
        {
            var d = SavedHoursAgo(10);
            service.Prepare(d, now, progress);                // 10h pending, unclaimed
            long later = now + 10 * Hour;                     // next launch 10h later
            service.Prepare(d, later, progress);
            Assert.AreEqual(12 * 3600, d.offline.effectiveSeconds, 1e-6, "pending + new never exceeds 12h");
        }

        [Test]
        public void NoRewardWhenClockWentBackwards()
        {
            var d = SavedHoursAgo(-5);
            Assert.IsFalse(service.Prepare(d, now, progress));
        }

        [Test]
        public void OfflineNeverChangesWorldOrProgression()
        {
            var d = SavedHoursAgo(30, chapter: 5, wave: 4);
            progress.SetRealm(2);
            progress.SetUpgradeLevel(cfg.economy.upgrades[0].id, 3);
            string before = WorldKey(d.world);
            int realm = progress.RealmIndex, highest = progress.HighestChapter, lvl = progress.GetUpgradeLevel(cfg.economy.upgrades[0].id);

            service.Prepare(d, now, progress);
            service.Claim(d, progress);

            Assert.AreEqual(before, WorldKey(d.world), "wave/chapter/boss state untouched");
            Assert.AreEqual(realm, progress.RealmIndex);
            Assert.AreEqual(highest, progress.HighestChapter);
            Assert.AreEqual(lvl, progress.GetUpgradeLevel(cfg.economy.upgrades[0].id));
        }

        [Test]
        public void OfflineEfficiencyUpgradeRaisesRewardButStaysCapped()
        {
            var eff = Array.Find(cfg.economy.upgrades, u => u.stat == StatType.OfflineEfficiency);
            var plain = SavedHoursAgo(2); service.Prepare(plain, now, progress);

            var p2 = new PlayerProgress(cfg.economy);
            p2.SetUpgradeLevel(eff.id, 5);
            var boosted = SavedHoursAgo(2); service.Prepare(boosted, now, p2);
            Assert.Greater(boosted.offline.linhThach, plain.offline.linhThach);

            var p3 = new PlayerProgress(cfg.economy);
            p3.SetUpgradeLevel(eff.id, 100000);
            var maxed = SavedHoursAgo(2); service.Prepare(maxed, now, p3);
            Assert.LessOrEqual(maxed.offline.efficiency, cfg.economy.maxOfflineEfficiency);
        }

        [Test]
        public void PendingRewardSurvivesSaveAndLoad()
        {
            var d = SavedHoursAgo(4);
            service.Prepare(d, now, progress);
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pi_off_" + Guid.NewGuid().ToString("N"));
            try
            {
                var sys = new SaveSystem(dir);
                Assert.IsTrue(sys.Save(d));
                var loaded = sys.Load().Data;
                Assert.IsTrue(loaded.offline.has);
                Assert.AreEqual(d.offline.linhThach, loaded.offline.linhThach, 1e-9);
                Assert.AreEqual(d.savedAtUtcTicks, loaded.savedAtUtcTicks);
            }
            finally { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
        }
    }
}
