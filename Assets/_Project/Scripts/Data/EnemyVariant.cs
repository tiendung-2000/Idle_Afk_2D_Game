using System;
using UnityEngine;

namespace PixelImmortal.Data
{
    /// <summary>Roster entry of a map. Stats are multipliers over the PrototypeConfig base enemy.</summary>
    [Serializable]
    public class EnemyVariant
    {
        public string id;
        // Editor-only label. Shown text comes from the localization table (key prefix + id).
        public string displayName;
        public Color color = Color.white;
        public float scale = 0.7f;
        public float hpMultiplier = 1f;
        public float attackMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float spawnWeight = 1f;
    }
}
