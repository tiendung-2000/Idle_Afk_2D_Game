using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PixelImmortal.Data;
using PixelImmortal.Localization;
using UnityEngine;

namespace PixelImmortal.UI
{
    /// <summary>Shared colours and display text helpers. Rarity is always shown as colour plus a numeral, never colour alone.</summary>
    public static class UiTheme
    {
        public static readonly Color Row = new Color(0.16f, 0.15f, 0.2f, 1f);
        public static readonly Color Action = new Color(0.25f, 0.5f, 0.3f, 1f);
        public static readonly Color Danger = new Color(0.7f, 0.15f, 0.15f, 1f);
        public static readonly Color TabOn = new Color(0.45f, 0.45f, 0.75f, 1f);
        public static readonly Color TabOff = new Color(0.25f, 0.25f, 0.4f, 1f);
        public static readonly Color Gold = new Color(1f, 0.85f, 0.5f, 1f);
        public static readonly Color Warning = new Color(1f, 0.6f, 0.4f, 1f);
        public static readonly Color Panel = new Color(0.05f, 0.05f, 0.08f, 0.96f);

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return new Color(0.4f, 0.8f, 0.45f);
                case Rarity.Rare: return new Color(0.35f, 0.6f, 1f);
                case Rarity.Epic: return new Color(0.7f, 0.4f, 0.95f);
                case Rarity.Legendary: return new Color(1f, 0.7f, 0.2f);
                default: return new Color(0.6f, 0.6f, 0.65f);
            }
        }

        public static string ShortKey(ResourceType t)
            => t == ResourceType.LinhThach ? "res.short.linh_thach" : (t == ResourceType.LinhKhi ? "res.short.linh_khi" : "res.short.tinh_hoa");

        public static string RarityMark(Rarity r) => new[] { "I", "II", "III", "IV", "V" }[Mathf.Clamp((int)r, 0, 4)];

        public static string ItemLabel(LocalizationService loc, ItemDefinition item)
            => loc.Format("item.label", loc.Get("item." + item.id), loc.Get("rarity." + item.rarity));

        public static string StatsText(LocalizationService loc, Dictionary<StatType, float> stats, bool signedDiff = false)
        {
            var b = new StringBuilder();
            foreach (var kv in stats)
            {
                if (b.Length > 0) b.Append("  ");
                b.Append(loc.Format("stat.line", loc.Get("stat." + kv.Key), (kv.Value * 100f).ToString("+0.#;-0.#;0", CultureInfo.InvariantCulture)));
            }
            return b.Length == 0 ? loc.Get(signedDiff ? "equip.same" : "equip.no_stats") : b.ToString();
        }
    }
}
