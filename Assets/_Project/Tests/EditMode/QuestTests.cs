using System;
using System.Linq;
using NUnit.Framework;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Offline;
using PixelImmortal.Quests;
using PixelImmortal.Save;
using UnityEditor;

namespace PixelImmortal.Tests
{
    public class QuestTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";

        PrototypeConfig cfg;
        EconomyConfig eco;
        PlayerProgress progress;
        DateTime clock;
        QuestSystem quests;

        [SetUp]
        public void SetUp()
        {
            cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            eco = cfg.economy;
            progress = new PlayerProgress(eco);
            clock = new DateTime(2030, 3, 10, 14, 0, 0, DateTimeKind.Local);
            quests = new QuestSystem(eco, progress, () => clock);
        }

        QuestDefinition Q(QuestObjective o) => eco.dailyQuests.First(q => q.objective == o);

        void Complete(QuestDefinition q) => quests.AddProgress(q.objective, q.targetCount);

        // ---- content ----------------------------------------------------------

        [Test]
        public void EveryObjectiveHasADailyQuestWithAReward()
        {
            foreach (QuestObjective o in Enum.GetValues(typeof(QuestObjective)))
            {
                var q = eco.dailyQuests.FirstOrDefault(x => x.objective == o);
                Assert.NotNull(q, "no quest for " + o);
                Assert.Greater(q.targetCount, 0);
                Assert.Greater(q.reward.linhThach + q.reward.linhKhi + q.reward.tinhHoa, 0, q.id + " has no reward");
            }
            Assert.AreEqual(eco.dailyQuests.Length, eco.dailyQuests.Select(q => q.id).Distinct().Count(), "ids unique");
        }

        // ---- progress and claim ------------------------------------------------

        [Test]
        public void StartsEmptyAndNothingClaimable()
        {
            Assert.AreEqual(0, quests.ClaimableCount);
            foreach (var q in eco.dailyQuests) Assert.AreEqual(0, quests.GetProgress(q));
        }

        [Test]
        public void ProgressAccumulatesAndCapsAtTarget()
        {
            var q = Q(QuestObjective.KillEnemies);
            quests.AddProgress(QuestObjective.KillEnemies, 3);
            Assert.AreEqual(3, quests.GetProgress(q));
            quests.AddProgress(QuestObjective.KillEnemies, q.targetCount * 10);
            Assert.AreEqual(q.targetCount, quests.GetProgress(q));
            Assert.IsTrue(quests.IsComplete(q));
        }

        [Test]
        public void InvalidProgressIsIgnored()
        {
            var q = Q(QuestObjective.KillEnemies);
            quests.AddProgress(QuestObjective.KillEnemies, -5);
            quests.AddProgress(QuestObjective.KillEnemies, double.NaN);
            Assert.AreEqual(0, quests.GetProgress(q));
        }

        [Test]
        public void ClaimBeforeCompletionFails()
        {
            var q = Q(QuestObjective.DefeatBoss);
            Assert.IsFalse(quests.TryClaim(q));
            Assert.AreEqual(0, progress.Wallet.Get(ResourceType.LinhThach));
        }

        [Test]
        public void ClaimIsIdempotent()
        {
            var q = Q(QuestObjective.KillEnemies);
            Complete(q);
            Assert.AreEqual(1, quests.ClaimableCount);
            Assert.IsTrue(quests.TryClaim(q));
            double lt = progress.Wallet.Get(ResourceType.LinhThach);
            Assert.AreEqual(q.reward.linhThach, lt, 1e-9);

            Assert.IsFalse(quests.TryClaim(q), "second claim refused");
            Assert.IsFalse(quests.TryClaim(q));
            Assert.AreEqual(lt, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);
            Assert.AreEqual(0, quests.ClaimableCount);
            Assert.IsTrue(quests.IsClaimed(q));
        }

        [Test]
        public void ClaimedQuestIgnoresFurtherProgress()
        {
            var q = Q(QuestObjective.KillEnemies);
            Complete(q);
            quests.TryClaim(q);
            quests.AddProgress(QuestObjective.KillEnemies, 5);
            Assert.IsFalse(quests.CanClaim(q));
        }

        [Test]
        public void QuestRewardsAndOfflineRewardsDoNotCountAsEarnedIncome()
        {
            int earned = 0;
            progress.Wallet.Gained += (t, a) => earned++;

            var q = Q(QuestObjective.KillEnemies);
            Complete(q);
            quests.TryClaim(q);
            Assert.AreEqual(0, earned, "quest payout must not feed 'earn' quests");

            var svc = new OfflineService(cfg);
            var save = new SaveData { savedAtUtcTicks = DateTime.UtcNow.AddHours(-3).Ticks };
            svc.Prepare(save, DateTime.UtcNow.Ticks, progress);
            svc.Claim(save, progress);
            Assert.AreEqual(0, earned, "offline payout must not feed quests");

            RewardMath.Grant(progress.Wallet, eco.normalKill, 1f); // live kill reward does count
            Assert.Greater(earned, 0);
        }

        [Test]
        public void OfflineProcessingDoesNotMoveQuestProgress()
        {
            var q = Q(QuestObjective.EarnLinhThach);
            var svc = new OfflineService(cfg);
            var save = new SaveData { savedAtUtcTicks = DateTime.UtcNow.AddHours(-12).Ticks };
            svc.Prepare(save, DateTime.UtcNow.Ticks, progress);
            svc.Claim(save, progress);
            Assert.AreEqual(0, quests.GetProgress(q));
            Assert.AreEqual(0, quests.ClaimableCount);
        }

        // ---- daily reset ------------------------------------------------------

        [Test]
        public void ResetsAtMidnightDeviceTime()
        {
            var q = Q(QuestObjective.KillEnemies);
            clock = new DateTime(2030, 3, 10, 23, 59, 0, DateTimeKind.Local);
            quests.AddProgress(QuestObjective.KillEnemies, 7);
            Assert.IsFalse(quests.EnsureCurrentDay());
            Assert.AreEqual(7, quests.GetProgress(q));

            clock = new DateTime(2030, 3, 11, 0, 0, 0, DateTimeKind.Local);
            Assert.IsTrue(quests.EnsureCurrentDay());
            Assert.AreEqual(0, quests.GetProgress(q));
        }

        [Test]
        public void ResetHappensOnlyOncePerDay()
        {
            clock = clock.AddDays(1);
            Assert.IsTrue(quests.EnsureCurrentDay());
            quests.AddProgress(QuestObjective.KillEnemies, 4);
            Assert.IsFalse(quests.EnsureCurrentDay(), "same day: no repeat reset");
            Assert.AreEqual(4, quests.GetProgress(Q(QuestObjective.KillEnemies)));
        }

        [Test]
        public void UnclaimedRewardExpiresWithTheDay()
        {
            var q = Q(QuestObjective.KillEnemies);
            Complete(q);
            Assert.AreEqual(1, quests.ClaimableCount);

            clock = clock.AddDays(1);
            Assert.IsFalse(quests.TryClaim(q), "yesterday's reward cannot be claimed");
            Assert.AreEqual(0, progress.Wallet.Get(ResourceType.LinhThach));
            Assert.AreEqual(0, quests.ClaimableCount);
            Assert.AreEqual(0, quests.GetProgress(q));
        }

        [Test]
        public void ClaimedQuestCanBeEarnedAgainTomorrow()
        {
            var q = Q(QuestObjective.KillEnemies);
            Complete(q);
            quests.TryClaim(q);
            clock = clock.AddDays(1);
            quests.EnsureCurrentDay();
            Assert.IsFalse(quests.IsClaimed(q));
            Complete(q);
            Assert.IsTrue(quests.TryClaim(q));
            Assert.AreEqual(q.reward.linhThach * 2, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);
        }

        [Test]
        public void MovingTheClockBackwardsNeverResetsOrReopensClaims()
        {
            var q = Q(QuestObjective.KillEnemies);
            Complete(q);
            quests.TryClaim(q);
            clock = clock.AddDays(-2);
            Assert.IsFalse(quests.EnsureCurrentDay());
            Assert.IsTrue(quests.IsClaimed(q));
            Assert.IsFalse(quests.TryClaim(q));
        }

        [Test]
        public void ProgressDuringRolloverCountsTowardsTheNewDay()
        {
            Complete(Q(QuestObjective.KillEnemies));
            clock = clock.AddDays(1);
            quests.AddProgress(QuestObjective.KillEnemies, 2); // AddProgress rolls the day first
            Assert.AreEqual(2, quests.GetProgress(Q(QuestObjective.KillEnemies)));
        }

        [Test]
        public void TimeUntilResetIsCorrect()
        {
            clock = new DateTime(2030, 3, 10, 23, 30, 0, DateTimeKind.Local);
            Assert.AreEqual(30, quests.TimeUntilReset.TotalMinutes, 1e-6);
            clock = new DateTime(2030, 3, 10, 0, 0, 0, DateTimeKind.Local);
            Assert.AreEqual(24, quests.TimeUntilReset.TotalHours, 1e-6);
        }

        // ---- save -------------------------------------------------------------

        [Test]
        public void SaveDataRoundTripsThroughJsonSameDay()
        {
            var q = Q(QuestObjective.KillEnemies);
            Complete(q);
            quests.TryClaim(q);
            quests.AddProgress(QuestObjective.UpgradeTimes, 2);

            string json = UnityEngine.JsonUtility.ToJson(quests.ToData());
            var restored = new QuestSystem(eco, new PlayerProgress(eco), () => clock);
            restored.ApplyData(UnityEngine.JsonUtility.FromJson<QuestSaveData>(json));

            Assert.IsTrue(restored.IsClaimed(q));
            Assert.AreEqual(2, restored.GetProgress(Q(QuestObjective.UpgradeTimes)));
        }

        [Test]
        public void LoadingOnANewDayStartsFresh()
        {
            Complete(Q(QuestObjective.KillEnemies));
            var data = quests.ToData();
            clock = clock.AddDays(3);
            var restored = new QuestSystem(eco, new PlayerProgress(eco), () => clock);
            restored.ApplyData(data);
            Assert.AreEqual(0, restored.ClaimableCount);
            Assert.AreEqual(0, restored.GetProgress(Q(QuestObjective.KillEnemies)));
        }

        [Test]
        public void CorruptQuestSaveIsSanitized()
        {
            var q = Q(QuestObjective.KillEnemies);
            var bad = new QuestSaveData { dayKey = QuestSystem.DayKeyOf(clock) };
            bad.entries.Add(new QuestEntry { id = q.id, progress = -50, claimed = true });                       // claimed without progress
            bad.entries.Add(new QuestEntry { id = "ghost_quest", progress = 99 });                              // unknown id
            bad.entries.Add(new QuestEntry { id = Q(QuestObjective.EarnLinhKhi).id, progress = double.NaN });
            bad.entries.Add(null);
            quests.ApplyData(bad);

            Assert.AreEqual(0, quests.GetProgress(q));
            Assert.IsFalse(quests.IsClaimed(q), "a claim without completion is invalid");
            Assert.AreEqual(0, quests.GetProgress(Q(QuestObjective.EarnLinhKhi)));
        }

        [Test]
        public void GarbageDayKeyDoesNotCrashAndResets()
        {
            quests.AddProgress(QuestObjective.KillEnemies, 5);
            quests.ApplyData(new QuestSaveData { dayKey = "not-a-date" });
            Assert.DoesNotThrow(() => quests.EnsureCurrentDay());
            Assert.AreEqual(0, quests.ClaimableCount);
        }

        [Test]
        public void OlderSchemaV1SaveGetsEmptyQuestsAndSettings()
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pi_q_" + Guid.NewGuid().ToString("N"));
            try
            {
                System.IO.Directory.CreateDirectory(dir);
                string payload = "{\"schemaVersion\":1,\"world\":{\"currentChapter\":3,\"currentWave\":2}}";
                var env = new SaveEnvelope { schemaVersion = 1, payload = payload, checksum = SaveSystem.Checksum(payload) };
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "save.json"), UnityEngine.JsonUtility.ToJson(env));
                var r = new SaveSystem(dir).Load();
                Assert.AreEqual(LoadStatus.Loaded, r.Status);
                Assert.AreEqual(3, r.Data.world.currentChapter, "v1 data preserved");
                Assert.AreEqual(SaveData.CurrentSchemaVersion, r.Data.schemaVersion);
                Assert.IsEmpty(r.Data.quests.entries);
                Assert.AreEqual("", r.Data.settings.language);
            }
            finally { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
        }
    }
}
