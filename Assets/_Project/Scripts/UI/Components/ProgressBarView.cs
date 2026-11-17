using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>Normalized bar with an optional centred label.</summary>
    public sealed class ProgressBarView
    {
        readonly RectTransform fill;
        readonly Image fillImage;
        readonly Text label;

        public RectTransform Rect { get; }

        public ProgressBarView(Transform parent, Vector2 min, Vector2 max, Color fillColor, int labelSize = 26)
        {
            Rect = UiKit.Panel("Bar", parent, min, max, new Color(0.12f, 0.12f, 0.16f));
            fill = UiKit.FillBar(Rect, fillColor);
            fillImage = fill.GetComponent<Image>();
            label = UiKit.Label(Rect, 0.02f, 0f, 0.98f, 1f, labelSize, TextAnchor.MiddleCenter);
        }

        public void Set(float ratio, string text)
        {
            UiKit.SetFill(fill, ratio);
            label.text = text;
        }

        public void SetColor(Color c) => fillImage.color = c;
    }
}
