using UnityEngine;

namespace PixelImmortal.Data
{
    [CreateAssetMenu(fileName = "Upgrade_", menuName = "PixelImmortal/Upgrade Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        [Tooltip("Stable ID used by saves. Never change after release.")]
        public string id;
        // Editor-only label. Shown text comes from the localization table (key prefix + id).
        public string displayName;
        public StatType stat;
        public ResourceType costResource = ResourceType.LinhThach;
        [Tooltip("Cost(level) = baseCost * growthRate ^ level")]
        public double baseCost = 20;
        public double growthRate = 1.15;
        [Tooltip("0 = unlimited")]
        public int maxLevel;
        [Tooltip("Fraction added per level (0.1 = +10%). For CritChance/OfflineEfficiency it is absolute.")]
        public float effectPerLevel = 0.1f;
        public int unlockRealmIndex;
    }
}
