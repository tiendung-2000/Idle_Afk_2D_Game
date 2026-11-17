using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PixelImmortal.Combat;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Equipment;
using PixelImmortal.Gacha;
using PixelImmortal.Loot;
using PixelImmortal.Offline;
using PixelImmortal.Save;
using UnityEditor;
using UnityEngine;

namespace PixelImmortal.Tests
{
    public class EquipmentTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";

        PrototypeConfig cfg;
        EconomyConfig eco;
        PlayerProgress progress;
        EquipmentSystem equip;

        [SetUp]
        public void SetUp()
        {
            cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            eco = cfg.economy;
            progress = new PlayerProgress(eco);
            equip = new EquipmentSystem(progress);
        }

        ItemDefinition Item(string id) => eco.FindItem(id);

        // ---- content -----------------------------------------------------------

        [Test]
        public void ItemsAreWellFormed()
        {
            Assert.AreEqual(15, eco.items.Length);
            Assert.AreEqual(eco.items.Length, eco.items.Select(i => i.id).Distinct().Count(), "ids unique");
            foreach (var i in eco.items)
            {
                Assert.IsNotEmpty(i.modifiers, i.id + " has no stats");
                foreach (var m in i.modifiers)
                {
                    Assert.IsFalse(float.IsNaN(m.value) || float.IsInfinity(m.value), i.id);
                    Assert.Greater(m.value, 0f, i.id);
                }
                Assert.LessOrEqual(i.requiredRealmIndex, eco.realms.Length - 1, i.id);
            }
            foreach (EquipSlot s in Enum.GetValues(typeof(EquipSlot)))
                foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
                    Assert.IsTrue(eco.items.Any(i => i.slot == s && i.rarity == r), "missing " + s + "/" + r);
        }

        [Test]
        public void EveryDropSourceHasItemsAndLegendariesAreGachaOnly()
        {
            foreach (var src in new[] { DropSource.NormalEnemy, DropSource.EliteEnemy, DropSource.Boss })
                Assert.IsTrue(eco.items.Any(i => (i.dropSources & src) != 0 && i.dropWeight > 0), "nothing drops from " + src);
            Assert.IsFalse(eco.items.Any(i => i.rarity == Rarity.Legendary && i.dropSources != DropSource.None));
        }

        // ---- equip rules --------------------------------------------------------

        [Test]
        public void CannotEquipWhatYouDoNotOwn()
        {
            Assert.AreEqual(EquipResult.NotOwned, equip.TryEquip(Item("sword_wood")));
            Assert.AreEqual(EquipResult.UnknownItem, equip.TryEquip(null));
            Assert.IsNull(equip.Equipped(EquipSlot.Weapon));
        }

        [Test]
        public void RealmRequirementIsEnforced()
        {
            progress.AddItem("sword_spirit");
            Assert.AreEqual(EquipResult.RealmTooLow, equip.TryEquip(Item("sword_spirit")));
            progress.SetRealm(Item("sword_spirit").requiredRealmIndex);
            Assert.AreEqual(EquipResult.Ok, equip.TryEquip(Item("sword_spirit")));
        }

        [Test]
        public void EquipGoesIntoItsOwnSlotAndKeepsTheOldItemOwned()
        {
            progress.AddItem("sword_wood");
            progress.AddItem("sword_iron");
            progress.AddItem("robe_cloth");
            equip.TryEquip(Item("sword_wood"));
            equip.TryEquip(Item("robe_cloth"));
            Assert.AreEqual("sword_wood", progress.GetEquippedId(EquipSlot.Weapon));
            Assert.AreEqual("robe_cloth", progress.GetEquippedId(EquipSlot.Armor));

            equip.TryEquip(Item("sword_iron"));
            Assert.AreEqual("sword_iron", progress.GetEquippedId(EquipSlot.Weapon));
            Assert.AreEqual(1, progress.GetItemCount("sword_wood"), "replaced item is not lost");
            Assert.AreEqual("robe_cloth", progress.GetEquippedId(EquipSlot.Armor), "other slots untouched");
        }

        [Test]
        public void UnequipKeepsTheItem()
        {
            progress.AddItem("sword_wood");
            equip.TryEquip(Item("sword_wood"));
            Assert.IsTrue(equip.Unequip(EquipSlot.Weapon));
            Assert.IsFalse(equip.Unequip(EquipSlot.Weapon), "nothing left to unequip");
            Assert.AreEqual(1, progress.GetItemCount("sword_wood"));
        }

        [Test]
        public void NextCandidateCyclesThroughOwnedItemsBestFirst()
        {
            progress.AddItem("sword_wood");
            progress.AddItem("sword_iron");
            progress.AddItem("sword_wood"); // duplicate copy does not create a second entry
            Assert.AreEqual("sword_iron", equip.NextCandidate(EquipSlot.Weapon).id, "nothing equipped -> best item");
            equip.TryEquip(Item("sword_iron"));
            Assert.AreEqual("sword_wood", equip.NextCandidate(EquipSlot.Weapon).id);
            equip.TryEquip(Item("sword_wood"));
            Assert.AreEqual("sword_iron", equip.NextCandidate(EquipSlot.Weapon).id, "wraps around");
        }

        [Test]
        public void NextCandidateIsNullWithOnlyTheEquippedItem()
        {
            progress.AddItem("sword_wood");
            equip.TryEquip(Item("sword_wood"));
            Assert.IsNull(equip.NextCandidate(EquipSlot.Weapon));
            Assert.IsNull(equip.NextCandidate(EquipSlot.Armor));
        }

        [Test]
        public void RemovingTheLastCopyUnequips()
        {
            progress.AddItem("sword_wood", 2);
            equip.TryEquip(Item("sword_wood"));
            Assert.IsTrue(progress.TryRemoveItem("sword_wood", 1));
            Assert.AreEqual("sword_wood", progress.GetEquippedId(EquipSlot.Weapon), "one copy left");
            Assert.IsTrue(progress.TryRemoveItem("sword_wood", 1));
            Assert.AreEqual("", progress.GetEquippedId(EquipSlot.Weapon));
            Assert.IsFalse(progress.TryRemoveItem("sword_wood", 1));
        }

        [Test]
        public void AddItemRejectsUnknownIdsAndBadCounts()
        {
            Assert.IsFalse(progress.AddItem("not_an_item"));
            Assert.IsFalse(progress.AddItem("sword_wood", 0));
            Assert.IsFalse(progress.AddItem("sword_wood", -3));
            Assert.AreEqual(0, progress.GetItemCount("sword_wood"));
        }

        // ---- stat modifiers -------------------------------------------------------

        [Test]
        public void EquippedItemsChangeStatsAndUnequipRestoresThem()
        {
            var before = PlayerStats.Compute(cfg, eco, progress);
            progress.AddItem("sword_iron");
            Assert.AreEqual(before.Attack, PlayerStats.Compute(cfg, eco, progress).Attack, 1e-5f, "owning is not enough");

            equip.TryEquip(Item("sword_iron"));
            var after = PlayerStats.Compute(cfg, eco, progress);
            Assert.AreEqual(before.Attack * 1.10f, after.Attack, 1e-3f);

            equip.Unequip(EquipSlot.Weapon);
            Assert.AreEqual(before.Attack, PlayerStats.Compute(cfg, eco, progress).Attack, 1e-5f);
        }

        [Test]
        public void ItemsStackWithUpgradesInTheSameBonusPool()
        {
            var up = eco.upgrades.First(u => u.stat == StatType.Attack);
            progress.SetUpgradeLevel(up.id, 10); // +100%
            progress.AddItem("sword_iron");
            equip.TryEquip(Item("sword_iron"));    // +10%
            Assert.AreEqual(cfg.playerAttack * (1f + 1.0f + 0.10f), PlayerStats.Compute(cfg, eco, progress).Attack, 1e-3f);
        }

        [Test]
        public void EquippingRaisesPlayerStatsEvent()
        {
            int fired = 0;
            progress.StatsChanged += () => fired++;
            progress.AddItem("robe_cloth");
            equip.TryEquip(Item("robe_cloth"));
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void EquippedItemRaisesOfflineEfficiency()
        {
            var plain = new SaveData { savedAtUtcTicks = DateTime.UtcNow.AddHours(-2).Ticks };
            var svc = new OfflineService(cfg);
            svc.Prepare(plain, DateTime.UtcNow.Ticks, progress);

            var p2 = new PlayerProgress(eco);
            p2.SetRealm(Item("amulet_spirit").requiredRealmIndex);
            p2.AddItem("amulet_spirit");
            new EquipmentSystem(p2).TryEquip(Item("amulet_spirit"));
            var boosted = new SaveData { savedAtUtcTicks = DateTime.UtcNow.AddHours(-2).Ticks };
            svc.Prepare(boosted, DateTime.UtcNow.Ticks, p2);
            Assert.Greater(boosted.offline.efficiency, plain.offline.efficiency);
        }

        [Test]
        public void DiffShowsOnlyRealChanges()
        {
            var d = ItemMath.Diff(Item("sword_wood"), Item("sword_spirit"));
            Assert.AreEqual(0.18f - 0.05f, d[StatType.Attack], 1e-5f);
            Assert.AreEqual(0.02f, d[StatType.CritChance], 1e-5f);

            var gone = ItemMath.Diff(Item("sword_spirit"), Item("sword_wood"));
            Assert.AreEqual(-0.02f, gone[StatType.CritChance], 1e-5f, "a stat the new item lacks shows as a loss");

            Assert.IsEmpty(ItemMath.Diff(Item("sword_wood"), Item("sword_wood")));
            Assert.AreEqual(0.05f, ItemMath.Diff(null, Item("sword_wood"))[StatType.Attack], 1e-5f);
        }

        // ---- save ------------------------------------------------------------------

        [Test]
        public void InventoryEquipmentAndPullsRoundTripThroughJson()
        {
            progress.SetRealm(2);
            progress.AddItem("sword_wood", 3);
            progress.AddItem("robe_jade");
            progress.AddPulls(7);
            equip.TryEquip(Item("sword_wood"));
            equip.TryEquip(Item("robe_jade"));

            string json = JsonUtility.ToJson(progress.ToData());
            var restored = new PlayerProgress(eco);
            restored.ApplyData(JsonUtility.FromJson<ProgressData>(json));

            Assert.AreEqual(3, restored.GetItemCount("sword_wood"));
            Assert.AreEqual(1, restored.GetItemCount("robe_jade"));
            Assert.AreEqual("sword_wood", restored.GetEquippedId(EquipSlot.Weapon));
            Assert.AreEqual("robe_jade", restored.GetEquippedId(EquipSlot.Armor));
            Assert.AreEqual("", restored.GetEquippedId(EquipSlot.Accessory));
            Assert.AreEqual(7, restored.TotalPulls);
            Assert.AreEqual(PlayerStats.Compute(cfg, eco, progress).Attack, PlayerStats.Compute(cfg, eco, restored).Attack, 1e-4f);
        }

        [Test]
        public void CorruptInventoryIsSanitizedWithoutLosingValidItems()
        {
            var bad = new ProgressData { totalPulls = -5 };
            bad.items.Add(new LevelEntry { id = "sword_wood", level = 2 });
            bad.items.Add(new LevelEntry { id = "ghost_item", level = 9 });         // unknown
            bad.items.Add(new LevelEntry { id = "sword_iron", level = -4 });        // negative
            bad.items.Add(new LevelEntry { id = "robe_cloth", level = int.MaxValue }); // absurd
            bad.items.Add(null);
            bad.equipped.AddRange(new[] { "robe_cloth", "robe_cloth", "ring_copper", "sword_wood" }); // wrong slot, not owned, extra entry

            progress.ApplyData(bad);

            Assert.AreEqual(2, progress.GetItemCount("sword_wood"));
            Assert.AreEqual(0, progress.GetItemCount("ghost_item"));
            Assert.AreEqual(0, progress.GetItemCount("sword_iron"));
            Assert.AreEqual(PlayerProgress.MaxItemCount, progress.GetItemCount("robe_cloth"));
            Assert.AreEqual("", progress.GetEquippedId(EquipSlot.Weapon), "robe in the weapon slot is rejected");
            Assert.AreEqual("robe_cloth", progress.GetEquippedId(EquipSlot.Armor));
            Assert.AreEqual("", progress.GetEquippedId(EquipSlot.Accessory), "not owned");
            Assert.AreEqual(0, progress.TotalPulls);
        }

        [Test]
        public void SchemaV2SaveLoadsWithAnEmptyInventory()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pi_eq_" + Guid.NewGuid().ToString("N"));
            try
            {
                System.IO.Directory.CreateDirectory(dir);
                string payload = "{\"schemaVersion\":2,\"progress\":{\"linhThach\":77},\"world\":{\"currentChapter\":2}}";
                var env = new SaveEnvelope { schemaVersion = 2, payload = payload, checksum = SaveSystem.Checksum(payload) };
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "save.json"), JsonUtility.ToJson(env));
                var r = new SaveSystem(dir).Load();
                Assert.AreEqual(LoadStatus.Loaded, r.Status);
                Assert.AreEqual(SaveData.CurrentSchemaVersion, r.Data.schemaVersion);
                Assert.AreEqual(77, r.Data.progress.linhThach, 1e-9);
                Assert.IsEmpty(r.Data.progress.items);
                var p = new PlayerProgress(eco);
                p.ApplyData(r.Data.progress);
                Assert.AreEqual(0, p.OwnedItems.Count());
            }
            finally { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
        }

        // ---- loot -----------------------------------------------------------------

        static Func<double> Seq(params double[] values)
        {
            int i = 0;
            return () => values[Math.Min(i++, values.Length - 1)];
        }

        [Test]
        public void LootNeedsTheChanceRollToSucceed()
        {
            var loot = new LootSystem(progress, Seq(0.99));           // 0.99 >= 2% -> no drop
            Assert.IsNull(loot.RollKill(false, false));
            Assert.AreEqual(0, progress.OwnedItems.Count());

            var loot2 = new LootSystem(progress, Seq(0.0, 0.0));      // chance hit, first eligible
            var got = loot2.RollKill(false, false);
            Assert.NotNull(got);
            Assert.AreEqual(1, progress.GetItemCount(got.id));
        }

        [Test]
        public void BossAlwaysDropsWithDefaultChance()
        {
            Assert.AreEqual(1f, eco.bossDropChance);
            var loot = new LootSystem(progress, Seq(0.9999, 0.5));
            Assert.NotNull(loot.RollKill(true, false));
        }

        [Test]
        public void LootOnlyDropsItemsAllowedForTheSource()
        {
            var rng = new System.Random(1234);
            var loot = new LootSystem(progress, rng.NextDouble);
            var seen = new Dictionary<DropSource, HashSet<string>>();
            foreach (var src in new[] { DropSource.NormalEnemy, DropSource.EliteEnemy, DropSource.Boss })
            {
                seen[src] = new HashSet<string>();
                for (int n = 0; n < 4000; n++)
                {
                    var it = loot.Roll(src);
                    if (it != null) { seen[src].Add(it.id); Assert.AreNotEqual(0, (int)(it.dropSources & src), it.id + " from " + src); }
                }
                Assert.IsNotEmpty(seen[src], "no drops from " + src);
            }
            Assert.IsFalse(seen.Values.Any(s => s.Any(id => Item(id).rarity == Rarity.Legendary)), "legendaries never drop");
        }

        [Test]
        public void NormalDropRateMatchesConfiguration()
        {
            var rng = new System.Random(99);
            var loot = new LootSystem(progress, rng.NextDouble);
            int drops = 0, kills = 20000;
            for (int i = 0; i < kills; i++) if (loot.RollKill(false, false) != null) drops++;
            double expected = kills * eco.normalDropChance;
            Assert.AreEqual(expected, drops, 5 * Math.Sqrt(expected), "drop rate far from the configured chance");
        }

        [Test]
        public void DropEventFiresAndInventoryGrows()
        {
            ItemDefinition seen = null;
            var loot = new LootSystem(progress, Seq(0.0, 0.0));
            loot.Dropped += i => seen = i;
            var got = loot.RollKill(false, true);
            Assert.AreSame(got, seen);
            Assert.AreEqual(1, progress.GetItemCount(got.id));
        }

        [Test]
        public void OfflineProcessingNeverDropsItems()
        {
            var svc = new OfflineService(cfg);
            var save = new SaveData { savedAtUtcTicks = DateTime.UtcNow.AddHours(-12).Ticks };
            svc.Prepare(save, DateTime.UtcNow.Ticks, progress);
            svc.Claim(save, progress);
            Assert.AreEqual(0, progress.OwnedItems.Count());
        }
    }
}
