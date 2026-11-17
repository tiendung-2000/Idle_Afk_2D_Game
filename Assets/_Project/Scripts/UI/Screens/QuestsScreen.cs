using System.Text;
using PixelImmortal.Data;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Daily quests: progress, reward, Claim per quest and Claim All. Each reward is claimed once by the quest system.
    /// Only daily quests exist in the game rules, so there are no Main or Side tabs. Resets at device-local midnight.
    /// </summary>
    public class QuestsScreen : FrameScreen
    {
        readonly StringBuilder sb = new StringBuilder();
        RowListView list;
        Text footer, claimAllLabel;
        Button claimAll;

        protected override void Build(RectTransform c)
        {
            list = new RowListView(c, new Vector2(0f, 0.2f), new Vector2(1f, 0.99f), i => Claim(i));
            footer = Body(c, 0.03f, 0.1f, 0.97f, 0.2f, 24, TextAnchor.MiddleCenter);
            footer.color = UiTheme.Gold;
            claimAll = Action(c, 0.2f, 0.01f, 0.8f, 0.1f, UiTheme.Action, ClaimAll, out claimAllLabel);
        }

        public override void OnExit() => G.SaveNow?.Invoke();

        void Claim(int i)
        {
            var q = G.Quests.Quests;
            if (i < q.Length) G.Quests.TryClaim(q[i]);
            Render();
        }

        void ClaimAll()
        {
            foreach (var q in G.Quests.Quests) G.Quests.TryClaim(q);
            Render();
        }

        protected override void Render()
        {
            var quests = G.Quests.Quests;
            list.Refresh(quests.Length, (i, row) =>
            {
                var q = quests[i];
                string text = Loc.Format("quest.row", Loc.Get("quest." + q.id), Loc.Format("quest.objective." + q.objective, UiKit.Short(q.targetCount)),
                    UiKit.Short(G.Quests.GetProgress(q)), UiKit.Short(q.targetCount), RewardText(q.reward));
                if (G.Quests.IsClaimed(q)) row.Set(text, Loc.Get("quest.status.claimed"), false);
                else if (G.Quests.CanClaim(q)) row.Set(text, Loc.Get("quest.status.claim"), true);
                else row.Set(text, Loc.Get("quest.status.progress"), false);
            });
            var left = G.Quests.TimeUntilReset;
            footer.text = Loc.Format("quest.footer", Loc.Format("time.hm", (int)left.TotalHours, left.Minutes));
            claimAll.gameObject.SetActive(G.Quests.ClaimableCount > 0);
            claimAllLabel.text = Loc.Format("quest.claim_all", G.Quests.ClaimableCount);
        }

        string RewardText(RewardEntry r)
        {
            sb.Length = 0;
            Part(r.linhThach, "res.short.linh_thach");
            Part(r.linhKhi, "res.short.linh_khi");
            Part(r.tinhHoa, "res.short.tinh_hoa");
            return sb.ToString();
        }

        void Part(double amount, string shortKey)
        {
            if (amount <= 0) return;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(Loc.Format("quest.reward_part", UiKit.Short(amount), Loc.Get(shortKey)));
        }
    }
}
