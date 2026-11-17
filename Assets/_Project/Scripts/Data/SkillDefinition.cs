using UnityEngine;

namespace PixelImmortal.Data
{
    [CreateAssetMenu(fileName = "Skill_", menuName = "PixelImmortal/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public string id;
        // Editor-only label. Shown text comes from the localization table (key prefix + id).
        public string displayName;
        public SkillKind kind;
        public int unlockRealmIndex;
        public float cooldown = 3f;
        [Tooltip("Damage = Attack * multiplier(level) * SkillDamage stat")]
        public float baseMultiplier = 1.5f;
        [Tooltip("multiplier(level) = base * (1 + perLevelBonus * (level-1))")]
        public float perLevelBonus = 0.15f;
        public float range = 6f;
        [Tooltip("AoE radius (AreaAtTarget / AreaAroundPlayer)")]
        public float radius = 1.5f;
        public float projectileSpeed = 8f;
        [Header("Status")]
        [Range(0f, 0.95f)] public float slowAmount;
        public float slowSeconds = 2f;
        [Range(0f, 0.95f)] public float shieldReduction = 0.4f;
        public float shieldSeconds = 4f;
        [Header("Upgrade (Tinh Hoa)")]
        public double upgradeBaseCost = 2;
        public double upgradeGrowthRate = 1.5;
        public int maxLevel = 20;
        [Header("Visual placeholder")]
        public Color vfxColor = Color.white;
        public float vfxSeconds = 0.25f;
    }
}
