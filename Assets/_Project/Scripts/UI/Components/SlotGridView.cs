using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>Paged grid of <see cref="SlotView"/>s. The page row only shows when there is more than one page.</summary>
    public sealed class SlotGridView
    {
        const float PagerHeight = 0.14f;

        readonly SlotView[] cells;
        readonly int perPage;
        readonly Action<int> onSelect;
        readonly GameObject pager;
        readonly Text pageLabel;
        Action<SlotView, int> lastFill;
        int lastSelected;
        int page;
        int total;

        public SlotGridView(Transform parent, Vector2 min, Vector2 max, int cols, int rows, Action<int> onSelect)
        {
            this.onSelect = onSelect;
            perPage = cols * rows;
            cells = new SlotView[perPage];
            var area = UiKit.Panel("Grid", parent, min, max, new Color(0f, 0f, 0f, 0f));
            area.GetComponent<Image>().raycastTarget = false;

            float cw = 1f / cols, ch = (1f - PagerHeight) / rows;
            for (int i = 0; i < perPage; i++)
            {
                int slot = i;
                int c = i % cols, r = i / cols;
                float x0 = c * cw, y1 = 1f - r * ch;
                cells[i] = new SlotView(area, new Vector2(x0 + 0.006f, y1 - ch + 0.006f), new Vector2(x0 + cw - 0.006f, y1 - 0.006f), () => Pick(slot));
            }

            var pagerRt = UiKit.Panel("Pager", area, Vector2.zero, new Vector2(1f, PagerHeight), new Color(0f, 0f, 0f, 0f));
            pagerRt.GetComponent<Image>().raycastTarget = false;
            pager = pagerRt.gameObject;
            UiKit.TextButton("Prev", pagerRt, new Vector2(0.02f, 0.08f), new Vector2(0.25f, 0.92f), UiTheme.TabOff, "<", 28, () => Turn(-1), out _);
            UiKit.TextButton("Next", pagerRt, new Vector2(0.75f, 0.08f), new Vector2(0.98f, 0.92f), UiTheme.TabOff, ">", 28, () => Turn(1), out _);
            pageLabel = UiKit.Label(pagerRt, 0.27f, 0f, 0.73f, 1f, 24, TextAnchor.MiddleCenter);
        }

        public int Total => total;

        void Pick(int cell)
        {
            int index = page * perPage + cell;
            if (index < total) onSelect(index);
        }

        public void ResetPage() => page = 0;

        void Turn(int delta)
        {
            page += delta;
            if (lastFill != null) Refresh(total, lastSelected, lastFill);
        }

        /// <param name="fill">Draws item <c>index</c> into the given cell.</param>
        public void Refresh(int itemCount, int selectedIndex, Action<SlotView, int> fill)
        {
            lastFill = fill;
            lastSelected = selectedIndex;
            total = itemCount;
            int pages = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)perPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            pager.SetActive(pages > 1);
            pageLabel.text = (page + 1) + "/" + pages;
            for (int i = 0; i < perPage; i++)
            {
                int index = page * perPage + i;
                cells[i].SetActive(index < itemCount);
                if (index < itemCount) fill(cells[i], index);
            }
        }
    }
}
