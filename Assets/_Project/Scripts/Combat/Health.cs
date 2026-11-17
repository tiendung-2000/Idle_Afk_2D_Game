using System;
using UnityEngine;

namespace PixelImmortal.Combat
{
    public class Health
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        /// <summary>(current, max)</summary>
        public event Action<float, float> Changed;
        public event Action Died;

        public void Reset(float max, float fraction = 1f)
        {
            Max = Mathf.Max(1f, max);
            Current = Mathf.Clamp(Max * fraction, 1f, Max);
            Changed?.Invoke(Current, Max);
        }

        /// <summary>Changes max HP keeping the current HP ratio (alive units stay alive).</summary>
        public void SetMax(float max)
        {
            float ratio = Max > 0f ? Current / Max : 1f;
            Max = Mathf.Max(1f, max);
            Current = IsAlive ? Mathf.Clamp(Max * ratio, 1f, Max) : 0f;
            Changed?.Invoke(Current, Max);
        }

        /// <summary>Raised for every hit that lands: (amount, wasCritical). Used by damage numbers.</summary>
        public event Action<float, bool> Damaged;

        public void TakeDamage(float amount, bool critical = false)
        {
            if (!IsAlive || float.IsNaN(amount) || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Changed?.Invoke(Current, Max);
            Damaged?.Invoke(amount, critical);
            if (Current <= 0f) Died?.Invoke();
        }
    }
}
