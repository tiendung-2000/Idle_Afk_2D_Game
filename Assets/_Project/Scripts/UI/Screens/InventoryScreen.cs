using System.Collections.Generic;
using System.Text;
using PixelImmortal.Data;
using PixelImmortal.Equipment;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>Everything owned, filterable by slot. The game has no materials or consumables yet, so there are no such tabs.</summary>
    public class InventoryScreen : FrameScreen
    {
        readonly StringBuilder sb = new StringBuilder();
        readonly List<ItemDefinition> items = new List<ItemDefinition>();
        readonly Image[] tabImages = new Image[4];
        SlotGridView grid;
        Text infoText, emptyText, equipLabel;
        Button equipButton;
        int filter = -1; // -1 = all, otherwise EquipSlot
        string selectedId;

        protected override void Build(RectTransform c)
        {
            string[] keys = { "inv.all", "slot.Weapon", "slot.Armor", "slot.Accessory" };
            for (int i = 0; i < keys.Length; i++)
            {
                int f = i - 1;
                float w = 1f / keys.Length;
                var b = UiKit.TextButton("Tab" + i, c, new Vector2(i * w + 0.01f, 0.89f), new Vector2((i + 1) * w - 0.01f, 0.99f), UiTheme.TabOff, "", 26, () => { filter = f; selectedId = null; }, out var label);
                Localization.LocalizedLabel.Bind(label, Loc, keys[i]);
                tabImages[i] = b.GetComponent<Image>();
            }
            grid = new SlotGridView(c, new Vector2(0.02f, 0.46f), new Vector2(0.98f, 0.87f), 5, 3, i => selectedId = items[i].id);
            emptyText = Body(c, 0.1f, 0.5f, 0.9f, 0.8f, 30, TextAnchor.MiddleCenter);
            infoText = Body(c, 0.04f, 0.14f, 0.96f, 0.45f, 28);
            equipButton = Action(c, 0.25f, 0.01f, 0.75f, 0.12f, UiTheme.Action, EquipSelected, out equipLabel);
        }

        protected override void Render()
        {
            items.Clear();
            foreach (var def in G.Progress.Economy.items)
                if (def != null && G.Progress.GetItemCount(def.id) > 0 && (filter < 0 || (int)def.slot == filter)) items.Add(def);
            items.Sort((a, b) =>
            {
                int r = b.rarity.CompareTo(a.rarity);
                return r != 0 ? r : string.CompareOrdinal(a.id, b.id);
            });

            for (int i = 0; i < tabImages.Length; i++) tabImages[i].color = i - 1 == filter ? UiTheme.TabOn : UiTheme.TabOff;

            if (selectedId == null || items.FindIndex(x => x.id == selectedId) < 0) selectedId = items.Count > 0 ? items[0].id : null;
            int selectedIndex = items.FindIndex(x => x.id == selectedId);

            grid.Refresh(items.Count, selectedIndex, (cell, i) =>
            {
                var it = items[i];
                cell.Set(it.iconColor, UiTheme.RarityMark(it.rarity), UiTheme.RarityColor(it.rarity), "x" + G.Progress.GetItemCount(it.id), i == selectedIndex);
            });
            emptyText.text = items.Count == 0 ? Loc.Get("inv.empty") : "";

            var chosen = selectedIndex >= 0 ? items[selectedIndex] : null;
            sb.Length = 0;
            if (chosen != null)
            {
                sb.Append(UiTheme.ItemLabel(Loc, chosen)).Append('\n');
                sb.Append(Loc.Format("inv.owned", G.Progress.GetItemCount(chosen.id), Loc.Get("slot." + chosen.slot))).Append('\n');
                sb.Append(UiTheme.StatsText(Loc, ItemMath.Totals(chosen)));
                var worn = G.Equipment.Equipped(chosen.slot);
                if (worn != null && worn.id == chosen.id) sb.Append('\n').Append(Loc.Get("equip.worn_tag"));
                else if (G.Progress.RealmIndex < chosen.requiredRealmIndex)
                    sb.Append('\n').Append(Loc.Format("equip.need_realm", Loc.Get("realm." + G.Progress.Economy.realms[Mathf.Clamp(chosen.requiredRealmIndex, 0, G.Progress.Economy.realms.Length - 1)].id)));
                if (chosen.dropSources != DropSource.None) sb.Append('\n').Append(Loc.Format("inv.source", Sources(chosen.dropSources)));
            }
            infoText.text = sb.ToString();

            equipLabel.text = Loc.Get("btn.equip_item");
            var wornNow = chosen != null ? G.Equipment.Equipped(chosen.slot) : null;
            equipButton.interactable = chosen != null && (wornNow == null || wornNow.id != chosen.id) && G.Equipment.CanEquip(chosen, out _);
        }

        string Sources(DropSource s)
        {
            var parts = new List<string>();
            if ((s & DropSource.NormalEnemy) != 0) parts.Add(Loc.Get("src.normal"));
            if ((s & DropSource.EliteEnemy) != 0) parts.Add(Loc.Get("src.elite"));
            if ((s & DropSource.Boss) != 0) parts.Add(Loc.Get("src.boss"));
            return string.Join(", ", parts);
        }

        void EquipSelected()
        {
            var item = G.Progress.Economy.FindItem(selectedId);
            if (item != null) G.Equipment.TryEquip(item);
            Render();
        }
    }
}
