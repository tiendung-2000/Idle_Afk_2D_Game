using System;

namespace PixelImmortal.Combat
{
    public static class CombatMath
    {
        public const float MaxDamage = 1e30f;

        /// <summary>RawDamage = Attack x SkillMultiplier x CycleModifier x CritModifier; each factor applied once.</summary>
        public static float Damage(float attack, float skillMultiplier, float cycleModifier, float critModifier, float minDamage = 1f)
        {
            double r = (double)attack * skillMultiplier * cycleModifier * critModifier;
            if (double.IsNaN(r) || r < 0) return 0f;
            return (float)Math.Min(MaxDamage, Math.Max(minDamage, r));
        }

        public static bool IsCrit(float chance, float roll01) => roll01 < chance;
    }
}
