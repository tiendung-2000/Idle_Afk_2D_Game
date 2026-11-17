using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    public enum ToastSeverity { Info, Success, Warning, Error }

    /// <summary>
    /// Owns the UI root: one canvas with ordered layers, the screen stack, the modal confirm popup, toasts and Back handling.
    /// Layers, bottom to top: Screen, HUD, Popup, Toast, Tutorial, Overlay. Screen/HUD/Toast/Tutorial sit inside the safe
    /// area; Popup and Overlay span the full screen so dimmers and fades cover notches too.
    /// The 1080x1920 reference is the spec's 270x480 at 4x, so the existing pixel sizes keep working.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public const float ToastSeconds = 2.5f;
        static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        readonly List<PopupBase> openPopups = new List<PopupBase>();
        readonly List<Func<bool>> backHandlers = new List<Func<bool>>();

        Image toastBack;
        Text toastText;
        float toastUntil;

        public RectTransform CanvasRect { get; private set; }
        public RectTransform ScreenLayer { get; private set; }
        public RectTransform HudLayer { get; private set; }
        public RectTransform PopupLayer { get; private set; }
        public RectTransform ToastLayer { get; private set; }
        public RectTransform TutorialLayer { get; private set; }
        public RectTransform OverlayLayer { get; private set; }

        public ScreenNavigator Navigator { get; private set; }

        /// <summary>The shared one/two-button modal. Reward and error flows reuse it.</summary>
        public PopupPanel Confirm { get; private set; }

        /// <summary>Back pressed with nothing to close (plain gameplay): the game decides, normally an exit confirmation.</summary>
        public Action BackAtRoot;

        public PopupBase TopPopup => openPopups.Count > 0 ? openPopups[openPopups.Count - 1] : null;
        public bool HasOpenPopup => openPopups.Count > 0;

        public static UIManager Create()
        {
            var ui = new GameObject("UIRoot").AddComponent<UIManager>();
            ui.Build();
            return ui;
        }

        void Build()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            CanvasRect = (RectTransform)canvasGo.transform;

            ScreenLayer = MakeLayer("ScreenLayer", true);
            HudLayer = MakeLayer("HUDLayer", true);
            PopupLayer = MakeLayer("PopupLayer", false);
            ToastLayer = MakeLayer("ToastLayer", true);
            TutorialLayer = MakeLayer("TutorialLayer", true);
            OverlayLayer = MakeLayer("OverlayLayer", false);

            Navigator = new ScreenNavigator(ScreenLayer);

            Confirm = new GameObject("ConfirmPopup").AddComponent<PopupPanel>();
            Confirm.transform.SetParent(transform, false);
            Confirm.Build(PopupLayer);
            RegisterPopup(Confirm);

            BuildToast();
            gameObject.AddComponent<UIBackHandler>().Init(this);
        }

        RectTransform MakeLayer(string name, bool safeArea)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(CanvasRect, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (safeArea) go.AddComponent<SafeAreaFitter>();
            return rt;
        }

        // ---- popups ---------------------------------------------------------------

        /// <summary>Tracks a popup so Back reaches the topmost open one first.</summary>
        public void RegisterPopup(PopupBase popup)
        {
            popup.Opened += p => { openPopups.Remove(p); openPopups.Add(p); };
            popup.Closed += p => openPopups.Remove(p);
        }

        // ---- back ---------------------------------------------------------------------

        /// <summary>Extra Back targets (an open menu, a tutorial). The most recently added is asked first; return true when it consumed the press.</summary>
        public void AddBackHandler(Func<bool> handler) => backHandlers.Add(handler);

        public void RemoveBackHandler(Func<bool> handler) => backHandlers.Remove(handler);

        /// <summary>Popup first, then registered handlers, then the screen stack, finally <see cref="BackAtRoot"/>.</summary>
        public bool HandleBack()
        {
            if (TopPopup != null) return TopPopup.HandleBack();
            for (int i = backHandlers.Count - 1; i >= 0; i--)
                if (backHandlers[i]()) return true;
            if (Navigator.Pop()) return true;
            if (BackAtRoot == null) return false;
            BackAtRoot();
            return true;
        }

        // ---- toast ----------------------------------------------------------------------

        void BuildToast()
        {
            toastBack = UiKit.Panel("Toast", ToastLayer, new Vector2(0.04f, 0.165f), new Vector2(0.96f, 0.205f), new Color(0f, 0f, 0f, 0.7f), true).GetComponent<Image>();
            toastBack.raycastTarget = false;
            toastText = UiKit.Label(toastBack.rectTransform, 0.02f, 0f, 0.98f, 1f, 28, TextAnchor.MiddleCenter);
            toastBack.gameObject.SetActive(false);
        }

        /// <summary>Non-blocking one-line feedback. A new toast replaces the current one. Not for rewards or confirmations.</summary>
        public void ShowToast(string text, ToastSeverity severity = ToastSeverity.Info)
        {
            toastText.text = text;
            toastText.color = severity == ToastSeverity.Success ? new Color(0.6f, 1f, 0.7f)
                : severity == ToastSeverity.Warning ? new Color(1f, 0.85f, 0.4f)
                : severity == ToastSeverity.Error ? new Color(1f, 0.5f, 0.45f)
                : Color.white;
            toastBack.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + ToastSeconds;
        }

        void Update()
        {
            if (toastBack != null && toastBack.gameObject.activeSelf && Time.unscaledTime >= toastUntil)
                toastBack.gameObject.SetActive(false);
        }
    }
}
