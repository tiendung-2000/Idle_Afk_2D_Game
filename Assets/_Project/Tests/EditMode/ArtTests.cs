using System.Linq;
using NUnit.Framework;
using PixelImmortal.Data;
using PixelImmortal.UI;
using UnityEditor;
using UnityEngine;

namespace PixelImmortal.Tests
{
    public class ArtTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";

        PrototypeConfig cfg;
        ArtCatalog art;

        [SetUp]
        public void SetUp()
        {
            cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            art = cfg.art;
        }

        // ---- completeness: everything the game can ask for has art ------------------------------

        [Test]
        public void CatalogIsWiredIn() => Assert.NotNull(art, "PrototypeConfig.art must reference the ArtCatalog");

        [Test]
        public void PlayerHasIdleAttackAndDeadFrames()
        {
            var f = art.Frames("player");
            Assert.NotNull(f);
            Assert.AreEqual(5, f.Length, "idle0, idle1, attack, dead, hit");
        }

        [Test]
        public void MonstersElitesAndBossesHaveCombatFrames()
        {
            foreach (var m in cfg.maps)
            {
                Assert.AreEqual(6, art.Frames("boss." + m.mapId).Length, "walk0, walk1, attack, hit, death, skill: boss." + m.mapId);
                foreach (var e in m.roster)
                {
                    Assert.AreEqual(5, art.Frames("enemy." + e.id).Length, "walk0, walk1, attack, hit, death: " + e.id);
                    Assert.AreEqual(5, art.Frames("elite." + e.id).Length, "elite: " + e.id);
                }
            }
        }

        [Test]
        public void CombatEffectsAndSkillIconsExist()
        {
            foreach (var id in new[] { "fx.hit", "fx.death", "fx.spawn", "fx.telegraph" }) Assert.IsTrue(art.Has(id), id);
            foreach (var s in cfg.economy.skills) Assert.IsTrue(art.Has("icon." + s.id), s.id);
        }

        [Test]
        public void EveryEnemyBossAndBackdropHasArt()
        {
            var missing = new System.Collections.Generic.List<string>();
            foreach (var m in cfg.maps)
            {
                if (!art.Has("boss." + m.mapId)) missing.Add("boss." + m.mapId);
                if (!art.Has("bg." + m.mapId)) missing.Add("bg." + m.mapId);
                foreach (var e in m.roster) if (!art.Has("enemy." + e.id)) missing.Add("enemy." + e.id);
            }
            CollectionAssert.IsEmpty(missing, "no art for: " + string.Join(", ", missing));
        }

        [Test]
        public void EverySkillHasAnEffectSprite()
        {
            foreach (var s in cfg.economy.skills) Assert.IsTrue(art.Has("vfx." + s.id), s.id);
        }

        [Test]
        public void UiChromeAndIconsExist()
        {
            foreach (var id in new[] { "ui.panel", "ui.frame", "ui.button", "ui.button_pressed", "ui.button_disabled", "icon.linh_thach", "icon.linh_khi", "icon.tinh_hoa" })
                Assert.IsTrue(art.Has(id), id);
            Assert.NotNull(UiSkin.From(art));
        }

        [Test]
        public void WalkCyclesHaveAtLeastTwoFrames()
        {
            foreach (var m in cfg.maps)
            {
                Assert.GreaterOrEqual(art.Frames("boss." + m.mapId).Length, 2);
                foreach (var e in m.roster) Assert.GreaterOrEqual(art.Frames("enemy." + e.id).Length, 2, e.id);
            }
        }

        [Test]
        public void NoEntryHasEmptySlots()
        {
            foreach (var e in art.entries)
            {
                Assert.IsNotEmpty(e.id);
                Assert.IsNotEmpty(e.frames, e.id);
                foreach (var s in e.frames) Assert.NotNull(s, e.id + " has a missing frame");
            }
            Assert.AreEqual(art.entries.Length, art.entries.Select(e => e.id).Distinct().Count(), "ids unique");
        }

        // ---- pixel-art quality rules ------------------------------------------------------------

        [Test]
        public void AllArtUsesPointFilteringNoMipmapsAndNoCompression()
        {
            foreach (var e in art.entries)
                foreach (var s in e.frames)
                {
                    var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(s));
                    Assert.AreEqual(FilterMode.Point, ti.filterMode, e.id);
                    Assert.IsFalse(ti.mipmapEnabled, e.id);
                    Assert.AreEqual(TextureImporterCompression.Uncompressed, ti.textureCompression, e.id);
                }
        }

        [Test]
        public void WorldArtSharesOnePixelDensity()
        {
            foreach (var e in art.entries.Where(x => !x.id.StartsWith("ui.") && !x.id.StartsWith("icon.")))
                foreach (var s in e.frames) Assert.AreEqual(32f, s.pixelsPerUnit, e.id);
        }

        [Test]
        public void UiSlicedSpritesHaveBorders()
        {
            foreach (var id in new[] { "ui.panel", "ui.frame", "ui.button", "ui.button_pressed", "ui.button_disabled" })
            {
                var b = art.First(id).border;
                Assert.Greater(b.x + b.y + b.z + b.w, 0f, id + " must be 9-sliceable");
            }
        }

        [Test]
        public void BackdropMatchesTheArenaSize()
        {
            foreach (var m in cfg.maps)
            {
                var size = art.First("bg." + m.mapId).bounds.size;
                Assert.AreEqual(cfg.arenaSize.x, size.x, 1e-3f, m.mapId);
                Assert.AreEqual(cfg.arenaSize.y, size.y, 1e-3f, m.mapId);
            }
        }

        [Test]
        public void FramesOfOneAnimationShareASize()
        {
            foreach (var e in art.entries)
            {
                var first = e.frames[0].rect.size;
                foreach (var s in e.frames) Assert.AreEqual(first, s.rect.size, e.id);
            }
        }

        // ---- the swap contract: missing art is never an error ---------------------------------------

        [Test]
        public void MissingIdsFallBackToNullInsteadOfThrowing()
        {
            Assert.IsNull(art.Frames("enemy.does_not_exist"));
            Assert.IsNull(art.First("nope"));
            Assert.IsFalse(art.Has(null));
            Assert.IsNull(art.Frames(null));
        }

        [Test]
        public void EmptyCatalogBehavesLikeNoArt()
        {
            var empty = ScriptableObject.CreateInstance<ArtCatalog>();
            try
            {
                Assert.IsNull(empty.Frames("player"));
                Assert.IsNull(UiSkin.From(empty), "no UI sprites -> flat placeholder UI");
                Assert.IsNull(UiSkin.From(null));
            }
            finally { Object.DestroyImmediate(empty); }
        }
    }
}
