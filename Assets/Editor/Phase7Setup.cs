#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Phase 7: 20ステージ自動生成 + Android ビルド設定
/// </summary>
public static class Phase7Setup
{
    static readonly string StageFolder = "Assets/Resources/Stages";

    // ---- ブロック種別ショートカット ----
    static readonly BlockType N = BlockType.Normal;
    static readonly BlockType D = BlockType.Durable;
    static readonly BlockType E = BlockType.Explosion;
    static readonly BlockType C = BlockType.Chain;

    // ---- キャラカラーセット ----
    struct CharColors
    {
        public string name;
        public Color c30, c60, cFull;
    }

    static CharColors[] charSets =
    {
        // Stage 1-5
        new CharColors { name = "Luna",   c30 = new Color(0.5f, 0.2f, 0.3f), c60 = new Color(0.7f, 0.3f, 0.5f), cFull = new Color(1.0f, 0.7f, 0.85f) },
        new CharColors { name = "Aria",   c30 = new Color(0.2f, 0.2f, 0.5f), c60 = new Color(0.3f, 0.3f, 0.7f), cFull = new Color(0.6f, 0.7f, 1.0f) },
        new CharColors { name = "Sera",   c30 = new Color(0.2f, 0.4f, 0.2f), c60 = new Color(0.3f, 0.6f, 0.3f), cFull = new Color(0.6f, 1.0f, 0.7f) },
        new CharColors { name = "Riko",   c30 = new Color(0.5f, 0.2f, 0.1f), c60 = new Color(0.7f, 0.3f, 0.1f), cFull = new Color(1.0f, 0.6f, 0.4f) },
        new CharColors { name = "Mika",   c30 = new Color(0.4f, 0.1f, 0.5f), c60 = new Color(0.6f, 0.2f, 0.7f), cFull = new Color(0.9f, 0.5f, 1.0f) },
        // Stage 6-10
        new CharColors { name = "Hana",   c30 = new Color(0.5f, 0.4f, 0.1f), c60 = new Color(0.7f, 0.6f, 0.1f), cFull = new Color(1.0f, 0.9f, 0.4f) },
        new CharColors { name = "Yuki",   c30 = new Color(0.2f, 0.4f, 0.5f), c60 = new Color(0.3f, 0.6f, 0.7f), cFull = new Color(0.5f, 0.9f, 1.0f) },
        new CharColors { name = "Nana",   c30 = new Color(0.4f, 0.2f, 0.4f), c60 = new Color(0.6f, 0.3f, 0.6f), cFull = new Color(0.9f, 0.6f, 0.9f) },
        new CharColors { name = "Sora",   c30 = new Color(0.1f, 0.3f, 0.5f), c60 = new Color(0.2f, 0.5f, 0.7f), cFull = new Color(0.4f, 0.8f, 1.0f) },
        new CharColors { name = "Rei",    c30 = new Color(0.5f, 0.1f, 0.2f), c60 = new Color(0.7f, 0.1f, 0.3f), cFull = new Color(1.0f, 0.3f, 0.5f) },
        // Stage 11-15
        new CharColors { name = "Rin",    c30 = new Color(0.1f, 0.5f, 0.4f), c60 = new Color(0.2f, 0.7f, 0.6f), cFull = new Color(0.4f, 1.0f, 0.9f) },
        new CharColors { name = "Ai",     c30 = new Color(0.5f, 0.3f, 0.1f), c60 = new Color(0.7f, 0.5f, 0.1f), cFull = new Color(1.0f, 0.8f, 0.3f) },
        new CharColors { name = "Kana",   c30 = new Color(0.3f, 0.1f, 0.5f), c60 = new Color(0.5f, 0.1f, 0.7f), cFull = new Color(0.8f, 0.4f, 1.0f) },
        new CharColors { name = "Mei",    c30 = new Color(0.1f, 0.5f, 0.2f), c60 = new Color(0.2f, 0.7f, 0.3f), cFull = new Color(0.4f, 1.0f, 0.6f) },
        new CharColors { name = "Noa",    c30 = new Color(0.5f, 0.5f, 0.1f), c60 = new Color(0.7f, 0.7f, 0.1f), cFull = new Color(1.0f, 1.0f, 0.4f) },
        // Stage 16-20
        new CharColors { name = "Ruka",   c30 = new Color(0.5f, 0.1f, 0.5f), c60 = new Color(0.7f, 0.1f, 0.7f), cFull = new Color(1.0f, 0.3f, 1.0f) },
        new CharColors { name = "Saki",   c30 = new Color(0.1f, 0.4f, 0.5f), c60 = new Color(0.1f, 0.6f, 0.7f), cFull = new Color(0.2f, 0.9f, 1.0f) },
        new CharColors { name = "Yuna",   c30 = new Color(0.5f, 0.3f, 0.3f), c60 = new Color(0.7f, 0.5f, 0.4f), cFull = new Color(1.0f, 0.8f, 0.7f) },
        new CharColors { name = "Tomo",   c30 = new Color(0.2f, 0.5f, 0.3f), c60 = new Color(0.3f, 0.7f, 0.4f), cFull = new Color(0.5f, 1.0f, 0.7f) },
        new CharColors { name = "Akari",  c30 = new Color(0.5f, 0.4f, 0.4f), c60 = new Color(0.8f, 0.6f, 0.5f), cFull = new Color(1.0f, 0.9f, 0.8f) },
    };

    // ---- ステージ生成メインメニュー ----

    [MenuItem("GachaBlock/Phase7/1. Generate 20 Stage Data")]
    static void GenerateStageData()
    {
        if (!Directory.Exists(StageFolder))
            Directory.CreateDirectory(StageFolder);

        // 既存の Stage*.asset を削除
        foreach (var f in Directory.GetFiles(StageFolder, "Stage*.asset"))
            AssetDatabase.DeleteAsset(f.Replace("\\", "/"));

        CreateAllStages();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Phase7", "20ステージのデータを生成しました！\n\nAssets/Resources/Stages/ を確認してください。", "OK");
    }

    static void CreateAllStages()
    {
        // ---- Stage 1-5: Normal ブロックのみ ----

        Create(1, Rect(2, 0, 5, 3));
        Create(2, Rect(1, 0, 7, 3));
        Create(3, Concat(Rect(2, 0, 5, 4)));
        Create(4, Concat(Rect(1, 0, 7, 4)));
        Create(5, Concat(Rect(1, 0, 7, 5)));

        // ---- Stage 6-10: Durable 追加 ----

        Create(6, Concat(
            Rect(2, 0, 5, 3),
            Row(2, 3, 5, D, 2)
        ));
        Create(7, Concat(
            Rect(1, 0, 7, 3),
            Row(1, 3, 7, D, 2),
            Row(1, 4, 7, D, 2)
        ));
        Create(8, Concat(
            Rect(1, 0, 7, 3),
            Row(1, 3, 7, D, 2),
            Row(1, 4, 7, D, 3)
        ));
        Create(9, Concat(
            Rect(1, 0, 7, 2),
            Checker(1, 2, 7, 2, N),          // チェッカー
            Row(1, 4, 7, D, 3)
        ));
        Create(10, Concat(                   // Stage 10: ボス的
            Row(1, 0, 7, D, 4),
            Rect(1, 1, 7, 3),
            Row(1, 4, 7, D, 4)
        ));

        // ---- Stage 11-15: Explosion / Chain 追加 ----

        Create(11, Concat(
            Rect(1, 0, 7, 3),
            Spots(new[]{
                Pair(2, 3, E), Pair(4, 3, E), Pair(6, 3, E)
            })
        ));
        Create(12, Concat(
            Rect(1, 0, 7, 3),
            Row(1, 3, 7, C)
        ));
        Create(13, Concat(
            Row(1, 0, 7, D, 2),
            Rect(1, 1, 7, 2),
            Spots(new[]{ Pair(2, 3, E), Pair(5, 3, E) }),
            Row(1, 3, 7, N)
        ));
        Create(14, Concat(
            Row(1, 0, 7, D, 3),
            Rect(1, 1, 7, 3),
            Row(1, 4, 7, C)
        ));
        Create(15, Concat(
            Row(1, 0, 7, D, 2),
            Rect(1, 1, 7, 2),
            Row(1, 3, 7, C),
            Spots(new[]{ Pair(2, 4, E), Pair(4, 4, E), Pair(6, 4, E) }),
            Row(1, 4, 7, N)
        ));

        // ---- Stage 16-20: 全種混合 ----

        Create(16, Concat(
            Row(0, 0, 9, D, 2),
            Rect(1, 1, 7, 3),
            Row(1, 4, 7, C),
            Spots(new[]{ Pair(1, 4, E), Pair(4, 4, E), Pair(7, 4, E) }),
            Row(0, 5, 9, D, 3)
        ));
        Create(17, Concat(
            Row(0, 0, 9, D, 3),
            Row(0, 1, 9, N),
            Checker(0, 2, 9, 2, C),
            Row(0, 4, 9, N),
            Spots(new[]{ Pair(2, 5, E), Pair(4, 5, E), Pair(6, 5, E) }),
            Row(0, 5, 9, N),
            Row(0, 6, 9, D, 4)
        ));
        Create(18, Concat(
            Row(0, 0, 9, D, 4),
            Rect(0, 1, 9, 3),
            Row(0, 4, 9, C),
            Spots(new[]{ Pair(1, 5, E), Pair(3, 5, E), Pair(5, 5, E), Pair(7, 5, E) }),
            Rect(0, 5, 9, 2),
            Row(0, 7, 9, D, 4)
        ));
        Create(19, Concat(
            Row(0, 0, 9, D, 4),
            Row(0, 1, 9, C),
            Rect(0, 2, 9, 3),
            Row(0, 5, 9, C),
            Spots(new[]{ Pair(0, 6, E), Pair(2, 6, E), Pair(4, 6, E), Pair(6, 6, E), Pair(8, 6, E) }),
            Rect(0, 6, 9, 2),
            Row(0, 8, 9, D, 5)
        ));
        Create(20, Concat(                   // Stage 20: 最終ボス
            Row(0, 0, 9, D, 5),
            Row(0, 1, 9, C),
            Row(0, 2, 9, D, 3),
            Checker(0, 3, 9, 2, E),
            Row(0, 5, 9, D, 3),
            Row(0, 6, 9, C),
            Rect(0, 7, 9, 2),
            Row(0, 9, 9, D, 5)
        ));
    }

    // ---- ステージ作成ヘルパー ----

    static void Create(int num, List<BlockPlacementData> blocks)
    {
        var data = ScriptableObject.CreateInstance<StageData>();
        data.stageNumber = num;
        data.blocks = blocks;
        int idx = num - 1;
        if (idx < charSets.Length)
        {
            data.characterName   = charSets[idx].name;
            data.illustColor30   = charSets[idx].c30;
            data.illustColor60   = charSets[idx].c60;
            data.illustColorFull = charSets[idx].cFull;
        }
        AssetDatabase.CreateAsset(data, $"{StageFolder}/Stage{num:D2}.asset");
    }

    static List<BlockPlacementData> Concat(params List<BlockPlacementData>[] lists)
    {
        var result = new List<BlockPlacementData>();
        foreach (var l in lists) result.AddRange(l);
        return result;
    }

    static List<BlockPlacementData> Concat(List<BlockPlacementData> first)
        => new List<BlockPlacementData>(first);

    /// <summary>矩形のブロック群</summary>
    static List<BlockPlacementData> Rect(int startCol, int startRow, int cols, int rows,
        BlockType type = BlockType.Normal, int hp = 1)
    {
        var list = new List<BlockPlacementData>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                list.Add(B(startCol + c, startRow + r, type, hp));
        return list;
    }

    /// <summary>1行分</summary>
    static List<BlockPlacementData> Row(int startCol, int row, int cols,
        BlockType type = BlockType.Normal, int hp = 1)
    {
        var list = new List<BlockPlacementData>();
        for (int c = 0; c < cols; c++)
            list.Add(B(startCol + c, row, type, hp));
        return list;
    }

    /// <summary>チェッカーパターン（市松模様）</summary>
    static List<BlockPlacementData> Checker(int startCol, int startRow, int cols, int rows,
        BlockType type = BlockType.Normal, int hp = 1)
    {
        var list = new List<BlockPlacementData>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if ((c + r) % 2 == 0)
                    list.Add(B(startCol + c, startRow + r, type, hp));
        return list;
    }

    /// <summary>指定座標のみ</summary>
    static List<BlockPlacementData> Spots(BlockPlacementData[] spots)
    {
        return new List<BlockPlacementData>(spots);
    }

    static BlockPlacementData Pair(int col, int row, BlockType type, int hp = 1)
        => B(col, row, type, hp);

    static BlockPlacementData B(int col, int row, BlockType type = BlockType.Normal, int hp = 1)
        => new BlockPlacementData
        {
            blockType = type,
            gridPosition = new Vector2Int(col, row),
            hp = hp
        };

    // ---- Android ビルド設定 ----

    [MenuItem("GachaBlock/Phase7/2. Configure Android Build Settings")]
    static void ConfigureAndroid()
    {
        PlayerSettings.productName = "Gacha Block Breaker";
        PlayerSettings.companyName = "YourStudio";
        PlayerSettings.bundleVersion = "0.7.0";
        PlayerSettings.SetApplicationIdentifier(
            BuildTargetGroup.Android, "com.yourstudio.gachablockbreaker");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.bundleVersionCode = 1;

        // 解像度設定（スマホ縦向き固定）
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        Debug.Log("[Phase7] Android ビルド設定を適用しました。");
        EditorUtility.DisplayDialog("Phase7",
            "Android ビルド設定を適用しました。\n\n" +
            "必要に応じて以下を変更してください:\n" +
            "・Company Name: YourStudio\n" +
            "・Package Name: com.yourstudio.gachablockbreaker\n\n" +
            "Edit > Project Settings > Player から変更できます。", "OK");
    }

    // ---- デバッグ ----

    [MenuItem("GachaBlock/Phase7/Debug: Restore Starter Characters (Luna/Aria/Sera)")]
    static void RestoreStarterChars()
    {
        // ファイル名で検索し、内部 characterName（日本語可）で PlayerPrefs に書く
        string[] fileNames = { "Luna", "Aria", "Sera" };
        int restored = 0;
        var sb = new System.Text.StringBuilder();

        foreach (var fileName in fileNames)
        {
            var asset = UnityEngine.Resources.Load<CharacterData>($"Characters/{fileName}");
            if (asset == null)
            {
                Debug.LogWarning($"[Phase7] Resources/Characters/{fileName}.asset が見つかりません。");
                sb.AppendLine($"✗ {fileName}: アセットなし");
                continue;
            }
            string actualName = asset.characterName;
            if (string.IsNullOrEmpty(actualName))
            {
                Debug.LogWarning($"[Phase7] {fileName}.asset の characterName が空です。");
                sb.AppendLine($"✗ {fileName}: characterName が空");
                continue;
            }
            if (!OrbManager.IsOwned(actualName))
            {
                OrbManager.SetOwned(actualName);
                restored++;
                sb.AppendLine($"✓ {actualName}: 所持済みに設定");
            }
            else
            {
                sb.AppendLine($"○ {actualName}: 既に所持済み");
            }
            if (OrbManager.GetCharCount(actualName) == 0)
                OrbManager.AddCharCount(actualName);
        }

        Debug.Log($"[Phase7] スターターキャラ復元完了:\n{sb}");
        EditorUtility.DisplayDialog("Phase7",
            $"スターターキャラ復元完了 (新規設定: {restored} 体)\n\n{sb}", "OK");
    }

    [MenuItem("GachaBlock/Phase7/Debug: Unlock All Stages")]
    static void UnlockAllStages()
    {
        if (Application.isPlaying)
        {
            ProgressManager.UnlockAll();
            Debug.Log("[Phase7] 全ステージをアンロックしました。");
        }
        else
        {
            UnityEngine.PlayerPrefs.SetInt("GachaBlock_MaxUnlocked", 20);
            UnityEngine.PlayerPrefs.Save();
            Debug.Log("[Phase7] 全ステージをアンロックしました（Editor）。");
            EditorUtility.DisplayDialog("Phase7", "全20ステージをアンロックしました。", "OK");
        }
    }

    [MenuItem("GachaBlock/Phase7/Debug: Reset Stage Progress")]
    static void ResetProgress()
    {
        ProgressManager.ResetAll();
        EditorUtility.DisplayDialog("Phase7", "ステージ進行をリセットしました。", "OK");
    }
}
#endif
