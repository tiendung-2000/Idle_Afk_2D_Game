using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PixelImmortal.Data;
using PixelImmortal.Localization;
using PixelImmortal.UI;
using UnityEditor;

namespace PixelImmortal.Tests
{
    public class LocalizationTests
    {
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset";

        PrototypeConfig cfg;
        LocalizationService loc;

        [SetUp]
        public void SetUp()
        {
            cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(ConfigPath);
            Assert.NotNull(cfg.localizationCsv, "PrototypeConfig needs the localization CSV");
            loc = new LocalizationService(cfg.localizationCsv.text, LocalizationService.English);
        }

        // ---- CSV parser -----------------------------------------------------

        [Test]
        public void CsvHandlesQuotesCommasAndDoubledQuotes()
        {
            var rows = LocalizationService.ParseCsv("key,vi,en\nk1,\"a, b\",\"say \"\"hi\"\"\"\nk2,x,y");
            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("a, b", rows[1][1]);
            Assert.AreEqual("say \"hi\"", rows[1][2]);
            Assert.AreEqual("y", rows[2][2]);
        }

        [Test]
        public void EscapedNewlinesBecomeRealNewlines()
        {
            var l = new LocalizationService("key,vi,en\nk,dòng 1\\ndòng 2,line 1\\nline 2", "en");
            Assert.AreEqual("line 1\nline 2", l.Get("k"));
        }

        // ---- lookup ---------------------------------------------------------

        [Test]
        public void SwitchingLanguageChangesTextAndRaisesEvent()
        {
            int fired = 0;
            loc.LanguageChanged += () => fired++;
            Assert.AreEqual("Upgrades", loc.Get("btn.upgrades"));
            loc.SetLanguage("vi");
            Assert.AreEqual("Nâng cấp", loc.Get("btn.upgrades"));
            Assert.AreEqual(1, fired);
            loc.SetLanguage("vi");
            Assert.AreEqual(1, fired, "no event when nothing changed");
        }

        [Test]
        public void UnsupportedLanguageFallsBackToEnglish()
        {
            loc.SetLanguage("vi");
            loc.SetLanguage("xx");
            Assert.AreEqual("en", loc.Language);
        }

        [Test]
        public void MissingKeyReturnsMarkerNotException()
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("Missing localization key"));
            Assert.AreEqual("[no.such.key]", loc.Get("no.such.key"));
        }

        [Test]
        public void EmptyCellFallsBackToOtherLanguage()
        {
            var l = new LocalizationService("key,vi,en\nonly_en,,Hello", "vi");
            Assert.AreEqual("Hello", l.Get("only_en"));
        }

        [Test]
        public void BadFormatArgumentsDoNotThrow()
        {
            Assert.DoesNotThrow(() => loc.Format("hud.title", 1));      // too few args
            Assert.DoesNotThrow(() => loc.Format("btn.upgrades", 1, 2)); // too many args
        }

        // ---- table completeness (the Phase 5 gate) ------------------------------

        [Test]
        public void EveryKeyHasBothLanguages()
        {
            Assert.Greater(loc.Count, 100);
            var missing = loc.Keys.Where(k => string.IsNullOrWhiteSpace(loc.Get(k, "vi")) || string.IsNullOrWhiteSpace(loc.Get(k, "en"))
                                              || loc.Get(k, "vi").StartsWith("[") || loc.Get(k, "en").StartsWith("[")).ToList();
            CollectionAssert.IsEmpty(missing, "keys missing a language: " + string.Join(", ", missing));
        }

        [Test]
        public void PlaceholdersMatchBetweenLanguages()
        {
            var bad = new List<string>();
            foreach (var k in loc.Keys)
            {
                var vi = Placeholders(loc.Get(k, "vi"));
                var en = Placeholders(loc.Get(k, "en"));
                if (!vi.SetEquals(en)) bad.Add(k);
            }
            CollectionAssert.IsEmpty(bad, "placeholder mismatch: " + string.Join(", ", bad));
        }

        static HashSet<string> Placeholders(string s)
            => new HashSet<string>(Regex.Matches(s, @"\{(\d+)").Cast<Match>().Select(m => m.Groups[1].Value));

        [Test]
        public void EveryContentIdHasAName()
        {
            var eco = cfg.economy;
            var missing = new List<string>();
            void Need(string key) { if (!loc.Has(key)) missing.Add(key); }

            foreach (var m in cfg.maps)
            {
                Need("map." + m.mapId);
                Need("boss." + m.mapId);
                foreach (var e in m.roster) Need("enemy." + e.id);
            }
            foreach (var u in eco.upgrades) Need("upgrade." + u.id);
            foreach (var s in eco.skills) Need("skill." + s.id);
            foreach (var r in eco.realms) Need("realm." + r.id);
            foreach (var q in eco.dailyQuests) Need("quest." + q.id);
            foreach (QuestObjective o in Enum.GetValues(typeof(QuestObjective))) Need("quest.objective." + o);
            foreach (var i in eco.items) Need("item." + i.id);
            foreach (var p in eco.gachaPools) Need("gacha." + p.id);
            foreach (Rarity r in Enum.GetValues(typeof(Rarity))) Need("rarity." + r);
            foreach (EquipSlot s in Enum.GetValues(typeof(EquipSlot))) Need("slot." + s);
            foreach (StatType s in Enum.GetValues(typeof(StatType))) Need("stat." + s);

            CollectionAssert.IsEmpty(missing, "missing keys: " + string.Join(", ", missing));
        }

        [Test]
        public void FontCoversEveryCharacterInBothLanguages()
        {
            var font = UiKit.Font;
            var missing = new HashSet<char>();
            foreach (var k in loc.Keys)
                foreach (var lang in LocalizationService.Supported)
                    foreach (char c in loc.Get(k, lang))
                        if (c != '\n' && c != '\r' && !font.HasCharacter(c)) missing.Add(c);
            CollectionAssert.IsEmpty(missing, "font lacks: " + string.Join("", missing));
        }

        [Test]
        public void VietnameseTextKeepsItsDiacritics()
        {
            loc.SetLanguage("vi");
            StringAssert.Contains("Thạch", loc.Get("res.linh_thach"));
            Assert.AreEqual("Thanh Trúc Lâm", loc.Get("map.thanh_truc_lam"));
        }
    }
}
