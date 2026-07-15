using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Resources/UI/ 配下のPNGを自動で9スライス用スプライトとして設定する。
/// （btn_round / panel_round は角丸26px前後なので border=32 で安全に切る）
/// </summary>
public class UISpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');

        if (p.Contains("Assets/Resources/UI/"))
        {
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = new Vector4(32f, 32f, 32f, 32f); // L,B,R,T
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed; // 小さいUI素材は無圧縮で綺麗に
        }
        else if (p.Contains("Assets/Resources/Blocks/"))
        {
            // ブロックタイル: 256x128 を PPU128 で 2x1 ユニットに（旧 _temp スプライトと同サイズ）
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 128f;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
