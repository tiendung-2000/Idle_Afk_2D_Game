using UnityEngine;

namespace PixelImmortal.Core
{
    /// <summary>Procedural placeholder art, 1 world unit per sprite.</summary>
    public static class PlaceholderSprites
    {
        static Sprite square, circle, bar;

        public static Sprite Square
        {
            get { if (square == null) square = Make("PH_Square", 8, false, new Vector2(0.5f, 0.5f)); return square; }
        }

        public static Sprite Circle
        {
            get { if (circle == null) circle = Make("PH_Circle", 16, true, new Vector2(0.5f, 0.5f)); return circle; }
        }

        /// <summary>Square with left-center pivot, for fill bars.</summary>
        public static Sprite Bar
        {
            get { if (bar == null) bar = Make("PH_Bar", 8, false, new Vector2(0f, 0.5f)); return bar; }
        }

        static Sprite Make(string name, int size, bool round, Vector2 pivot)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f, r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                bool on = !round || dx * dx + dy * dy <= r * r;
                px[y * size + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), pivot, size);
            s.name = name;
            return s;
        }
    }
}
