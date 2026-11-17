using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace PixelImmortal.Localization
{
    /// <summary>
    /// Key -> text lookup for Vietnamese ("vi") and English ("en"), loaded from a CSV (key,vi,en).
    /// Gameplay code passes keys/arguments only; every visible string lives in the table.
    /// </summary>
    public class LocalizationService
    {
        public const string Vietnamese = "vi";
        public const string English = "en";
        public static readonly string[] Supported = { Vietnamese, English };

        readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>(); // key -> [vi, en]
        readonly HashSet<string> reportedMissing = new HashSet<string>();
        CultureInfo culture = CultureInfo.InvariantCulture;

        public string Language { get; private set; } = English;
        public event Action LanguageChanged;
        public IEnumerable<string> Keys => table.Keys;
        public int Count => table.Count;

        public LocalizationService(string csvText, string initialLanguage)
        {
            Load(csvText);
            SetLanguage(initialLanguage, false);
        }

        public static string DetectSystemLanguage()
            => Application.systemLanguage == SystemLanguage.Vietnamese ? Vietnamese : English;

        public static bool IsSupported(string code) => Array.IndexOf(Supported, code) >= 0;

        public void SetLanguage(string code, bool notify = true)
        {
            if (!IsSupported(code)) code = English;
            bool changed = code != Language;
            Language = code;
            culture = code == Vietnamese ? new CultureInfo("vi-VN") : new CultureInfo("en-US");
            if (changed && notify) LanguageChanged?.Invoke();
        }

        public void Toggle() => SetLanguage(Language == Vietnamese ? English : Vietnamese);

        public bool Has(string key) => table.ContainsKey(key);

        /// <summary>Text for the current language; falls back to the other language, then to "[key]" (reported once).</summary>
        public string Get(string key) => Get(key, Language);

        public string Get(string key, string language)
        {
            if (table.TryGetValue(key, out var row))
            {
                int i = language == Vietnamese ? 0 : 1;
                if (!string.IsNullOrEmpty(row[i])) return row[i];
                if (!string.IsNullOrEmpty(row[1 - i])) return row[1 - i];
            }
            if (reportedMissing.Add(key)) Debug.LogWarning("Missing localization key: " + key);
            return "[" + key + "]";
        }

        public string Format(string key, params object[] args)
        {
            string template = Get(key);
            try { return string.Format(culture, template, args); }
            catch (FormatException) { return template; }
        }


        // ---- CSV ----------------------------------------------------------------

        void Load(string csv)
        {
            table.Clear();
            if (string.IsNullOrEmpty(csv)) return;
            var rows = ParseCsv(csv);
            for (int r = 1; r < rows.Count; r++) // row 0 = header
            {
                var cells = rows[r];
                if (cells.Count < 1 || string.IsNullOrWhiteSpace(cells[0]) || cells[0].StartsWith("#")) continue;
                string vi = cells.Count > 1 ? Unescape(cells[1]) : "";
                string en = cells.Count > 2 ? Unescape(cells[2]) : "";
                table[cells[0].Trim()] = new[] { vi, en };
            }
        }

        static string Unescape(string s) => s.Replace("\\n", "\n");

        /// <summary>RFC-4180-style parser: quoted fields, doubled quotes, commas and newlines inside quotes.</summary>
        public static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                        else quoted = false;
                    }
                    else sb.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { row.Add(sb.ToString()); sb.Length = 0; }
                else if (c == '\r') { /* ignore */ }
                else if (c == '\n')
                {
                    row.Add(sb.ToString()); sb.Length = 0;
                    rows.Add(row); row = new List<string>();
                }
                else sb.Append(c);
            }
            if (sb.Length > 0 || row.Count > 0) { row.Add(sb.ToString()); rows.Add(row); }
            return rows;
        }
    }
}
