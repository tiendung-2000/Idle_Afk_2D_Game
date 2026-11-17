using UnityEngine;

namespace PixelImmortal.Data
{
    [CreateAssetMenu(fileName = "Map_", menuName = "PixelImmortal/Map Definition")]
    public class MapDefinition : ScriptableObject
    {
        public string mapId;
        // Editor-only label. Shown text comes from the localization table (key prefix + id)..
        public string displayName;
        public string bossName;
        public Color backgroundColor = Color.gray;
        public Color borderColor = Color.black;
        public Color bossColor = Color.red;
        [Tooltip("Boss base stats are PrototypeConfig boss stats times these.")]
        public float bossHpMultiplier = 1f;
        public float bossAttackMultiplier = 1f;
        public EnemyVariant[] roster;
        [Tooltip("Normal waves in order. The Boss Wave follows the last entry.")]
        public WaveDefinition[] waves;
    }
}
