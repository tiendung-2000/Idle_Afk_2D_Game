using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>Subtle press feedback: the button shrinks slightly while held. Disabled buttons do not react.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float Pressed = 0.95f;
        Selectable selectable;

        void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData e)
        {
            if (selectable == null || selectable.interactable) transform.localScale = new Vector3(Pressed, Pressed, 1f);
        }

        public void OnPointerUp(PointerEventData e) => Restore();

        public void OnPointerExit(PointerEventData e) => Restore();

        void OnDisable() => Restore();

        void Restore() => transform.localScale = Vector3.one;
    }
}
