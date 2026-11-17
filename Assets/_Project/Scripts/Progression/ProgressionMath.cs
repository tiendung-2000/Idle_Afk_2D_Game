using System;

namespace PixelImmortal.Progression
{
    public static class ProgressionMath
    {
        public const int MapsPerCycle = 3;

        public static int CycleIndex(int chapterIndex) => (Math.Max(1, chapterIndex) - 1) / MapsPerCycle + 1;

        /// <summary>1-based map slot within a cycle.</summary>
        public static int MapIndex(int chapterIndex) => (Math.Max(1, chapterIndex) - 1) % MapsPerCycle + 1;

        /// <summary>factor(c) = 1 + a*(c-1) + b*ln(c); cycle 1 is always 1. Finite, within [1, maxFactor].</summary>
        public static float CycleFactor(int cycleIndex, float a, float b, float maxFactor)
        {
            int c = Math.Max(1, cycleIndex);
            double f = 1.0 + Math.Max(0f, a) * (c - 1) + Math.Max(0f, b) * Math.Log(c);
            double cap = Math.Max(1f, maxFactor);
            if (double.IsNaN(f) || double.IsInfinity(f)) return (float)cap;
            return (float)Math.Min(Math.Max(f, 1.0), cap);
        }
    }
}
