using PixelImmortal.Data;
using PixelImmortal.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>Permanent upgrades bought with resources (includes the offline-efficiency upgrade). Costs and caps come from the upgrade system.</summary>
    public class UpgradesScreen : FrameScreen
    {
        RowListView list;
        Text footer;

        protected override void Build(RectTransform c)
        {
            list = new RowListView(c, new Vector2(0f, 0.08f), new Vector2(1f, 0.99f), i => Buy(i));
            footer = Body(c, 0.03f, 0f, 0.97f, 0.08f, 26, TextAnchor.MiddleCenter);
            footer.color = UiTheme.Gold;
        }

        void Buy(int i)
        {
            var defs = G.Progress.Economy.upgrades;
            if (i < defs.Length) G.Upgrades.TryBuy(defs[i]);
            Render();
        }

        protected override void Render()
        {
            var defs = G.Progress.Economy.upgrades;
            list.Refresh(defs.Length, (i, row) =>
            {
                var d = defs[i];
                int lv = G.Progress.GetUpgradeLevel(d.id);
                string max = d.maxLevel > 0 ? "/" + d.maxLevel : "";
                string text = Loc.Format("menu.upgrade_row", Loc.Get("upgrade." + d.id), lv, max,
                    (d.effectPerLevel * lv * 100f).ToString("0.#"), (d.effectPerLevel * 100f).ToString("0.#"));
                if (!G.Upgrades.IsUnlocked(d)) row.Set(text, Loc.Get("btn.locked"), false);
                else if (G.Upgrades.IsMaxed(d)) row.Set(text, Loc.Get("btn.max"), false);
                else row.Set(text, Loc.Format("menu.upgrade_cost", UiKit.Short(G.Upgrades.Cost(d)), Loc.Get(UiTheme.ShortKey(d.costResource))), G.Upgrades.CanBuy(d));
            });
            var w = G.Progress.Wallet;
            footer.text = Loc.Format("upgrades.footer", UiKit.Short(w.Get(ResourceType.LinhThach)), UiKit.Short(w.Get(ResourceType.LinhKhi)), UiKit.Short(w.Get(ResourceType.TinhHoa)));
        }
    }
}
