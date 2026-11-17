using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelImmortal.UI
{
    /// <summary>Forwards the Back input (Android back button arrives as Escape) to the <see cref="UIManager"/>.</summary>
    public class UIBackHandler : MonoBehaviour
    {
        UIManager ui;

        public void Init(UIManager manager) => ui = manager;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (ui != null && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ui.HandleBack();
        }
    }
}
