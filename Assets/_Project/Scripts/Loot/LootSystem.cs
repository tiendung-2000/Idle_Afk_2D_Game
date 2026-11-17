using System;
using PixelImmortal.Data;
using PixelImmortal.Economy;

namespace PixelImmortal.Loot
{
    /// <summary>Equipment drops from live kills (never offline). Chance per source, then a weighted pick among items allowed for it.</summary>
    public class LootSystem
    {
        readonly PlayerProgress progress;
        readonly Func<double> rng;

        public event Action<ItemDefinition> Dropped;

        public LootSystem(PlayerProgress progress, Func<double> roll = null)
        {
            this.progress = progress;
            rng = roll ?? new Random().NextDouble;
        }

        public ItemDefinition RollKill(bool isBoss, bool isElite)
            => Roll(isBoss ? DropSource.Boss : (isElite ? DropSource.EliteEnemy : DropSource.NormalEnemy));

        public ItemDefinition Roll(DropSource source)
        {
            var eco = progress.Economy;
            double chance = source == DropSource.Boss ? eco.bossDropChance
                : source == DropSource.EliteEnemy ? eco.eliteDropChance : eco.normalDropChance;
            if (!(rng() < chance)) return null;

            double total = 0;
            foreach (var i in eco.items) if (Eligible(i, source)) total += i.dropWeight;
            if (total <= 0) return null;

            double target = rng() * total, acc = 0;
            ItemDefinition last = null;
            foreach (var i in eco.items)
            {
                if (!Eligible(i, source)) continue;
                acc += i.dropWeight;
                last = i;
                if (target < acc) break;
            }
            if (last == null || !progress.AddItem(last.id, 1)) return null;
            Dropped?.Invoke(last);
            return last;
        }

        static bool Eligible(ItemDefinition i, DropSource source)
            => i != null && (i.dropSources & source) != 0 && i.dropWeight > 0 && !float.IsNaN(i.dropWeight);
    }
}
