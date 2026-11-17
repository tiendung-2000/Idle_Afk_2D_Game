using UnityEngine;

namespace PixelImmortal.Data
{
    /// <summary>Phase 1 tunables. All values are defaults, not hard requirements.</summary>
    [CreateAssetMenu(fileName = "PrototypeConfig", menuName = "PixelImmortal/Prototype Config")]
    public class PrototypeConfig : ScriptableObject
    {
        [Header("World")]
        public MapDefinition[] maps;
        public CycleScalingConfig scaling;
        public EconomyConfig economy;
        [Tooltip("Optional. Without it the game uses placeholder shapes.")]
        public ArtCatalog art;
        [Tooltip("CSV with columns key,vi,en")]
        public TextAsset localizationCsv;
        public Vector2 arenaSize = new Vector2(8f, 10f);

        [Header("Player")]
        public float playerMaxHp = 300f;
        public float playerAttack = 12f;
        public float playerAttackInterval = 0.5f;
        public float playerAttackRange = 6f;
        public float respawnSeconds = 2f;
        public float respawnInvulnerableSeconds = 1.5f;

        [Header("Enemy")]
        public float enemyHp = 30f;
        public float enemyAttack = 4f;
        public float enemyAttackInterval = 1f;
        public float enemyMoveSpeed = 1.4f;
        public float enemyAttackRange = 0.9f;
        public float enemyScale = 0.7f;

        [Header("Elite (final group of each wave)")]
        public float eliteHpMultiplier = 3f;
        public float eliteAttackMultiplier = 1.5f;
        public float eliteScale = 1f;

        [Header("Boss")]
        public float bossHp = 400f;
        public float bossAttack = 10f;
        public float bossAttackInterval = 1.2f;
        /// <summary>Warning time before a boss strike lands (boss stops, shows its wind-up frame and a ground ring). 0 = no telegraph.</summary>
        public float bossTelegraphSeconds = 0.5f;
        public float bossMoveSpeed = 0.9f;
        public float bossAttackRange = 1.4f;
        public float bossScale = 1.8f;

        [Header("Wave layout")]
        public float spawnStaggerSeconds = 0.25f;
        public float groupIntervalSeconds = 1f;
        public int maxActiveEnemies = 24;
        public float waveTransitionSeconds = 0.5f;
        public float minSpawnDistanceFromPlayer = 3f;


        [Header("Boss / map flow")]
        public float bossResultSeconds = 1.5f;
        public float mapFadeSeconds = 0.5f;
    }
}
