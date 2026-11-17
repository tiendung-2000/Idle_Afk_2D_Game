using System;
using PixelImmortal.Data;

namespace PixelImmortal.Economy
{
    /// <summary>Three resources as doubles. All mutation is validated; never negative, NaN or infinite.</summary>
    public class Wallet
    {
        public const double MaxAmount = 1e300;
        readonly double[] amounts = new double[3];

        public event Action Changed;

        public double Get(ResourceType t) => amounts[(int)t];

        public bool CanAfford(ResourceType t, double cost)
            => !double.IsNaN(cost) && !double.IsInfinity(cost) && cost >= 0 && amounts[(int)t] >= cost;

        /// <summary>Raised for resources earned through live play (kills). Not raised for offline/quest rewards.</summary>
        public event Action<ResourceType, double> Gained;

        /// <param name="earned">True only for income from live play; quests count these.</param>
        public void Add(ResourceType t, double value, bool earned = false)
        {
            if (double.IsNaN(value) || value <= 0) return;
            amounts[(int)t] = Math.Min(MaxAmount, amounts[(int)t] + value);
            Changed?.Invoke();
            if (earned) Gained?.Invoke(t, value);
        }

        public bool TrySpend(ResourceType t, double cost)
        {
            if (!CanAfford(t, cost)) return false;
            amounts[(int)t] -= cost;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Used by save restore; sanitizes the value.</summary>
        public void Set(ResourceType t, double value)
        {
            if (double.IsNaN(value) || value < 0) value = 0;
            amounts[(int)t] = Math.Min(MaxAmount, value);
            Changed?.Invoke();
        }
    }
}
