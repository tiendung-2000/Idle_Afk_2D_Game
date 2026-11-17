using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelImmortal.EditorTools
{
    /// <summary>
    /// Import rules for everything under Assets/_Project/Art: sprites, point filtering, no mipmaps, no compression.
    /// World art is 32 px per unit; UI art is 16 px per unit (so one texel is a chunky ~6 canvas pixels).
    /// Applies to generated placeholder art and to any final art dropped into the folder later.
    /// </summary>
    public class PixelArtImportSettings : AssetPostprocessor
    {
        const string Root = "Assets/_Project/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = false;
            ti.isReadable = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.npotScale = TextureImporterNPOTScale.None;

            bool ui = assetPath.Contains("/UI/") || assetPath.Contains("/Portraits/") || assetPath.Contains("/Icons/");
            ti.spritePixelsPerUnit = ui ? 16f : 32f;

            string name = Path.GetFileNameWithoutExtension(assetPath);
            if (name == "ui_panel" || name == "ui_frame") ti.spriteBorder = new Vector4(8, 8, 8, 8);
            else if (name.StartsWith("ui_button")) ti.spriteBorder = new Vector4(4, 4, 4, 4);
        }
    }
}
