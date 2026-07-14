using UnityEngine;
using UnityEditor;

/// <summary>
/// ビルドサイズ最適化: テクスチャと音声の圧縮設定を一括適用する。
/// Tools > デバッグ > ビルドサイズ最適化を実行
///
/// - テクスチャ: Android 向けに ASTC 6x6 圧縮（イラスト系。アニメ調なら画質劣化はほぼ不可視）
/// - 音声: BGM/ボイスを Vorbis 品質50%（ストリーミング再生）
/// 適用後は再インポートが走るため、アセット数によっては数分かかる。
/// </summary>
public static class BuildSizeOptimizer
{
    [MenuItem("Tools/デバッグ/ビルドサイズ最適化を実行")]
    public static void Optimize()
    {
        if (!EditorUtility.DisplayDialog("ビルドサイズ最適化",
            "全テクスチャに ASTC 8x8 圧縮、BGM に Vorbis 品質40%（その他音声は50%）を適用します。\n" +
            "再インポートに数分かかることがあります。実行しますか？",
            "実行する", "キャンセル"))
            return;

        int texCount = 0, audioCount = 0;

        // ---- テクスチャ: Android オーバーライドで ASTC 8x8 ----
        // （Unity の Android デフォルトが ASTC 6x6 相当のため、6x6 指定では削減にならない）
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            var android = importer.GetPlatformTextureSettings("Android");
            // 既に同じ設定ならスキップ（無駄な再インポート防止）
            if (android.overridden && android.format == TextureImporterFormat.ASTC_8x8)
                continue;

            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_8x8;
            android.maxTextureSize = Mathf.Min(importer.maxTextureSize, 2048);
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            texCount++;
        }

        // ---- 音声: Vorbis 品質 0.5 ----
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;

            var settings = importer.defaultSampleSettings;
            bool isBGM = path.Contains("/BGM/");
            // BGM は曲数が多くサイズ支配的なので一段強めに圧縮
            float targetQuality = isBGM ? 0.4f : 0.5f;

            if (settings.compressionFormat == AudioCompressionFormat.Vorbis
                && Mathf.Abs(settings.quality - targetQuality) < 0.01f
                && (!isBGM || settings.loadType == AudioClipLoadType.Streaming))
                continue;

            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = targetQuality;
            // BGM は長尺なのでストリーミング再生（メモリ節約）、SE/ボイスは既定のまま
            if (isBGM) settings.loadType = AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            audioCount++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("ビルドサイズ最適化",
            $"完了しました。\nテクスチャ: {texCount}件\n音声: {audioCount}件\n\n再度 AAB をビルドしてサイズを確認してください。",
            "OK");
        Debug.Log($"[BuildSizeOptimizer] テクスチャ{texCount}件 / 音声{audioCount}件 に圧縮設定を適用");
    }
}
