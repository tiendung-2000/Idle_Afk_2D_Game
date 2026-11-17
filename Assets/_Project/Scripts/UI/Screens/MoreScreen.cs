using UnityEngine;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Hub for the secondary systems that exist in the game today. Achievements, daily login, mail and help are
    /// not implemented in the game rules yet, so they are intentionally not listed.
    /// </summary>
    public class MoreScreen : FrameScreen
    {
        static readonly string[] Targets = { ScreenIds.Quests, ScreenIds.Stage, ScreenIds.Settings };
        static readonly string[] Keys = { "more.quests", "more.stage", "more.settings" };
        RowListView list;

        protected override void Build(RectTransform c)
            => list = new RowListView(c, new Vector2(0f, 0.01f), new Vector2(1f, 0.99f), i => G.Ui.Navigator.Replace(Targets[i]));

        protected override void Render()
        {
            list.Refresh(Targets.Length, (i, row) =>
            {
                string text = Loc.Get(Keys[i]);
                if (Targets[i] == ScreenIds.Quests && G.Quests.ClaimableCount > 0)
                    text += "  " + Loc.Format("more.claimable", G.Quests.ClaimableCount);
                row.Set(text, Loc.Get("btn.open"), true);
            });
        }
    }
}
