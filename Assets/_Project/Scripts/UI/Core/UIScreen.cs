using UnityEngine;

namespace PixelImmortal.UI
{
    /// <summary>Base for full feature screens (Character, Skills, ...). Shown and hidden by <see cref="ScreenNavigator"/>.</summary>
    public abstract class UIScreen : MonoBehaviour
    {
        public const float FadeSeconds = 0.15f;

        CanvasGroup group;
        float fade = 1f;

        /// <summary>Id the screen was registered under.</summary>
        public string Id { get; internal set; }

        /// <summary>Called once when the screen is pushed or replaced in.</summary>
        public virtual void OnEnter(object args) { }

        /// <summary>Called when the screen becomes visible again after a screen above it was popped.</summary>
        public virtual void OnRefresh() { }

        /// <summary>Called once when the screen leaves the stack.</summary>
        public virtual void OnExit() { }

        CanvasGroup Group
        {
            get
            {
                if (group == null)
                {
                    group = GetComponent<CanvasGroup>();
                    if (group == null) group = gameObject.AddComponent<CanvasGroup>();
                }
                return group;
            }
        }

        internal void Show()
        {
            gameObject.SetActive(true);
            fade = 0f;
            Group.alpha = 0f;
        }

        internal void Hide() => gameObject.SetActive(false);

        /// <summary>Called every frame while the screen is visible.</summary>
        protected virtual void OnTick() { }

        void Update()
        {
            OnTick();
            if (fade >= 1f) return;
            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / FadeSeconds);
            Group.alpha = fade;
        }
    }
}
