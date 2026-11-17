using System;
using System.Collections.Generic;

namespace PixelImmortal.Save
{
    [Serializable]
    public class LevelEntry
    {
        public string id;
        public int level;
    }

    /// <summary>Player-owned progression DTO (no ScriptableObject or scene references). Persisted inside SaveData.</summary>
    [Serializable]
    public class ProgressData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public double linhThach;
        public double linhKhi;
        public double tinhHoa;
        public int realmIndex;
        public int highestChapter = 1;
        public List<LevelEntry> upgrades = new List<LevelEntry>();
        public List<LevelEntry> skills = new List<LevelEntry>();
        /// <summary>Owned items: id + count (level field holds the count).</summary>
        public List<LevelEntry> items = new List<LevelEntry>();
        /// <summary>Equipped item id per slot index (Weapon, Armor, Accessory); empty string = nothing.</summary>
        public List<string> equipped = new List<string>();
        public int totalPulls;
    }
}
