using System;
using System.Collections.Generic;
using PixelImmortal.Data;
using PixelImmortal.Save;

namespace PixelImmortal.Economy
{
    /// <summary>Runtime player progression state: wallet, upgrade/skill levels, realm, highest chapter.</summary>
    public class PlayerProgress
    {
        readonly Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();
        readonly Dictionary<string, int> skillLevels = new Dictionary<string, int>();

        public EconomyConfig Economy { get; }
        public Wallet Wallet { get; } = new Wallet();
        public int RealmIndex { get; private set; }
        public int HighestChapter { get; private set; } = 1;

        /// <summary>Raised when something that affects player stats changes (upgrade, skill level, realm).</summary>
        public event Action StatsChanged;

        readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>();
        readonly string[] equipped = new string[EquipSlotCount];

        public const int EquipSlotCount = 3;
        public const int MaxItemCount = 999999;

        public int TotalPulls { get; private set; }

        /// <summary>Raised when the inventory or equipped items change (drops, pulls, equip, unequip).</summary>
        public event Action InventoryChanged;

        public int GetItemCount(string id) => id != null && itemCounts.TryGetValue(id, out var c) ? c : 0;

        public string GetEquippedId(EquipSlot slot) => equipped[(int)slot];

        public IEnumerable<KeyValuePair<string, int>> OwnedItems => itemCounts;

        /// <summary>Adds copies of a known item. Unknown ids and non-positive counts are ignored. Returns true if added.</summary>
        public bool AddItem(string id, int count = 1)
        {
            if (count <= 0 || Economy.FindItem(id) == null) return false;
            itemCounts[id] = (int)Math.Min((long)MaxItemCount, (long)GetItemCount(id) + count);
            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>Removes copies (used to roll back a failed transaction). Unequips if the last copy goes. Returns true if removed.</summary>
        public bool TryRemoveItem(string id, int count = 1)
        {
            if (count <= 0 || GetItemCount(id) < count) return false;
            int left = GetItemCount(id) - count;
            if (left > 0) itemCounts[id] = left;
            else
            {
                itemCounts.Remove(id);
                for (int s = 0; s < equipped.Length; s++)
                    if (equipped[s] == id) { equipped[s] = ""; StatsChanged?.Invoke(); }
            }
            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>Low-level slot assignment; rules (owned, realm, slot match) live in EquipmentSystem.</summary>
        public void SetEquipped(EquipSlot slot, string id)
        {
            equipped[(int)slot] = id ?? "";
            StatsChanged?.Invoke();
            InventoryChanged?.Invoke();
        }

        public void AddPulls(int count)
        {
            if (count > 0) TotalPulls = (int)Math.Min(int.MaxValue, (long)TotalPulls + count);
        }

        public PlayerProgress(EconomyConfig economy)
        {
            Economy = economy;
            for (int i = 0; i < equipped.Length; i++) equipped[i] = "";
        }

        public int GetUpgradeLevel(string id) => upgradeLevels.TryGetValue(id, out var l) ? l : 0;

        /// <summary>Skills start at level 1 once unlocked.</summary>
        public int GetSkillLevel(string id) => skillLevels.TryGetValue(id, out var l) ? Math.Max(1, l) : 1;

        public bool IsSkillUnlocked(SkillDefinition s) => RealmIndex >= s.unlockRealmIndex;

        public void SetUpgradeLevel(string id, int level)
        {
            upgradeLevels[id] = Math.Max(0, level);
            StatsChanged?.Invoke();
        }

        public void SetSkillLevel(string id, int level)
        {
            skillLevels[id] = Math.Max(1, level);
            StatsChanged?.Invoke();
        }

        public void SetRealm(int index)
        {
            int max = Economy.realms != null ? Math.Max(0, Economy.realms.Length - 1) : 0;
            RealmIndex = Math.Min(Math.Max(0, index), max);
            StatsChanged?.Invoke();
        }

        public void NoteChapter(int chapter)
        {
            if (chapter > HighestChapter) HighestChapter = chapter;
        }

        // ---- save DTO -------------------------------------------------------

        public ProgressData ToData()
        {
            var d = new ProgressData
            {
                linhThach = Wallet.Get(ResourceType.LinhThach),
                linhKhi = Wallet.Get(ResourceType.LinhKhi),
                tinhHoa = Wallet.Get(ResourceType.TinhHoa),
                realmIndex = RealmIndex,
                highestChapter = HighestChapter
            };
            foreach (var kv in upgradeLevels) d.upgrades.Add(new LevelEntry { id = kv.Key, level = kv.Value });
            foreach (var kv in skillLevels) d.skills.Add(new LevelEntry { id = kv.Key, level = kv.Value });
            foreach (var kv in itemCounts) d.items.Add(new LevelEntry { id = kv.Key, level = kv.Value });
            foreach (var e in equipped) d.equipped.Add(e ?? "");
            d.totalPulls = TotalPulls;
            return d;
        }

        /// <summary>
        /// Applies a DTO defensively: negative/NaN resources become 0, unknown IDs are dropped,
        /// levels are clamped to [0, max] and the realm index to the valid range.
        /// </summary>
        public void ApplyData(ProgressData d)
        {
            if (d == null) return;
            upgradeLevels.Clear();
            skillLevels.Clear();

            Wallet.Set(ResourceType.LinhThach, d.linhThach);
            Wallet.Set(ResourceType.LinhKhi, d.linhKhi);
            Wallet.Set(ResourceType.TinhHoa, d.tinhHoa);
            HighestChapter = Math.Max(1, d.highestChapter);

            if (d.upgrades != null)
                foreach (var e in d.upgrades)
                {
                    var def = e != null ? Economy.FindUpgrade(e.id) : null;
                    if (def == null) continue;
                    int max = def.maxLevel > 0 ? def.maxLevel : int.MaxValue;
                    upgradeLevels[def.id] = Math.Min(Math.Max(0, e.level), max);
                }

            if (d.skills != null)
                foreach (var e in d.skills)
                {
                    var def = e != null ? Economy.FindSkill(e.id) : null;
                    if (def == null) continue;
                    skillLevels[def.id] = Math.Min(Math.Max(1, e.level), Math.Max(1, def.maxLevel));
                }

            itemCounts.Clear();
            for (int i = 0; i < equipped.Length; i++) equipped[i] = "";
            TotalPulls = Math.Max(0, d.totalPulls);

            if (d.items != null)
                foreach (var e in d.items)
                {
                    var def = e != null ? Economy.FindItem(e.id) : null;
                    if (def == null || e.level <= 0) continue;
                    itemCounts[def.id] = Math.Min(e.level, MaxItemCount);
                }

            // Equipped entries must be known, owned and match their slot; anything else is dropped (the item itself is kept).
            if (d.equipped != null)
                for (int s = 0; s < equipped.Length && s < d.equipped.Count; s++)
                {
                    var def = Economy.FindItem(d.equipped[s]);
                    if (def != null && (int)def.slot == s && GetItemCount(def.id) > 0) equipped[s] = def.id;
                }

            SetRealm(d.realmIndex);
        }
    }
}
