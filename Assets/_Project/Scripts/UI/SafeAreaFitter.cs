using UnityEngine;

namespace PixelImmortal.UI
{
    /// <summary>Keeps a RectTransform inside Screen.safeArea (notches, rounded corners, gesture bars).</summary>
    public class SafeAreaFitter : MonoBehaviour
    {
        Rect last;
        int lastW, lastH;

        /// <summary>Safe area as fractions of the screen. Full screen when the device reports none.</summary>
        public static Rect Normalized
        {
            get
            {
                if (Screen.width <= 0 || Screen.height <= 0) return new Rect(0, 0, 1, 1);
                var sa = Screen.safeArea;
                return new Rect(sa.x / Screen.width, sa.y / Screen.height, sa.width / Screen.width, sa.height / Screen.height);
            }
        }

        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == last && Screen.width == lastW && Screen.height == lastH) return;
            last = sa; lastW = Screen.width; lastH = Screen.height;
            var n = Normalized;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(n.xMin, n.yMin);
            rt.anchorMax = new Vector2(n.xMax, n.yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
