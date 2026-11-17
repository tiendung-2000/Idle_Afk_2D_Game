using System.Collections.Generic;
using PixelImmortal.Data;
using PixelImmortal.Gacha;
using PixelImmortal.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Banner, balance, single and multi pull with explicit costs, rates and the last result. Odds and costs come from the
    /// pool asset; the charge and the grant happen atomically inside <see cref="GachaSystem"/>.
    /// </summary>
    public class SummonScreen : FrameScreen
    {
        readonly List<PullResult> last = new List<PullResult>();
        readonly List<PullResult> attempt = new List<PullResult>();
        Image[] tabImages;
        Text poolText, balanceText, reasonText, ruleText, footerText, singleLabel, multiLabel, lastLabel;
        Button singleButton, multiButton, lastButton;
        RatesPopup rates;
        SummonResultPopup result;
        int poolIndex;

        GachaPoolDefinition Pool
        {
            get
            {
                var pools = G.Progress.Economy.gachaPools;
                return pools != null && pools.Length > 0 ? pools[Mathf.Clamp(poolIndex, 0, pools.Length - 1)] : null;
            }
        }

        int MultiCount => G.Progress.Economy.maxPullCount;

        protected override void Build(RectTransform c)
        {
            var pools = G.Progress.Economy.gachaPools ?? new GachaPoolDefinition[0];
            tabImages = new Image[pools.Length];
            if (pools.Length > 1)
            {
                float w = 1f / pools.Length;
                for (int i = 0; i < pools.Length; i++)
                {
                    int index = i;
                    var b = UiKit.TextButton("Banner" + i, c, new Vector2(i * w + 0.01f, 0.9f), new Vector2((i + 1) * w - 0.01f, 0.99f), UiTheme.TabOff, "", 26, () => poolIndex = index, out var tl);
                    LocalizedLabel.Bind(tl, Loc, "gacha." + pools[i].id);
                    tabImages[i] = b.GetComponent<Image>();
                }
            }

            poolText = Body(c, 0.04f, 0.74f, 0.96f, 0.88f, 38, TextAnchor.MiddleCenter);
            poolText.color = UiTheme.Gold;
            balanceText = Body(c, 0.04f, 0.65f, 0.96f, 0.74f, 30, TextAnchor.MiddleCenter);
            singleButton = UiKit.TextButton("Pull1", c, new Vector2(0.04f, 0.44f), new Vector2(0.48f, 0.63f), UiTheme.Action, "", 30, () => Pull(1), out singleLabel);
            multiButton = UiKit.TextButton("PullN", c, new Vector2(0.52f, 0.44f), new Vector2(0.96f, 0.63f), UiTheme.Action, "", 30, () => Pull(MultiCount), out multiLabel);
            reasonText = Body(c, 0.04f, 0.35f, 0.96f, 0.43f, 28, TextAnchor.MiddleCenter);
            reasonText.color = UiTheme.Warning;
            var ratesButton = UiKit.TextButton("Rates", c, new Vector2(0.04f, 0.23f), new Vector2(0.48f, 0.33f), UiTheme.TabOff, "", 28, ShowRates, out var ratesLabel);
            LocalizedLabel.Bind(ratesLabel, Loc, "summon.rates");
            lastButton = UiKit.TextButton("Last", c, new Vector2(0.52f, 0.23f), new Vector2(0.96f, 0.33f), UiTheme.TabOff, "", 28, () => result.Show(last), out lastLabel);
            ruleText = Body(c, 0.04f, 0.09f, 0.96f, 0.22f, 26, TextAnchor.MiddleCenter);
            footerText = Body(c, 0.04f, 0.01f, 0.96f, 0.09f, 26, TextAnchor.MiddleCenter);
            footerText.color = UiTheme.Gold;

            rates = NewPopup<RatesPopup>("RatesPopup");
            rates.Build(G.Ui.PopupLayer, Loc);
            result = NewPopup<SummonResultPopup>("SummonResultPopup");
            result.Build(G.Ui.PopupLayer, Loc, G.Progress);
        }

        T NewPopup<T>(string name) where T : PopupBase
        {
            var p = new GameObject(name).AddComponent<T>();
            p.transform.SetParent(G.Ui.transform, false);
            G.Ui.RegisterPopup(p);
            return p;
        }

        protected override void OnOpen(object args) => poolIndex = 0;

        protected override void Render()
        {
            var pool = Pool;
            for (int i = 0; i < tabImages.Length; i++) if (tabImages[i] != null) tabImages[i].color = i == poolIndex ? UiTheme.TabOn : UiTheme.TabOff;

            lastLabel.text = Loc.Get("summon.last");
            lastButton.interactable = last.Count > 0;
            footerText.text = Loc.Format("gacha.footer", G.Progress.TotalPulls);

            if (pool == null)
            {
                poolText.text = Loc.Get("gacha.invalid");
                singleButton.interactable = multiButton.interactable = false;
                return;
            }

            string res = Loc.Get(RatesPopup.ResKey(pool.costResource));
            poolText.text = Loc.Get("gacha." + pool.id);
            balanceText.text = Loc.Format("summon.balance", UiKit.Short(G.Progress.Wallet.Get(pool.costResource)), res);
            singleLabel.text = Loc.Format("summon.pull_btn", 1, UiKit.Short(G.Gacha.TotalCost(pool, 1)), res);
            multiLabel.text = Loc.Format("summon.pull_btn", MultiCount, UiKit.Short(G.Gacha.TotalCost(pool, MultiCount)), res);
            singleButton.interactable = G.Gacha.CanAfford(pool, 1);
            multiButton.interactable = G.Gacha.CanAfford(pool, MultiCount);
            ruleText.text = Loc.Get("summon.note");

            double have = G.Progress.Wallet.Get(pool.costResource);
            double need = G.Gacha.TotalCost(pool, 1);
            reasonText.text = have < need ? Loc.Format("summon.short", UiKit.Short(need - have), res) : "";
        }

        void ShowRates()
        {
            var pool = Pool;
            if (pool != null) rates.Show(pool, Loc.Get("gacha." + pool.id));
        }

        void Pull(int count)
        {
            var pool = Pool;
            if (pool == null) return;
            var status = G.Gacha.TryPull(pool, count, attempt);
            if (status == PullStatus.Ok)
            {
                last.Clear();
                last.AddRange(attempt);
                result.Show(last);
            }
            else G.Ui.ShowToast(Loc.Get(status == PullStatus.NotEnoughResources ? "gacha.not_enough" : "gacha.invalid"), ToastSeverity.Warning);
            Render();
        }
    }
}
