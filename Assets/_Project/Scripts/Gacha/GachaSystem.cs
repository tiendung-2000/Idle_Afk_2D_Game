using System;
using System.Collections.Generic;
using PixelImmortal.Data;
using PixelImmortal.Economy;

namespace PixelImmortal.Gacha
{
    public enum PullStatus
    {
        Ok,
        InvalidPool,
        InvalidCount,
        NotEnoughResources,
        Failed          // grant failed after rolling back the charge
    }

    public struct PullResult
    {
        public ItemDefinition item;
        public bool isNew;
    }

    /// <summary>
    /// Pulls are transactional: validate, check funds, roll everything first, then charge and grant.
    /// If anything fails after the charge, items are removed again and the resources refunded.
    /// </summary>
    public class GachaSystem
    {
        readonly PlayerProgress progress;
        readonly Func<double> rng;

        /// <summary>Raised after a successful pull (count of pulls).</summary>
        public event Action<int> Pulled;

        /// <param name="roll">Returns values in [0,1). Defaults to a System.Random; tests inject fixed rolls.</param>
        public GachaSystem(PlayerProgress progress, Func<double> roll = null)
        {
            this.progress = progress;
            rng = roll ?? new Random().NextDouble;
        }

        public double TotalCost(GachaPoolDefinition pool, int count) => pool.costPerPull * count;

        public bool CanAfford(GachaPoolDefinition pool, int count)
            => pool != null && count >= 1 && progress.Wallet.CanAfford(pool.costResource, pool.costPerPull * count);

        public PullStatus TryPull(GachaPoolDefinition pool, int count, List<PullResult> results)
        {
            results.Clear();
            var eco = progress.Economy;

            if (GachaMath.Validate(pool, eco).Count > 0) return PullStatus.InvalidPool;
            if (count < 1 || count > eco.maxPullCount) return PullStatus.InvalidCount;

            double cost = TotalCost(pool, count);
            if (!progress.Wallet.CanAfford(pool.costResource, cost)) return PullStatus.NotEnoughResources;

            // 1) roll everything before touching any state
            var rolled = new List<ItemDefinition>(count);
            for (int i = 0; i < count; i++)
            {
                int index = GachaMath.Pick(pool, rng());
                if (index < 0) return PullStatus.InvalidPool;
                rolled.Add(pool.entries[index].item);
            }

            // 2) charge
            if (!progress.Wallet.TrySpend(pool.costResource, cost)) return PullStatus.NotEnoughResources;

            // 3) grant; roll back and refund on any failure
            var granted = new List<string>(count);
            bool ok = true;
            foreach (var item in rolled)
            {
                bool isNew = progress.GetItemCount(item.id) == 0;
                if (!progress.AddItem(item.id, 1)) { ok = false; break; }
                granted.Add(item.id);
                results.Add(new PullResult { item = item, isNew = isNew });
            }

            if (!ok)
            {
                foreach (var id in granted) progress.TryRemoveItem(id, 1);
                progress.Wallet.Add(pool.costResource, cost, false);
                results.Clear();
                return PullStatus.Failed;
            }

            progress.AddPulls(count);
            Pulled?.Invoke(count);
            return PullStatus.Ok;
        }
    }
}
