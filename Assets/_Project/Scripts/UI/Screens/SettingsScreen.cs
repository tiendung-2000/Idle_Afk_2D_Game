using UnityEngine;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Sound and language, plus the reset, kept apart and behind two confirmations. Only settings the game
    /// actually has are listed (no music, graphics or notification options exist yet).
    /// </summary>
    public class SettingsScreen : FrameScreen
    {
        RowListView list;

        protected override void Build(RectTransform c)
            => list = new RowListView(c, new Vector2(0f, 0.01f), new Vector2(1f, 0.99f), OnRow);

        protected override void Render()
        {
            list.Refresh(3, (i, row) =>
            {
                switch (i)
                {
                    case 0: row.Set(Loc.Get("settings.sound"), Loc.Get(G.Audio.Muted ? "settings.off" : "settings.on"), true); break;
                    case 1: row.Set(Loc.Get("settings.language"), Loc.Language.ToUpperInvariant(), true); break;
                    default: row.Set(Loc.Get("settings.reset"), Loc.Get("settings.reset_btn"), true); break;
                }
            });
        }

        void OnRow(int i)
        {
            if (i == 0) G.Audio.Toggle();
            else if (i == 1) Loc.Toggle();
            else AskReset();
            Render();
        }

        void AskReset()
        {
            G.Ui.Confirm.Show(Loc.Get("settings.reset_confirm1"), Loc.Get("btn.continue"), () =>
                G.Ui.Confirm.Show(Loc.Get("settings.reset_confirm2"), Loc.Get("settings.reset_final"), () => G.ResetData?.Invoke(),
                    Loc.Get("btn.cancel"), null, true),
                Loc.Get("btn.cancel"), null, true);
        }
    }
}
