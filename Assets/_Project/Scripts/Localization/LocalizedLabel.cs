using UnityEngine;
using UnityEngine.UI;

namespace PixelImmortal.Localization
{
    /// <summary>Keeps a static UI label in sync with the current language.</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedLabel : MonoBehaviour
    {
        Text text;
        LocalizationService loc;
        string key;

        public static void Bind(Text target, LocalizationService service, string textKey)
        {
            var l = target.gameObject.GetComponent<LocalizedLabel>() ?? target.gameObject.AddComponent<LocalizedLabel>();
            l.Setup(target, service, textKey);
        }

        void Setup(Text t, LocalizationService service, string textKey)
        {
            if (loc != null) loc.LanguageChanged -= Refresh;
            text = t;
            loc = service;
            key = textKey;
            loc.LanguageChanged += Refresh;
            Refresh();
        }

        void Refresh() { if (text != null) text.text = loc.Get(key); }

        void OnDestroy() { if (loc != null) loc.LanguageChanged -= Refresh; }
    }
}
