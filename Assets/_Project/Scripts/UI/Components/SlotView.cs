using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>
    /// One square slot used for items, equipment and skills: a placeholder icon, a mark in the top-left
    /// corner (rarity numeral), a badge at the bottom (count or level), and selected / locked / empty states.
    /// </summary>
    public sealed class SlotView
    {
        readonly Button button;
        readonly Image back, icon, selection;
        readonly Text mark, badge;

        public GameObject Root => back.gameObject;

        public SlotView(Transform parent, Vector2 min, Vector2 max, Action onClick)
        {
            button = UiKit.TextButton("Slot", parent, min, max, UiTheme.Row, "", 24, () => onClick(), out badge);
            back = button.GetComponent<Image>();

            var iconRt = UiKit.Panel("Icon", back.rectTransform, new Vector2(0.18f, 0.25f), new Vector2(0.82f, 0.85f), Color.white);
            icon = iconRt.GetComponent<Image>();
            icon.raycastTarget = false;

            var selRt = UiKit.Panel("Selected", back.rectTransform, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.28f));
            selection = selRt.GetComponent<Image>();
            selection.raycastTarget = false;

            mark = UiKit.Label(back.rectTransform, 0.05f, 0.7f, 0.55f, 1f, 22, TextAnchor.UpperLeft);
            var br = badge.rectTransform;
            br.anchorMin = new Vector2(0.3f, 0f);
            br.anchorMax = new Vector2(0.97f, 0.28f);
            badge.alignment = TextAnchor.LowerRight;
        }

        public void SetActive(bool on) => back.gameObject.SetActive(on);

        /// <param name="frame">Rarity colour; the numeral in <paramref name="markText"/> repeats it for colour-blind players.</param>
        /// <param name="iconSprite">Optional icon art; it is drawn untinted (dimmed when locked) instead of the coloured placeholder square.</param>
        public void Set(Color iconColor, string markText, Color frame, string badgeText, bool selected, bool locked = false, Sprite iconSprite = null)
        {
            back.color = frame;
            icon.sprite = iconSprite;
            icon.preserveAspect = iconSprite != null;
            if (iconSprite != null) iconColor = Color.white;
            icon.color = locked ? new Color(iconColor.r * 0.4f, iconColor.g * 0.4f, iconColor.b * 0.4f, 0.6f) : iconColor;
            icon.enabled = true;
            mark.text = markText;
            badge.text = badgeText;
            selection.enabled = selected;
        }

        public void SetEmpty(bool selected, string caption = "")
        {
            back.color = UiTheme.Row;
            icon.enabled = false;
            mark.text = "";
            badge.text = caption;
            selection.enabled = selected;
        }
    }
}
