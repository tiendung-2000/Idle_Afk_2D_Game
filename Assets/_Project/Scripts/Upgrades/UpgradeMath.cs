using System;

namespace PixelImmortal.Upgrades
{
    public static class UpgradeMath
    {
        /// <summary>Cost(level) = baseCost * growth^level. Overflow/NaN yields +Infinity (never affordable).</summary>
        public static double Cost(double baseCost, double growthRate, int level)
        {
            if (level < 0) level = 0;
            double c = Math.Max(0.0, baseCost) * Math.Pow(Math.Max(1.0, growthRate), level);
            return double.IsNaN(c) || double.IsInfinity(c) ? double.PositiveInfinity : c;
        }
    }
}
