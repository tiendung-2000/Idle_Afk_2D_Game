using System;

namespace PixelImmortal.Save
{
    /// <summary>Where the player is in the world. Resume restores this exactly; boss fights resume as BossReady.</summary>
    [Serializable]
    public class WorldData
    {
        public int currentChapter = 1;
        public int currentWave = 1;
        public bool bossReady;
        public int kills;
        public int bossesDefeated;
    }

    /// <summary>
    /// Offline reward waiting to be claimed. Persisted so quitting before claiming neither loses nor
    /// duplicates it. Cleared on claim.
    /// </summary>
    [Serializable]
    public class OfflinePendingData
    {
        public bool has;
        public double effectiveSeconds;
        public float efficiency;
        public double linhThach;
        public double linhKhi;
        public double tinhHoa;
    }

    [Serializable]
    public class QuestEntry
    {
        public string id;
        public double progress;
        public bool claimed;
    }

    /// <summary>Daily quest state. dayKey is the device-local date (yyyy-MM-dd) of the last reset, to avoid repeat resets.</summary>
    [Serializable]
    public class QuestSaveData
    {
        public string dayKey = "";
        public System.Collections.Generic.List<QuestEntry> entries = new System.Collections.Generic.List<QuestEntry>();
    }

    [Serializable]
    public class SettingsData
    {
        public string language = "";   // "" = not chosen yet, use system language
        public bool muted;             // sound effects off
        public bool introSeen;         // first-run hint already shown
    }

    /// <summary>Everything persisted. No scene, GameObject or ScriptableObject references.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentSchemaVersion = 3;

        public int schemaVersion = CurrentSchemaVersion;
        /// <summary>UTC ticks of the last time the game was running (last save). Offline time starts here.</summary>
        public long savedAtUtcTicks;
        public ProgressData progress = new ProgressData();
        public WorldData world = new WorldData();
        public OfflinePendingData offline = new OfflinePendingData();
        public QuestSaveData quests = new QuestSaveData();
        public SettingsData settings = new SettingsData();
    }

    /// <summary>On-disk wrapper: schema version + checksum of the payload text, so truncation/corruption is detected.</summary>
    [Serializable]
    public class SaveEnvelope
    {
        public int schemaVersion;
        public string checksum;
        public string payload;
    }
}
