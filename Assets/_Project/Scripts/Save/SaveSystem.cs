using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PixelImmortal.Save
{
    public enum LoadStatus
    {
        NewGame,          // no save files exist
        Loaded,           // main file OK
        LoadedFromBackup, // main file bad, backup OK
        Corrupt           // files exist but none is usable; nothing was overwritten
    }

    public class LoadResult
    {
        public LoadStatus Status;
        public SaveData Data;
        public string Error;
    }

    /// <summary>
    /// Local save with main + backup file and atomic-ish writes (write temp, validate, replace).
    /// Never silently overwrites an unreadable save: callers must call <see cref="MoveCorruptAside"/> explicitly.
    /// </summary>
    public class SaveSystem
    {
        readonly string mainPath;
        readonly string backupPath;
        readonly string tempPath;

        public string Directory { get; }

        public SaveSystem(string directory)
        {
            Directory = directory;
            mainPath = Path.Combine(directory, "save.json");
            backupPath = Path.Combine(directory, "save.bak.json");
            tempPath = Path.Combine(directory, "save.tmp.json");
        }

        public string MainPath => mainPath;
        public string BackupPath => backupPath;

        // ---- load -------------------------------------------------------------

        public LoadResult Load()
        {
            bool mainExists = File.Exists(mainPath);
            bool backupExists = File.Exists(backupPath);
            if (!mainExists && !backupExists) return new LoadResult { Status = LoadStatus.NewGame };

            string mainError = null;
            if (mainExists && TryRead(mainPath, out var data, out mainError))
                return new LoadResult { Status = LoadStatus.Loaded, Data = data };

            string backupError = null;
            if (backupExists && TryRead(backupPath, out data, out backupError))
                return new LoadResult { Status = LoadStatus.LoadedFromBackup, Data = data, Error = mainError ?? "Main save missing." };

            return new LoadResult { Status = LoadStatus.Corrupt, Error = "main: " + (mainError ?? "missing") + "; backup: " + (backupError ?? "missing") };
        }

        bool TryRead(string path, out SaveData data, out string error)
        {
            data = null;
            try
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                var env = JsonUtility.FromJson<SaveEnvelope>(text);
                if (env == null || string.IsNullOrEmpty(env.payload) || string.IsNullOrEmpty(env.checksum))
                {
                    error = "Envelope incomplete.";
                    return false;
                }
                if (!string.Equals(env.checksum, Checksum(env.payload), StringComparison.Ordinal))
                {
                    error = "Checksum mismatch.";
                    return false;
                }
                return SaveMigrator.TryMigrate(env.schemaVersion, env.payload, out data, out error);
            }
            catch (Exception e)
            {
                error = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        // ---- save -------------------------------------------------------------

        /// <summary>
        /// Writes temp, verifies it reads back, then replaces main. The previous main becomes the backup
        /// only if it was itself valid, so a corrupt main never overwrites a good backup.
        /// </summary>
        public bool Save(SaveData data)
        {
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                data.schemaVersion = SaveData.CurrentSchemaVersion;
                string payload = JsonUtility.ToJson(data);
                var env = new SaveEnvelope { schemaVersion = data.schemaVersion, checksum = Checksum(payload), payload = payload };
                File.WriteAllText(tempPath, JsonUtility.ToJson(env), new UTF8Encoding(false));

                if (!TryRead(tempPath, out _, out string verifyError))
                {
                    Debug.LogError("Save verification failed, keeping previous files: " + verifyError);
                    SafeDelete(tempPath);
                    return false;
                }

                if (File.Exists(mainPath))
                {
                    if (TryRead(mainPath, out _, out _)) File.Copy(mainPath, backupPath, true);
                    File.Delete(mainPath);
                }
                File.Move(tempPath, mainPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Save failed: " + e.Message);
                SafeDelete(tempPath);
                return false;
            }
        }

        /// <summary>
        /// Moves unreadable files aside (never deletes) so a fresh save can start. Only call after the
        /// player explicitly chose to start over.
        /// </summary>
        public void MoveCorruptAside()
        {
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            MoveAside(mainPath, "save.corrupt-" + stamp + ".json");
            MoveAside(backupPath, "save.bak.corrupt-" + stamp + ".json");
        }

        void MoveAside(string path, string newName)
        {
            try { if (File.Exists(path)) File.Move(path, Path.Combine(Directory, newName)); }
            catch (Exception e) { Debug.LogError("Could not move aside " + path + ": " + e.Message); }
        }

        static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
        }

        public static string Checksum(string text)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
