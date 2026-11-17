using NUnit.Framework;
using PixelImmortal.UI;
using UnityEngine;

namespace PixelImmortal.Tests
{
    public class SafeAreaTests
    {
        [Test]
        public void FullScreenSafeAreaKeepsTheOriginalLayout()
        {
            var v = BattleHud.ArenaViewportFor(new Rect(0, 0, 1, 1));
            Assert.AreEqual(0f, v.x, 1e-5f);
            Assert.AreEqual(0.16f, v.y, 1e-5f);
            Assert.AreEqual(1f, v.width, 1e-5f);
            Assert.AreEqual(0.70f, v.height, 1e-5f);
        }

        [Test]
        public void NotchAndGestureBarShrinkTheArenaViewportInsideTheSafeArea()
        {
            var safe = new Rect(0.04f, 0.04f, 0.92f, 0.90f);   // insets on every side
            var v = BattleHud.ArenaViewportFor(safe);

            Assert.GreaterOrEqual(v.xMin, safe.xMin - 1e-5f);
            Assert.LessOrEqual(v.xMax, safe.xMax + 1e-5f);
            Assert.GreaterOrEqual(v.yMin, safe.yMin);
            Assert.LessOrEqual(v.yMax, safe.yMax);
            Assert.AreEqual(safe.y + safe.height * 0.16f, v.y, 1e-5f, "starts above the bottom bar");
            Assert.AreEqual(safe.y + safe.height * 0.86f, v.yMax, 1e-5f, "ends below the top bar");
        }

        [Test]
        public void ViewportAlwaysLeavesRoomForBothBars()
        {
            foreach (var safe in new[] { new Rect(0, 0, 1, 1), new Rect(0, 0.05f, 1, 0.9f), new Rect(0.05f, 0, 0.9f, 0.95f) })
            {
                var v = BattleHud.ArenaViewportFor(safe);
                float bottomBar = v.yMin - safe.yMin, topBar = safe.yMax - v.yMax;
                Assert.Greater(bottomBar, 0.1f * safe.height, "bottom bar needs height");
                Assert.Greater(topBar, 0.1f * safe.height, "top bar needs height");
                Assert.Greater(v.height, 0.5f * safe.height, "the arena keeps most of the screen");
            }
        }

        [Test]
        public void NormalizedSafeAreaIsAFractionOfTheScreen()
        {
            var n = SafeAreaFitter.Normalized;
            Assert.GreaterOrEqual(n.xMin, 0f); Assert.GreaterOrEqual(n.yMin, 0f);
            Assert.LessOrEqual(n.xMax, 1.0001f); Assert.LessOrEqual(n.yMax, 1.0001f);
            Assert.Greater(n.width, 0f); Assert.Greater(n.height, 0f);
        }
    }
}
