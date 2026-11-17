using System;
using System.Globalization;
using System.Text;
using PixelImmortal.Data;
using PixelImmortal.Gacha;
using PixelImmortal.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>P09: the complete probability table for a pool, straight from the configured weights, plus the cost rules.</summary>
    public class RatesPopup : PopupBase
    {
        readonly StringBuilder sb = new StringBuilder();
        LocalizationService loc;
        Text title, body;

        public override bool CloseOnDimmerTap => true;
        public override bool CloseOnBack => true;

        public void Build(Transform layer, LocalizationService localization)
        {
            loc = localization;
            var box = BuildFrame(layer, "RatesPopup", new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.8f));
            title = UiKit.Label(box, 0.05f, 0.9f, 0.95f, 0.99f, 34, TextAnchor.MiddleCenter);
            title.color = UiTheme.Gold;

            var viewport = UiKit.Panel("Viewport", box, new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.89f), new Color(0f, 0f, 0f, 0.35f));
            viewport.gameObject.AddComponent<RectMask2D>();

            body = UiKit.Label(viewport, 0f, 0f, 1f, 1f, 26, TextAnchor.UpperLeft);
            body.resizeTextForBestFit = false;
            body.fontSize = 26;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            var content = body.rectTransform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(12f, 0f);
            content.offsetMax = new Vector2(-12f, 0f);
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            UiKit.TextButton("Close", box, new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.13f), UiTheme.Action, "", 30, Close, out var closeLabel);
            LocalizedLabel.Bind(closeLabel, loc, "btn.close");
        }

        public void Show(GachaPoolDefinition pool, string poolName)
        {
            title.text = loc.Format("summon.rates_title", poolName);
            sb.Length = 0;
            sb.Append(loc.Format("summon.rule", UiKit.Short(pool.costPerPull), loc.Get(ResKey(pool.costResource)))).Append("\n\n");

            foreach (var kv in GachaMath.RarityRates(pool))
                sb.Append(loc.Format("summon.rate_line", loc.Get("rarity." + kv.Key), Pct(kv.Value))).Append('\n');
            sb.Append('\n');

            for (int i = 0; i < pool.entries.Length; i++)
            {
                var item = pool.entries[i].item;
                if (item == null) continue;
                sb.Append(loc.Format("summon.rate_line", UiTheme.ItemLabel(loc, item), Pct(GachaMath.Probability(pool, i)))).Append('\n');
            }
            body.text = sb.ToString();
            Open();
        }

        static string Pct(double p) => (p * 100.0).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        internal static string ResKey(ResourceType t)
            => t == ResourceType.LinhThach ? "res.linh_thach" : (t == ResourceType.LinhKhi ? "res.linh_khi" : "res.tinh_hoa");
    }
}
