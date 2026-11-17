using PixelImmortal.Combat;
using PixelImmortal.Cultivation;
using PixelImmortal.Economy;
using PixelImmortal.Equipment;
using PixelImmortal.Localization;
using PixelImmortal.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    public static class ScreenIds
    {
        public const string Character = "character";
        public const string Cultivation = "cultivation";
        public const string Skills = "skills";
        public const string Equipment = "equipment";
        public const string Inventory = "inventory";
        public const string Summon = "summon";
        public const string Upgrades = "upgrades";
        public const string Quests = "quests";
        public const string More = "more";
        public const string Stage = "stage";
        public const string Settings = "settings";
    }

    /// <summary>What feature screens may read and call. Screens never touch save data or compute game rules themselves.</summary>
    public sealed class GameContext
    {
        public UIManager Ui;
        public LocalizationService Loc;
        public PlayerProgress Progress;
        public UpgradeSystem Upgrades;
        public CultivationSystem Cultivation;
        public EquipmentSystem Equipment;
        public PixelImmortal.Gacha.GachaSystem Gacha;
        public PixelImmortal.Quests.QuestSystem Quests;
        public PixelImmortal.Progression.BattleDirector Director;
        public PixelImmortal.Audio.AudioService Audio;
        /// <summary>Set after the save coordinator exists.</summary>
        public System.Action SaveNow;
        /// <summary>Moves the save aside and restarts the game from scratch. Set after the save coordinator exists.</summary>
        public System.Action ResetData;
        public System.Func<PlayerStats> Stats;
    }

    /// <summary>
    /// A feature screen with a title bar and close button over the arena area, between the HUD's top and bottom bars.
    /// Subclasses build once into <see cref="Content"/> and redraw from domain state in <see cref="Render"/>,
    /// which runs on enter, on return, on language change and ten times a second while visible.
    /// </summary>
    public abstract class FrameScreen : UIScreen
    {
        const float Interval = 0.1f;
        const float HeaderBottom = 0.915f;

        float nextRender;

        protected GameContext G { get; private set; }
        protected LocalizationService Loc => G.Loc;
        protected RectTransform Content { get; private set; }

        public static T Create<T>(RectTransform parent, GameContext g, string titleKey) where T : FrameScreen
        {
            var rt = UiKit.Panel(typeof(T).Name, parent, new Vector2(0.03f, 0.17f), new Vector2(0.97f, 0.85f), UiTheme.Panel, true);
            UiKit.AddFrame(rt);
            var screen = rt.gameObject.AddComponent<T>();
            screen.G = g;

            var title = UiKit.Label(rt, 0.04f, HeaderBottom, 0.78f, 0.995f, 36, TextAnchor.MiddleLeft);
            title.color = UiTheme.Gold;
            LocalizedLabel.Bind(title, g.Loc, titleKey);
            UiKit.TextButton("Close", rt, new Vector2(0.82f, HeaderBottom + 0.005f), new Vector2(0.98f, 0.995f), UiTheme.Danger, "X", 32, () => g.Ui.Navigator.Pop(), out _);

            var content = UiKit.Panel("Content", rt, Vector2.zero, new Vector2(1f, HeaderBottom - 0.005f), new Color(0f, 0f, 0f, 0f));
            content.GetComponent<Image>().raycastTarget = false;
            screen.Content = content;
            g.Loc.LanguageChanged += screen.Render;
            screen.Build(content);
            return screen;
        }

        protected abstract void Build(RectTransform content);

        protected abstract void Render();

        protected virtual void OnOpen(object args) { }

        public override void OnEnter(object args)
        {
            OnOpen(args);
            Render();
        }

        public override void OnRefresh() => Render();

        protected override void OnTick()
        {
            if (Time.unscaledTime < nextRender) return;
            nextRender = Time.unscaledTime + Interval;
            Render();
        }

        void OnDestroy()
        {
            if (G != null && G.Loc != null) G.Loc.LanguageChanged -= Render;
        }

        // ---- small builders shared by the screens ------------------------------------------------

        protected static Text Body(RectTransform parent, float x0, float y0, float x1, float y1, int size, TextAnchor anchor = TextAnchor.UpperLeft)
            => UiKit.Label(parent, x0, y0, x1, y1, size, anchor);

        protected static Button Action(RectTransform parent, float x0, float y0, float x1, float y1, Color color, UnityEngine.Events.UnityAction onClick, out Text label)
            => UiKit.TextButton("Action", parent, new Vector2(x0, y0), new Vector2(x1, y1), color, "", 30, onClick, out label);
    }
}
