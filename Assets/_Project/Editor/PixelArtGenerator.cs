using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PixelImmortal.Data;

namespace PixelImmortal.EditorTools
{
    /// <summary>
    /// Generates PLACEHOLDER pixel art procedurally (no AI image tool is used): player, enemies, bosses, map
    /// backdrops, UI chrome, icons and skill effects, then builds the ArtCatalog that wires them in.
    /// It is deterministic, so running it again reproduces the same files. Real art replaces these PNGs
    /// (same names) or individual catalog entries; gameplay code does not change.
    /// </summary>
    public static class PixelArtGenerator
    {
        const string ArtRoot = "Assets/_Project/Art";

        // ===================================================================== canvas

        sealed class Pix
        {
            public readonly int w, h;
            public readonly Color32[] p;

            public Pix(int w, int h) { this.w = w; this.h = h; p = new Color32[w * h]; }

            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < w && y < h) p[y * w + x] = c; }
            public Color32 Get(int x, int y) => p[y * w + x];
            public bool Solid(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && p[y * w + x].a > 0;

            public void Rect(int x0, int y0, int x1, int y1, Color32 c)
            {
                for (int y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
                    for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++) Set(x, y, c);
            }

            public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
            {
                rx = Math.Max(0.5f, rx); ry = Math.Max(0.5f, ry);
                for (int y = (int)Math.Floor(cy - ry); y <= (int)Math.Ceiling(cy + ry); y++)
                    for (int x = (int)Math.Floor(cx - rx); x <= (int)Math.Ceiling(cx + rx); x++)
                    {
                        float dx = (x - cx) / rx, dy = (y - cy) / ry;
                        if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                    }
            }

            public void Line(int x0, int y0, int x1, int y1, Color32 c, int thick = 1)
            {
                int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx - dy;
                while (true)
                {
                    for (int a = 0; a < thick; a++) for (int b = 0; b < thick; b++) Set(x0 + a - thick / 2, y0 + b - thick / 2, c);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 > -dy) { err -= dy; x0 += sx; }
                    if (e2 < dx) { err += dx; y0 += sy; }
                }
            }

            /// <summary>One-pixel dark outline around the opaque shape (classic pixel-art silhouette).</summary>
            public void Outline(Color32 c)
            {
                var copy = (Color32[])p.Clone();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (copy[y * w + x].a > 0) continue;
                        bool near = (x > 0 && copy[y * w + x - 1].a > 0) || (x < w - 1 && copy[y * w + x + 1].a > 0)
                                    || (y > 0 && copy[(y - 1) * w + x].a > 0) || (y < h - 1 && copy[(y + 1) * w + x].a > 0);
                        if (near) p[y * w + x] = c;
                    }
            }

            /// <summary>Light from the top: lighten top edges, darken bottom/right edges.</summary>
            public void Shade()
            {
                var copy = (Color32[])p.Clone();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        var c = copy[y * w + x];
                        if (c.a == 0) continue;
                        bool topEdge = y == h - 1 || copy[(y + 1) * w + x].a == 0;
                        bool lowEdge = y == 0 || copy[(y - 1) * w + x].a == 0 || x == w - 1 || copy[y * w + x + 1].a == 0;
                        if (topEdge) p[y * w + x] = Mix(c, new Color32(255, 255, 255, c.a), 0.22f);
                        else if (lowEdge) p[y * w + x] = Mix(c, new Color32(0, 0, 0, c.a), 0.22f);
                    }
            }

            public void ShiftX(int dx)
            {
                var copy = (Color32[])p.Clone();
                Array.Clear(p, 0, p.Length);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int nx = x + dx;
                        if (nx >= 0 && nx < w) p[y * w + nx] = copy[y * w + x];
                    }
            }

            /// <summary>Squashes the picture vertically toward the bottom edge (collapsed death pose).</summary>
            public void SquashY(float k)
            {
                var copy = (Color32[])p.Clone();
                Array.Clear(p, 0, p.Length);
                for (int y = 0; y < h; y++)
                {
                    int src = Mathf.RoundToInt(y / k);
                    if (src >= h) continue;
                    for (int x = 0; x < w; x++) p[y * w + x] = copy[src * w + x];
                }
            }

            /// <summary>Soft glow painted only on empty pixels inside an ellipse centred on the canvas.</summary>
            public void Aura(Color32 c, float rx, float ry)
            {
                float cx = (w - 1) / 2f, cy = (h - 1) / 2f;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (p[y * w + x].a > 0) continue;
                        float dx = (x - cx) / rx, dy = (y - cy) / ry, d2 = dx * dx + dy * dy;
                        if (d2 <= 1f) p[y * w + x] = new Color32(c.r, c.g, c.b, (byte)(c.a * (1f - d2 * 0.7f)));
                    }
            }

            /// <summary>Blends every opaque pixel toward a colour (used for the white hit-flash frame).</summary>
            public void Tint(Color32 c, float t)
            {
                for (int i = 0; i < p.Length; i++)
                    if (p[i].a > 0) p[i] = Mix(p[i], new Color32(c.r, c.g, c.b, p[i].a), t);
            }

            public void FlipX()
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w / 2; x++)
                    {
                        var t = p[y * w + x]; p[y * w + x] = p[y * w + (w - 1 - x)]; p[y * w + (w - 1 - x)] = t;
                    }
            }

            public byte[] ToPng()
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.SetPixels32(p);
                tex.Apply();
                var bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);
                return bytes;
            }
        }

        /// <summary>Normalised drawing (0..1, y up) into a square canvas, optionally inset so decorations fit around a shape.</summary>
        sealed class Draw
        {
            public readonly Pix px;
            public readonly int n;
            readonly float ox, oy, s;
            readonly int e;

            public Draw(Pix px, float ox = 0f, float oy = 0f, float s = 1f)
            {
                this.px = px; n = px.w; this.ox = ox; this.oy = oy; this.s = s; e = Math.Max(1, n / 16);
            }

            int X(float f) => Mathf.RoundToInt((ox + f * s) * n);
            int Y(float f) => Mathf.RoundToInt((oy + f * s) * n);

            public void R(float x0, float y0, float x1, float y1, Color32 c) => px.Rect(X(x0), Y(y0), X(x1) - 1, Y(y1) - 1, c);
            public void E(float cx, float cy, float rx, float ry, Color32 c)
                => px.Ellipse((ox + cx * s) * n - 0.5f, (oy + cy * s) * n - 0.5f, rx * s * n, ry * s * n, c);
            public void L(float x0, float y0, float x1, float y1, Color32 c) => px.Line(X(x0), Y(y0), X(x1), Y(y1), c, e);
            public void Dot(float fx, float fy, Color32 c) => px.Rect(X(fx), Y(fy), X(fx) + e - 1, Y(fy) + e - 1, c);
            public void Clear(float x0, float y0, float x1, float y1) => R(x0, y0, x1, y1, new Color32(0, 0, 0, 0));
        }

        sealed class Pal
        {
            public Color32 main, dark, light, accent, eye;
            public Pal(Color32 main, Color32 dark, Color32 light, Color32 accent, Color32 eye) { this.main = main; this.dark = dark; this.light = light; this.accent = accent; this.eye = eye; }
        }

        static Color32 K(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);
        static Color32 Mix(Color32 a, Color32 b, float t)
            => new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), a.a);
        static readonly Color32 OutlineColor = K(20, 14, 26);

        // Frames of every monster and boss: 0/1 = walk cycle, 2 = attack, 3 = hit, 4 = death, 5 = skill wind-up (bosses only). Art faces right.
        const int AttackFrame = 2, HitFrame = 3, DeathFrame = 4, SkillFrame = 5;

        /// <summary>Attack: lunge forward with a white strike streak. Hit: recoil. Death: collapse. Skill: raised arms of energy. Call before Shade/Outline.</summary>
        static void Pose(Pix px, int f, Color32 accent)
        {
            if (f == AttackFrame)
            {
                px.ShiftX(Math.Max(1, px.w / 12));
                px.Line(px.w - px.w / 4, px.h * 2 / 3, px.w - 2, px.h / 3, K(255, 255, 255), Math.Max(1, px.w / 24));
            }
            else if (f == HitFrame) px.ShiftX(-Math.Max(1, px.w / 16));
            else if (f == DeathFrame) { px.SquashY(0.5f); px.ShiftX(-Math.Max(1, px.w / 24)); }
            else if (f == SkillFrame)
            {
                int t = Math.Max(1, px.w / 24);
                px.Line(px.w * 3 / 10, px.h / 2, px.w / 6, px.h - 3, accent, t);
                px.Line(px.w * 7 / 10, px.h / 2, px.w * 5 / 6, px.h - 3, accent, t);
            }
        }

        /// <summary>One monster or boss frame: body (drawn by <paramref name="body"/> with the walk frame to use), pose, shading, outline, optional aura.</summary>
        static Pix BuildFrame(int size, Action<Pix, int> body, int f, Color32 accent, Color32 aura, float auraAlpha)
        {
            var px = new Pix(size, size);
            body(px, f >= AttackFrame ? 0 : f);
            Pose(px, f, accent);
            px.Shade();
            if (f == HitFrame) px.Tint(K(255, 255, 255), 0.55f);
            if (f == DeathFrame) px.Tint(K(40, 36, 50), 0.5f);
            px.Outline(OutlineColor);
            if (auraAlpha > 0f && f != DeathFrame) px.Aura(new Color32(aura.r, aura.g, aura.b, (byte)(255 * auraAlpha)), size * 0.5f, size * 0.5f);
            return px;
        }

        // ===================================================================== monster bodies (frame 0/1 = walk cycle)

        static void Slime(Draw d, Pal p, int f)
        {
            float cy = f == 0 ? 0.36f : 0.33f, ry = f == 0 ? 0.30f : 0.25f, rx = f == 0 ? 0.38f : 0.43f;
            d.E(0.5f, cy - 0.04f, rx + 0.02f, ry, p.dark);
            d.E(0.5f, cy, rx, ry, p.main);
            d.Clear(0f, 0f, 1f, 0.12f);
            d.E(0.34f, cy + 0.12f, 0.08f, 0.06f, p.light);
            d.Dot(0.38f, cy + 0.04f, p.eye); d.Dot(0.60f, cy + 0.04f, p.eye);
        }

        static void Wolf(Draw d, Pal p, int f)
        {
            d.E(0.42f, 0.40f, 0.30f, 0.15f, p.main);
            d.E(0.30f, 0.46f, 0.14f, 0.06f, p.light);
            d.E(0.74f, 0.50f, 0.15f, 0.13f, p.main);
            d.R(0.84f, 0.42f, 0.98f, 0.50f, p.light);
            d.R(0.70f, 0.60f, 0.75f, 0.74f, p.dark); d.R(0.80f, 0.60f, 0.85f, 0.74f, p.dark);
            d.L(0.14f, 0.46f, 0.04f, 0.64f, p.dark);
            float[] xs = { 0.22f, 0.34f, 0.52f, 0.62f };
            for (int i = 0; i < 4; i++) { float lift = (i + f) % 2 == 0 ? 0.05f : 0f; d.R(xs[i], 0.10f + lift, xs[i] + 0.07f, 0.30f, p.dark); }
            d.Dot(0.78f, 0.54f, p.eye);
        }

        static void Insect(Draw d, Pal p, int f)
        {
            float[] xs = { 0.22f, 0.46f, 0.70f };
            float[] rs = { 0.13f, 0.14f, 0.11f };
            float wing = f == 0 ? 0.10f : 0.03f;
            d.E(0.46f, 0.64f, 0.20f, wing, new Color32(p.light.r, p.light.g, p.light.b, 210));
            for (int i = 0; i < 3; i++) d.E(xs[i], 0.40f, rs[i], rs[i] * 0.9f, i == 1 ? p.dark : p.main);
            d.E(0.86f, 0.42f, 0.10f, 0.10f, p.main);
            for (int i = 0; i < 3; i++) d.L(xs[i], 0.28f, xs[i] + (f == 0 ? -0.06f : 0.06f), 0.10f, p.dark);
            d.L(0.88f, 0.50f, 0.97f, 0.66f, p.dark);
            d.Dot(0.89f, 0.44f, p.eye);
        }

        static void Humanoid(Draw d, Pal p, int f)
        {
            float bob = f == 0 ? 0f : -0.03f;
            for (int k = 0; k < 4; k++) d.R(0.32f + k * 0.03f, 0.80f + bob + k * 0.04f, 0.68f - k * 0.03f, 0.84f + bob + k * 0.04f, p.accent);
            d.E(0.5f, 0.70f + bob, 0.16f, 0.14f, p.light);
            d.R(0.32f, 0.26f, 0.68f, 0.56f + bob, p.main);
            d.R(0.32f, 0.40f, 0.68f, 0.44f + bob, p.dark);
            float arm = f == 0 ? 0f : 0.04f;
            d.R(0.22f, 0.30f + arm, 0.30f, 0.54f + bob, p.main); d.R(0.70f, 0.30f - arm, 0.78f, 0.54f + bob, p.main);
            d.R(0.36f, 0.08f, 0.46f, f == 0 ? 0.26f : 0.22f, p.dark); d.R(0.54f, 0.08f, 0.64f, f == 0 ? 0.22f : 0.26f, p.dark);
            d.Dot(0.43f, 0.70f + bob, p.eye); d.Dot(0.57f, 0.70f + bob, p.eye);
        }

        static void Flame(Draw d, Pal p, int f)
        {
            float flick = f == 0 ? 0f : 0.06f;
            d.E(0.5f, 0.34f, 0.32f, 0.26f, p.dark);
            d.E(0.5f, 0.42f, 0.25f, 0.30f + flick * 0.5f, p.main);
            d.E(0.5f, 0.48f, 0.14f, 0.26f + flick, p.light);
            d.E(0.5f, 0.78f + flick, 0.07f, 0.10f, p.main);
            d.Dot(0.40f, 0.40f, p.eye); d.Dot(0.58f, 0.40f, p.eye);
        }

        static void Armored(Draw d, Pal p, int f)
        {
            d.R(0.16f, 0.24f, 0.84f, 0.60f, p.main);
            d.R(0.16f, 0.24f, 0.84f, 0.30f, p.dark);
            d.R(0.20f, 0.38f, 0.80f, 0.41f, p.dark); d.R(0.20f, 0.50f, 0.80f, 0.53f, p.dark);
            for (int i = 0; i < 4; i++) d.R(0.22f + i * 0.14f, 0.60f, 0.28f + i * 0.14f, 0.70f, p.light);
            d.R(0.70f, 0.36f, 0.96f, 0.56f, p.light);
            float[] xs = { 0.22f, 0.38f, 0.54f, 0.70f };
            for (int i = 0; i < 4; i++) { float lift = (i + f) % 2 == 0 ? 0.04f : 0f; d.R(xs[i], 0.08f + lift, xs[i] + 0.08f, 0.26f, p.dark); }
            d.Dot(0.86f, 0.48f, p.accent);
        }

        static void Fiend(Draw d, Pal p, int f)
        {
            d.R(0.30f, 0.14f, 0.70f, 0.58f, p.main);
            d.R(0.20f, 0.44f, 0.80f, 0.60f, p.dark);
            d.R(0.36f, 0.60f, 0.64f, 0.80f, p.main);
            d.R(0.32f, 0.78f, 0.38f, 0.94f, p.light); d.R(0.62f, 0.78f, 0.68f, 0.94f, p.light);
            d.Dot(0.43f, 0.70f, p.accent); d.Dot(0.57f, 0.70f, p.accent);
            d.R(0.44f, 0.63f, 0.56f, 0.65f, p.dark);
            d.R(0.14f, 0.20f + (f == 0 ? 0f : 0.05f), 0.24f, 0.52f, p.dark);
            d.R(0.76f, 0.20f + (f == 0 ? 0.05f : 0f), 0.86f, 0.52f, p.dark);
            d.R(0.34f, 0.04f, 0.46f, 0.16f, p.dark); d.R(0.54f, 0.04f, 0.66f, 0.16f, p.dark);
        }

        static void Bird(Draw d, Pal p, int f)
        {
            d.R(0.14f, 0.40f, 0.30f, 0.46f, p.dark);
            d.E(0.48f, 0.42f, 0.20f, 0.16f, p.main);
            d.E(0.48f, 0.36f, 0.14f, 0.08f, p.light);
            d.E(0.72f, 0.56f, 0.11f, 0.10f, p.main);
            d.R(0.80f, 0.52f, 0.94f, 0.57f, p.accent);
            if (f == 0) d.E(0.42f, 0.68f, 0.10f, 0.18f, p.light); else d.E(0.42f, 0.26f, 0.10f, 0.14f, p.light);
            d.L(0.42f, 0.28f, 0.40f, 0.14f, p.dark); d.L(0.54f, 0.28f, 0.56f, 0.14f, p.dark);
            d.Dot(0.75f, 0.58f, p.eye);
        }

        static void Ghost(Draw d, Pal p, int f)
        {
            d.E(0.5f, 0.58f, 0.30f, 0.30f, p.main);
            d.R(0.20f, 0.18f, 0.80f, 0.56f, p.main);
            int tooth = Math.Max(1, d.n / 8);
            for (int x = 0; x < d.n; x++)
            {
                bool low = ((x + f * 2) / tooth) % 2 == 0;
                float cut = low ? 0.18f : 0.26f;
                d.px.Rect(x, 0, x, Mathf.RoundToInt(cut * d.n) - 1, new Color32(0, 0, 0, 0));
            }
            d.E(0.24f, 0.38f, 0.06f, 0.10f, p.light); d.E(0.76f, 0.38f, 0.06f, 0.10f, p.light);
            d.E(0.5f, 0.60f, 0.16f, 0.16f, p.dark);
            d.Dot(0.42f, 0.60f, p.accent); d.Dot(0.56f, 0.60f, p.accent);
        }

        /// <summary>Hopping undead in an official's hat with stiff arms held out front and a paper talisman.</summary>
        static void Jiangshi(Draw d, Pal p, int f)
        {
            float hop = f == 0 ? 0f : 0.04f;
            d.R(0.34f, 0.86f + hop, 0.66f, 0.97f + hop, p.dark);
            d.R(0.30f, 0.80f + hop, 0.70f, 0.86f + hop, p.main);
            d.R(0.47f, 0.88f + hop, 0.53f, 0.96f + hop, K(240, 220, 90));
            d.R(0.38f, 0.62f + hop, 0.62f, 0.80f + hop, p.light);
            d.Dot(0.43f, 0.71f + hop, p.eye); d.Dot(0.56f, 0.71f + hop, p.eye);
            d.R(0.32f, 0.28f + hop, 0.68f, 0.62f + hop, p.main);
            d.R(0.32f, 0.40f + hop, 0.68f, 0.44f + hop, p.dark);
            d.R(0.66f, 0.52f + hop, 0.98f, 0.59f + hop, p.main); d.R(0.90f, 0.52f + hop, 0.98f, 0.59f + hop, p.light);
            d.R(0.66f, 0.42f + hop, 0.92f, 0.49f + hop, p.main); d.R(0.84f, 0.42f + hop, 0.92f, 0.49f + hop, p.light);
            d.R(0.38f, 0.08f, 0.47f, 0.28f + hop, p.dark); d.R(0.53f, 0.08f, 0.62f, 0.28f + hop, p.dark);
        }

        /// <summary>Signature gear for an elite: a gold crown and shoulder spikes (the guide wants one accessory plus a stronger aura).</summary>
        static void EliteGear(Pix px)
        {
            var gold = K(255, 208, 70);
            int minX = px.w, maxX = -1, maxY = -1;
            for (int y = 0; y < px.h; y++)
                for (int x = 0; x < px.w; x++)
                    if (px.Solid(x, y)) { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
            if (maxY < 0) return;

            // crown sits on the highest point of the body (the head, whatever the species), shoulder spikes at the sides of the bounding box
            long sx = 0, n = 0;
            for (int x = 0; x < px.w; x++) if (px.Solid(x, maxY) || px.Solid(x, maxY - 1)) { sx += x; n++; }
            int cx = (int)(sx / Math.Max(1, n)), top = Math.Min(px.h - 2, maxY);
            px.Rect(cx - 3, top, cx + 3, top + 1, gold);
            px.Rect(cx - 3, top + 2, cx - 3, top + 3 > px.h - 1 ? px.h - 1 : top + 3, gold);
            px.Rect(cx, top + 2, cx, Math.Min(px.h - 1, top + 4), gold);
            px.Rect(cx + 3, top + 2, cx + 3, Math.Min(px.h - 1, top + 3), gold);
            int midY = px.h * 2 / 5;
            px.Rect(Math.Max(0, minX - 2), midY, Math.Max(0, minX - 1), midY + 2, gold);
            px.Rect(Math.Min(px.w - 1, maxX + 1), midY, Math.Min(px.w - 1, maxX + 2), midY + 2, gold);
        }

        sealed class Spec
        {
            public readonly Action<Draw, Pal, int> paint;
            public readonly Pal pal;
            public Spec(Action<Draw, Pal, int> paint, Pal pal) { this.paint = paint; this.pal = pal; }
        }

        static readonly Dictionary<string, Spec> Enemies = new Dictionary<string, Spec>
        {
            { "linh_slime", new Spec(Slime, new Pal(K(110, 217, 110), K(51, 128, 64), K(204, 255, 204), K(255, 255, 255), K(20, 20, 30))) },
            { "yeu_lang", new Spec(Wolf, new Pal(K(64, 60, 76), K(30, 28, 42), K(120, 110, 142), K(255, 255, 255), K(196, 110, 255))) },   // Hắc Lang: charcoal, violet eyes
            { "yeu_trung", new Spec(Insect, new Pal(K(214, 150, 48), K(88, 52, 28), K(255, 214, 120), K(255, 255, 255), K(30, 14, 10))) },   // Ma Côn: amber, dark brown
            { "tinh_linh_rung", new Spec(Humanoid, new Pal(K(77, 191, 166), K(38, 102, 89), K(217, 255, 230), K(90, 200, 90), K(20, 40, 30))) },
            { "hoa_linh", new Spec(Flame, new Pal(K(255, 128, 38), K(179, 51, 26), K(255, 230, 102), K(255, 255, 255), K(40, 10, 10))) },
            { "thach_giap_thu", new Spec(Armored, new Pal(K(100, 98, 108), K(56, 40, 48), K(156, 150, 164), K(214, 44, 44), K(20, 20, 30))) },   // Huyết Thiết: iron, dark red
            { "quy_nham", new Spec(Fiend, new Pal(K(168, 32, 52), K(34, 12, 22), K(226, 84, 90), K(255, 230, 80), K(20, 20, 30))) },   // crimson and black
            { "hoa_dieu", new Spec(Bird, new Pal(K(255, 191, 51), K(204, 102, 26), K(255, 242, 128), K(230, 60, 40), K(30, 10, 10))) },
            { "bang_lang", new Spec(Wolf, new Pal(K(179, 224, 255), K(102, 140, 204), K(242, 255, 255), K(255, 255, 255), K(60, 200, 255))) },
            { "tuyet_yeu", new Spec(Jiangshi, new Pal(K(96, 120, 176), K(48, 62, 110), K(214, 226, 230), K(120, 190, 255), K(30, 40, 90))) },   // Cương Thi: muted blue, pale skin
            { "bang_giap_thu", new Spec(Armored, new Pal(K(115, 166, 217), K(64, 102, 153), K(204, 235, 255), K(255, 255, 255), K(20, 30, 60))) },
            { "han_hon", new Spec(Ghost, new Pal(K(150, 220, 240), K(52, 124, 170), K(226, 250, 255), K(80, 230, 255), K(10, 40, 70))) },   // Ngư Hồn: ice blue, cyan
        };

        // ===================================================================== bosses (big bodies + crown/aura)

        static void DrawBoss(Pix px, string mapId, int f)
        {
            var big = new Draw(px, 0.07f, 0.04f, 0.86f);
            if (mapId == "thanh_truc_lam")
            {
                var pal = new Pal(K(46, 170, 170), K(20, 90, 110), K(190, 255, 245), K(120, 230, 220), K(255, 240, 120));   // Thanh Long: cyan, turquoise
                var full = new Draw(px);
                for (int i = 0; i < 3; i++) { full.R(0.04f + i * 0.045f, 0.06f, 0.075f + i * 0.045f, 0.96f - i * 0.05f, K(60, 150, 150)); full.R(0.96f - i * 0.045f - 0.035f, 0.06f, 0.96f - i * 0.045f, 0.96f - i * 0.05f, K(60, 150, 150)); }
                Humanoid(big, pal, f);
                for (int k = 0; k < 5; k++) full.R(0.28f + k * 0.11f, 0.86f - Math.Abs(2 - k) * 0.02f, 0.33f + k * 0.11f, 0.98f - Math.Abs(2 - k) * 0.03f, pal.accent);
                full.Dot(0.42f, 0.66f, pal.eye); full.Dot(0.56f, 0.66f, pal.eye);
            }
            else if (mapId == "hoa_son_coc")
            {
                var pal = new Pal(K(64, 16, 30), K(22, 8, 16), K(210, 56, 60), K(255, 220, 60), K(255, 70, 60));   // Tử Ma Đế: black, crimson, gold crown
                var full = new Draw(px);
                for (int i = 0; i < 7; i++) full.E(0.10f + i * 0.13f, 0.18f + (i % 2) * 0.10f, 0.08f, 0.16f + (i % 3) * 0.05f, new Color32(255, (byte)(110 + (i % 3) * 40), 30, 200));
                Fiend(big, pal, f);
                for (int k = 0; k < 3; k++) full.R(0.30f + k * 0.20f, 0.86f, 0.35f + k * 0.20f, 0.98f - (k == 1 ? 0f : 0.04f), K(255, 200, 60));
            }
            else
            {
                var pal = new Pal(K(160, 200, 255), K(60, 90, 160), K(240, 250, 255), K(255, 255, 255), K(40, 80, 255));
                var full = new Draw(px);
                for (int i = 0; i < 6; i++) { float x = 0.08f + i * 0.17f; full.R(x, 0.04f, x + 0.05f, 0.20f + (i % 2) * 0.12f, K(190, 230, 255)); }
                Ghost(big, pal, f);
                for (int k = 0; k < 5; k++) full.R(0.26f + k * 0.12f, 0.79f, 0.30f + k * 0.12f, 0.88f + (k == 2 ? 0.05f : 0f), K(235, 248, 255));
                big.Dot(0.42f, 0.60f, K(120, 190, 255)); big.Dot(0.58f, 0.60f, K(120, 190, 255));
            }
        }

        // ===================================================================== player (28 px, faces right)

        static void DrawPlayer(Pix px, int f)
        {
            var d = new Draw(px);
            var skin = K(255, 217, 179); var hair = K(38, 30, 51); var robe = K(64, 176, 204); var robeDark = K(26, 48, 112);
            var sash = K(120, 214, 176); var boot = K(51, 38, 51); var steel = K(220, 225, 235); var gold = K(255, 200, 60);

            if (f == 3)
            {
                d.E(0.5f, 0.20f, 0.38f, 0.12f, robe);
                d.E(0.5f, 0.17f, 0.34f, 0.07f, robeDark);
                d.E(0.16f, 0.24f, 0.11f, 0.10f, skin);
                d.E(0.14f, 0.28f, 0.12f, 0.06f, hair);
                d.L(0.62f, 0.10f, 0.96f, 0.10f, steel);
                d.Dot(0.58f, 0.10f, gold);
                return;
            }

            float bob = f == 1 ? -0.035f : 0f;
            d.R(0.20f, 0.34f + bob, 0.29f, 0.58f + bob, robeDark);
            d.E(0.45f, 0.24f + bob, 0.22f, 0.10f, robe);
            d.R(0.28f, 0.24f + bob, 0.62f, 0.62f + bob, robe);
            d.R(0.28f, 0.40f + bob, 0.62f, 0.45f + bob, sash);
            d.R(0.22f, 0.18f + bob, 0.29f, 0.42f + bob, sash);       // sash tail
            d.R(0.40f, 0.56f + bob, 0.50f, 0.62f + bob, sash);       // layered collar
            d.R(0.34f, 0.06f, 0.43f, 0.22f, boot); d.R(0.48f, 0.06f, 0.57f, 0.22f, boot);
            d.R(0.20f, 0.46f + bob, 0.30f, 0.86f + bob, hair);   // long hair down the back
            d.E(0.45f, 0.78f + bob, 0.15f, 0.14f, skin);
            d.E(0.45f, 0.85f + bob, 0.16f, 0.09f, hair);
            d.Dot(0.52f, 0.77f + bob, hair);
            if (f == 2)
            {
                d.R(0.62f, 0.48f, 0.80f, 0.57f, skin);
                d.L(0.78f, 0.53f, 0.99f, 0.53f, steel);
                d.Dot(0.76f, 0.53f, gold);
                d.L(0.86f, 0.80f, 0.97f, 0.50f, K(255, 255, 255));
                d.L(0.80f, 0.86f, 0.92f, 0.56f, K(120, 230, 255));   // cyan sword-energy trail
            }
            else
            {
                d.R(0.62f, 0.38f + bob, 0.72f, 0.47f + bob, skin);
                d.L(0.70f, 0.42f + bob, 0.88f, 0.66f + bob, steel);
                d.Dot(0.69f, 0.42f + bob, gold);
                d.Dot(0.88f, 0.66f + bob, K(120, 230, 255));         // glowing blade tip
            }
        }

        // ===================================================================== environments (256 x 320, 8 x 10 world units)

        static Pix DrawBackground(string mapId, int seed)
        {
            const int W = 256, H = 320;
            var px = new Pix(W, H);
            var rng = new System.Random(seed);
            Color32[] tones; Color32 speck;
            int kind = mapId == "thanh_truc_lam" ? 0 : (mapId == "hoa_son_coc" ? 1 : 2);
            if (kind == 0) { tones = new[] { K(31, 77, 51), K(36, 87, 56), K(27, 68, 46) }; speck = K(70, 130, 80); }
            else if (kind == 1) { tones = new[] { K(66, 30, 25), K(77, 36, 28), K(56, 25, 22) }; speck = K(110, 60, 45); }
            else { tones = new[] { K(46, 77, 105), K(54, 88, 117), K(40, 68, 94) }; speck = K(150, 195, 230); }

            for (int ty = 0; ty < H / 16; ty++)
                for (int tx = 0; tx < W / 16; tx++)
                {
                    var tone = tones[rng.Next(tones.Length)];
                    px.Rect(tx * 16, ty * 16, tx * 16 + 15, ty * 16 + 15, tone);
                    for (int i = 0; i < 7; i++) px.Set(tx * 16 + rng.Next(16), ty * 16 + rng.Next(16), Mix(tone, speck, 0.45f));
                    if (kind != 0) px.Rect(tx * 16, ty * 16, tx * 16 + 15, ty * 16, Mix(tone, K(0, 0, 0), 0.18f)); // faint tile seams
                }

            bool Outer(int x, int y) => x < 64 || x > W - 65 || y < 64 || y > H - 65;

            if (kind == 0)
            {
                for (int side = 0; side < 2; side++)
                    for (int i = 0; i < 4; i++)
                    {
                        int x = side == 0 ? 3 + i * 10 : W - 8 - i * 10;
                        var stalk = K(60 + i * 6, 150 + i * 8, 80);
                        px.Rect(x, 0, x + 3, H - 1, stalk);
                        px.Rect(x, 0, x, H - 1, Mix(stalk, K(255, 255, 255), 0.25f));
                        for (int y = 14 + rng.Next(10); y < H; y += 40 + rng.Next(25)) px.Rect(x - 1, y, x + 4, y + 1, Mix(stalk, K(0, 0, 0), 0.4f));
                    }
                for (int i = 0; i < 12; i++) { int mx = rng.Next(W), my = rng.Next(H); if (Outer(mx, my)) px.Ellipse(mx, my, 6 + rng.Next(6), 3 + rng.Next(3), K(24, 60, 40)); }
            }
            else if (kind == 1)
            {
                for (int c = 0; c < 16; c++)
                {
                    int x, y; do { x = rng.Next(W); y = rng.Next(H); } while (!Outer(x, y));
                    for (int s = 0; s < 14; s++)
                    {
                        int nx = x + rng.Next(-6, 7), ny = y + rng.Next(-6, 7);
                        px.Line(x, y, nx, ny, K(255, 120 + rng.Next(80), 30), 1);
                        px.Set(x + 1, y, K(120, 30, 20));
                        x = nx; y = ny;
                    }
                }
                for (int i = 0; i < 40; i++) px.Set(rng.Next(W), rng.Next(H), K(255, 190, 80));
            }
            else
            {
                for (int c = 0; c < 14; c++)
                {
                    int x, y; do { x = rng.Next(W); y = rng.Next(H); } while (!Outer(x, y));
                    int hgt = 10 + rng.Next(14);
                    for (int k = 0; k < hgt; k++) px.Rect(x - (hgt - k) / 4, y + k, x + (hgt - k) / 4, y + k, k > hgt - 4 ? K(240, 252, 255) : K(150, 200, 240));
                }
                for (int i = 0; i < 70; i++) px.Set(rng.Next(W), rng.Next(H), K(255, 255, 255));
            }

            // vignette keeps the centre readable for sprites
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x - W / 2f) / (W / 2f), dy = (y - H / 2f) / (H / 2f);
                    float v = Mathf.Clamp01((dx * dx + dy * dy - 0.35f) * 0.5f);
                    px.p[y * W + x] = Mix(px.p[y * W + x], K(0, 0, 0), v * 0.5f);
                }
            return px;
        }

        // ===================================================================== UI chrome and icons (UI art is 16 px per unit)

        static Pix UiPanel()
        {
            var px = new Pix(24, 24);
            px.Rect(0, 0, 23, 23, K(60, 60, 60));
            px.Rect(1, 1, 22, 22, K(210, 210, 210));
            px.Rect(1, 22, 22, 22, K(255, 255, 255)); px.Rect(1, 1, 1, 22, K(255, 255, 255));
            px.Rect(1, 1, 22, 1, K(150, 150, 150)); px.Rect(22, 1, 22, 22, K(150, 150, 150));
            foreach (var (x, y) in new[] { (0, 0), (23, 0), (0, 23), (23, 23) }) px.Set(x, y, K(0, 0, 0, 0));
            return px;
        }

        static Pix UiFrame()
        {
            var px = new Pix(24, 24);
            Color32 dark = K(40, 26, 12), gold = K(235, 185, 70), hi = K(255, 235, 160);
            for (int i = 0; i < 24; i++)
            {
                foreach (var (x, y) in new[] { (i, 0), (i, 23), (0, i), (23, i) }) px.Set(x, y, dark);
                foreach (var (x, y) in new[] { (i, 1), (i, 22), (1, i), (22, i) }) px.Set(x, y, gold);
                foreach (var (x, y) in new[] { (i, 2), (2, i) }) px.Set(x, y, hi);
                foreach (var (x, y) in new[] { (i, 21), (21, i) }) px.Set(x, y, Mix(gold, dark, 0.5f));
            }
            foreach (var (cx, cy) in new[] { (0, 0), (20, 0), (0, 20), (20, 20) })
            {
                px.Rect(cx, cy, cx + 3, cy + 3, dark);
                px.Rect(cx + 1, cy + 1, cx + 2, cy + 2, hi);
            }
            return px;
        }

        static Pix UiButton(int state)   // 0 normal, 1 pressed, 2 disabled
        {
            var px = new Pix(16, 16);
            float fill = state == 0 ? 0.92f : (state == 1 ? 0.72f : 0.48f);
            px.Rect(0, 0, 15, 15, K(40, 40, 40));
            px.Rect(1, 1, 14, 14, Gray(fill));
            if (state != 2)
            {
                var hi = Gray(state == 0 ? 1f : 0.5f); var lo = Gray(state == 0 ? 0.55f : 0.9f);
                px.Rect(1, 13, 14, 14, hi); px.Rect(1, 1, 2, 14, hi);
                px.Rect(1, 1, 14, 2, lo); px.Rect(13, 1, 14, 14, lo);
            }
            foreach (var (x, y) in new[] { (0, 0), (15, 0), (0, 15), (15, 15) }) px.Set(x, y, K(0, 0, 0, 0));
            return px;
        }

        static Color32 Gray(float v) => K((int)(v * 255), (int)(v * 255), (int)(v * 255));

        static Pix IconGem()
        {
            var px = new Pix(16, 16);
            for (int y = 0; y < 14; y++) { int half = y < 7 ? 2 + y : 14 - y; px.Rect(8 - half, 14 - y, 7 + half, 14 - y, y < 4 ? K(255, 240, 150) : (y < 8 ? K(255, 205, 70) : K(220, 150, 40))); }
            px.Rect(7, 12, 8, 13, K(255, 255, 220));
            px.Outline(OutlineColor);
            return px;
        }

        static Pix IconQi()
        {
            var px = new Pix(16, 16);
            px.Ellipse(7.5f, 7.5f, 6f, 6f, K(70, 200, 255));
            px.Ellipse(7.5f, 7.5f, 4f, 4f, K(20, 80, 150));
            px.Ellipse(9f, 9f, 3f, 3f, K(150, 235, 255));
            px.Ellipse(8f, 8f, 1.2f, 1.2f, K(20, 80, 150));
            px.Outline(OutlineColor);
            return px;
        }

        static Pix IconEssence()
        {
            var px = new Pix(16, 16);
            px.Ellipse(7.5f, 5.5f, 4.5f, 4.5f, K(255, 120, 220));
            for (int y = 0; y < 7; y++) px.Rect(7 - (6 - y) / 2, 8 + y, 8 + (6 - y) / 2, 8 + y, K(255, 120, 220));
            px.Ellipse(6f, 7f, 1.3f, 1.8f, K(255, 220, 245));
            px.Outline(OutlineColor);
            return px;
        }

        // ===================================================================== skill effects (near-white so SkillDefinition.vfxColor tints them)

        static Pix VfxSlash()
        {
            var px = new Pix(32, 32);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float dx = x - 15.5f, dy = y - 8f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (a > 20f && a < 160f && r > 11f && r < 15f - (Mathf.Abs(a - 90f) / 70f) * 3f)
                        px.Set(x, y, r > 13f ? K(255, 255, 255) : K(255, 240, 190));
                }
            return px;
        }

        static Pix VfxOrb()
        {
            var px = new Pix(16, 16);
            px.Ellipse(7.5f, 7.5f, 7f, 7f, K(255, 180, 110));
            px.Ellipse(7.5f, 7.5f, 5f, 5f, K(255, 225, 160));
            px.Ellipse(7f, 8f, 2.5f, 2.5f, K(255, 255, 240));
            return px;
        }

        static Pix VfxRing(int size, bool shards)
        {
            var px = new Pix(size, size);
            float c = size / 2f - 0.5f, rOut = size * 0.44f, rIn = size * 0.36f;
            px.Ellipse(c, c, rIn, rIn, new Color32(210, 240, 255, (byte)(shards ? 70 : 50)));
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    if (r >= rIn && r <= rOut) px.Set(x, y, shards ? K(225, 248, 255) : new Color32(225, 248, 255, 230));
                }
            if (shards)
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    px.Line(Mathf.RoundToInt(c + Mathf.Cos(a) * rOut), Mathf.RoundToInt(c + Mathf.Sin(a) * rOut),
                            Mathf.RoundToInt(c + Mathf.Cos(a) * (size * 0.5f - 1f)), Mathf.RoundToInt(c + Mathf.Sin(a) * (size * 0.5f - 1f)), K(255, 255, 255), 2);
                }
            else px.Line(Mathf.RoundToInt(c - rIn * 0.6f), Mathf.RoundToInt(c + rIn * 0.6f), Mathf.RoundToInt(c - rIn * 0.2f), Mathf.RoundToInt(c + rIn * 0.85f), K(255, 255, 255), 2);
            return px;
        }

        static Pix VfxBlades()
        {
            var px = new Pix(64, 64);
            float c = 31.5f;
            px.Ellipse(c, c, 9f, 9f, new Color32(255, 255, 255, 90));
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                px.Line(Mathf.RoundToInt(c + Mathf.Cos(a) * 8f), Mathf.RoundToInt(c + Mathf.Sin(a) * 8f),
                        Mathf.RoundToInt(c + Mathf.Cos(a) * 30f), Mathf.RoundToInt(c + Mathf.Sin(a) * 30f), i % 2 == 0 ? K(255, 255, 255) : K(215, 235, 255), 2);
            }
            px.Ellipse(c, c, 4f, 4f, K(255, 255, 255));
            return px;
        }

        /// <summary>Reads a PNG from the project into a Pix (null when missing).</summary>
        static Pix LoadPix(string rel)
        {
            string abs = Abs(rel);
            if (!File.Exists(abs)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(abs))) { UnityEngine.Object.DestroyImmediate(tex); return null; }
            var px = new Pix(tex.width, tex.height);
            Array.Copy(tex.GetPixels32(), px.p, px.p.Length);
            UnityEngine.Object.DestroyImmediate(tex);
            return px;
        }

        static Color32 BossAccent(string mapId)
            => mapId == "thanh_truc_lam" ? K(120, 230, 220) : mapId == "hoa_son_coc" ? K(255, 200, 60) : K(200, 235, 255);

        static Pix FxHit()
        {
            var px = new Pix(16, 16);
            var w = K(255, 255, 255);
            px.Line(8, 1, 8, 14, w, 2); px.Line(1, 8, 14, 8, w, 2);
            px.Line(3, 3, 12, 12, w); px.Line(3, 12, 12, 3, w);
            px.Ellipse(7.5f, 7.5f, 2f, 2f, K(255, 240, 200));
            return px;
        }

        static Pix FxPuff()
        {
            var px = new Pix(32, 32);
            var w = new Color32(255, 255, 255, 230);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f + 0.2f, r = i % 2 == 0 ? 11f : 8f;
                px.Ellipse(15.5f + Mathf.Cos(a) * r, 15.5f + Mathf.Sin(a) * r, 4f, 4f, w);
            }
            px.Ellipse(15.5f, 15.5f, 6f, 6f, new Color32(255, 255, 255, 160));
            return px;
        }

        /// <summary>Hollow ring; <paramref name="ticks"/> adds four inward ticks (the boss warning marker).</summary>
        static Pix FxRing(int size, int thick, bool ticks)
        {
            var px = new Pix(size, size);
            float c = size / 2f - 0.5f, rOut = size * 0.48f, rIn = rOut - thick;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    if (r >= rIn && r <= rOut) px.Set(x, y, K(255, 255, 255));
                }
            if (ticks)
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI / 2f + Mathf.PI / 4f;
                    px.Line(Mathf.RoundToInt(c + Mathf.Cos(a) * rIn), Mathf.RoundToInt(c + Mathf.Sin(a) * rIn),
                            Mathf.RoundToInt(c + Mathf.Cos(a) * rIn * 0.7f), Mathf.RoundToInt(c + Mathf.Sin(a) * rIn * 0.7f), K(255, 255, 255), 2);
                }
            return px;
        }

        /// <summary>64 px skill icon: dark tile with a gold border and the (near-white) effect sprite tinted by the skill colour.</summary>
        static Pix SkillIcon(Pix src, Color tint)
        {
            const int N = 64;
            var px = new Pix(N, N);
            for (int y = 2; y < N - 2; y++)
                for (int x = 2; x < N - 2; x++)
                {
                    bool corner = (x < 5 || x > N - 6) && (y < 5 || y > N - 6) && (Mathf.Abs(x - N / 2) + Mathf.Abs(y - N / 2) > 58);
                    if (corner) continue;
                    bool edge = x < 4 || y < 4 || x > N - 5 || y > N - 5;
                    px.Set(x, y, edge ? K(214, 168, 64) : Mix(K(30, 24, 48), K(66, 52, 96), y / (float)N));
                }
            // enlarge small effects by whole factors, shrink big ones (real art) to fit the tile
            int big = Math.Max(src.w, src.h);
            int sc = Math.Max(1, 44 / big);
            int dw = big > 50 ? src.w * 50 / big : src.w * sc, dh = big > 50 ? src.h * 50 / big : src.h * sc;
            int ox = (N - dw) / 2, oy = (N - dh) / 2;
            var t = (Color32)tint;
            for (int y = 0; y < dh; y++)
                for (int x = 0; x < dw; x++)
                {
                    var c = src.Get(Math.Min(src.w - 1, x * src.w / dw), Math.Min(src.h - 1, y * src.h / dh));
                    if (c.a == 0) continue;
                    var col = new Color32((byte)(c.r * t.r / 255), (byte)(c.g * t.g / 255), (byte)(c.b * t.b / 255), 255);
                    px.Set(ox + x, oy + y, c.a == 255 || px.Get(ox + x, oy + y).a == 0 ? col : Mix(px.Get(ox + x, oy + y), col, c.a / 255f));
                }
            return px;
        }

        // ===================================================================== damage-number font (5 x 7, white so it can be tinted)

        static readonly Dictionary<string, string[]> Glyphs = new Dictionary<string, string[]>
        {
            { "0", new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" } },
            { "1", new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" } },
            { "2", new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" } },
            { "3", new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" } },
            { "4", new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" } },
            { "5", new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" } },
            { "6", new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" } },
            { "7", new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" } },
            { "8", new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" } },
            { "9", new[] { "01110", "10001", "10001", "01111", "00001", "00010", "01100" } },
            { "k", new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" } },
            { "m", new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" } },
        };

        static Pix Glyph(string[] rows)
        {
            var px = new Pix(7, 9);                       // 1 px margin for the outline
            for (int r = 0; r < 7; r++)
                for (int c = 0; c < 5; c++)
                    if (rows[r][c] == '1') px.Set(c + 1, 7 - r, K(255, 255, 255));
            px.Outline(OutlineColor);
            return px;
        }

        // ===================================================================== driver

        static string Abs(string rel) => Path.Combine(Application.dataPath, "..", rel).Replace('\\', '/');

        /// <summary>Files replaced by real art (listed by <see cref="ExternalArtImporter"/>); regenerating placeholders must not overwrite them.</summary>
        static HashSet<string> finalArt = new HashSet<string>();

        static void Write(Pix px, string rel, List<string> written)
        {
            written.Add(rel);
            if (finalArt.Contains(rel)) return;
            string abs = Abs(rel);
            Directory.CreateDirectory(Path.GetDirectoryName(abs));
            File.WriteAllBytes(abs, px.ToPng());
        }

        /// <summary>Generates all art, imports it and builds the catalog. Optionally writes an enlarged contact sheet PNG for review.</summary>
        public static string Generate(string previewPath = null)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<PrototypeConfig>("Assets/_Project/ScriptableObjects/Economy/PrototypeConfig.asset");
            var written = new List<string>();
            var sheet = new List<Pix>();
            finalArt = ExternalArtImporter.LoadFinalList();

            // player
            // frames: idle, idle, attack, dead, hit
            for (int f = 0; f < 5; f++)
            {
                var px = new Pix(28, 28);
                DrawPlayer(px, f == 4 ? 0 : f);
                if (f == 4) { px.ShiftX(-2); px.Tint(K(255, 255, 255), 0.55f); }
                px.Outline(OutlineColor);
                Write(px, $"{ArtRoot}/Characters/player_{f}.png", written); sheet.Add(px);
            }

            // enemies: walk, walk, attack, hit, death
            foreach (var kv in Enemies)
            {
                var spec = kv.Value;
                for (int f = 0; f <= DeathFrame; f++)
                {
                    int ff = f;
                    var px = BuildFrame(24, (p, wf) => spec.paint(new Draw(p, 0.04f, 0.04f, ff >= AttackFrame ? 0.88f : 0.92f), spec.pal, wf), f, spec.pal.light, spec.pal.light, 0f);
                    Write(px, $"{ArtRoot}/Enemies/{kv.Key}_{f}.png", written); sheet.Add(px);
                }

                // elite: own 32 px sprite (finer pixels than a scaled-up monster) with a gold crown, shoulder spikes and aura
                for (int f = 0; f <= DeathFrame; f++)
                {
                    int ff = f;
                    var px = BuildFrame(32, (p, wf) =>
                    {
                        spec.paint(new Draw(p, 0.10f, 0.06f, ff >= AttackFrame ? 0.74f : 0.78f), spec.pal, wf);
                        EliteGear(p);
                    }, f, K(255, 208, 70), K(255, 208, 70), 0.38f);
                    Write(px, $"{ArtRoot}/Elites/{kv.Key}_{f}.png", written); sheet.Add(px);
                }
            }

            // bosses: walk, walk, attack, hit, death, skill wind-up
            foreach (var m in cfg.maps)
                for (int f = 0; f <= SkillFrame; f++)
                {
                    string map = m.mapId;
                    var accent = BossAccent(map);
                    var px = BuildFrame(56, (p, wf) => DrawBoss(p, map, wf), f, accent, accent, f == SkillFrame ? 0.45f : 0f);
                    Write(px, $"{ArtRoot}/Bosses/{m.mapId}_{f}.png", written); sheet.Add(px);
                }

            // backdrops
            int seed = 11;
            foreach (var m in cfg.maps) { var bg = DrawBackground(m.mapId, seed++); Write(bg, $"{ArtRoot}/Environments/bg_{m.mapId}.png", written); sheet.Add(bg); }

            // UI
            Write(UiPanel(), $"{ArtRoot}/UI/ui_panel.png", written);
            Write(UiFrame(), $"{ArtRoot}/UI/ui_frame.png", written);
            Write(UiButton(0), $"{ArtRoot}/UI/ui_button.png", written);
            Write(UiButton(1), $"{ArtRoot}/UI/ui_button_pressed.png", written);
            Write(UiButton(2), $"{ArtRoot}/UI/ui_button_disabled.png", written);
            var gem = IconGem(); var qi = IconQi(); var ess = IconEssence();
            Write(gem, $"{ArtRoot}/UI/icon_linh_thach.png", written);
            Write(qi, $"{ArtRoot}/UI/icon_linh_khi.png", written);
            Write(ess, $"{ArtRoot}/UI/icon_tinh_hoa.png", written);
            sheet.AddRange(new[] { UiPanel(), UiFrame(), UiButton(0), UiButton(1), UiButton(2), gem, qi, ess });

            // skill effects
            var vfx = new Dictionary<string, Pix>
            {
                { "skill_kiem_khi", VfxSlash() }, { "skill_hoa_cau", VfxOrb() }, { "skill_bang_hoan", VfxRing(48, true) },
                { "skill_ho_the", VfxRing(48, false) }, { "skill_van_kiem", VfxBlades() }
            };
            foreach (var kv in vfx) { Write(kv.Value, $"{ArtRoot}/VFX/{kv.Key}.png", written); sheet.Add(kv.Value); }

            foreach (var kv in Glyphs) Write(Glyph(kv.Value), $"{ArtRoot}/VFX/digit_{kv.Key}.png", written);

            // combat feedback effects (near-white, tinted in code): hit spark, death puff, spawn ring, boss telegraph ring
            Write(FxHit(), $"{ArtRoot}/VFX/fx_hit.png", written);
            Write(FxPuff(), $"{ArtRoot}/VFX/fx_death.png", written);
            Write(FxRing(32, 2, false), $"{ArtRoot}/VFX/fx_spawn.png", written);
            Write(FxRing(64, 2, true), $"{ArtRoot}/VFX/fx_telegraph.png", written);

            // skill icons: the effect sprite tinted with the skill colour on a framed tile
            foreach (var s in cfg.economy.skills)
                if (vfx.TryGetValue(s.id, out var src))
                {
                    // real coloured effect art goes into the icon untinted
                    string vfxRel = $"{ArtRoot}/VFX/{s.id}.png";
                    bool real = finalArt.Contains(vfxRel);
                    if (real) src = LoadPix(vfxRel) ?? src;
                    var icon = SkillIcon(src, real ? Color.white : s.vfxColor);
                    Write(icon, $"{ArtRoot}/Icons/{s.id}.png", written); sheet.Add(icon);
                }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var rel in written) AssetDatabase.ImportAsset(rel, ImportAssetOptions.ForceSynchronousImport);

            BuildCatalog(cfg);
            if (!string.IsNullOrEmpty(previewPath)) WritePreview(sheet, previewPath);
            return written.Count + " PNG files written";
        }

        static Sprite S(string rel) => AssetDatabase.LoadAssetAtPath<Sprite>(rel);

        static string[] Frames(string folder, string id, int count)
        {
            var rels = new string[count];
            for (int i = 0; i < count; i++) rels[i] = $"{ArtRoot}/{folder}/{id}_{i}.png";
            return rels;
        }

        static ArtCatalog.Entry Entry(string id, params string[] rels)
        {
            var frames = new List<Sprite>();
            foreach (var r in rels) { var s = S(r); if (s != null) frames.Add(s); }
            return new ArtCatalog.Entry { id = id, frames = frames.ToArray() };
        }

        static void BuildCatalog(PrototypeConfig cfg)
        {
            var entries = new List<ArtCatalog.Entry>();
            entries.Add(Entry("player", $"{ArtRoot}/Characters/player_0.png", $"{ArtRoot}/Characters/player_1.png", $"{ArtRoot}/Characters/player_2.png", $"{ArtRoot}/Characters/player_3.png", $"{ArtRoot}/Characters/player_4.png"));
            foreach (var m in cfg.maps)
            {
                entries.Add(Entry("boss." + m.mapId, $"{ArtRoot}/Bosses/{m.mapId}_0.png", $"{ArtRoot}/Bosses/{m.mapId}_1.png", $"{ArtRoot}/Bosses/{m.mapId}_2.png", $"{ArtRoot}/Bosses/{m.mapId}_3.png", $"{ArtRoot}/Bosses/{m.mapId}_4.png", $"{ArtRoot}/Bosses/{m.mapId}_5.png"));
                entries.Add(Entry("bg." + m.mapId, $"{ArtRoot}/Environments/bg_{m.mapId}.png"));
                foreach (var e in m.roster)
                {
                    entries.Add(Entry("enemy." + e.id, Frames("Enemies", e.id, 5)));
                    entries.Add(Entry("elite." + e.id, Frames("Elites", e.id, 5)));
                }
            }
            foreach (var fx in new[] { "hit", "death", "spawn", "telegraph" }) entries.Add(Entry("fx." + fx, $"{ArtRoot}/VFX/fx_{fx}.png"));
            foreach (var s in cfg.economy.skills) entries.Add(Entry("icon." + s.id, $"{ArtRoot}/Icons/{s.id}.png"));
            foreach (var s in cfg.economy.skills)
            {
                string rel = $"{ArtRoot}/VFX/{s.id}.png";
                var e = Entry("vfx." + s.id, rel);
                e.fullColor = finalArt.Contains(rel);
                entries.Add(e);
            }
            foreach (var id in new[] { "ui_panel", "ui_frame", "ui_button", "ui_button_pressed", "ui_button_disabled" })
                entries.Add(Entry(id.Replace("ui_", "ui."), $"{ArtRoot}/UI/{id}.png"));
            entries.Add(Entry("portrait.player", $"{ArtRoot}/Portraits/portrait_player.png"));
            foreach (var m in cfg.maps) entries.Add(Entry("portrait.boss." + m.mapId, $"{ArtRoot}/Portraits/portrait_boss_{m.mapId}.png"));
            foreach (var id in new[] { "linh_thach", "linh_khi", "tinh_hoa" }) entries.Add(Entry("icon." + id, $"{ArtRoot}/UI/icon_{id}.png"));
            foreach (var key in Glyphs.Keys) entries.Add(Entry("digit." + key, $"{ArtRoot}/VFX/digit_{key}.png"));

            const string path = "Assets/_Project/ScriptableObjects/Economy/ArtCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(path);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<ArtCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
            catalog.entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            cfg.art = catalog;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        static void WritePreview(List<Pix> sheet, string path)
        {
            const int Width = 1100;
            int x = 6, y = 6, rowH = 0;
            var canvas = new Pix(Width, 1400);
            for (int i = 0; i < canvas.p.Length; i++) canvas.p[i] = K(70, 62, 90);
            foreach (var px in sheet)
            {
                int scale = px.w >= 200 ? 1 : (px.w >= 48 ? 3 : (px.w >= 24 ? 4 : 5));
                int ww = px.w * scale / (px.w >= 200 ? 2 : 1), hh = px.h * scale / (px.w >= 200 ? 2 : 1);
                if (x + ww + 6 > Width) { x = 6; y += rowH + 6; rowH = 0; }
                if (y + hh + 6 > canvas.h) break;
                int step = px.w >= 200 ? 2 : 1;
                for (int sy = 0; sy < hh; sy++)
                    for (int sx = 0; sx < ww; sx++)
                    {
                        int srcX = (sx / (px.w >= 200 ? 1 : scale)) * step, srcY = (sy / (px.w >= 200 ? 1 : scale)) * step;
                        if (srcX >= px.w || srcY >= px.h) continue;
                        var c = px.Get(srcX, srcY);
                        if (c.a == 0) { canvas.Set(x + sx, y + sy, ((sx / 4 + sy / 4) % 2 == 0) ? K(95, 86, 118) : K(84, 76, 106)); continue; }
                        canvas.Set(x + sx, y + sy, c);
                    }
                x += ww + 6; rowH = Math.Max(rowH, hh);
            }
            File.WriteAllBytes(path, canvas.ToPng());
        }

        [MenuItem("PixelImmortal/Generate Placeholder Pixel Art")]
        static void MenuGenerate()
        {
            // The contact sheet goes to the project's Temp folder (not part of the project assets).
            string preview = Path.Combine(Application.dataPath, "..", "Temp", "art_preview.png").Replace('\\', '/');
            Debug.Log(Generate(preview) + "; contact sheet: " + preview);
        }
    }
}
