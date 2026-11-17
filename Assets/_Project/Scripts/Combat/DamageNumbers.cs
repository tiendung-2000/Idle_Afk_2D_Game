using PixelImmortal.Data;
using UnityEngine;

namespace PixelImmortal.Combat
{
    /// <summary>Digit codes for a damage number: 0-9, 10 = K, 11 = M. Pure so it can be tested.</summary>
    public static class NumberGlyphs
    {
        public const int K = 10, M = 11;

        /// <summary>Writes the glyph codes into buffer and returns how many. 12,345 -> "12K"; 1,500,000 -> "1M".</summary>
        public static int Format(double value, int[] buffer)
        {
            if (double.IsNaN(value) || value < 0) value = 0;
            int suffix = -1;
            if (value >= 1e6) { value /= 1e6; suffix = M; }
            else if (value >= 10000) { value /= 1000; suffix = K; }
            if (suffix >= 0 && value > 999) value = 999;       // "999M" is the largest label
            if (suffix < 0 && value > 9999) value = 9999;

            long v = (long)value;
            int digits = 1;
            for (long t = v; t >= 10; t /= 10) digits++;
            int n = digits + (suffix >= 0 ? 1 : 0);
            if (n > buffer.Length) return 0;

            for (int i = digits - 1; i >= 0; i--) { buffer[i] = (int)(v % 10); v /= 10; }
            if (suffix >= 0) buffer[digits] = suffix;
            return n;
        }
    }

    /// <summary>
    /// Floating pixel-font damage numbers from a fixed pool of sprite renderers (no per-hit allocation).
    /// Silently disabled when the art catalog has no digit sprites.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        const int Slots = 16;
        const int MaxGlyphs = 6;
        const float Life = 0.8f;
        const float Rise = 0.9f;

        sealed class Slot
        {
            public Transform root;
            public SpriteRenderer[] glyphs;
            public int count;
            public float age;
            public float baseScale;
            public Color color;
            public bool active;
        }

        Slot[] slots;
        Sprite[] sprites;            // 0-9, K, M
        float glyphWidth;
        readonly int[] buffer = new int[MaxGlyphs];

        public bool Enabled { get; private set; }

        /// <summary>Numbers currently on screen (diagnostics).</summary>
        public int ActiveCount
        {
            get
            {
                int c = 0;
                if (slots != null) foreach (var s in slots) if (s.active) c++;
                return c;
            }
        }

        public void Init(ArtCatalog art)
        {
            if (art == null) return;
            sprites = new Sprite[12];
            for (int i = 0; i < 10; i++) sprites[i] = art.First("digit." + i);
            sprites[NumberGlyphs.K] = art.First("digit.k");
            sprites[NumberGlyphs.M] = art.First("digit.m");
            foreach (var s in sprites) if (s == null) return;

            glyphWidth = sprites[0].bounds.size.x;
            slots = new Slot[Slots];
            for (int i = 0; i < Slots; i++)
            {
                var slot = new Slot { root = new GameObject("Number" + i).transform, glyphs = new SpriteRenderer[MaxGlyphs] };
                slot.root.SetParent(transform, false);
                for (int g = 0; g < MaxGlyphs; g++)
                {
                    var sr = new GameObject("G" + g).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(slot.root, false);
                    sr.sortingOrder = 700;
                    sr.enabled = false;
                    slot.glyphs[g] = sr;
                }
                slots[i] = slot;
            }
            Enabled = true;
        }

        public void Show(Vector3 position, float amount, bool critical, Color color)
        {
            if (!Enabled) return;
            Slot slot = null;
            for (int i = 0; i < slots.Length; i++) if (!slots[i].active) { slot = slots[i]; break; }
            if (slot == null) return;                       // pool exhausted: drop the number, never allocate

            int n = NumberGlyphs.Format(amount, buffer);
            if (n == 0) return;

            slot.count = n;
            slot.age = 0f;
            slot.active = true;
            slot.baseScale = critical ? 1.9f : 1.4f;
            slot.color = color;
            position.x += Random.Range(-0.25f, 0.25f);
            slot.root.position = position;

            float step = glyphWidth * 0.9f;
            for (int g = 0; g < MaxGlyphs; g++)
            {
                var sr = slot.glyphs[g];
                bool used = g < n;
                sr.enabled = used;
                if (!used) continue;
                sr.sprite = sprites[buffer[g]];
                sr.color = color;
                sr.transform.localPosition = new Vector3((g - (n - 1) * 0.5f) * step, 0f, 0f);
            }
            slot.root.localScale = Vector3.one * slot.baseScale;
        }

        void Update()
        {
            if (!Enabled) return;
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (!s.active) continue;
                s.age += Time.deltaTime;
                float k = s.age / Life;
                if (k >= 1f)
                {
                    s.active = false;
                    for (int g = 0; g < MaxGlyphs; g++) s.glyphs[g].enabled = false;
                    continue;
                }
                var p = s.root.position;
                p.y += Rise / Life * Time.deltaTime * (1f - k * 0.6f);
                s.root.position = p;

                float pop = k < 0.15f ? Mathf.Lerp(1.5f, 1f, k / 0.15f) : 1f;   // quick "pop" on spawn
                s.root.localScale = Vector3.one * s.baseScale * pop;
                var c = s.color;
                c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                for (int g = 0; g < s.count; g++) s.glyphs[g].color = c;
            }
        }
    }
}
