using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>One list row: a wrapped description on the left and an optional action button on the right.</summary>
    public sealed class RowView
    {
        readonly GameObject root;
        readonly Text label;
        readonly Text buttonLabel;
        readonly Button button;

        public RowView(Transform parent, Vector2 min, Vector2 max, Action onClick)
        {
            var rt = UiKit.Panel("Row", parent, min, max, UiTheme.Row, true);
            root = rt.gameObject;
            label = UiKit.Label(rt, 0.02f, 0f, 0.68f, 1f, 26, TextAnchor.MiddleLeft);
            button = UiKit.TextButton("Btn", rt, new Vector2(0.7f, 0.12f), new Vector2(0.98f, 0.88f), UiTheme.Action, "", 26, () => onClick(), out buttonLabel);
        }

        public void SetActive(bool on) => root.SetActive(on);

        public void Set(string text, string buttonText, bool interactable)
        {
            label.text = text;
            ShowButton(true);
            buttonLabel.text = buttonText;
            button.interactable = interactable;
        }

        /// <summary>A row with no action: the text takes the full width.</summary>
        public void SetInfo(string text)
        {
            label.text = text;
            ShowButton(false);
        }

        void ShowButton(bool on)
        {
            button.gameObject.SetActive(on);
            label.rectTransform.anchorMax = new Vector2(on ? 0.68f : 0.98f, 1f);
        }
    }

    /// <summary>Up to six rows per page with a pager when there are more. Item index is passed to the click and fill callbacks.</summary>
    public sealed class RowListView
    {
        public const int RowsPerPage = 6;
        const float PagerHeight = 0.12f;

        readonly RowView[] rows = new RowView[RowsPerPage];
        readonly GameObject pager;
        readonly Text pageLabel;
        Action<int, RowView> lastFill;
        int page;
        int total;

        public RowListView(Transform parent, Vector2 min, Vector2 max, Action<int> onAction)
        {
            var area = UiKit.Panel("List", parent, min, max, new Color(0f, 0f, 0f, 0f));
            area.GetComponent<Image>().raycastTarget = false;
            float h = (1f - PagerHeight) / RowsPerPage;
            for (int i = 0; i < RowsPerPage; i++)
            {
                int slot = i;
                float top = 1f - i * h;
                rows[i] = new RowView(area, new Vector2(0.01f, top - h + 0.004f), new Vector2(0.99f, top - 0.004f), () => onAction(page * RowsPerPage + slot));
            }
            var pagerRt = UiKit.Panel("Pager", area, Vector2.zero, new Vector2(1f, PagerHeight), new Color(0f, 0f, 0f, 0f));
            pagerRt.GetComponent<Image>().raycastTarget = false;
            pager = pagerRt.gameObject;
            UiKit.TextButton("Prev", pagerRt, new Vector2(0.02f, 0.08f), new Vector2(0.25f, 0.92f), UiTheme.TabOff, "<", 28, () => Turn(-1), out _);
            UiKit.TextButton("Next", pagerRt, new Vector2(0.75f, 0.08f), new Vector2(0.98f, 0.92f), UiTheme.TabOff, ">", 28, () => Turn(1), out _);
            pageLabel = UiKit.Label(pagerRt, 0.27f, 0f, 0.73f, 1f, 24, TextAnchor.MiddleCenter);
        }

        void Turn(int delta)
        {
            page += delta;
            if (lastFill != null) Refresh(total, lastFill);
        }

        public void Refresh(int count, Action<int, RowView> fill)
        {
            lastFill = fill;
            total = count;
            int pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)RowsPerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            pager.SetActive(pages > 1);
            pageLabel.text = (page + 1) + "/" + pages;
            for (int i = 0; i < RowsPerPage; i++)
            {
                int index = page * RowsPerPage + i;
                rows[i].SetActive(index < count);
                if (index < count) fill(index, rows[i]);
            }
        }
    }
}
