using System;
using PixelImmortal.Combat;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Save;

namespace PixelImmortal.Offline
{
    /// <summary>
    /// Turns "time away" into a pending reward stored in the save, and applies it on claim.
    /// The saved wave is only read for its reward data; world state is never changed here.
    /// </summary>
    public class OfflineService
    {
        readonly PrototypeConfig config;

        public OfflineService(PrototypeConfig config) { this.config = config; }

        double CapSeconds => config.economy.offlineMaxHours * 3600.0;

        public float MaxHours => config.economy.offlineMaxHours;

        /// <summary>
        /// Computes the reward for the time since the save and merges it into <c>save.offline</c>.
        /// Total effective time (pending + new) never exceeds the cap. Returns true if there is something to show.
        /// </summary>
        public bool Prepare(SaveData save, long nowUtcTicks, PlayerProgress progress)
        {
            var eco = config.economy;
            double elapsed = OfflineMath.ValidElapsedSeconds(save.savedAtUtcTicks, nowUtcTicks, out _);
            var pending = save.offline ?? (save.offline = new OfflinePendingData());

            if (elapsed >= eco.offlineMinSeconds)
            {
                var stats = PlayerStats.Compute(config, eco, progress);
                var rate = RateForSavedPosition(save.world);
                double room = Math.Max(0, CapSeconds - pending.effectiveSeconds);
                var r = OfflineMath.Compute(Math.Min(elapsed, room), rate, stats.OfflineEfficiency, CapSeconds);

                pending.has = true;
                pending.effectiveSeconds += r.effectiveSeconds;
                pending.efficiency = r.efficiency;
                pending.linhThach += r.linhThach;
                pending.linhKhi += r.linhKhi;
                pending.tinhHoa += r.tinhHoa;
            }

            // The elapsed gap is consumed now, whether or not it paid out; it can never be counted twice.
            save.savedAtUtcTicks = nowUtcTicks;
            return pending.has && pending.effectiveSeconds > 0;
        }

        /// <summary>Normal-wave reward rate at the saved position. Boss-ready saves farm the last normal wave.</summary>
        public RewardEntry RateForSavedPosition(WorldData world)
        {
            int chapter = Math.Max(1, world.currentChapter);
            int mapNumber = Progression.ProgressionMath.MapIndex(chapter);
            var map = config.maps[(mapNumber - 1) % config.maps.Length];
            int wave = world.bossReady ? map.waves.Length : world.currentWave;
            wave = Math.Min(Math.Max(1, wave), map.waves.Length);
            return OfflineMath.RatePerSecond(map.waves[wave - 1], chapter, config.scaling, config.economy);
        }

        /// <summary>Adds the pending reward to the wallet exactly once and clears it. Returns what was granted (zero if none).</summary>
        public RewardEntry Claim(SaveData save, PlayerProgress progress)
        {
            var p = save.offline;
            var granted = new RewardEntry();
            if (p == null || !p.has) return granted;

            granted.linhThach = p.linhThach;
            granted.linhKhi = p.linhKhi;
            granted.tinhHoa = p.tinhHoa;

            // Clear first so a failure after this point can never grant twice.
            save.offline = new OfflinePendingData();

            progress.Wallet.Add(ResourceType.LinhThach, granted.linhThach, false);
            progress.Wallet.Add(ResourceType.LinhKhi, granted.linhKhi, false);
            progress.Wallet.Add(ResourceType.TinhHoa, granted.tinhHoa, false);
            return granted;
        }
    }
}
