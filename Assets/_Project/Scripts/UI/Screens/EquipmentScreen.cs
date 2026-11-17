using System.Collections.Generic;
using System.Text;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Equipment;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>Equipped slots, owned items for the chosen slot, a comparison against what is worn, and Equip / Unequip.</summary>
    public class EquipmentScreen : FrameScreen
    {
        readonly StringBuilder sb = new StringBuilder();
        readonly SlotView[] slotViews = new SlotView[PlayerProgress.EquipSlotCount];
        readonly List<ItemDefinition> items = new List<ItemDefinition>();
        SlotGridView grid;
        Text infoText, equipLabel, unequipLabel;
        Button equipButton, unequipButton;
        EquipSlot slot;
        string selectedId;

        protected override void Build(RectTransform c)
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                var s = (EquipSlot)i;
                float w = 1f / slotViews.Length;
                slotViews[i] = new SlotView(c, new Vector2(i * w + 0.02f, 0.8f), new Vector2((i + 1) * w - 0.02f, 0.99f), () => { slot = s; selectedId = null; });
            }
            grid = new SlotGridView(c, new Vector2(0.02f, 0.5f), new Vector2(0.98f, 0.78f), 5, 2, i => selectedId = items[i].id);
            infoText = Body(c, 0.04f, 0.14f, 0.96f, 0.5f, 28);
            equipButton = Action(c, 0.04f, 0.01f, 0.48f, 0.12f, UiTheme.Action, EquipSelected, out equipLabel);
            unequipButton = Action(c, 0.52f, 0.01f, 0.96f, 0.12f, UiTheme.TabOff, UnequipSlot, out unequipLabel);
        }

        protected override void Render()
        {
            var eco = G.Progress.Economy;
            for (int i = 0; i < slotViews.Length; i++)
            {
                var worn = G.Equipment.Equipped((EquipSlot)i);
                string name = Loc.Get("slot." + (EquipSlot)i);
                if (worn == null) slotViews[i].SetEmpty(i == (int)slot, name);
                else slotViews[i].Set(worn.iconColor, UiTheme.RarityMark(worn.rarity), UiTheme.RarityColor(worn.rarity), name, i == (int)slot);
            }

            items.Clear();
            items.AddRange(G.Equipment.OwnedForSlot(slot));
            var current = G.Equipment.Equipped(slot);
            if (selectedId == null || items.FindIndex(x => x.id == selectedId) < 0)
                selectedId = current != null ? current.id : (items.Count > 0 ? items[0].id : null);
            int selectedIndex = items.FindIndex(x => x.id == selectedId);

            grid.Refresh(items.Count, selectedIndex, (cell, i) =>
            {
                var it = items[i];
                bool worn = current != null && current.id == it.id;
                cell.Set(it.iconColor, UiTheme.RarityMark(it.rarity), UiTheme.RarityColor(it.rarity), worn ? Loc.Get("equip.worn_tag") : "x" + G.Progress.GetItemCount(it.id), i == selectedIndex);
            });

            var chosen = selectedIndex >= 0 ? items[selectedIndex] : null;
            sb.Length = 0;
            sb.Append(Loc.Format("equip.slot_row", Loc.Get("slot." + slot), current != null ? UiTheme.ItemLabel(Loc, current) : Loc.Get("equip.none"),
                current != null ? UiTheme.StatsText(Loc, ItemMath.Totals(current)) : "")).Append('\n');
            if (chosen == null) sb.Append(Loc.Get("equip.empty_slot"));
            else if (current == null || chosen.id != current.id)
            {
                sb.Append(UiTheme.ItemLabel(Loc, chosen)).Append('\n');
                sb.Append(Loc.Format("equip.change", UiTheme.StatsText(Loc, ItemMath.Diff(current, chosen), true)));
                if (G.Progress.RealmIndex < chosen.requiredRealmIndex)
                    sb.Append('\n').Append(Loc.Format("equip.need_realm", Loc.Get("realm." + eco.realms[Mathf.Clamp(chosen.requiredRealmIndex, 0, eco.realms.Length - 1)].id)));
            }
            infoText.text = sb.ToString();

            equipLabel.text = Loc.Get("btn.equip_item");
            unequipLabel.text = Loc.Get("btn.unequip");
            equipButton.interactable = chosen != null && (current == null || current.id != chosen.id) && G.Equipment.CanEquip(chosen, out _);
            unequipButton.interactable = current != null;
        }

        void EquipSelected()
        {
            var item = G.Progress.Economy.FindItem(selectedId);
            if (item != null) G.Equipment.TryEquip(item);
            Render();
        }

        void UnequipSlot()
        {
            G.Equipment.Unequip(slot);
            Render();
        }
    }
}
