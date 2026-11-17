using NUnit.Framework;
using PixelImmortal.Data;
using PixelImmortal.Progression;
using UnityEditor;

namespace PixelImmortal.Tests
{
    public class ProgressionTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";

        static PrototypeConfig Config() => AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);

        [TestCase(1, 1, 1)]
        [TestCase(2, 1, 2)]
        [TestCase(3, 1, 3)]
        [TestCase(4, 2, 1)]
        [TestCase(6, 2, 3)]
        [TestCase(7, 3, 1)]
        [TestCase(9, 3, 3)]
        [TestCase(10, 4, 1)]
        public void ChapterMapsToCycleAndMap(int chapter, int cycle, int map)
        {
            Assert.AreEqual(cycle, ProgressionMath.CycleIndex(chapter));
            Assert.AreEqual(map, ProgressionMath.MapIndex(chapter));
        }

        [Test]
        public void IndexingClampsInvalidChapter()
        {
            Assert.AreEqual(1, ProgressionMath.CycleIndex(0));
            Assert.AreEqual(1, ProgressionMath.MapIndex(-5));
        }

        [Test]
        public void CycleOneFactorIsOne()
            => Assert.AreEqual(1f, ProgressionMath.CycleFactor(1, 0.7f, 0.5f, 100f), 1e-6f);

        [Test]
        public void CycleFactorIsMonotonicAndClamped()
        {
            float prev = 0f;
            for (int c = 1; c <= 200; c++)
            {
                float f = ProgressionMath.CycleFactor(c, 0.7f, 0.5f, 50f);
                Assert.GreaterOrEqual(f, prev);
                Assert.LessOrEqual(f, 50f);
                prev = f;
            }
        }

        [Test]
        public void CycleFactorSurvivesBadInput()
        {
            Assert.IsFalse(float.IsNaN(ProgressionMath.CycleFactor(int.MaxValue, float.MaxValue, float.MaxValue, 100f)));
            Assert.AreEqual(100f, ProgressionMath.CycleFactor(int.MaxValue, float.MaxValue, float.MaxValue, 100f));
            Assert.GreaterOrEqual(ProgressionMath.CycleFactor(5, -3f, -3f, 100f), 1f);
        }

        [Test]
        public void DefaultScalingNeverDropsAcrossWavesMapsAndCycles()
        {
            var cfg = Config();
            Assert.NotNull(cfg);
            var s = cfg.scaling;
            float prevHp = 0f, prevAtk = 0f, prevReward = 0f, prevDensity = 0f;
            for (int chapter = 1; chapter <= 30; chapter++)
            {
                var map = cfg.maps[(ProgressionMath.MapIndex(chapter) - 1) % cfg.maps.Length];
                foreach (var w in map.waves)
                {
                    float hp = s.EnemyHp(chapter, w.difficultyOffset);
                    float atk = s.EnemyAttack(chapter, w.difficultyOffset);
                    Assert.GreaterOrEqual(hp, prevHp, "hp dropped at chapter " + chapter);
                    Assert.GreaterOrEqual(atk, prevAtk, "attack dropped at chapter " + chapter);
                    prevHp = hp; prevAtk = atk;
                }
                Assert.GreaterOrEqual(s.Reward(chapter), prevReward);
                Assert.GreaterOrEqual(s.Density(chapter), prevDensity);
                prevReward = s.Reward(chapter);
                prevDensity = s.Density(chapter);
            }
        }

        [Test]
        public void ScalingIsFiniteAtHugeChapters()
        {
            var s = Config().scaling;
            Assert.IsFalse(float.IsInfinity(s.EnemyHp(int.MaxValue, 0f)));
            Assert.IsFalse(float.IsNaN(s.BossAttack(int.MaxValue)));
            Assert.LessOrEqual(s.Density(int.MaxValue), s.density.maxFactor);
        }

        [Test]
        public void BossHpGrowsWithCycleAndKeepsBaseDesign()
        {
            var s = Config().scaling;
            Assert.AreEqual(1f, s.BossHp(1), 1e-6f);
            Assert.Greater(s.BossHp(4), s.BossHp(3));
            Assert.Greater(s.BossHp(7), s.BossHp(4));
        }

        [Test]
        public void EveryMapHasTenWavesRosterAndBoss()
        {
            var cfg = Config();
            Assert.AreEqual(3, cfg.maps.Length);
            foreach (var m in cfg.maps)
            {
                Assert.AreEqual(10, m.waves.Length, m.mapId);
                Assert.AreEqual(4, m.roster.Length, m.mapId);
                Assert.IsNotEmpty(m.bossName, m.mapId);
                var last = m.waves[m.waves.Length - 1];
                Assert.GreaterOrEqual(last.eliteCountInFinalGroup, 1, m.mapId);
                foreach (var w in m.waves)
                {
                    Assert.GreaterOrEqual(w.groupCount, 1);
                    Assert.GreaterOrEqual(w.enemiesPerGroup, 1);
                    Assert.GreaterOrEqual(w.eliteCountInFinalGroup, 1, "each wave's final group has an elite");
                }
                foreach (var r in m.roster) Assert.Greater(r.spawnWeight, 0f);
            }
        }
    }
}
