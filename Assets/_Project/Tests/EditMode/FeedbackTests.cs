using System;
using System.Linq;
using NUnit.Framework;
using PixelImmortal.Audio;
using PixelImmortal.Combat;
using PixelImmortal.Data;
using PixelImmortal.Save;
using UnityEditor;
using UnityEngine;

namespace PixelImmortal.Tests
{
    public class FeedbackTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";

        // ---- procedural sound effects ------------------------------------------------------------

        static Sfx[] AllSfx => (Sfx[])Enum.GetValues(typeof(Sfx));

        [Test]
        public void EverySoundIsAudibleBoundedAndShort()
        {
            foreach (var sfx in AllSfx)
            {
                var s = ProceduralSfx.Samples(sfx);
                Assert.Greater(s.Length, ProceduralSfx.SampleRate / 50, sfx + " too short");
                Assert.Less(s.Length, ProceduralSfx.SampleRate * 1.5, sfx + " too long");
                double energy = 0;
                foreach (var v in s)
                {
                    Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v), sfx.ToString());
                    Assert.LessOrEqual(Math.Abs(v), 1f, sfx + " clips");
                    energy += v * v;
                }
                Assert.Greater(Math.Sqrt(energy / s.Length), 0.02, sfx + " is nearly silent");
            }
        }

        [Test]
        public void SoundsStartAndEndWithoutClicks()
        {
            foreach (var sfx in AllSfx)
            {
                var s = ProceduralSfx.Samples(sfx);
                Assert.Less(Math.Abs(s[0]), 0.08f, sfx + " pops at the start");
                int tail = Math.Max(1, s.Length / 100);
                for (int i = s.Length - tail; i < s.Length; i++) Assert.Less(Math.Abs(s[i]), 0.08f, sfx + " pops at the end");
            }
        }

        [Test]
        public void SoundsAreDeterministic()
        {
            foreach (var sfx in AllSfx) CollectionAssert.AreEqual(ProceduralSfx.Samples(sfx), ProceduralSfx.Samples(sfx), sfx.ToString());
        }

        [Test]
        public void ClipsCanBeCreatedForEverySound()
        {
            foreach (var sfx in AllSfx)
            {
                var clip = ProceduralSfx.CreateClip(sfx);
                try { Assert.AreEqual(ProceduralSfx.SampleRate, clip.frequency); Assert.Greater(clip.length, 0.02f); }
                finally { UnityEngine.Object.DestroyImmediate(clip); }
            }
        }

        [Test]
        public void MuteTogglesAndNotifiesOnce()
        {
            var go = new GameObject("audio_test");
            try
            {
                var audio = go.AddComponent<AudioService>();
                audio.Init(false);
                int fired = 0;
                audio.MutedChanged += () => fired++;
                audio.SetMuted(true);
                audio.SetMuted(true);
                Assert.IsTrue(audio.Muted);
                Assert.AreEqual(1, fired, "no event when nothing changed");
                Assert.DoesNotThrow(() => audio.Play(Sfx.Hit), "muted play is a no-op");
                audio.Toggle();
                Assert.IsFalse(audio.Muted);
                Assert.AreEqual(2, fired);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void PlayingBeforeInitIsSafe()
        {
            var go = new GameObject("audio_test2");
            try { Assert.DoesNotThrow(() => go.AddComponent<AudioService>().Play(Sfx.Click)); }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        // ---- damage-number glyphs ---------------------------------------------------------------------

        static int[] Glyphs(double v)
        {
            var buf = new int[6];
            int n = NumberGlyphs.Format(v, buf);
            return buf.Take(n).ToArray();
        }

        [TestCase(0, new[] { 0 })]
        [TestCase(7, new[] { 7 })]
        [TestCase(42, new[] { 4, 2 })]
        [TestCase(9999, new[] { 9, 9, 9, 9 })]
        [TestCase(10000, new[] { 1, 0, NumberGlyphs.K })]
        [TestCase(12345, new[] { 1, 2, NumberGlyphs.K })]
        [TestCase(999999, new[] { 9, 9, 9, NumberGlyphs.K })]
        [TestCase(1500000, new[] { 1, NumberGlyphs.M })]
        public void GlyphsFormatNumbersCompactly(double value, int[] expected) => CollectionAssert.AreEqual(expected, Glyphs(value));

        [Test]
        public void GlyphsHandleBadAndHugeValues()
        {
            CollectionAssert.AreEqual(new[] { 0 }, Glyphs(double.NaN));
            CollectionAssert.AreEqual(new[] { 0 }, Glyphs(-50));
            CollectionAssert.AreEqual(new[] { 9, 9, 9, NumberGlyphs.M }, Glyphs(1e15));
            CollectionAssert.AreEqual(new[] { 9, 9, 9, NumberGlyphs.M }, Glyphs(double.MaxValue));
            Assert.AreEqual(0, NumberGlyphs.Format(12345, new int[2]), "too small a buffer shows nothing instead of overflowing");
        }

        [Test]
        public void DamageNumbersPoolNeverOverflowsAndWorksWithoutArt()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            var go = new GameObject("numbers_test");
            try
            {
                var n = go.AddComponent<DamageNumbers>();
                n.Init(null);
                Assert.IsFalse(n.Enabled, "no art -> disabled, not an error");
                Assert.DoesNotThrow(() => n.Show(Vector3.zero, 10f, false, Color.white));

                var n2 = new GameObject("numbers_test2").AddComponent<DamageNumbers>();
                n2.Init(cfg.art);
                Assert.IsTrue(n2.Enabled, "catalog has digit sprites");
                Assert.DoesNotThrow(() => { for (int i = 0; i < 200; i++) n2.Show(Vector3.zero, i * 137f, i % 3 == 0, Color.white); });
                UnityEngine.Object.DestroyImmediate(n2.gameObject);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void DigitArtExistsInTheCatalog()
        {
            var art = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath).art;
            for (int i = 0; i < 10; i++) Assert.IsTrue(art.Has("digit." + i), "digit." + i);
            Assert.IsTrue(art.Has("digit.k"));
            Assert.IsTrue(art.Has("digit.m"));
        }

        // ---- settings persistence ----------------------------------------------------------------------

        [Test]
        public void MuteAndIntroFlagsSurviveASaveRoundTrip()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pi_fb_" + Guid.NewGuid().ToString("N"));
            try
            {
                var sys = new SaveSystem(dir);
                var d = new SaveData();
                d.settings.language = "vi"; d.settings.muted = true; d.settings.introSeen = true;
                Assert.IsTrue(sys.Save(d));
                var r = sys.Load().Data;
                Assert.IsTrue(r.settings.muted);
                Assert.IsTrue(r.settings.introSeen);
                Assert.AreEqual("vi", r.settings.language);
            }
            finally { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
        }

        [Test]
        public void OlderSavesWithoutTheNewFlagsDefaultToSoundOnAndHintNotSeen()
        {
            var s = JsonUtility.FromJson<SettingsData>("{\"language\":\"en\"}");
            Assert.IsFalse(s.muted);
            Assert.IsFalse(s.introSeen);
        }
    }
}
