using System;
using System.Collections.Generic;
using PixelImmortal.Data;

namespace PixelImmortal.Gacha
{
    /// <summary>Pure pool math: validation, weighted pick, displayed probabilities.</summary>
    public static class GachaMath
    {
        /// <summary>Returns a list of problems; empty means the pool is safe to use. A pool with problems is never pulled from.</summary>
        public static List<string> Validate(GachaPoolDefinition pool, EconomyConfig eco)
        {
            var errors = new List<string>();
            if (pool == null) { errors.Add("pool is null"); return errors; }
            if (string.IsNullOrEmpty(pool.id)) errors.Add("pool has no id");
            if (double.IsNaN(pool.costPerPull) || double.IsInfinity(pool.costPerPull) || pool.costPerPull <= 0) errors.Add("cost per pull must be a positive number");
            if (pool.entries == null || pool.entries.Length == 0) { errors.Add("pool has no entries"); return errors; }

            double total = 0;
            var seen = new HashSet<string>();
            for (int i = 0; i < pool.entries.Length; i++)
            {
                var e = pool.entries[i];
                if (e == null || e.item == null) { errors.Add("entry " + i + " has no item"); continue; }
                if (eco.FindItem(e.item.id) == null) errors.Add("entry " + i + " item '" + e.item.id + "' is not in the economy item list");
                if (!seen.Add(e.item.id)) errors.Add("item '" + e.item.id + "' appears twice");
                if (float.IsNaN(e.weight) || float.IsInfinity(e.weight) || e.weight <= 0f) errors.Add("entry " + i + " weight must be positive");
                else total += e.weight;
            }
            if (total <= 0) errors.Add("total weight is zero");
            return errors;
        }

        public static double TotalWeight(GachaPoolDefinition pool)
        {
            double t = 0;
            if (pool?.entries == null) return 0;
            foreach (var e in pool.entries) if (e?.item != null && e.weight > 0 && !float.IsNaN(e.weight)) t += e.weight;
            return t;
        }

        public static double Probability(GachaPoolDefinition pool, int index)
        {
            double total = TotalWeight(pool);
            var e = pool.entries[index];
            return total > 0 && e?.item != null && e.weight > 0 ? e.weight / total : 0;
        }

        /// <summary>Index of the entry hit by a roll in [0,1). Out-of-range or NaN rolls are clamped.</summary>
        public static int Pick(GachaPoolDefinition pool, double roll01)
        {
            double total = TotalWeight(pool);
            if (total <= 0) return -1;
            if (double.IsNaN(roll01) || roll01 < 0) roll01 = 0;
            if (roll01 >= 1) roll01 = 1 - 1e-12;

            double target = roll01 * total, acc = 0;
            int last = -1;
            for (int i = 0; i < pool.entries.Length; i++)
            {
                var e = pool.entries[i];
                if (e?.item == null || e.weight <= 0 || float.IsNaN(e.weight)) continue;
                acc += e.weight;
                last = i;
                if (target < acc) return i;
            }
            return last;
        }

        public static Dictionary<Rarity, double> RarityRates(GachaPoolDefinition pool)
        {
            var rates = new Dictionary<Rarity, double>();
            double total = TotalWeight(pool);
            if (total <= 0) return rates;
            foreach (var e in pool.entries)
            {
                if (e?.item == null || e.weight <= 0) continue;
                rates.TryGetValue(e.item.rarity, out var v);
                rates[e.item.rarity] = v + e.weight / total;
            }
            return rates;
        }
    }
}
