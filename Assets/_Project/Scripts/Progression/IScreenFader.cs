using System.Collections;

namespace PixelImmortal.Progression
{
    public interface IScreenFader
    {
        IEnumerator FadeTo(float alpha, float seconds);

        /// <summary>Called while the screen is dark so the new map name can be shown.</summary>
        void ShowMapTitle();
    }
}
