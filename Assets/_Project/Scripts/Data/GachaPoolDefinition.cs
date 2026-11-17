using System;
using UnityEngine;

namespace PixelImmortal.Data
{
    [Serializable]
    public class GachaEntry
    {
        public ItemDefinition item;
        [Tooltip("Relative weight; probability = weight / sum of weights.")]
        public float weight = 1f;
    }

    /// <summary>
    /// A gacha pool: weighted item entries and a per-pull cost. No pity, banners or paid currency
    /// (not confirmed in the GDD). Name key = "gacha." + id.
    /// </summary>
    [CreateAssetMenu(fileName = "Gacha_", menuName = "PixelImmortal/Gacha Pool")]
    public class GachaPoolDefinition : ScriptableObject
    {
        public string id;
        public ResourceType costResource = ResourceType.LinhThach;
        [Tooltip("Cost of a single pull. A multi-pull costs this times the count (no discount).")]
        public double costPerPull = 150;
        public GachaEntry[] entries;
    }
}
