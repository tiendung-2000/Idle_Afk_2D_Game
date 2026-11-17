using System;
using UnityEngine;

namespace PixelImmortal.Data
{
    [Serializable]
    public class RealmDefinition
    {
        public string id;
        // Editor-only label. Shown text comes from the localization table (key prefix + id).
        public string displayName;
        public double linhKhiCost;
        public double linhThachCost;
        [Tooltip("Highest chapter reached required to break through into this realm.")]
        public int minChapter = 1;
        public float attackMultiplier = 1f;
        public float hpMultiplier = 1f;
    }

    [Serializable]
    public class RewardEntry
    {
        public double linhThach;
        public double linhKhi;
        public double tinhHoa;
    }

    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "PixelImmortal/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        public UpgradeDefinition[] upgrades;
        public SkillDefinition[] skills;
        [Tooltip("Index 0 is the starting realm and has no breakthrough cost.")]
        public RealmDefinition[] realms;

        [Header("Equipment and loot")]
        public ItemDefinition[] items;
        public GachaPoolDefinition[] gachaPools;
        [Range(0f, 1f)] public float normalDropChance = 0.02f;
        [Range(0f, 1f)] public float eliteDropChance = 0.15f;
        [Range(0f, 1f)] public float bossDropChance = 1f;
        [Tooltip("Largest multi-pull allowed in one action.")]
        public int maxPullCount = 10;

        [Tooltip("Daily quests. All reset at 00:00 device time.")]
        public QuestDefinition[] dailyQuests;

        [Header("Rewards (multiplied by the cycle reward factor)")]
        public RewardEntry normalKill = new RewardEntry { linhThach = 5, linhKhi = 1.5 };
        public RewardEntry eliteKill = new RewardEntry { linhThach = 25, linhKhi = 8, tinhHoa = 1 };
        public RewardEntry bossKill = new RewardEntry { linhThach = 250, linhKhi = 80, tinhHoa = 10 };

        [Header("Stat bases and caps")]
        public float baseCritChance = 0.05f;
        public float maxCritChance = 0.75f;
        public float baseCritMultiplier = 1.5f;
        public float baseDamageReduction;
        public float maxDamageReduction = 0.8f;
        public float minAttackInterval = 0.15f;
        public float baseOfflineEfficiency = 0.8f;
        public float maxOfflineEfficiency = 1f;

        [Header("Offline")]
        public float offlineMaxHours = 12f;
        [Tooltip("Shorter absences are ignored (no popup).")]
        public float offlineMinSeconds = 60f;

        public ItemDefinition FindItem(string id)
        {
            if (items != null && !string.IsNullOrEmpty(id)) foreach (var i in items) if (i != null && i.id == id) return i;
            return null;
        }

        public GachaPoolDefinition FindPool(string id)
        {
            if (gachaPools != null) foreach (var p in gachaPools) if (p != null && p.id == id) return p;
            return null;
        }

        public QuestDefinition FindQuest(string id)
        {
            if (dailyQuests != null) foreach (var q in dailyQuests) if (q != null && q.id == id) return q;
            return null;
        }

        public UpgradeDefinition FindUpgrade(string id)
        {
            if (upgrades != null) foreach (var u in upgrades) if (u != null && u.id == id) return u;
            return null;
        }

        public SkillDefinition FindSkill(string id)
        {
            if (skills != null) foreach (var s in skills) if (s != null && s.id == id) return s;
            return null;
        }
    }
}
