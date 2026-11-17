using PixelImmortal.Cultivation;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Realm, requirements and the Breakthrough action. The button only appears once every requirement is met;
    /// until then the exact missing requirement is shown. Costs and gains come from the realm definitions.
    /// </summary>
    public class CultivationScreen : FrameScreen
    {
        Text realmText, rewardText, statusText, buttonLabel;
        ProgressBarView linhKhiBar, linhThachBar, chapterBar;
        GameObject buttonObject;

        protected override void Build(RectTransform c)
        {
            realmText = Body(c, 0.04f, 0.76f, 0.96f, 0.98f, 34, TextAnchor.MiddleCenter);
            realmText.color = UiTheme.Gold;
            linhKhiBar = new ProgressBarView(c, new Vector2(0.04f, 0.6f), new Vector2(0.96f, 0.71f), new Color(0.3f, 0.65f, 0.9f));
            linhThachBar = new ProgressBarView(c, new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.57f), new Color(0.85f, 0.7f, 0.25f));
            chapterBar = new ProgressBarView(c, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.43f), new Color(0.5f, 0.8f, 0.5f));
            rewardText = Body(c, 0.04f, 0.2f, 0.96f, 0.3f, 30, TextAnchor.MiddleCenter);
            statusText = Body(c, 0.04f, 0.1f, 0.96f, 0.2f, 28, TextAnchor.MiddleCenter);
            statusText.color = UiTheme.Warning;
            var button = Action(c, 0.2f, 0.01f, 0.8f, 0.1f, UiTheme.Action, AskBreakthrough, out buttonLabel);
            buttonObject = button.gameObject;
        }

        protected override void Render()
        {
            var next = G.Cultivation.Next;
            string current = Loc.Get("realm." + G.Cultivation.Current.id);
            if (next == null)
            {
                realmText.text = Loc.Format("cult.max", current);
                linhKhiBar.Rect.gameObject.SetActive(false);
                linhThachBar.Rect.gameObject.SetActive(false);
                chapterBar.Rect.gameObject.SetActive(false);
                rewardText.text = "";
                statusText.text = "";
                buttonObject.SetActive(false);
                return;
            }

            linhKhiBar.Rect.gameObject.SetActive(true);
            linhThachBar.Rect.gameObject.SetActive(true);
            chapterBar.Rect.gameObject.SetActive(true);

            var w = G.Progress.Wallet;
            realmText.text = Loc.Format("cult.header", current, Loc.Get("realm." + next.id));
            Bar(linhKhiBar, "res.linh_khi", w.Get(ResourceType.LinhKhi), next.linhKhiCost);
            Bar(linhThachBar, "res.linh_thach", w.Get(ResourceType.LinhThach), next.linhThachCost);
            chapterBar.Set(Mathf.Clamp01(G.Progress.HighestChapter / (float)Mathf.Max(1, next.minChapter)),
                Loc.Format("cult.chapter", G.Progress.HighestChapter, next.minChapter));
            rewardText.text = Loc.Format("cult.gain", next.attackMultiplier.ToString("0.##"), next.hpMultiplier.ToString("0.##"));

            var block = G.Cultivation.Check();
            buttonObject.SetActive(block == BreakthroughBlock.None);
            buttonLabel.text = Loc.Get("btn.breakthrough");
            statusText.text = block == BreakthroughBlock.NeedLinhKhi ? Loc.Format("cult.need", Loc.Get("res.linh_khi"), UiKit.Short(next.linhKhiCost - w.Get(ResourceType.LinhKhi)))
                : block == BreakthroughBlock.NeedLinhThach ? Loc.Format("cult.need", Loc.Get("res.linh_thach"), UiKit.Short(next.linhThachCost - w.Get(ResourceType.LinhThach)))
                : block == BreakthroughBlock.NeedChapter ? Loc.Format("cult.need_chapter", next.minChapter)
                : "";
        }

        void Bar(ProgressBarView bar, string nameKey, double have, double need)
            => bar.Set(need <= 0 ? 1f : (float)System.Math.Min(1.0, have / need), Loc.Format("cult.bar", Loc.Get(nameKey), UiKit.Short(have), UiKit.Short(need)));

        void AskBreakthrough()
        {
            var next = G.Cultivation.Next;
            if (next == null || G.Cultivation.Check() != BreakthroughBlock.None) return;
            G.Ui.Confirm.Show(
                Loc.Format("cult.confirm", Loc.Get("realm." + next.id), UiKit.Short(next.linhKhiCost), Loc.Get("res.linh_khi"),
                    UiKit.Short(next.linhThachCost), Loc.Get("res.linh_thach"), next.attackMultiplier.ToString("0.##"), next.hpMultiplier.ToString("0.##")),
                Loc.Get("btn.breakthrough"), Confirmed, Loc.Get("btn.cancel"), null, true);
        }

        void Confirmed()
        {
            if (!G.Cultivation.TryBreakthrough()) return;
            var now = G.Cultivation.Current;
            G.Ui.Confirm.Show(Loc.Format("cult.success", Loc.Get("realm." + now.id), now.attackMultiplier.ToString("0.##"), now.hpMultiplier.ToString("0.##")),
                Loc.Get("btn.ok"), null);
            Render();
        }
    }
}
