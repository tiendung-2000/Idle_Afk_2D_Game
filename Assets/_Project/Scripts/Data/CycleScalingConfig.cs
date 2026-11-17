using System;
using UnityEngine;
using PixelImmortal.Progression;

namespace PixelImmortal.Data
{
    [Serializable]
    public struct CycleCurve
    {
        /// <summary>factor(c) = 1 + a*(c-1) + b*ln(c), clamped to [1, maxFactor].</summary>
        public float a;
        public float b;
        public float maxFactor;
    }

    /// <summary>
    /// Difficulty curves. Cycle factor is the main driver; map step and wave offset are additive so
    /// difficulty never drops between waves, maps or cycles as long as a &gt;= mapStep*(maps-1) + max wave offset.
    /// </summary>
    [CreateAssetMenu(fileName = "CycleScalingConfig", menuName = "PixelImmortal/Cycle Scaling Config")]
    public class CycleScalingConfig : ScriptableObject
    {
        public CycleCurve hp = new CycleCurve { a = 0.7f, b = 0.5f, maxFactor = 1000f };
        public CycleCurve attack = new CycleCurve { a = 0.7f, b = 0.5f, maxFactor = 1000f };
        public CycleCurve density = new CycleCurve { a = 0.1f, b = 0.3f, maxFactor = 3f };
        public CycleCurve reward = new CycleCurve { a = 0.6f, b = 0.4f, maxFactor = 1000f };
        [Tooltip("Additive difficulty per map slot inside one cycle (map 2 = +1 step).")]
        public float mapStep = 0.2f;

        public float EnemyHp(int chapter, float waveOffset) => Stat(chapter, hp, waveOffset);
        public float EnemyAttack(int chapter, float waveOffset) => Stat(chapter, attack, waveOffset);
        public float BossHp(int chapter) => Stat(chapter, hp, 0f);
        public float BossAttack(int chapter) => Stat(chapter, attack, 0f);
        public float Density(int chapter) => ProgressionMath.CycleFactor(ProgressionMath.CycleIndex(chapter), density.a, density.b, density.maxFactor);
        public float Reward(int chapter) => ProgressionMath.CycleFactor(ProgressionMath.CycleIndex(chapter), reward.a, reward.b, reward.maxFactor);

        float Stat(int chapter, CycleCurve c, float waveOffset)
        {
            float f = ProgressionMath.CycleFactor(ProgressionMath.CycleIndex(chapter), c.a, c.b, c.maxFactor)
                      + mapStep * (ProgressionMath.MapIndex(chapter) - 1) + Mathf.Max(0f, waveOffset);
            return float.IsNaN(f) || float.IsInfinity(f) ? c.maxFactor : Mathf.Min(f, c.maxFactor);
        }
    }
}
