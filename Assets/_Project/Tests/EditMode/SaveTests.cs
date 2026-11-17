using System;
using System.IO;
using NUnit.Framework;
using PixelImmortal.Data;
using PixelImmortal.Save;
using UnityEngine;

namespace PixelImmortal.Tests
{
    public class SaveTests
    {
        string dir;
        SaveSystem sys;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "pi_save_test_" + Guid.NewGuid().ToString("N"));
            sys = new SaveSystem(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        static SaveData Sample(int chapter = 2, double lt = 100)
        {
            var d = new SaveData { savedAtUtcTicks = 123456789L };
            d.progress.linhThach = lt;
            d.progress.realmIndex = 2;
            d.world.currentChapter = chapter;
            d.world.currentWave = 7;
            d.world.kills = 50;
            d.offline.has = true;
            d.offline.effectiveSeconds = 3600;
            d.offline.linhThach = 999;
            return d;
        }

        static void WriteEnvelope(string path, int version, string payload, string checksum = null)
        {
            var env = new SaveEnvelope { schemaVersion = version, payload = payload, checksum = checksum ?? SaveSystem.Checksum(payload) };
            File.WriteAllText(path, JsonUtility.ToJson(env));
        }

        // ---- basics ---------------------------------------------------------

        [Test]
        public void NoFilesMeansNewGame()
            => Assert.AreEqual(LoadStatus.NewGame, sys.Load().Status);

        [Test]
        public void SaveThenLoadRoundTrips()
        {
            Assert.IsTrue(sys.Save(Sample()));
            var r = sys.Load();
            Assert.AreEqual(LoadStatus.Loaded, r.Status);
            Assert.AreEqual(2, r.Data.world.currentChapter);
            Assert.AreEqual(7, r.Data.world.currentWave);
            Assert.AreEqual(100, r.Data.progress.linhThach, 1e-9);
            Assert.AreEqual(2, r.Data.progress.realmIndex);
            Assert.IsTrue(r.Data.offline.has);
            Assert.AreEqual(999, r.Data.offline.linhThach, 1e-9);
            Assert.AreEqual(123456789L, r.Data.savedAtUtcTicks);
        }

        [Test]
        public void SaveLeavesNoTempFile()
        {
            sys.Save(Sample());
            sys.Save(Sample());
            CollectionAssert.DoesNotContain(Directory.GetFiles(dir), Path.Combine(dir, "save.tmp.json"));
        }

        [Test]
        public void SecondSaveKeepsPreviousAsBackup()
        {
            sys.Save(Sample(chapter: 2));
            sys.Save(Sample(chapter: 3));
            File.Delete(sys.MainPath);
            var r = sys.Load();
            Assert.AreEqual(LoadStatus.LoadedFromBackup, r.Status);
            Assert.AreEqual(2, r.Data.world.currentChapter);
        }

        // ---- damaged files --------------------------------------------------

        [Test]
        public void GarbageMainFallsBackToBackup()
        {
            sys.Save(Sample(chapter: 2));
            sys.Save(Sample(chapter: 3));
            File.WriteAllText(sys.MainPath, "not json at all {{{");
            var r = sys.Load();
            Assert.AreEqual(LoadStatus.LoadedFromBackup, r.Status);
            Assert.AreEqual(2, r.Data.world.currentChapter);
            Assert.IsNotEmpty(r.Error);
        }

        [Test]
        public void TruncatedMainFallsBackToBackup()
        {
            sys.Save(Sample(chapter: 2));
            sys.Save(Sample(chapter: 3));
            string text = File.ReadAllText(sys.MainPath);
            File.WriteAllText(sys.MainPath, text.Substring(0, text.Length / 2));
            Assert.AreEqual(LoadStatus.LoadedFromBackup, sys.Load().Status);
        }

        [Test]
        public void TamperedPayloadFailsChecksum()
        {
            sys.Save(Sample());
            string text = File.ReadAllText(sys.MainPath);
            File.WriteAllText(sys.MainPath, text.Replace("currentChapter", "currentChapteX"));
            Assert.AreEqual(LoadStatus.Corrupt, sys.Load().Status);
        }

        [Test]
        public void EmptyFileIsCorruptNotCrash()
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(sys.MainPath, "");
            Assert.AreEqual(LoadStatus.Corrupt, sys.Load().Status);
        }

        [Test]
        public void BothCorruptReportsCorruptAndTouchesNothing()
        {
            sys.Save(Sample());
            sys.Save(Sample());
            File.WriteAllText(sys.MainPath, "bad main");
            File.WriteAllText(sys.BackupPath, "bad backup");

            var r = sys.Load();
            Assert.AreEqual(LoadStatus.Corrupt, r.Status);
            Assert.IsNull(r.Data);
            Assert.AreEqual("bad main", File.ReadAllText(sys.MainPath), "no silent overwrite");
            Assert.AreEqual("bad backup", File.ReadAllText(sys.BackupPath));
        }

        [Test]
        public void MoveCorruptAsidePreservesFilesAndAllowsFreshSave()
        {
            sys.Save(Sample());
            sys.Save(Sample());
            File.WriteAllText(sys.MainPath, "bad main");
            File.WriteAllText(sys.BackupPath, "bad backup");

            sys.MoveCorruptAside();
            Assert.IsFalse(File.Exists(sys.MainPath));
            Assert.AreEqual(2, Directory.GetFiles(dir, "*corrupt*").Length, "corrupt files kept, not deleted");
            Assert.AreEqual(LoadStatus.NewGame, sys.Load().Status);

            Assert.IsTrue(sys.Save(Sample(chapter: 5)));
            Assert.AreEqual(5, sys.Load().Data.world.currentChapter);
        }

        [Test]
        public void CorruptMainNeverOverwritesGoodBackup()
        {
            sys.Save(Sample(chapter: 1));
            sys.Save(Sample(chapter: 2)); // backup = chapter 1
            File.WriteAllText(sys.MainPath, "bad");
            sys.Save(Sample(chapter: 3)); // main replaced; backup must stay chapter 1
            Assert.AreEqual(3, sys.Load().Data.world.currentChapter);
            File.Delete(sys.MainPath);
            Assert.AreEqual(1, sys.Load().Data.world.currentChapter);
        }

        // ---- versions and migration ----------------------------------------

        [Test]
        public void NewerSchemaVersionIsRejectedNotGuessed()
        {
            Directory.CreateDirectory(dir);
            WriteEnvelope(sys.MainPath, SaveData.CurrentSchemaVersion + 5, JsonUtility.ToJson(Sample()));
            var r = sys.Load();
            Assert.AreEqual(LoadStatus.Corrupt, r.Status);
            StringAssert.Contains("newer", r.Error);
        }

        [Test]
        public void OlderVersionMigratesWithSafeDefaults()
        {
            Directory.CreateDirectory(dir);
            // v0: no world/offline sections, and a timestamp that must not be trusted
            WriteEnvelope(sys.MainPath, 0, "{\"savedAtUtcTicks\":99999,\"progress\":{\"linhThach\":42}}");
            var r = sys.Load();
            Assert.AreEqual(LoadStatus.Loaded, r.Status);
            Assert.AreEqual(SaveData.CurrentSchemaVersion, r.Data.schemaVersion);
            Assert.AreEqual(42, r.Data.progress.linhThach, 1e-9);
            Assert.AreEqual(1, r.Data.world.currentChapter);
            Assert.IsFalse(r.Data.offline.has);
            Assert.AreEqual(0L, r.Data.savedAtUtcTicks, "v0 timestamp discarded so no bogus offline gap");
        }

        [Test]
        public void MissingFieldsGetDefaults()
        {
            Directory.CreateDirectory(dir);
            WriteEnvelope(sys.MainPath, SaveData.CurrentSchemaVersion, "{}");
            var r = sys.Load();
            Assert.AreEqual(LoadStatus.Loaded, r.Status);
            Assert.AreEqual(1, r.Data.world.currentChapter);
            Assert.AreEqual(1, r.Data.world.currentWave);
            Assert.IsNotNull(r.Data.progress);
            Assert.IsNotNull(r.Data.offline);
        }

        [Test]
        public void MigratorRejectsBadInput()
        {
            Assert.IsFalse(SaveMigrator.TryMigrate(1, "", out _, out var e1));
            Assert.IsNotEmpty(e1);
            Assert.IsFalse(SaveMigrator.TryMigrate(-1, "{}", out _, out _));
            Assert.IsFalse(SaveMigrator.TryMigrate(1, "{{broken", out _, out _));
        }
    }
}
