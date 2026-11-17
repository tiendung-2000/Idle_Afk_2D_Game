using System;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Progression;
using PixelImmortal.Save;

namespace PixelImmortal.Offline
{
    public struct OfflineReward
    {
        public double elapsedSeconds;      // validated time away
        public double effectiveSeconds;    // after the 12h cap
        public float efficiency;
        public double linhThach, linhKhi, tinhHoa;
        public bool clockAnomaly;
        public bool WasCapped => elapsedSeconds > effectiveSeconds;
    }

    /// <summary>
    /// Offline rewards. Pure functions: they never touch wave/boss/quest progress, only produce resource amounts.
    /// effectiveOfflineSeconds = min(validElapsedSeconds, cap); reward = ratePerSecond x effectiveSeconds x efficiency.
    /// </summary>
    public static class OfflineMath
    {
        /// <summary>Elapsed seconds between save and now; 0 for negative gaps or timestamps from the future (clock errors).</summary>
        public static double ValidElapsedSeconds(long savedAtUtcTicks, long nowUtcTicks, out bool anomaly)
        {
            anomaly = false;
            if (savedAtUtcTicks <= 0) return 0;                       // never saved / unknown
            double elapsed = (nowUtcTicks - savedAtUtcTicks) / (double)TimeSpan.TicksPerSecond;
            if (double.IsNaN(elapsed)) { anomaly = true; return 0; }
            if (elapsed < 0)
            {
                anomaly = true;                                       // clock moved backwards: no reward, no penalty
                return 0;
            }
            return elapsed;
        }

        /// <summary>Resources per second while farming the given normal wave (final-group elites included).</summary>
        public static RewardEntry RatePerSecond(WaveDefinition wave, int chapter, CycleScalingConfig scaling, EconomyConfig eco)
        {
            var rate = new RewardEntry();
            if (wave == null) return rate;

            int groups = Math.Max(1, wave.groupCount);
            // Base group size on purpose: a higher cycle spawns more enemies but the wave also takes
            // proportionally longer to clear, so density must not raise the per-second rate.
            int perGroup = Math.Max(1, wave.enemiesPerGroup);
            int total = groups * perGroup;
            int elites = Math.Min(Math.Max(0, wave.eliteCountInFinalGroup), perGroup);
            int normals = total - elites;
            double mult = scaling.Reward(chapter);
            double seconds = Math.Max(1.0, wave.estimatedClearSeconds);

            rate.linhThach = (normals * eco.normalKill.linhThach + elites * eco.eliteKill.linhThach) * mult / seconds;
            rate.linhKhi = (normals * eco.normalKill.linhKhi + elites * eco.eliteKill.linhKhi) * mult / seconds;
            rate.tinhHoa = (normals * eco.normalKill.tinhHoa + elites * eco.eliteKill.tinhHoa) * mult / seconds;
            return rate;
        }

        public static OfflineReward Compute(double validElapsedSeconds, RewardEntry ratePerSecond, float efficiency, double capSeconds)
        {
            double elapsed = Math.Max(0, double.IsNaN(validElapsedSeconds) ? 0 : validElapsedSeconds);
            double effective = Math.Min(elapsed, Math.Max(0, capSeconds));
            float eff = float.IsNaN(efficiency) ? 0f : Math.Max(0f, efficiency);
            return new OfflineReward
            {
                elapsedSeconds = elapsed,
                effectiveSeconds = effective,
                efficiency = eff,
                linhThach = ratePerSecond.linhThach * effective * eff,
                linhKhi = ratePerSecond.linhKhi * effective * eff,
                tinhHoa = ratePerSecond.tinhHoa * effective * eff
            };
        }
    }
}
