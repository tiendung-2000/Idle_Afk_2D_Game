using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelImmortal.EditorTools
{
    /// <summary>
    /// Takes real (externally generated or hand-drawn) sprite strips from Assets/_Project/ArtIncoming and turns them into the
    /// per-frame PNGs the ArtCatalog expects, replacing the procedural placeholders.
    ///
    /// File name = kind + "_" + id, e.g. player.png, enemy_yeu_lang.png, elite_yeu_lang.png, boss_thanh_truc_lam.png.
    /// Each file is ONE horizontal strip of square frames (frame size = image height, frame count = width / height),
    /// frames in the order listed in Docs/ART_MANIFEST.md. A flat-colour background (no alpha) is keyed out using the top-left pixel.
    /// Frames are box-averaged down (or nearest-neighbour up) to the catalog size, with a hard alpha edge. Imported files are recorded in Art/final_art.txt so that
    /// "Generate Placeholder Pixel Art" never overwrites them. See Docs/ART_PROMPTS.md.
    /// </summary>
    public static class ExternalArtImporter
    {
        const string Incoming = "Assets/_Project/ArtIncoming";
        const string ArtRoot = "Assets/_Project/Art";
        const string FinalList = "Assets/_Project/Art/final_art.txt";
        const int KeyTolerance = 24;

        public static HashSet<string> LoadFinalList()
        {
            var set = new HashSet<string>();
            string path = Path.Combine(Application.dataPath, "..", FinalList);
            if (File.Exists(path))
                foreach (var line in File.ReadAllLines(path))
                    if (!string.IsNullOrWhiteSpace(line)) set.Add(line.Trim());
            return set;
        }

        /// <summary>Where frames of a kind go, and the pixel size of one frame. Returns false for an unknown kind.</summary>
        static bool Target(string kind, out string folder, out int size)
        {
            switch (kind)
            {
                case "player": folder = "Characters"; size = 28; return true;
                case "enemy": folder = "Enemies"; size = 24; return true;
                case "elite": folder = "Elites"; size = 32; return true;
                case "boss": folder = "Bosses"; size = 56; return true;
                default: folder = null; size = 0; return false;
            }
        }

        [MenuItem("PixelImmortal/Import External Art")]
        static void Menu() => Debug.Log(Import());

        public static string Import()
        {
            string dir = Path.Combine(Application.dataPath, "..", Incoming);
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); return "Created " + Incoming + "; drop sprite strips there (see Docs/ART_PROMPTS.md)."; }

            var final = LoadFinalList();
            var report = new List<string>();
            foreach (var file in Directory.GetFiles(dir, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                int us = name.IndexOf('_');
                string kind = us < 0 ? name : name.Substring(0, us);
                string id = us < 0 ? name : name.Substring(us + 1);
                if (!Target(kind, out string folder, out int size)) { report.Add("skipped " + name + " (unknown kind)"); continue; }

                var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!src.LoadImage(File.ReadAllBytes(file))) { report.Add("skipped " + name + " (not a readable PNG)"); Object.DestroyImmediate(src); continue; }
                int fh = src.height, count = src.width / fh;
                if (count < 1) { report.Add("skipped " + name + " (frames must be square; width >= height)"); Object.DestroyImmediate(src); continue; }
                bool key = !HasAlpha(src);
                var keyColor = src.GetPixel(0, src.height - 1);

                for (int f = 0; f < count; f++)
                {
                    var frame = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            frame.SetPixel(x, y, Sample(src, f * fh, fh, size, x, y, key, keyColor));
                        }
                    frame.Apply();
                    string rel = kind == "player" ? $"{ArtRoot}/{folder}/player_{f}.png" : $"{ArtRoot}/{folder}/{id}_{f}.png";
                    File.WriteAllBytes(Path.Combine(Application.dataPath, "..", rel), frame.EncodeToPNG());
                    final.Add(rel);
                    Object.DestroyImmediate(frame);
                }
                report.Add($"imported {name}: {count} frame(s) at {size}px" + (key ? " (background keyed out)" : ""));
                Object.DestroyImmediate(src);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", FinalList)));
            File.WriteAllLines(Path.Combine(Application.dataPath, "..", FinalList), new List<string>(final).ToArray());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PixelArtGenerator.Generate();   // rebuilds the catalog; files in final_art.txt are left untouched
            return report.Count == 0 ? "Nothing to import in " + Incoming : string.Join("\n", report);
        }

        /// <summary>One target pixel: box average (alpha-weighted) when the frame is shrunk, nearest neighbour when it is enlarged.</summary>
        static Color Sample(Texture2D src, int ox, int fh, int size, int x, int y, bool key, Color keyColor)
        {
            int x0 = x * fh / size, x1 = Mathf.Max(x0 + 1, (x + 1) * fh / size);
            int y0 = y * fh / size, y1 = Mathf.Max(y0 + 1, (y + 1) * fh / size);
            float r = 0, g = 0, b = 0, a = 0; int n = 0;
            for (int sy = y0; sy < y1; sy++)
                for (int sx = x0; sx < x1; sx++)
                {
                    var c = src.GetPixel(ox + sx, sy);
                    if (key && Close(c, keyColor)) c = Color.clear;
                    r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a; n++;
                }
            if (a <= 0f) return Color.clear;
            float alpha = a / n;
            return new Color(r / a, g / a, b / a, alpha < 0.5f ? 0f : 1f);   // hard alpha keeps pixel-art edges crisp
        }

        static bool HasAlpha(Texture2D t)
        {
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i += 7) if (px[i].a < 250) return true;
            return false;
        }

        static bool Close(Color a, Color b)
            => Mathf.Abs(a.r - b.r) * 255f < KeyTolerance && Mathf.Abs(a.g - b.g) * 255f < KeyTolerance && Mathf.Abs(a.b - b.b) * 255f < KeyTolerance;
    }
}
