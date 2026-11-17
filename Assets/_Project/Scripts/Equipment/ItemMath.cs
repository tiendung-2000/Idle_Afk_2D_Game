using System.Collections.Generic;
using PixelImmortal.Data;

namespace PixelImmortal.Equipment
{
    public static class ItemMath
    {
        /// <summary>Total bonus per stat for an item (items may list the same stat twice).</summary>
        public static Dictionary<StatType, float> Totals(ItemDefinition item)
        {
            var t = new Dictionary<StatType, float>();
            if (item == null || item.modifiers == null) return t;
            foreach (var m in item.modifiers)
            {
                t.TryGetValue(m.stat, out var v);
                t[m.stat] = v + m.value;
            }
            return t;
        }

        /// <summary>candidate minus current, per stat (only non-zero differences). Null items count as no bonuses.</summary>
        public static Dictionary<StatType, float> Diff(ItemDefinition current, ItemDefinition candidate)
        {
            var a = Totals(current);
            var b = Totals(candidate);
            var d = new Dictionary<StatType, float>();
            foreach (var kv in b)
            {
                a.TryGetValue(kv.Key, out var cur);
                float delta = kv.Value - cur;
                if (delta > 1e-6f || delta < -1e-6f) d[kv.Key] = delta;
            }
            foreach (var kv in a) if (!b.ContainsKey(kv.Key) && (kv.Value > 1e-6f || kv.Value < -1e-6f)) d[kv.Key] = -kv.Value;
            return d;
        }
    }
}
