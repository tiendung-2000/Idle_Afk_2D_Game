using PixelImmortal.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>Optional sprites for the UI chrome. Missing sprites mean flat colour, which is the placeholder look.</summary>
    public class UiSkin
    {
        public Sprite panel, frame, button, buttonPressed, buttonDisabled;
        public Sprite iconLinhThach, iconLinhKhi, iconTinhHoa;
        ArtCatalog art;

        /// <summary>HUD portrait for "player" or "boss.&lt;mapId&gt;", or null when there is no art for it.</summary>
        public Sprite Portrait(string id) => art != null ? art.First("portrait." + id) : null;

        /// <summary>Icon for a skill id (e.g. "skill_kiem_khi"), or null when there is no art for it.</summary>
        public Sprite SkillIcon(string skillId) => art != null ? art.First("icon." + skillId) : null;

        public static UiSkin From(ArtCatalog art)
        {
            if (art == null) return null;
            var s = new UiSkin
            {
                art = art,
                panel = art.First("ui.panel"),
                frame = art.First("ui.frame"),
                button = art.First("ui.button"),
                buttonPressed = art.First("ui.button_pressed"),
                buttonDisabled = art.First("ui.button_disabled"),
                iconLinhThach = art.First("icon.linh_thach"),
                iconLinhKhi = art.First("icon.linh_khi"),
                iconTinhHoa = art.First("icon.tinh_hoa")
            };
            return s.panel == null && s.button == null ? null : s;
        }
    }

    /// <summary>Code-built UI helpers. Chrome sprites come from <see cref="Skin"/> when art exists.</summary>
    public static class UiKit
    {
        static Font font;

        /// <summary>Set once by the HUD when the art catalog has UI sprites.</summary>
        public static UiSkin Skin;

        /// <summary>Played on every button press (wired to the audio service at startup).</summary>
        public static System.Action ClickSound;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <param name="skinned">Use the sliced panel sprite (tinted by <paramref name="color"/>) when a skin exists.</param>
        public static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color, bool skinned = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            if (skinned && Skin != null && Skin.panel != null)
            {
                img.sprite = Skin.panel;
                img.type = Image.Type.Sliced;
            }
            return rt;
        }

        /// <summary>Ornamental border drawn over a panel (centre left empty). No-op without a frame sprite.</summary>
        public static void AddFrame(RectTransform target)
        {
            if (Skin == null || Skin.frame == null) return;
            var go = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(target, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = Skin.frame;
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            img.raycastTarget = false;
        }

        public static Image AddIcon(RectTransform parent, Sprite sprite, float xMin, float yMin, float xMax, float yMax)
        {
            if (sprite == null) return null;
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(RectTransform parent, float xMin, float yMin, float xMax, float yMax, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 12;
            t.resizeTextMaxSize = size;
            return t;
        }

        /// <summary>Panel + Button + centered label. With a skin: sliced button sprite with pressed/disabled swaps.</summary>
        public static Button TextButton(string name, Transform parent, Vector2 min, Vector2 max, Color color, string text, int size, UnityAction onClick, out Text label)
        {
            var rt = Panel(name, parent, min, max, color);
            var img = rt.GetComponent<Image>();
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            if (Skin != null && Skin.button != null)
            {
                img.sprite = Skin.button;
                img.type = Image.Type.Sliced;
                btn.transition = Selectable.Transition.SpriteSwap;
                btn.spriteState = new SpriteState
                {
                    pressedSprite = Skin.buttonPressed != null ? Skin.buttonPressed : Skin.button,
                    highlightedSprite = Skin.button,
                    selectedSprite = Skin.button,
                    disabledSprite = Skin.buttonDisabled != null ? Skin.buttonDisabled : Skin.button
                };
            }
            else
            {
                var colors = btn.colors;
                colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
                btn.colors = colors;
            }

            rt.gameObject.AddComponent<PressScale>();
            btn.onClick.AddListener(() => { ClickSound?.Invoke(); onClick(); });
            label = Label(rt, 0.04f, 0.06f, 0.96f, 0.94f, size, TextAnchor.MiddleCenter);
            label.text = text;
            return btn;
        }

        /// <summary>Flat bar fill; deliberately never skinned.</summary>
        public static RectTransform FillBar(RectTransform back, Color color)
        {
            var rt = Panel("Fill", back, Vector2.zero, Vector2.one, color);
            rt.GetComponent<Image>().raycastTarget = false;
            return rt;
        }

        public static void SetFill(RectTransform fill, float ratio) => fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);

        public static string Short(double v)
        {
            if (double.IsInfinity(v) || double.IsNaN(v)) return "--";
            if (v < 10000) return v < 100 ? v.ToString("0.#") : v.ToString("0");
            string[] suffix = { "", "K", "M", "B", "T", "Qa", "Qi" };
            int i = 0;
            while (v >= 1000 && i < suffix.Length - 1) { v /= 1000; i++; }
            return v.ToString("0.##") + suffix[i];
        }
    }
}
