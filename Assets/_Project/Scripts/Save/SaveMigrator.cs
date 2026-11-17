using System;
using UnityEngine;

namespace PixelImmortal.Save
{
    /// <summary>
    /// Upgrades older payloads step by step to <see cref="SaveData.CurrentSchemaVersion"/>.
    /// Add one step per schema bump. Newer-than-known versions are rejected, never guessed.
    /// </summary>
    public static class SaveMigrator
    {
        public static bool TryMigrate(int fromVersion, string payload, out SaveData data, out string error)
        {
            data = null;
            error = null;

            if (fromVersion > SaveData.CurrentSchemaVersion)
            {
                error = "Save was written by a newer game version (" + fromVersion + ").";
                return false;
            }
            if (fromVersion < 0 || string.IsNullOrEmpty(payload))
            {
                error = "Invalid save version or empty payload.";
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveData>(payload);
            }
            catch (Exception e)
            {
                error = "Payload is not valid JSON: " + e.Message;
                return false;
            }
            if (data == null) { error = "Payload parsed to null."; return false; }

            // v0 -> v1: introduced world state and offline pending. Missing fields deserialize to defaults;
            // make sure a v0 save without a timestamp cannot yield a bogus offline gap.
            if (fromVersion < 1)
            {
                if (data.world == null) data.world = new WorldData();
                if (data.offline == null) data.offline = new OfflinePendingData();
                data.savedAtUtcTicks = 0;
            }

            // v1 -> v2: added daily quests and settings. Missing sections default to empty (quests reset on first load).
            if (fromVersion < 2)
            {
                data.quests = new QuestSaveData();
                data.settings = new SettingsData();
            }

            // v2 -> v3: items, equipped slots and pull count were added to progress; they default to empty.
            if (data.quests == null) data.quests = new QuestSaveData();
            if (data.settings == null) data.settings = new SettingsData();
            if (data.progress == null) data.progress = new ProgressData();
            if (data.world == null) data.world = new WorldData();
            if (data.offline == null) data.offline = new OfflinePendingData();
            data.schemaVersion = SaveData.CurrentSchemaVersion;
            return true;
        }
    }
}
