using System.Globalization;
using System.Text;
using PixelImmortal.Data;
using PixelImmortal.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Skill grid plus detail and upgrade. Skills fire automatically in battle, so there is deliberately no cast button.
    /// </summary>
    public class SkillsScreen : FrameScreen
    {
        readonly StringBuilder sb = new StringBuilder();
        SlotGridView grid;
        Text nameText, infoText, buttonLabel;
        Button upgradeButton;
        int selected;

        protected override void Build(RectTransform c)
        {
            grid = new SlotGridView(c, new Vector2(0.02f, 0.72f), new Vector2(0.98f, 0.99f), 5, 1, i => selected = i);
            nameText = Body(c, 0.04f, 0.6f, 0.96f, 0.7f, 36, TextAnchor.MiddleLeft);
            nameText.color = UiTheme.Gold;
            infoText = Body(c, 0.04f, 0.14f, 0.96f, 0.6f, 28);
            upgradeButton = Action(c, 0.15f, 0.01f, 0.85f, 0.12f, UiTheme.Action, UpgradeSelected, out buttonLabel);
        }

        protected override void Render()
        {
            var skills = G.Progress.Economy.skills;
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, skills.Length - 1));
            grid.Refresh(skills.Length, selected, (cell, i) =>
            {
                var s = skills[i];
                bool unlocked = G.Progress.IsSkillUnlocked(s);
                cell.Set(s.vfxColor, "", UiTheme.Row, unlocked ? Loc.Format("skill.level_badge", G.Progress.GetSkillLevel(s.id)) : Loc.Get("btn.locked"), i == selected, !unlocked, UiKit.Skin?.SkillIcon(s.id));
            });
            if (skills.Length == 0) return;

            var skill = skills[selected];
            nameText.text = Loc.Get("skill." + skill.id);
            bool open = G.Progress.IsSkillUnlocked(skill);
            int level = G.Progress.GetSkillLevel(skill.id);
            sb.Length = 0;

            if (!open)
            {
                sb.Append(Loc.Format("skill.unlock_at", Loc.Get("realm." + G.Progress.Economy.realms[skill.unlockRealmIndex].id)));
                buttonLabel.text = Loc.Get("btn.locked");
                upgradeButton.interactable = false;
            }
            else
            {
                bool shield = skill.kind == SkillKind.SelfShield;
                sb.Append(Loc.Format("skill.cooldown", skill.cooldown.ToString("0.#", CultureInfo.InvariantCulture))).Append('\n');
                sb.Append(Loc.Format("skill.now", Effect(skill, level))).Append('\n');
                if (G.Upgrades.IsSkillMaxed(skill))
                {
                    sb.Append(Loc.Get("skill.max_level"));
                    buttonLabel.text = Loc.Get("btn.max");
                    upgradeButton.interactable = false;
                }
                else
                {
                    sb.Append(Loc.Format("skill.next", shield ? Loc.Get("skill.no_scaling") : Effect(skill, level + 1))).Append('\n');
                    double cost = G.Upgrades.SkillCost(skill);
                    double have = G.Progress.Wallet.Get(ResourceType.TinhHoa);
                    if (have < cost) sb.Append(Loc.Format("skill.short", UiKit.Short(cost - have), Loc.Get("res.tinh_hoa")));
                    buttonLabel.text = Loc.Format("btn.upgrade_cost", UiKit.Short(cost), Loc.Get("res.short.tinh_hoa"));
                    upgradeButton.interactable = G.Upgrades.CanUpgradeSkill(skill);
                }
                sb.Append('\n').Append(Loc.Get("skill.auto"));
            }
            infoText.text = sb.ToString();
        }

        string Effect(SkillDefinition s, int level)
            => s.kind == SkillKind.SelfShield
                ? Loc.Format("skill.effect_shield", (s.shieldReduction * 100f).ToString("0"), s.shieldSeconds.ToString("0.#"))
                : Loc.Format("skill.effect_damage", UpgradeSystem.SkillMultiplierAt(s, level).ToString("0.00", CultureInfo.InvariantCulture));

        void UpgradeSelected()
        {
            var skills = G.Progress.Economy.skills;
            if (selected < skills.Length) G.Upgrades.TryUpgradeSkill(skills[selected]);
            Render();
        }
    }
}
