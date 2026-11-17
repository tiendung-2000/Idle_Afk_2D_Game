using PixelImmortal.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Current stage, wave progress, Boss status with the manual retry, and the map list of the current cycle.
    /// Maps unlock strictly in order by defeating each Boss, so locked maps only explain that requirement.
    /// Recommended power is not defined by the game rules and is not shown.
    /// </summary>
    public class StageScreen : FrameScreen
    {
        Text headerText, stateText, bossText, noteText, retryLabel;
        Image bossPortrait;
        ProgressBarView waveBar;
        GameObject retryObject;
        RowListView maps;

        protected override void Build(RectTransform c)
        {
            headerText = Body(c, 0.04f, 0.84f, 0.96f, 0.99f, 32, TextAnchor.MiddleCenter);
            headerText.color = UiTheme.Gold;
            waveBar = new ProgressBarView(c, new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.83f), new Color(0.3f, 0.65f, 0.9f));
            // Boss portrait is chosen at Render time (it follows the current map); the text shifts right only when art exists.
            bool hasPortrait = UiKit.Skin != null && UiKit.Skin.Portrait("boss." + G.Director.Map.mapId) != null;
            float textX = hasPortrait ? 0.26f : 0.04f;
            stateText = Body(c, textX, 0.66f, 0.96f, 0.74f, 28, TextAnchor.MiddleCenter);
            bossText = Body(c, textX, 0.56f, 0.96f, 0.66f, 28, TextAnchor.MiddleCenter);
            if (hasPortrait)
            {
                bossPortrait = UiKit.AddIcon(c, UiKit.Skin.Portrait("boss." + G.Director.Map.mapId), 0.04f, 0.555f, 0.24f, 0.745f);
            }
            var retry = Action(c, 0.2f, 0.45f, 0.8f, 0.55f, UiTheme.Danger, () => G.Director.RequestBoss(), out retryLabel);
            retryObject = retry.gameObject;
            noteText = Body(c, 0.04f, 0.35f, 0.96f, 0.45f, 24, TextAnchor.MiddleCenter);
            maps = new RowListView(c, new Vector2(0f, 0.01f), new Vector2(1f, 0.34f), _ => { });
        }

        protected override void Render()
        {
            var d = G.Director;
            headerText.text = Loc.Format("hud.title", d.Chapter, d.Cycle, Loc.Get("map." + d.Map.mapId));
            waveBar.Set(Mathf.Clamp01(d.Wave / (float)Mathf.Max(1, d.WavesPerMap)), Loc.Format("stage.wave", d.Wave, d.WavesPerMap));
            stateText.text = d.State switch
            {
                BattleState.NormalWave => Loc.Get("state.farming"),
                BattleState.WaveTransition => Loc.Get("state.wave_cleared"),
                BattleState.BossReady => Loc.Format("state.boss_ready", d.Wave),
                BattleState.BossCombat => Loc.Get("state.boss_fight"),
                BattleState.BossResult => Loc.Get("state.boss_win"),
                _ => Loc.Get("state.travel")
            };
            bossText.text = Loc.Format("stage.boss", Loc.Get("boss." + d.Map.mapId), d.BossesDefeated, d.RewardMultiplier.ToString("0.##"));

            if (bossPortrait != null)
            {
                var portrait = UiKit.Skin.Portrait("boss." + d.Map.mapId);
                if (portrait != null) bossPortrait.sprite = portrait;
            }

            bool ready = d.State == BattleState.BossReady;
            retryObject.SetActive(ready);
            retryLabel.text = Loc.Get("btn.challenge_boss");
            noteText.text = Loc.Format("stage.retry_note", d.WavesPerMap);

            var all = d.Maps;
            maps.Refresh(all.Length, (i, row) =>
            {
                int slot = i + 1; // 1-based position in the cycle
                string name = Loc.Get("map." + all[i].mapId);
                string status = slot < d.MapNumber ? Loc.Get("stage.map_cleared")
                    : slot == d.MapNumber ? Loc.Get("stage.map_current")
                    : Loc.Get("stage.map_locked");
                row.SetInfo(Loc.Format("stage.map_row", slot, name, status));
            });
        }
    }
}
