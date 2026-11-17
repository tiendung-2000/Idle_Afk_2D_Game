using PixelImmortal.Core;
using UnityEngine;

namespace PixelImmortal.Combat
{
    public class HealthBarView : MonoBehaviour
    {
        Transform fill;
        float width;
        float height;
        Health health;

        public void Build(float barWidth, float barHeight, float yOffset, Color fillColor)
        {
            width = barWidth;
            height = barHeight;

            var bg = new GameObject("BarBg").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(transform, false);
            bg.sprite = PlaceholderSprites.Square;
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            bg.sortingOrder = 500;
            bg.transform.localPosition = new Vector3(0f, yOffset, 0f);
            bg.transform.localScale = new Vector3(width + 0.06f, height + 0.06f, 1f);

            var f = new GameObject("BarFill").AddComponent<SpriteRenderer>();
            f.transform.SetParent(transform, false);
            f.sprite = PlaceholderSprites.Bar;
            f.color = fillColor;
            f.sortingOrder = 501;
            f.transform.localPosition = new Vector3(-width * 0.5f, yOffset, 0f);
            fill = f.transform;
            SetRatio(1f);
        }

        public void Bind(Health h)
        {
            Unbind();
            health = h;
            health.Changed += OnChanged;
            OnChanged(h.Current, h.Max);
        }

        void OnDisable() => Unbind();

        void Unbind()
        {
            if (health == null) return;
            health.Changed -= OnChanged;
            health = null;
        }

        void OnChanged(float current, float max) => SetRatio(max > 0f ? current / max : 0f);

        void SetRatio(float r) => fill.localScale = new Vector3(width * Mathf.Clamp01(r), height, 1f);
    }
}
