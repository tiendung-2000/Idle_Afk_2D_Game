using System;
using UnityEngine;

namespace PixelImmortal.Data
{
    public enum EquipSlot
    {
        Weapon = 0,
        Armor = 1,
        Accessory = 2
    }

    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    [Flags]
    public enum DropSource
    {
        None = 0,
        NormalEnemy = 1,
        EliteEnemy = 2,
        Boss = 4
    }

    /// <summary>One stat bonus. Same units as upgrades: fraction added (0.1 = +10%); CritChance/OfflineEfficiency are absolute.</summary>
    [Serializable]
    public class StatModifier
    {
        public StatType stat;
        public float value;
    }

    /// <summary>
    /// Equipment piece. Display name key = "item." + id. Affixes are not implemented (TODO: later phase);
    /// base stats live in <see cref="modifiers"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "PixelImmortal/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable ID used by saves and localization. Never change after release.")]
        public string id;
        public EquipSlot slot;
        public Rarity rarity;
        public StatModifier[] modifiers;
        [Tooltip("Breakthrough realm index needed to equip.")]
        public int requiredRealmIndex;
        [Tooltip("Which kills can drop this item (gacha pools are configured separately).")]
        public DropSource dropSources;
        [Tooltip("Relative weight among items that share a drop source.")]
        public float dropWeight = 10f;
        [Tooltip("Placeholder icon tint until real icons exist.")]
        public Color iconColor = Color.white;
    }
}
