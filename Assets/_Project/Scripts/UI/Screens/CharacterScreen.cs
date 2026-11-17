using System.Globalization;
using PixelImmortal.Data;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>Realm and current combat stats, with links to Cultivation, Skills and Equipment. Combat power is not defined by the game rules, so none is shown.</summary>
    public class CharacterScreen : FrameScreen
    {
        const int StatRows = 9;
        readonly Text[] names = new Text[StatRows];
        readonly Text[] values = new Text[StatRows];
        Text realmText;

        protected override void Build(RectTransform c)
        {
            var portrait = UiKit.Skin?.Portrait("player");
            UiKit.AddIcon(c, portrait, 0.04f, 0.86f, 0.2f, 0.99f);
            realmText = Body(c, portrait != null ? 0.22f : 0.04f, 0.86f, 0.96f, 0.99f, 36, TextAnchor.MiddleCenter);
            realmText.color = UiTheme.Gold;

            float top = 0.84f, h = 0.075f;
            for (int i = 0; i < StatRows; i++)
            {
                var row = UiKit.Panel("Stat" + i, c, new Vector2(0.03f, top - (i + 1) * h + 0.005f), new Vector2(0.97f, top - i * h - 0.005f), UiTheme.Row, true);
                names[i] = UiKit.Label(row, 0.03f, 0f, 0.6f, 1f, 28, TextAnchor.MiddleLeft);
                values[i] = UiKit.Label(row, 0.55f, 0f, 0.97f, 1f, 28, TextAnchor.MiddleRight);
            }

            Link(0.03f, 0.31f, ScreenIds.Cultivation, "btn.cultivate");
            Link(0.345f, 0.655f, ScreenIds.Skills, "btn.skills");
            Link(0.69f, 0.97f, ScreenIds.Equipment, "btn.equipment");
        }

        void Link(float x0, float x1, string id, string key)
        {
            UiKit.TextButton("Link" + id, Content, new Vector2(x0, 0.01f), new Vector2(x1, 0.1f), UiTheme.TabOff, "", 28, () => G.Ui.Navigator.Replace(id), out var label);
            Localization.LocalizedLabel.Bind(label, Loc, key);
        }

        protected override void Render()
        {
            var s = G.Stats();
            var eco = G.Progress.Economy;
            realmText.text = Loc.Format("char.realm", Loc.Get("realm." + G.Cultivation.Current.id), G.Progress.HighestChapter);

            Set(0, Loc.Get("stat.Attack"), UiKit.Short(s.Attack));
            Set(1, Loc.Get("stat.MaxHp"), UiKit.Short(s.MaxHp));
            Set(2, Loc.Get("stat.AttackSpeed"), Loc.Format("char.per_second", (1f / s.AttackInterval).ToString("0.00", CultureInfo.InvariantCulture)));
            Set(3, Loc.Get("stat.CritChance"), Pct(s.CritChance));
            Set(4, Loc.Get("char.crit_mult"), "x" + s.CritMultiplier.ToString("0.##", CultureInfo.InvariantCulture));
            Set(5, Loc.Get("stat.SkillDamage"), "x" + s.SkillDamageMultiplier.ToString("0.##", CultureInfo.InvariantCulture));
            Set(6, Loc.Get("char.damage_reduction"), Pct(s.DamageReduction));
            Set(7, Loc.Get("stat.OfflineEfficiency"), Pct(s.OfflineEfficiency));
            Set(8, Loc.Get("char.max_offline"), Loc.Format("time.hm", (int)eco.offlineMaxHours, 0));
        }

        void Set(int i, string name, string value)
        {
            names[i].text = name;
            values[i].text = value;
        }

        static string Pct(float v) => (v * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
    }
}
