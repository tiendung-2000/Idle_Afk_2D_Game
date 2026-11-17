using System.Collections.Generic;
using System.Text;
using PixelImmortal.Economy;
using PixelImmortal.Equipment;
using PixelImmortal.Gacha;
using PixelImmortal.Localization;
using UnityEngine;
using UnityEngine.UI;
using PixelImmortal.Data;

namespace PixelImmortal.UI
{
    /// <summary>
    /// P08: what a pull produced. Identical items are grouped with a count; first-time items are tagged NEW.
    /// The items are already in the inventory when this opens, so Back and Continue just close it.
    /// </summary>
    public class SummonResultPopup : PopupBase
    {
        struct Group
        {
            public ItemDefinition item;
            public int count;
            public bool isNew;
        }

        readonly List<Group> groups = new List<Group>();
        readonly StringBuilder sb = new StringBuilder();
        LocalizationService loc;
        PlayerProgress progress;
        SlotGridView grid;
        Text title, detail;
        int selected;

        public override bool CloseOnBack => true;

        public void Build(Transform layer, LocalizationService localization, PlayerProgress playerProgress)
        {
            loc = localization;
            progress = playerProgress;
            var box = BuildFrame(layer, "SummonResultPopup", new Vector2(0.04f, 0.2f), new Vector2(0.96f, 0.8f));
            title = UiKit.Label(box, 0.05f, 0.9f, 0.95f, 0.99f, 36, TextAnchor.MiddleCenter);
            title.color = UiTheme.Gold;
            grid = new SlotGridView(box, new Vector2(0.03f, 0.5f), new Vector2(0.97f, 0.89f), 5, 2, i => { selected = i; Redraw(); });
            detail = UiKit.Label(box, 0.05f, 0.17f, 0.95f, 0.49f, 28, TextAnchor.UpperLeft);
            UiKit.TextButton("Continue", box, new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.15f), UiTheme.Action, "", 32, Close, out var label);
            LocalizedLabel.Bind(label, loc, "btn.continue");
        }

        public void Show(IReadOnlyList<PullResult> pulls)
        {
            groups.Clear();
            foreach (var p in pulls)
            {
                int at = groups.FindIndex(g => g.item.id == p.item.id);
                if (at < 0) groups.Add(new Group { item = p.item, count = 1, isNew = p.isNew });
                else groups[at] = new Group { item = p.item, count = groups[at].count + 1, isNew = groups[at].isNew || p.isNew };
            }
            // best finds first
            groups.Sort((a, b) => b.item.rarity.CompareTo(a.item.rarity));
            selected = 0;
            grid.ResetPage();
            title.text = loc.Format("summon.result_title", pulls.Count);
            Redraw();
            Open();
        }

        void Redraw()
        {
            grid.Refresh(groups.Count, selected, (cell, i) =>
            {
                var g = groups[i];
                string badge = g.isNew ? loc.Get("gacha.new_tag") : (g.count > 1 ? "x" + g.count : "");
                cell.Set(g.item.iconColor, UiTheme.RarityMark(g.item.rarity), UiTheme.RarityColor(g.item.rarity), badge, i == selected);
            });
            if (groups.Count == 0) { detail.text = ""; return; }

            var sel = groups[selected];
            sb.Length = 0;
            sb.Append(UiTheme.ItemLabel(loc, sel.item)).Append('\n');
            sb.Append(UiTheme.StatsText(loc, ItemMath.Totals(sel.item))).Append('\n');
            sb.Append(loc.Format("summon.got_owned", sel.count, progress.GetItemCount(sel.item.id)));
            if (sel.isNew) sb.Append("  ").Append(loc.Get("gacha.new_tag"));
            detail.text = sb.ToString();
        }
    }
}
