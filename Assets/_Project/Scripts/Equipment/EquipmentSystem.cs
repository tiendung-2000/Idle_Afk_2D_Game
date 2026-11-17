using System.Collections.Generic;
using PixelImmortal.Data;
using PixelImmortal.Economy;

namespace PixelImmortal.Equipment
{
    public enum EquipResult
    {
        Ok,
        NotOwned,
        WrongSlot,
        RealmTooLow,
        UnknownItem
    }

    /// <summary>Rules for equipping, unequipping and browsing owned items. UI calls these; it never trusts button state.</summary>
    public class EquipmentSystem
    {
        readonly PlayerProgress progress;
        readonly List<ItemDefinition> scratch = new List<ItemDefinition>();

        public EquipmentSystem(PlayerProgress progress) { this.progress = progress; }

        public ItemDefinition Equipped(EquipSlot slot) => progress.Economy.FindItem(progress.GetEquippedId(slot));

        public bool CanEquip(ItemDefinition item, out EquipResult why)
        {
            why = EquipResult.Ok;
            if (item == null) { why = EquipResult.UnknownItem; return false; }
            if (progress.GetItemCount(item.id) <= 0) { why = EquipResult.NotOwned; return false; }
            if (progress.RealmIndex < item.requiredRealmIndex) { why = EquipResult.RealmTooLow; return false; }
            return true;
        }

        /// <summary>Equips into the item's own slot. The previously equipped item stays in the inventory.</summary>
        public EquipResult TryEquip(ItemDefinition item)
        {
            if (!CanEquip(item, out var why)) return why;
            progress.SetEquipped(item.slot, item.id);
            return EquipResult.Ok;
        }

        public bool Unequip(EquipSlot slot)
        {
            if (string.IsNullOrEmpty(progress.GetEquippedId(slot))) return false;
            progress.SetEquipped(slot, "");
            return true;
        }

        /// <summary>Owned items for a slot, best rarity first, then id (stable order for UI cycling).</summary>
        public List<ItemDefinition> OwnedForSlot(EquipSlot slot)
        {
            scratch.Clear();
            foreach (var kv in progress.OwnedItems)
            {
                var def = progress.Economy.FindItem(kv.Key);
                if (def != null && def.slot == slot && kv.Value > 0) scratch.Add(def);
            }
            scratch.Sort((a, b) =>
            {
                int r = b.rarity.CompareTo(a.rarity);
                return r != 0 ? r : string.CompareOrdinal(a.id, b.id);
            });
            return scratch;
        }

        /// <summary>
        /// The next owned item after the currently equipped one (wraps around). Null if the player owns
        /// nothing else for that slot. Used by the placeholder "next" button and the comparison preview.
        /// </summary>
        public ItemDefinition NextCandidate(EquipSlot slot)
        {
            var list = OwnedForSlot(slot);
            if (list.Count == 0) return null;
            string current = progress.GetEquippedId(slot);
            int index = list.FindIndex(i => i.id == current);
            if (index < 0) return list[0];
            return list.Count > 1 ? list[(index + 1) % list.Count] : null;
        }
    }
}
