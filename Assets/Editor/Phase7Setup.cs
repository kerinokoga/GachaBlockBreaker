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
    static readonly BlockType S = BlockType.Speed;
    static readonly BlockType Bo = BlockType.Boss;

    // ---- キャラカラーセット ----
    struct CharColors
    {
        public string name;
        public Color c30, c60, cFull;
    }

    static CharColors[] charSets =
    {
        // Stage 1-5
        new CharColors { name = "ルナ",     c30 = new Color(0.5f, 0.2f, 0.3f), c60 = new Color(0.7f, 0.3f, 0.5f), cFull = new Color(1.0f, 0.7f, 0.85f) },
        new CharColors { name = "アリア",   c30 = new Color(0.2f, 0.2f, 0.5f), c60 = new Color(0.3f, 0.3f, 0.7f), cFull = new Color(0.6f, 0.7f, 1.0f) },
        new CharColors { name = "セラ",     c30 = new Color(0.2f, 0.4f, 0.2f), c60 = new Color(0.3f, 0.6f, 0.3f), cFull = new Color(0.6f, 1.0f, 0.7f) },
        new CharColors { name = "リコ",     c30 = new Color(0.5f, 0.2f, 0.1f), c60 = new Color(0.7f, 0.3f, 0.1f), cFull = new Color(1.0f, 0.6f, 0.4f) },
        new CharColors { name = "ミカ",     c30 = new Color(0.4f, 0.1f, 0.5f), c60 = new Color(0.6f, 0.2f, 0.7f), cFull = new Color(0.9f, 0.5f, 1.0f) },
        // Stage 6-10
        new CharColors { name = "ハナ",     c30 = new Color(0.5f, 0.4f, 0.1f), c60 = new Color(0.7f, 0.6f, 0.1f), cFull = new Color(1.0f, 0.9f, 0.4f) },
        new CharColors { name = "ユキ",     c30 = new Color(0.2f, 0.4f, 0.5f), c60 = new Color(0.3f, 0.6f, 0.7f), cFull = new Color(0.5f, 0.9f, 1.0f) },
        new CharColors { name = "ナナ",     c30 = new Color(0.4f, 0.2f, 0.4f), c60 = new Color(0.6f, 0.3f, 0.6f), cFull = new Color(0.9f, 0.6f, 0.9f) },
        new CharColors { name = "ソラ",     c30 = new Color(0.1f, 0.3f, 0.5f), c60 = new Color(0.2f, 0.5f, 0.7f), cFull = new Color(0.4f, 0.8f, 1.0f) },
        new CharColors { name = "レイ",     c30 = new Color(0.5f, 0.1f, 0.2f), c60 = new Color(0.7f, 0.1f, 0.3f), cFull = new Color(1.0f, 0.3f, 0.5f) },
        // Stage 11-15
        new CharColors { name = "リン",     c30 = new Color(0.1f, 0.5f, 0.4f), c60 = new Color(0.2f, 0.7f, 0.6f), cFull = new Color(0.4f, 1.0f, 0.9f) },
        new CharColors { name = "アイ",     c30 = new Color(0.5f, 0.3f, 0.1f), c60 = new Color(0.7f, 0.5f, 0.1f), cFull = new Color(1.0f, 0.8f, 0.3f) },
        new CharColors { name = "カナ",     c30 = new Color(0.3f, 0.1f, 0.5f), c60 = new Color(0.5f, 0.1f, 0.7f), cFull = new Color(0.8f, 0.4f, 1.0f) },
        new CharColors { name = "メイ",     c30 = new Color(0.1f, 0.5f, 0.2f), c60 = new Color(0.2f, 0.7f, 0.3f), cFull = new Color(0.4f, 1.0f, 0.6f) },
        new CharColors { name = "ノア",     c30 = new Color(0.5f, 0.5f, 0.1f), c60 = new Color(0.7f, 0.7f, 0.1f), cFull = new Color(1.0f, 1.0f, 0.4f) },
        // Stage 16-20
        new CharColors { name = "ルカ",     c30 = new Color(0.5f, 0.1f, 0.5f), c60 = new Color(0.7f, 0.1f, 0.7f), cFull = new Color(1.0f, 0.3f, 1.0f) },
        new CharColors { name = "サキ",     c30 = new Color(0.1f, 0.4f, 0.5f), c60 = new Color(0.1f, 0.6f, 0.7f), cFull = new Color(0.2f, 0.9f, 1.0f) },
        new CharColors { name = "ユナ",     c30 = new Color(0.5f, 0.3f, 0.3f), c60 = new Color(0.7f, 0.5f, 0.4f), cFull = new Color(1.0f, 0.8f, 0.7f) },
        new CharColors { name = "トモ",     c30 = new Color(0.2f, 0.5f, 0.3f), c60 = new Color(0.3f, 0.7f, 0.4f), cFull = new Color(0.5f, 1.0f, 0.7f) },
        new CharColors { name = "アカリ",   c30 = new Color(0.5f, 0.4f, 0.4f), c60 = new Color(0.8f, 0.6f, 0.5f), cFull = new Color(1.0f, 0.9f, 0.8f) },
    };

    // ローマ字→カタカナ対応表（ファイル名→表示名）
    static readonly Dictionary<string, string> RomajiToKatakana = new Dictionary<string, string>
    {
        { "Luna", "ルナ" }, { "Aria", "アリア" }, { "Sera", "セラ" },
        { "Riko", "リコ" }, { "Mika", "ミカ" }, { "Hana", "ハナ" },
        { "Yuki", "ユキ" }, { "Nana", "ナナ" }, { "Sora", "ソラ" },
        { "Rei", "レイ" }, { "Rin", "リン" }, { "Ai", "アイ" },
        { "Kana", "カナ" }, { "Mei", "メイ" }, { "Noa", "ノア" },
        { "Ruka", "ルカ" }, { "Saki", "サキ" }, { "Yuna", "ユナ" },
        { "Tomo", "トモ" }, { "Akari", "アカリ" },
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
        // ================================================================
        //  Stage 1-5: 入門（Normal + Durable少量）パドル通常
        // ================================================================

        Create(1, Concat(Rect(2, 0, 5, 2, D, 5), Row(2, 2, 5, D, 8), Row(3, 3, 3, D, 10),
            Spots(new[]{ Pair(4, -3, Bo, 50) })));
        Create(2, Concat(Rect(1, 0, 7, 2, D, 5), Row(1, 2, 7, D, 8), Row(2, 3, 5, D, 12),
            Spots(new[]{ Pair(3, 5, S), Pair(5, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 60) })));
        Create(3, Concat(Rect(2, 0, 5, 2, D, 6), Row(2, 2, 5, D, 10), Row(2, 3, 5, D, 12), Row(3, 4, 3, D, 15),
            Spots(new[]{ Pair(3, 5, S), Pair(5, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 70) })));
        Create(4, Concat(Row(1, 0, 7, D, 16), Rect(1, 1, 7, 3, D, 10), Row(1, 4, 7, D, 24), Row(2, 5, 5, D, 30),
            Spots(new[]{ Pair(3, 5, S), Pair(5, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 200) })));
        Create(5, Concat(
            Row(1, 0, 7, D, 20),
            Rect(1, 1, 7, 3, D, 12),
            Row(1, 4, 7, D, 30),
            Row(2, 5, 5, D, 40),
            Spots(new[]{ Pair(3, 5, S), Pair(5, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 300) })
        ), 0.95f, trueBlocks: Concat(
            // 裏 5: チェッカー + 爆発混入
            Checker(1, 0, 7, 3, D, 30),
            Row(1, 3, 7, D, 50),
            Spots(new[]{ Pair(2, 4, E), Pair(4, 4, E), Pair(6, 4, E) }),
            Row(2, 5, 5, D, 60),
            Spots(new[]{ Pair(1, 5, S), Pair(7, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 300) })
        ));

        // ================================================================
        //  Stage 6-10: 中盤前半（HP20-60 + SpeedBlock登場）
        // ================================================================

        Create(6, Concat(
            Row(1, 0, 7, D, 20),
            Rect(1, 1, 7, 2, D, 16),
            Row(1, 3, 7, D, 30),
            Row(2, 4, 5, D, 40),
            Spots(new[]{ Pair(3, 5, S), Pair(5, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 400) })
        ), 0.95f);
        Create(7, Concat(
            Row(1, 0, 7, D, 24),
            Rect(1, 1, 7, 2, D, 16),
            Row(1, 3, 7, D, 36),
            Row(2, 4, 5, D, 50),
            Spots(new[]{ Pair(2, 5, S), Pair(6, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 500) })
        ), 0.90f);
        Create(8, Concat(
            Row(1, 0, 7, D, 30),
            Rect(1, 1, 7, 3, D, 20),
            Row(1, 4, 7, D, 40),
            Row(1, 5, 7, D, 50),
            Spots(new[]{ Pair(3, 5, S), Pair(5, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 700) })
        ), 0.90f);
        Create(9, Concat(
            Row(1, 0, 7, D, 36),
            Checker(1, 1, 7, 2, D, 20),
            Rect(1, 3, 7, 2, D, 16),
            Spots(new[]{ Pair(1, 5, S), Pair(4, 5, S), Pair(7, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 800) })
        ), 0.90f);
        Create(10, Concat(
            Row(1, 0, 7, D, 50),
            Rect(1, 1, 7, 2, D, 24),
            Row(1, 3, 7, D, 40),
            Row(1, 4, 7, D, 60),
            Spots(new[]{ Pair(2, 5, S), Pair(6, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1000) })
        ), 0.85f, trueBlocks: Concat(
            // 裏 10: 連鎖反応 + 爆発散布
            Row(1, 0, 7, D, 70),
            Row(1, 1, 7, C),
            Rect(1, 2, 7, 2, D, 50),
            Row(1, 4, 7, D, 90),
            Spots(new[]{ Pair(2, 5, E), Pair(4, 5, E), Pair(6, 5, E) }),
            Spots(new[]{ Pair(3, 5, S), Pair(5, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1000) })
        ));

        // ================================================================
        //  Stage 11-15: 中盤後半（HP40-100 + Explosion/Chain + SpeedBlock）
        // ================================================================

        Create(11, Concat(
            Row(1, 0, 7, D, 40),
            Rect(1, 1, 7, 2, D, 24),
            Spots(new[]{ Pair(2, 3, E), Pair(4, 3, E), Pair(6, 3, E) }),
            Row(1, 3, 7, D, 50),
            Row(2, 4, 5, D, 70),
            Spots(new[]{ Pair(3, 5, S), Pair(5, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1100) })
        ), 0.85f);
        Create(12, Concat(
            Rect(1, 0, 7, 2, D, 30),
            Row(1, 2, 7, C),
            Row(1, 3, 7, D, 50),
            Row(2, 4, 5, D, 70),
            Spots(new[]{ Pair(1, 5, S), Pair(7, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1200) })
        ), 0.85f);
        Create(13, Concat(
            Row(1, 0, 7, D, 50),
            Rect(1, 1, 7, 2, D, 30),
            Spots(new[]{ Pair(2, 3, E), Pair(5, 3, E) }),
            Row(1, 3, 7, D, 40),
            Row(1, 4, 7, C),
            Spots(new[]{ Pair(2, 5, S), Pair(4, 5, S), Pair(6, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1300) })
        ), 0.80f);
        Create(14, Concat(
            Row(0, 0, 9, D, 60),
            Rect(0, 1, 9, 2, D, 36),
            Row(0, 3, 9, C),
            Row(0, 4, 9, D, 50),
            Spots(new[]{ Pair(1, 5, S), Pair(4, 5, S), Pair(7, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1400) })
        ), 0.80f);
        Create(15, Concat(
            Row(0, 0, 9, D, 60),
            Rect(0, 1, 9, 2, D, 40),
            Row(0, 3, 9, C),
            Spots(new[]{ Pair(2, 4, E), Pair(4, 4, E), Pair(6, 4, E) }),
            Row(0, 4, 9, D, 70),
            Spots(new[]{ Pair(0, 5, S), Pair(3, 5, S), Pair(5, 5, S), Pair(8, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1600) })
        ), 0.80f, trueBlocks: Concat(
            // 裏 15: 全種混合 + Chain ダブル
            Row(0, 0, 9, D, 90),
            Row(0, 1, 9, C),
            Rect(0, 2, 9, 2, D, 60),
            Row(0, 4, 9, C),
            Checker(0, 5, 9, 1, E),
            Spots(new[]{ Pair(0, 5, S), Pair(3, 5, S), Pair(5, 5, S), Pair(8, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1600) })
        ));

        // ================================================================
        //  Stage 16-20: 終盤（HP80-200 + 全種混合 + SpeedBlock大量）
        // ================================================================

        Create(16, Concat(
            Row(0, 0, 9, D, 80),
            Rect(0, 1, 9, 2, D, 50),
            Row(0, 3, 9, C),
            Spots(new[]{ Pair(1, 4, E), Pair(4, 4, E), Pair(7, 4, E) }),
            Row(0, 4, 9, D, 100),
            Spots(new[]{ Pair(1, 5, S), Pair(3, 5, S), Pair(5, 5, S), Pair(7, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1700) })
        ), 0.75f);
        Create(17, Concat(
            Row(0, 0, 9, D, 100),
            Row(0, 1, 9, D, 60),
            Checker(0, 2, 9, 2, C),
            Spots(new[]{ Pair(2, 4, E), Pair(4, 4, E), Pair(6, 4, E) }),
            Row(0, 4, 9, D, 120),
            Spots(new[]{ Pair(1, 5, S), Pair(3, 5, S), Pair(5, 5, S), Pair(7, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1800) })
        ), 0.75f);
        Create(18, Concat(
            Row(0, 0, 9, D, 120),
            Rect(0, 1, 9, 2, D, 70),
            Row(0, 3, 9, C),
            Spots(new[]{ Pair(1, 4, E), Pair(3, 4, E), Pair(5, 4, E), Pair(7, 4, E) }),
            Row(0, 4, 9, D, 160),
            Spots(new[]{ Pair(1, 5, S), Pair(3, 5, S), Pair(5, 5, S), Pair(7, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 1900) })
        ), 0.70f);
        Create(19, Concat(
            Row(0, 0, 9, D, 140),
            Row(0, 1, 9, C),
            Rect(0, 2, 9, 2, D, 110),
            Spots(new[]{ Pair(0, 4, E), Pair(2, 4, E), Pair(4, 4, E), Pair(6, 4, E), Pair(8, 4, E) }),
            Spots(new[]{ Pair(1, 5, S), Pair(3, 5, S), Pair(5, 5, S), Pair(7, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 2000) })
        ), 0.70f);
        Create(20, Concat(
            Row(0, 0, 9, D, 160),
            Row(0, 1, 9, C),
            Rect(0, 2, 9, 2, D, 140),
            Checker(0, 4, 9, 1, E),
            Row(0, 5, 9, D, 200),
            Spots(new[]{ Pair(1, 5, S), Pair(3, 5, S), Pair(5, 5, S), Pair(7, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 2000) })
        ), 0.65f, trueBlocks: Concat(
            // 裏 20: 極限混沌配置
            Rect(0, 0, 9, 2, D, 200),
            Row(0, 2, 9, C),
            Rect(0, 3, 9, 2, D, 180),
            Checker(0, 5, 9, 1, E),
            Spots(new[]{ Pair(0, 5, S), Pair(2, 5, S), Pair(4, 5, S), Pair(6, 5, S), Pair(8, 5, S) }),
            Spots(new[]{ Pair(4, -3, Bo, 2000) })
        ));
    }

    // ---- ステージ作成ヘルパー ----

    static void Create(int num, List<BlockPlacementData> blocks, float paddleScale = 1.0f,
                       List<BlockPlacementData> trueBlocks = null)
    {
        var data = ScriptableObject.CreateInstance<StageData>();
        data.stageNumber = num;
        data.blocks = blocks;
        data.paddleScale = paddleScale;
        int idx = num - 1;
        if (idx < charSets.Length)
        {
            data.characterName   = charSets[idx].name;
            data.illustColor30   = charSets[idx].c30;
            data.illustColor60   = charSets[idx].c60;
            data.illustColorFull = charSets[idx].cFull;
        }

        // 裏ステージ設定（Stage 5/10/15/20 のみ trueBlocks が渡される）
        if (trueBlocks != null && trueBlocks.Count > 0)
        {
            data.hasTrueStage        = true;
            data.trueBlocks          = trueBlocks;
            data.trueBossHPMul       = 2.0f;
            data.trueAttackMul       = 2.0f;
            data.truePaddleShrinkMul = 3.0f;
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

    /// <summary>矩形のブロック群（type/hp指定なし → Normal/HP1）</summary>
    static List<BlockPlacementData> Rect(int startCol, int startRow, int cols, int rows)
        => Rect(startCol, startRow, cols, rows, BlockType.Normal, 1);

    /// <summary>矩形のブロック群（type/hp指定あり）</summary>
    static List<BlockPlacementData> Rect(int startCol, int startRow, int cols, int rows,
        BlockType type, int hp)
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
        BlockType type, int hp = 1)
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

    static BlockPlacementData B(int col, int row, BlockType type = BlockType.Normal, int hp = 1, float spdMul = 1.15f)
        => new BlockPlacementData
        {
            blockType = type,
            gridPosition = new Vector2Int(col, row),
            hp = hp,
            speedMultiplier = spdMul
        };

    // ---- ボスブロック Prefab 作成 ----

    [MenuItem("GachaBlock/Phase7/9. Create Boss Block Prefab")]
    static void CreateBossBlockPrefab()
    {
        string prefabFolder = "Assets/Prefabs/Blocks";
        string prefabPath = $"{prefabFolder}/BossBlock.prefab";

        if (!Directory.Exists(prefabFolder))
            Directory.CreateDirectory(prefabFolder);

        // 既存チェック
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Phase7", "BossBlock.prefab は既に存在します。", "OK");
            return;
        }

        // 既存 DurableBlock Prefab をベースにスケールだけ大きくする
        var durablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{prefabFolder}/DurableBlock.prefab");

        var go = new GameObject("BossBlock");

        // SpriteRenderer（既存ブロックと同じスプライトを使う）
        var sr = go.AddComponent<SpriteRenderer>();
        if (durablePrefab != null)
        {
            var durSR = durablePrefab.GetComponent<SpriteRenderer>();
            if (durSR != null) sr.sprite = durSR.sprite;
        }
        sr.color = new Color(0.6f, 0.05f, 0.1f, 0.8f);
        sr.sortingOrder = 5;

        // BoxCollider2D
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        // BossBlock コンポーネント
        go.AddComponent<BossBlock>();

        // 大型サイズ（通常の約2倍）
        go.transform.localScale = new Vector3(3.0f, 1.6f, 1f);

        // Tag 設定
        go.tag = "Block";

        // Prefab 保存
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log("[Phase7] BossBlock.prefab を作成しました。");
        EditorUtility.DisplayDialog("Phase7",
            "BossBlock.prefab を作成しました！\n\n" +
            "GameScene の StageManager > Block Prefabs 配列に\n" +
            "Index 5 として BossBlock を追加してください。", "OK");
    }

    // ---- GameScene の StageManager に BossBlock Prefab を自動割当 ----

    [MenuItem("GachaBlock/Phase7/10. Assign BossBlock to StageManager")]
    static void AssignBossBlockToStageManager()
    {
        // GameScene を開く
        string scenePath = "Assets/Scenes/GameScene.unity";
        if (!File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Phase7", "GameScene.unity が見つかりません。", "OK");
            return;
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,
            UnityEditor.SceneManagement.OpenSceneMode.Single);

        // StageManager を探す
        var sm = Object.FindObjectOfType<StageManager>();
        if (sm == null)
        {
            EditorUtility.DisplayDialog("Phase7", "GameScene 内に StageManager が見つかりません。", "OK");
            return;
        }

        // blockPrefabs フィールドを SerializedObject 経由で編集
        var so = new SerializedObject(sm);
        var prop = so.FindProperty("blockPrefabs");

        // 現在の配列サイズを確認、6未満なら拡張
        if (prop.arraySize < 6)
            prop.arraySize = 6;

        // Index 5 に BossBlock Prefab を割当
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Blocks/BossBlock.prefab");
        if (bossPrefab == null)
        {
            EditorUtility.DisplayDialog("Phase7",
                "BossBlock.prefab が見つかりません。\n先に「9. Create Boss Block Prefab」を実行してください。", "OK");
            return;
        }

        prop.GetArrayElementAtIndex(5).objectReferenceValue = bossPrefab;
        so.ApplyModifiedProperties();

        // シーンを保存
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        // 確認表示
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("blockPrefabs 配列:");
        for (int i = 0; i < prop.arraySize; i++)
        {
            var obj = prop.GetArrayElementAtIndex(i).objectReferenceValue;
            sb.AppendLine($"  [{i}] {(obj != null ? obj.name : "(空)")}");
        }

        EditorUtility.DisplayDialog("Phase7",
            $"BossBlock を StageManager に割り当てました！\n\n{sb}", "OK");
    }

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

    // ---- キャラ名カタカナ化 ----

    [MenuItem("GachaBlock/Phase7/3. Rename Characters to Katakana")]
    static void RenameCharactersToKatakana()
    {
        var allChars = Resources.LoadAll<CharacterData>("Characters");
        int renamed = 0;
        var sb = new System.Text.StringBuilder();

        foreach (var cd in allChars)
        {
            string oldName = cd.characterName;
            // ローマ字→カタカナ辞書で変換
            if (RomajiToKatakana.TryGetValue(oldName, out string katakana))
            {
                // PlayerPrefs キーの移行（旧キーの値を新キーにコピー）
                MigratePlayerPrefsKey($"GachaBlock_Owned_{oldName}", $"GachaBlock_Owned_{katakana}");
                MigratePlayerPrefsKey($"GachaBlock_Count_{oldName}", $"GachaBlock_Count_{katakana}");
                MigratePlayerPrefsKey($"GachaBlock_Level_{oldName}", $"GachaBlock_Level_{katakana}");

                cd.characterName = katakana;
                EditorUtility.SetDirty(cd);
                renamed++;
                sb.AppendLine($"✓ {oldName} → {katakana}");
            }
            else if (oldName == cd.characterName) // 既にカタカナの場合
            {
                sb.AppendLine($"○ {oldName}: 変更なし");
            }
        }

        // StageData の characterName も更新
        var allStages = Resources.LoadAll<StageData>("Stages");
        foreach (var sd in allStages)
        {
            if (RomajiToKatakana.TryGetValue(sd.characterName, out string katakana))
            {
                sd.characterName = katakana;
                EditorUtility.SetDirty(sd);
            }
        }

        AssetDatabase.SaveAssets();
        PlayerPrefs.Save();

        Debug.Log($"[Phase7] キャラ名カタカナ化完了:\n{sb}");
        EditorUtility.DisplayDialog("Phase7",
            $"キャラ名カタカナ化完了 ({renamed} 体リネーム)\n\n{sb}", "OK");
    }

    static void MigratePlayerPrefsKey(string oldKey, string newKey)
    {
        if (oldKey == newKey) return;
        if (PlayerPrefs.HasKey(oldKey))
        {
            int val = PlayerPrefs.GetInt(oldKey, 0);
            PlayerPrefs.SetInt(newKey, val);
            PlayerPrefs.DeleteKey(oldKey);
        }
    }

    // ---- 全キャラ CharacterData 生成 ----

    struct CharDef
    {
        public string fileName;   // アセットファイル名（ローマ字）
        public string dispName;   // 表示名（カタカナ）
        public Rarity rarity;
        public Color rarCol;
        public PassiveEffectType passive;
        public float passiveVal;
        public PassiveEffectType passive2;  // 複合パッシブ用（Noneならなし）
        public float passiveVal2;
        public UltimateSkillType ult;
        public float ultVal;
        public float ultDur;
        public int ultCost;
        public string desc;
    }

    // レアリティカラー定数
    static readonly Color ColSSR = new Color(1.0f, 0.85f, 0.1f);
    static readonly Color ColSR  = new Color(0.75f, 0.3f, 1.0f);
    static readonly Color ColR   = new Color(0.3f, 0.5f, 1.0f);
    static readonly Color ColN   = new Color(0.6f, 0.6f, 0.6f);

    static readonly CharDef[] AllCharDefs =
    {
        // ---- Stage 1-5 ----
        new CharDef { fileName="Luna",  dispName="ルナ",   rarity=Rarity.SR,  rarCol=ColSR,
            passive=PassiveEffectType.BallDamageUp, passiveVal=1.5f,
            ult=UltimateSkillType.PowerBurst, ultVal=2f, ultDur=30f, ultCost=10,
            desc="月光の魔力でダメージを増幅する銀髪の魔法少女。奥義で30秒間ダメージ2倍！" },

        new CharDef { fileName="Aria",  dispName="アリア", rarity=Rarity.R,   rarCol=ColR,
            passive=PassiveEffectType.ExtraDamage, passiveVal=1f,
            ult=UltimateSkillType.MassDestroy, ultVal=8f, ultDur=0f, ultCost=10,
            desc="海の祈りで敵を砕く蒼髪の巫女。追加ダメージ＆奥義で全体8ダメージ！" },

        new CharDef { fileName="Sera",  dispName="セラ",   rarity=Rarity.N,   rarCol=ColN,
            passive=PassiveEffectType.ExtraDamage, passiveVal=1f,
            ult=UltimateSkillType.StockRecover, ultVal=2f, ultDur=0f, ultCost=10,
            desc="森の癒しを操る緑髪の少女。追加ダメージ＆奥義でストック+2回復。" },

        new CharDef { fileName="Riko",  dispName="リコ",   rarity=Rarity.R,   rarCol=ColR,
            passive=PassiveEffectType.ExtraDamage, passiveVal=1f,
            ult=UltimateSkillType.StockRecover, ultVal=2f, ultDur=0f, ultCost=10,
            desc="炎の剣を振るう赤髪の騎士。追加ダメージ＆奥義でストック+2回復！" },

        new CharDef { fileName="Mika",  dispName="ミカ",   rarity=Rarity.SR,  rarCol=ColSR,
            passive=PassiveEffectType.UltGaugeBoost, passiveVal=3f,
            ult=UltimateSkillType.MassDestroy, ultVal=10f, ultDur=0f, ultCost=10,
            desc="闇の魔術を操る紫髪の魔導師。ゲージ3倍速＆奥義で全体10ダメージ！" },

        // ---- Stage 6-10 ----
        new CharDef { fileName="Hana",  dispName="ハナ",   rarity=Rarity.R,   rarCol=ColR,
            passive=PassiveEffectType.ExtraDamage, passiveVal=2f,
            ult=UltimateSkillType.StockRecover, ultVal=2f, ultDur=0f, ultCost=10,
            desc="花の力で守護する金髪の舞姫。追加ダメージ+2＆奥義でストック+2回復！" },

        new CharDef { fileName="Yuki",  dispName="ユキ",   rarity=Rarity.R,   rarCol=ColR,
            passive=PassiveEffectType.BallDamageUp, passiveVal=1.3f,
            ult=UltimateSkillType.Penetrate, ultVal=1f, ultDur=20f, ultCost=10,
            desc="氷の結晶を纏う白蒼の姫。ダメージ1.3倍＆奥義で20秒間貫通！" },

        new CharDef { fileName="Nana",  dispName="ナナ",   rarity=Rarity.N,   rarCol=ColN,
            passive=PassiveEffectType.ExtraDamage, passiveVal=1f,
            ult=UltimateSkillType.StockRecover, ultVal=2f, ultDur=0f, ultCost=10,
            desc="桜吹雪を舞わせるピンク髪のアイドル。追加ダメージ＆奥義でストック+2回復！" },

        new CharDef { fileName="Sora",  dispName="ソラ",   rarity=Rarity.SR,  rarCol=ColSR,
            passive=PassiveEffectType.BallDamageUp, passiveVal=1.3f,
            ult=UltimateSkillType.PowerBurst, ultVal=2f, ultDur=30f, ultCost=10,
            desc="空を駆ける蒼風のパイロット。ダメージ1.3倍＆奥義で30秒間ダメージ2倍！" },

        new CharDef { fileName="Rei",   dispName="レイ",   rarity=Rarity.SSR, rarCol=ColSSR,
            passive=PassiveEffectType.ExtraDamage, passiveVal=3f,
            passive2=PassiveEffectType.CriticalRangeUp, passiveVal2=3f,
            ult=UltimateSkillType.BallSplit, ultVal=0f, ultDur=0f, ultCost=10,
            desc="紅蓮の侍。追加ダメージ+3＆クリティカル範囲+3%＆奥義でボール分裂！圧倒的手数で敵を殲滅。" },

        // ---- Stage 11-15 ----
        new CharDef { fileName="Rin",   dispName="リン",   rarity=Rarity.R,   rarCol=ColR,
            passive=PassiveEffectType.UltGaugeBoost, passiveVal=2f,
            ult=UltimateSkillType.PowerBurst, ultVal=2f, ultDur=25f, ultCost=10,
            desc="翠玉の暗殺者。ゲージ2倍速＆奥義で25秒間ダメージ2倍！" },

        new CharDef { fileName="Ai",    dispName="アイ",   rarity=Rarity.N,   rarCol=ColN,
            passive=PassiveEffectType.ExtraStock, passiveVal=1f,
            ult=UltimateSkillType.StockRecover, ultVal=2f, ultDur=0f, ultCost=10,
            desc="黄金の巫女。開始時ストック+1＆奥義でストック+2回復。安定感のあるサポーター。" },

        new CharDef { fileName="Kana",  dispName="カナ",   rarity=Rarity.R,   rarCol=ColR,
            passive=PassiveEffectType.UltGaugeBoost, passiveVal=2f,
            ult=UltimateSkillType.MassDestroy, ultVal=7f, ultDur=0f, ultCost=10,
            desc="銀河の魔女。ゲージ2倍速＆奥義で全体7ダメージ！" },

        new CharDef { fileName="Mei",   dispName="メイ",   rarity=Rarity.N,   rarCol=ColN,
            passive=PassiveEffectType.ExtraDamage, passiveVal=1f,
            ult=UltimateSkillType.StockRecover, ultVal=2f, ultDur=0f, ultCost=10,
            desc="春風の妖精。追加ダメージ＆奥義でストック+2回復。攻守バランス型。" },

        new CharDef { fileName="Noa",   dispName="ノア",   rarity=Rarity.SSR, rarCol=ColSSR,
            passive=PassiveEffectType.ExtraDamage, passiveVal=3f,
            passive2=PassiveEffectType.CriticalRangeUp, passiveVal2=3f,
            ult=UltimateSkillType.MassDestroy, ultVal=20f, ultDur=0f, ultCost=10,
            desc="太陽の戦乙女。追加ダメージ+3＆クリティカル範囲+3%＆奥義で全体20ダメージ！" },

        // ---- Stage 16-20 ----
        new CharDef { fileName="Ruka",  dispName="ルカ",   rarity=Rarity.SR,  rarCol=ColSR,
            passive=PassiveEffectType.ExtraDamage, passiveVal=2f,
            ult=UltimateSkillType.PowerBurst, ultVal=2.5f, ultDur=30f, ultCost=10,
            desc="黄昏の女帝。追加ダメージ+2＆奥義で30秒間ダメージ2.5倍！" },

        new CharDef { fileName="Saki",  dispName="サキ",   rarity=Rarity.N,   rarCol=ColN,
            passive=PassiveEffectType.BallDamageUp, passiveVal=1.2f,
            ult=UltimateSkillType.StockRecover, ultVal=2f, ultDur=0f, ultCost=10,
            desc="海風のセイレーン。ダメージ1.2倍＆奥義でストック+2回復。" },

        new CharDef { fileName="Yuna",  dispName="ユナ",   rarity=Rarity.N,   rarCol=ColN,
            passive=PassiveEffectType.ExtraDamage, passiveVal=1f,
            ult=UltimateSkillType.StockRecover, ultVal=2f, ultDur=0f, ultCost=10,
            desc="暁のヒーラー。追加ダメージ＆奥義でストック+2回復。初心者向きのキャラ。" },

        new CharDef { fileName="Tomo",  dispName="トモ",   rarity=Rarity.SR,  rarCol=ColSR,
            passive=PassiveEffectType.ExtraDamage, passiveVal=2f,
            ult=UltimateSkillType.Penetrate, ultVal=1f, ultDur=30f, ultCost=10,
            desc="風の弓使い。追加ダメージ+2＆奥義で30秒間貫通！攻守兼備の実力者。" },

        new CharDef { fileName="Akari", dispName="アカリ", rarity=Rarity.SSR, rarCol=ColSSR,
            passive=PassiveEffectType.BallDamageUp, passiveVal=2f,
            passive2=PassiveEffectType.CriticalRangeUp, passiveVal2=3f,
            ult=UltimateSkillType.BallSplit, ultVal=0f, ultDur=0f, ultCost=10,
            desc="灯火の巫女。ダメージ2倍＆クリティカル範囲+3%＆奥義でボール分裂！無数の光で全てを貫く。" },
    };

    [MenuItem("GachaBlock/Phase7/6. Generate All Character Data")]
    static void GenerateAllCharacterData()
    {
        string folder = "Assets/Resources/Characters";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Characters");

        // 定義に含まれない古いアセットを削除
        var existingGuids = AssetDatabase.FindAssets("t:CharacterData", new[] { folder });
        var validFileNames = new HashSet<string>();
        foreach (var def in AllCharDefs) validFileNames.Add(def.fileName);
        foreach (var guid in existingGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetFileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            // メインフォルダ直下の定義済みアセット以外はすべて削除（サブフォルダの重複含む）
            bool isMainFolder = System.IO.Path.GetDirectoryName(assetPath).Replace("\\", "/") == folder;
            if (!isMainFolder || !validFileNames.Contains(assetFileName))
            {
                AssetDatabase.DeleteAsset(assetPath);
                Debug.Log($"[Phase7] 不要アセット削除: {assetPath}");
            }
        }

        int created = 0;
        int updated = 0;
        var sb = new System.Text.StringBuilder();

        foreach (var def in AllCharDefs)
        {
            string path = $"{folder}/{def.fileName}.asset";
            var cd = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            bool isNew = (cd == null);

            if (isNew)
            {
                cd = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(cd, path);
                created++;
            }
            else
            {
                updated++;
            }

            cd.characterName    = def.dispName;
            cd.rarity           = def.rarity;
            cd.rarityColor      = def.rarCol;
            cd.passiveType      = def.passive;
            cd.passiveValue     = def.passiveVal;
            cd.passiveType2     = def.passive2;
            cd.passiveValue2    = def.passiveVal2;
            cd.ultimateType     = def.ult;
            cd.ultimateValue    = def.ultVal;
            cd.ultimateDuration = def.ultDur;
            cd.ultimateGaugeCost = def.ultCost;
            cd.description      = def.desc;
            EditorUtility.SetDirty(cd);

            string status = isNew ? "新規" : "更新";
            sb.AppendLine($"{status} {def.dispName} [{def.rarity}] ({def.fileName}.asset)");
        }



        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Phase7] 全キャラ生成完了:\n{sb}");
        EditorUtility.DisplayDialog("Phase7",
            $"全キャラ CharacterData 生成完了！\n新規: {created} 体 / 更新: {updated} 体\n\n{sb}", "OK");
    }

    // ---- 画像自動割当 ----

    [MenuItem("GachaBlock/Phase7/4. Assign Character Icons")]
    static void AssignCharacterIcons()
    {
        var allChars = Resources.LoadAll<CharacterData>("Characters");
        int assigned = 0;
        var sb = new System.Text.StringBuilder();

        foreach (var cd in allChars)
        {
            // ファイル名はアセット名（ローマ字）で検索
            string assetName = System.IO.Path.GetFileNameWithoutExtension(
                AssetDatabase.GetAssetPath(cd));
            string iconPath = $"Assets/Art/Icons/{assetName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite != null)
            {
                cd.icon = sprite;
                EditorUtility.SetDirty(cd);
                assigned++;
                sb.AppendLine($"✓ {cd.characterName}: {iconPath}");
            }
            else
            {
                sb.AppendLine($"✗ {cd.characterName}: {iconPath} が見つかりません");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Phase7] アイコン割当完了:\n{sb}");
        EditorUtility.DisplayDialog("Phase7",
            $"アイコン割当完了 ({assigned} 体)\n\n{sb}", "OK");
    }

    [MenuItem("GachaBlock/Phase7/5. Assign Stage Illustrations")]
    static void AssignStageIllustrations()
    {
        var allStages = Resources.LoadAll<StageData>("Stages");
        int assigned = 0;
        var sb = new System.Text.StringBuilder();

        // カタカナ→ローマ字の逆引き辞書
        var katakanaToRomaji = new Dictionary<string, string>();
        foreach (var kv in RomajiToKatakana)
            katakanaToRomaji[kv.Value] = kv.Key;

        // ファイル名パターン: {Name}_0.png, {Name}_30.png, {Name}_60.png, {Name}_100.png
        foreach (var sd in allStages)
        {
            string romaji = "";
            if (katakanaToRomaji.TryGetValue(sd.characterName, out string r))
                romaji = r;
            else
                romaji = sd.characterName;

            string basePath = $"Assets/Art/Illustrations/{romaji}";
            var spr0    = AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}_0.png");
            var spr30   = AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}_30.png");
            var spr60   = AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}_60.png");
            var sprFull = AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}_100.png");

            int count = 0;
            if (spr0    != null) { sd.illustSprite0    = spr0;    count++; }
            if (spr30   != null) { sd.illustSprite30   = spr30;   count++; }
            if (spr60   != null) { sd.illustSprite60   = spr60;   count++; }
            if (sprFull != null) { sd.illustSpriteFull = sprFull; count++; }

            // 裏ステージ用イラスト（hasTrueStage のステージのみ対象）
            // ファイル名パターン: {Name}_True_0.png, {Name}_True_30.png, {Name}_True_60.png, {Name}_True_100.png
            int trueCount = 0;
            if (sd.hasTrueStage)
            {
                var tspr0    = AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}_True_0.png");
                var tspr30   = AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}_True_30.png");
                var tspr60   = AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}_True_60.png");
                var tsprFull = AssetDatabase.LoadAssetAtPath<Sprite>($"{basePath}_True_100.png");

                if (tspr0    != null) { sd.trueIllustSprite0    = tspr0;    trueCount++; }
                if (tspr30   != null) { sd.trueIllustSprite30   = tspr30;   trueCount++; }
                if (tspr60   != null) { sd.trueIllustSprite60   = tspr60;   trueCount++; }
                if (tsprFull != null) { sd.trueIllustSpriteFull = tsprFull; trueCount++; }
            }

            if (count > 0 || trueCount > 0)
            {
                EditorUtility.SetDirty(sd);
                assigned++;
                string suffix = sd.hasTrueStage ? $" / 裏{trueCount}/4" : "";
                sb.AppendLine($"✓ Stage{sd.stageNumber} ({sd.characterName}): {count}/4 枚割当{suffix}");
            }
            else
            {
                sb.AppendLine($"✗ Stage{sd.stageNumber} ({sd.characterName}): {basePath}_*.png が見つかりません");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Phase7] イラスト割当完了:\n{sb}");
        EditorUtility.DisplayDialog("Phase7",
            $"イラスト割当完了 ({assigned} ステージ)\n\n{sb}", "OK");
    }

    [MenuItem("GachaBlock/Phase7/11. Assign Character Voices")]
    static void AssignCharacterVoices()
    {
        // フィールド名と対応する CharacterData のフィールド
        string[] voiceFieldNames =
        {
            "voiceTitle",        // タイトル画面用「ぶろっくぶれいかー♡」
            "voiceSelect",
            "voiceUlt",
            "voiceStageStart",
            "voiceVictory",
            "voiceDefeat",
            "voiceDestroy30",
            "voiceDestroy60",
            "voiceBossDamaged",
            "voiceBossAttack",
        };

        var allChars = Resources.LoadAll<CharacterData>("Characters");
        int totalAssigned = 0;
        int charsTouched = 0;
        var sb = new System.Text.StringBuilder();

        foreach (var cd in allChars)
        {
            // アセットファイル名（ローマ字）を取得
            string assetName = System.IO.Path.GetFileNameWithoutExtension(
                AssetDatabase.GetAssetPath(cd));
            string folder = $"Assets/Audio/Voice/{assetName}";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                sb.AppendLine($"- {cd.characterName}: {folder} フォルダなし（スキップ）");
                continue;
            }

            var so = new SerializedObject(cd);
            int perCharAssigned = 0;

            foreach (var fieldName in voiceFieldNames)
            {
                // .wav を優先、なければ .mp3, .ogg も試す
                AudioClip clip = null;
                foreach (var ext in new[] { ".wav", ".mp3", ".ogg" })
                {
                    string path = $"{folder}/{fieldName}{ext}";
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null) break;
                }

                if (clip == null) continue;

                var prop = so.FindProperty(fieldName);
                if (prop == null)
                {
                    sb.AppendLine($"  ⚠ {fieldName} プロパティが CharacterData にありません");
                    continue;
                }

                prop.objectReferenceValue = clip;
                perCharAssigned++;
            }

            if (perCharAssigned > 0)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(cd);
                totalAssigned += perCharAssigned;
                charsTouched++;
                sb.AppendLine($"✓ {cd.characterName} ({assetName}): {perCharAssigned}/10 割当");
            }
            else
            {
                sb.AppendLine($"- {cd.characterName}: 音声ファイルなし");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Phase7] ボイス割当完了: {charsTouched}キャラ / {totalAssigned}ファイル\n{sb}");
        EditorUtility.DisplayDialog("Phase7",
            $"ボイス割当完了\n{charsTouched}キャラ / {totalAssigned}ファイル\n\n{sb}", "OK");
    }

    [MenuItem("GachaBlock/Phase7/7. Assign SpeedBlock Prefab to StageManager")]
    static void AssignSpeedBlockPrefab()
    {
        // GameScene 内の StageManager を探す
        var sm = Object.FindObjectOfType<StageManager>();
        if (sm == null)
        {
            EditorUtility.DisplayDialog("Phase7", "StageManager が見つかりません。\nGameScene を開いてから実行してください。", "OK");
            return;
        }

        // SerializedObject 経由で blockPrefabs 配列を操作
        var so = new SerializedObject(sm);
        var prop = so.FindProperty("blockPrefabs");

        // 現在のサイズが4以下なら5に拡張
        if (prop.arraySize < 5)
            prop.arraySize = 5;

        // SpeedBlock Prefab を検索
        string[] guids = AssetDatabase.FindAssets("t:Prefab SpeedBlock");
        GameObject speedPrefab = null;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null && go.GetComponent<SpeedBlock>() != null)
            {
                speedPrefab = go;
                break;
            }
        }

        if (speedPrefab == null)
        {
            EditorUtility.DisplayDialog("Phase7",
                "SpeedBlock Prefab が見つかりません。\n" +
                "Assets/Prefabs/Blocks/ に SpeedBlock Prefab を作成してください。", "OK");
            return;
        }

        // index 4 に SpeedBlock をセット
        prop.GetArrayElementAtIndex(4).objectReferenceValue = speedPrefab;
        so.ApplyModifiedProperties();

        // シーンを保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Phase7",
            $"SpeedBlock Prefab をセットしました！\n" +
            $"blockPrefabs[4] = {speedPrefab.name}\n\n" +
            "Ctrl+S でシーンを保存してください。", "OK");
    }
    [MenuItem("GachaBlock/Phase7/8. Resize All Block Prefabs")]
    static void ResizeAllBlockPrefabs()
    {
        // ブロック Prefab を検索
        string[] searchFolders = { "Assets/Prefabs" };
        string[] guids = AssetDatabase.FindAssets("t:Prefab", searchFolders);

        int count = 0;
        float newX = 1.15f;
        float newY = 1.1f;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null || go.GetComponent<BlockBase>() == null) continue;

            // Prefab を編集
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            prefabRoot.transform.localScale = new Vector3(newX, newY, 1f);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            count++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Phase7",
            $"ブロック Prefab {count}個のサイズを変更しました。\n" +
            $"Scale: X={newX}, Y={newY}\n\n" +
            $"（旧: X=0.9, Y=0.4）", "OK");
    }
}
#endif
