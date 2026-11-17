using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.UI
{
    /// <summary>Modal message with one or two buttons (offline rewards, save problems, confirmations). Blocks input behind it.</summary>
    public class PopupPanel : PopupBase
    {
        Text message;
        Button button1, button2;
        Text label1, label2;
        Action action1, action2;
        bool backPressesSecond;

        public void Build(Transform layer)
        {
            var box = BuildFrame(layer, "Popup", new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.7f));
            message = UiKit.Label(box, 0.05f, 0.3f, 0.95f, 0.97f, 34, TextAnchor.MiddleCenter);
            button1 = UiKit.TextButton("Btn1", box, new Vector2(0.05f, 0.05f), new Vector2(0.48f, 0.25f), new Color(0.25f, 0.5f, 0.3f), "", 32, () => Click(1), out label1);
            button2 = UiKit.TextButton("Btn2", box, new Vector2(0.52f, 0.05f), new Vector2(0.95f, 0.25f), new Color(0.4f, 0.3f, 0.3f), "", 32, () => Click(2), out label2);
        }

        /// <summary>
        /// With one button it is centered; with two they sit side by side. Back is swallowed unless
        /// <paramref name="backPressesSecond"/> is set, in which case it acts as the second (cancel) button.
        /// </summary>
        public void Show(string text, string button1Text, Action onButton1, string button2Text = null, Action onButton2 = null, bool backPressesSecond = false)
        {
            message.text = text;
            label1.text = button1Text;
            action1 = onButton1;
            bool two = button2Text != null;
            this.backPressesSecond = two && backPressesSecond;
            button2.gameObject.SetActive(two);
            if (two)
            {
                label2.text = button2Text;
                action2 = onButton2;
            }
            var rt1 = (RectTransform)button1.transform;
            rt1.anchorMin = two ? new Vector2(0.05f, 0.05f) : new Vector2(0.25f, 0.05f);
            rt1.anchorMax = two ? new Vector2(0.48f, 0.25f) : new Vector2(0.75f, 0.25f);
            Open();
        }

        public override bool HandleBack()
        {
            if (backPressesSecond) Click(2);
            return true;
        }

        void Click(int which)
        {
            var a = which == 1 ? action1 : action2;
            Close();
            a?.Invoke();
        }
    }
}
