using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>
    /// Shared modal frame: full-screen dimmer that blocks input, a centered box and close rules.
    /// Critical popups (reward claims, destructive confirmations) ignore outside taps and Back.
    /// </summary>
    public abstract class PopupBase : MonoBehaviour
    {
        const float OpenSeconds = 0.12f;

        RectTransform root;
        CanvasGroup fade;
        float anim = 1f;

        /// <summary>The popup's content box. Build children into this.</summary>
        protected RectTransform Box { get; private set; }

        public bool IsOpen => root != null && root.gameObject.activeSelf;

        /// <summary>Tapping the dimmer closes the popup. Off for anything critical.</summary>
        public virtual bool CloseOnDimmerTap => false;

        /// <summary>Back closes the popup. Off for anything critical; Back is then swallowed, never passed to the screen below.</summary>
        public virtual bool CloseOnBack => false;

        public event Action<PopupBase> Opened;
        public event Action<PopupBase> Closed;

        protected RectTransform BuildFrame(Transform layer, string name, Vector2 boxMin, Vector2 boxMax)
        {
            root = UiKit.Panel(name, layer, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.7f));
            var dimmer = root.gameObject.AddComponent<Button>();
            dimmer.transition = Selectable.Transition.None;
            dimmer.onClick.AddListener(() => { if (CloseOnDimmerTap) Close(); });

            Box = UiKit.Panel("Box", root, boxMin, boxMax, new Color(0.1f, 0.09f, 0.14f, 1f), true);
            UiKit.AddFrame(Box);
            root.gameObject.SetActive(false);
            return Box;
        }

        protected void Open()
        {
            bool wasOpen = IsOpen;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            if (!wasOpen)
            {
                anim = 0f;
                ApplyAnim();
                Opened?.Invoke(this);
            }
        }

        /// <summary>Short fade and scale on open (unscaled time, so fast-forward does not change it). Content is usable immediately.</summary>
        void Update()
        {
            if (anim >= 1f || !IsOpen) return;
            anim = Mathf.Min(1f, anim + Time.unscaledDeltaTime / OpenSeconds);
            ApplyAnim();
        }

        void ApplyAnim()
        {
            if (fade == null && !root.TryGetComponent(out fade)) fade = root.gameObject.AddComponent<CanvasGroup>();
            float eased = 1f - (1f - anim) * (1f - anim);
            fade.alpha = eased;
            float s = Mathf.Lerp(0.92f, 1f, eased);
            Box.localScale = new Vector3(s, s, 1f);
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.gameObject.SetActive(false);
            Closed?.Invoke(this);
        }

        /// <summary>Returns true when the press was consumed, which is always, so Back never leaks past an open popup.</summary>
        public virtual bool HandleBack()
        {
            if (CloseOnBack) Close();
            return true;
        }
    }
}
