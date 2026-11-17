using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Gacha;
using UnityEditor;
using UnityEngine;

namespace PixelImmortal.Tests
{
    public class GachaTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";

        PrototypeConfig cfg;
        EconomyConfig eco;
        PlayerProgress progress;
        GachaPoolDefinition pool;
        readonly List<PullResult> results = new List<PullResult>();

        [SetUp]
        public void SetUp()
        {
            cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            eco = cfg.economy;
            progress = new PlayerProgress(eco);
            pool = eco.gachaPools[0];
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            created.Clear();
        }

        readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        GachaPoolDefinition MakePool(params (ItemDefinition item, float weight)[] entries)
        {
            var p = ScriptableObject.CreateInstance<GachaPoolDefinition>();
            created.Add(p);
            p.id = "test"; p.costResource = ResourceType.LinhThach; p.costPerPull = 100;
            p.entries = entries.Select(e => new GachaEntry { item = e.item, weight = e.weight }).ToArray();
            return p;
        }

        ItemDefinition I(string id) => eco.FindItem(id);

        // ---- validation (probabilities) ------------------------------------------------

        [Test]
        public void ShippedPoolIsValidAndProbabilitiesSumToOne()
        {
            Assert.IsEmpty(GachaMath.Validate(pool, eco));
            double sum = 0;
            for (int i = 0; i < pool.entries.Length; i++) sum += GachaMath.Probability(pool, i);
            Assert.AreEqual(1.0, sum, 1e-9);
        }

        [Test]
        public void ShippedRarityRatesMatchTheDesign()
        {
            var rates = GachaMath.RarityRates(pool);
            Assert.AreEqual(0.55, rates[Rarity.Common], 1e-9);
            Assert.AreEqual(0.28, rates[Rarity.Uncommon], 1e-9);
            Assert.AreEqual(0.12, rates[Rarity.Rare], 1e-9);
            Assert.AreEqual(0.04, rates[Rarity.Epic], 1e-9);
            Assert.AreEqual(0.01, rates[Rarity.Legendary], 1e-9);
        }

        [Test]
        public void InvalidPoolsAreRejected()
        {
            Assert.IsNotEmpty(GachaMath.Validate(null, eco));
            Assert.IsNotEmpty(GachaMath.Validate(MakePool(), eco), "empty");
            Assert.IsNotEmpty(GachaMath.Validate(MakePool((I("sword_wood"), 0f)), eco), "zero weight");
            Assert.IsNotEmpty(GachaMath.Validate(MakePool((I("sword_wood"), -1f)), eco), "negative weight");
            Assert.IsNotEmpty(GachaMath.Validate(MakePool((I("sword_wood"), float.NaN)), eco), "NaN weight");
            Assert.IsNotEmpty(GachaMath.Validate(MakePool((I("sword_wood"), float.PositiveInfinity)), eco), "infinite weight");
            Assert.IsNotEmpty(GachaMath.Validate(MakePool((null, 1f)), eco), "missing item");
            Assert.IsNotEmpty(GachaMath.Validate(MakePool((I("sword_wood"), 1f), (I("sword_wood"), 2f)), eco), "duplicate item");

            var stranger = ScriptableObject.CreateInstance<ItemDefinition>();
            created.Add(stranger);
            stranger.id = "not_in_economy";
            Assert.IsNotEmpty(GachaMath.Validate(MakePool((stranger, 1f)), eco), "item outside the economy list");

            var free = MakePool((I("sword_wood"), 1f));
            free.costPerPull = 0;
            Assert.IsNotEmpty(GachaMath.Validate(free, eco), "free pulls");
            free.costPerPull = double.NaN;
            Assert.IsNotEmpty(GachaMath.Validate(free, eco), "NaN cost");
        }

        [Test]
        public void ValidSmallPoolPasses()
            => Assert.IsEmpty(GachaMath.Validate(MakePool((I("sword_wood"), 3f), (I("sword_iron"), 1f)), eco));

        // ---- picking --------------------------------------------------------------------

        [Test]
        public void PickHandlesBoundaryRolls()
        {
            int last = pool.entries.Length - 1;
            Assert.AreEqual(0, GachaMath.Pick(pool, 0.0));
            Assert.AreEqual(last, GachaMath.Pick(pool, 0.9999999999));
            Assert.AreEqual(last, GachaMath.Pick(pool, 1.0), "roll of exactly 1 is clamped");
            Assert.AreEqual(last, GachaMath.Pick(pool, 7.0));
            Assert.AreEqual(0, GachaMath.Pick(pool, -3.0));
            Assert.AreEqual(0, GachaMath.Pick(pool, double.NaN));
        }

        [Test]
        public void PickFollowsCumulativeWeights()
        {
            var p = MakePool((I("sword_wood"), 3f), (I("sword_iron"), 1f)); // 75% / 25%
            Assert.AreEqual(0, GachaMath.Pick(p, 0.74));
            Assert.AreEqual(1, GachaMath.Pick(p, 0.76));
        }

        [Test]
        public void PickIsStatisticallyFaithfulToTheWeights()
        {
            var rng = new System.Random(2024);
            int n = 200000;
            var hits = new int[pool.entries.Length];
            for (int i = 0; i < n; i++) hits[GachaMath.Pick(pool, rng.NextDouble())]++;
            for (int i = 0; i < hits.Length; i++)
            {
                double p = GachaMath.Probability(pool, i);
                Assert.AreEqual(p, hits[i] / (double)n, 0.005, "entry " + pool.entries[i].item.id);
            }
        }

        // ---- transactions -----------------------------------------------------------------

        [Test]
        public void SuccessfulPullChargesExactlyAndGrantsTheItems()
        {
            progress.Wallet.Add(ResourceType.LinhThach, pool.costPerPull * 12);
            var gacha = new GachaSystem(progress, new System.Random(5).NextDouble);

            Assert.AreEqual(PullStatus.Ok, gacha.TryPull(pool, 10, results));
            Assert.AreEqual(10, results.Count);
            Assert.AreEqual(pool.costPerPull * 2, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);
            Assert.AreEqual(10, progress.OwnedItems.Sum(kv => kv.Value), "every pulled item is in the inventory");
            Assert.AreEqual(10, progress.TotalPulls);
        }

        [Test]
        public void NewFlagOnlyOnTheFirstCopy()
        {
            progress.Wallet.Add(ResourceType.LinhThach, pool.costPerPull * 5);
            var gacha = new GachaSystem(progress, () => 0.0); // always the first entry
            gacha.TryPull(pool, 3, results);
            Assert.IsTrue(results[0].isNew);
            Assert.IsFalse(results[1].isNew);
            Assert.IsFalse(results[2].isNew);
            Assert.AreEqual(3, progress.GetItemCount(pool.entries[0].item.id));
        }

        [Test]
        public void NotEnoughResourcesChangesNothing()
        {
            progress.Wallet.Add(ResourceType.LinhThach, pool.costPerPull - 1);
            var gacha = new GachaSystem(progress, () => 0.5);
            Assert.AreEqual(PullStatus.NotEnoughResources, gacha.TryPull(pool, 1, results));
            Assert.AreEqual(pool.costPerPull - 1, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);
            Assert.AreEqual(0, progress.OwnedItems.Count());
            Assert.AreEqual(0, progress.TotalPulls);
            Assert.IsEmpty(results);
        }

        [Test]
        public void ExactFundsSucceedAndLeaveZero()
        {
            progress.Wallet.Add(ResourceType.LinhThach, pool.costPerPull);
            var gacha = new GachaSystem(progress, () => 0.5);
            Assert.AreEqual(PullStatus.Ok, gacha.TryPull(pool, 1, results));
            Assert.AreEqual(0, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);
        }

        [Test]
        public void MultiPullUsesTheMultiplierAndCanBeUnaffordableWhileSingleIsFine()
        {
            progress.Wallet.Add(ResourceType.LinhThach, pool.costPerPull * 3);
            var gacha = new GachaSystem(progress, () => 0.5);
            Assert.IsTrue(gacha.CanAfford(pool, 3));
            Assert.IsFalse(gacha.CanAfford(pool, 4));
            Assert.AreEqual(PullStatus.NotEnoughResources, gacha.TryPull(pool, 10, results));
            Assert.AreEqual(pool.costPerPull * 3, progress.Wallet.Get(ResourceType.LinhThach), 1e-9);
        }

        [Test]
        public void BadCountsAreRefusedBeforeCharging()
        {
            progress.Wallet.Add(ResourceType.LinhThach, 1e6);
            var gacha = new GachaSystem(progress, () => 0.5);
            foreach (int bad in new[] { 0, -1, eco.maxPullCount + 1, int.MaxValue })
                Assert.AreEqual(PullStatus.InvalidCount, gacha.TryPull(pool, bad, results), "count " + bad);
            Assert.AreEqual(1e6, progress.Wallet.Get(ResourceType.LinhThach), 1e-6);
            Assert.AreEqual(0, progress.OwnedItems.Count());
        }

        [Test]
        public void InvalidPoolIsNeverChargedOrGranted()
        {
            progress.Wallet.Add(ResourceType.LinhThach, 1e6);
            var gacha = new GachaSystem(progress, () => 0.5);
            var broken = MakePool((I("sword_wood"), 0f));
            Assert.AreEqual(PullStatus.InvalidPool, gacha.TryPull(broken, 1, results));
            Assert.AreEqual(PullStatus.InvalidPool, gacha.TryPull(null, 1, results));
            Assert.AreEqual(1e6, progress.Wallet.Get(ResourceType.LinhThach), 1e-6);
            Assert.AreEqual(0, progress.OwnedItems.Count());
        }

        [Test]
        public void PullsCostOnlyTheConfiguredResource()
        {
            progress.Wallet.Add(ResourceType.LinhThach, pool.costPerPull * 3);
            progress.Wallet.Add(ResourceType.LinhKhi, 500);
            progress.Wallet.Add(ResourceType.TinhHoa, 50);
            new GachaSystem(progress, () => 0.5).TryPull(pool, 2, results);
            Assert.AreEqual(500, progress.Wallet.Get(ResourceType.LinhKhi), 1e-9);
            Assert.AreEqual(50, progress.Wallet.Get(ResourceType.TinhHoa), 1e-9);
        }

        [Test]
        public void LongRunNeverLosesAnythingAndConservesResources()
        {
            progress.Wallet.Add(ResourceType.LinhThach, 1e6);
            var gacha = new GachaSystem(progress, new System.Random(77).NextDouble);
            int pulls = 0;
            for (int i = 0; i < 300; i++)
            {
                int n = 1 + i % eco.maxPullCount;
                if (gacha.TryPull(pool, n, results) == PullStatus.Ok) pulls += n;
            }
            Assert.AreEqual(1e6 - pulls * pool.costPerPull, progress.Wallet.Get(ResourceType.LinhThach), 1e-6);
            Assert.AreEqual(pulls, progress.OwnedItems.Sum(kv => kv.Value), "items granted == pulls paid for");
            Assert.AreEqual(pulls, progress.TotalPulls);
        }
    }
}
