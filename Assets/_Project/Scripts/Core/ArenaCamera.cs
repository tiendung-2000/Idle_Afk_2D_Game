using System;
using UnityEngine;

namespace PixelImmortal.Core
{
    /// <summary>
    /// Fits the fixed arena into a viewport sub-rect so HUD bars sit outside the combat area.
    /// The viewport is re-read when the screen size or safe area changes (rotation, notches).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ArenaCamera : MonoBehaviour
    {
        const float Margin = 0.3f;

        Camera cam;
        Rect arena;
        Func<Rect> viewportProvider;
        Rect lastSafe;
        int lastW, lastH;

        public void Configure(Rect arenaRect, Func<Rect> viewport)
        {
            cam = GetComponent<Camera>();
            arena = arenaRect;
            viewportProvider = viewport;
            cam.orthographic = true;
            Fit();
        }

        void LateUpdate()
        {
            if (cam != null && (Screen.width != lastW || Screen.height != lastH || Screen.safeArea != lastSafe)) Fit();
        }

        void Fit()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            lastSafe = Screen.safeArea;
            var viewport = viewportProvider();
            cam.rect = viewport;
            float aspect = (Screen.width * viewport.width) / Mathf.Max(1f, Screen.height * viewport.height);
            float halfH = arena.height * 0.5f + Margin;
            float halfW = arena.width * 0.5f + Margin;
            cam.orthographicSize = Mathf.Max(halfH, halfW / aspect);
            transform.position = new Vector3(arena.center.x, arena.center.y, -10f);
        }
    }
}
