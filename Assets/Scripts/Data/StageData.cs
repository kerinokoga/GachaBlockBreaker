using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ブロックの種類
/// </summary>
public enum BlockType { Normal, Durable, Explosion, Chain, Speed, Boss }

/// <summary>
/// 1ブロックの配置データ
/// </summary>
[System.Serializable]
public class BlockPlacementData
{
    public BlockType blockType = BlockType.Normal;
    public Vector2Int gridPosition;  // グリッド上の位置（列, 行）
    [Range(1, 100)]
    public int hp = 1;               // DurableBlock 用
    public float speedMultiplier = 1.1f; // SpeedBlock 用
}

/// <summary>
/// ステージ1つ分のデータ（ScriptableObject）
/// Inspector でブロック配置を編集できる
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "GachaBlock/StageData")]
public class StageData : ScriptableObject
{
    [Header("ステージ情報")]
    public int stageNumber = 1;

    [Header("ブロック配置")]
    public List<BlockPlacementData> blocks = new List<BlockPlacementData>();

    [Header("グリッド設定")]
    public float cellWidth = 1.0f;
    public float cellHeight = 1.2f;
    public Vector2 originOffset = new Vector2(-4.5f, 3.8f);

    [Header("キャラクターイラスト")]
    public string characterName = "";
    /// <summary>0%（初期状態）イラスト。</summary>
    public Sprite illustSprite0;
    /// <summary>30%破壊時イラスト。</summary>
    public Sprite illustSprite30;
    /// <summary>60%破壊時イラスト。</summary>
    public Sprite illustSprite60;
    /// <summary>100%破壊時イラスト。</summary>
    public Sprite illustSpriteFull;

    [Header("難易度設定")]
    [Range(0.5f, 1.5f)]
    public float paddleScale = 1.0f;   // パドル幅倍率（1.0=通常、小さいほど短い）

    [Header("解放カラー（イラスト未設定時に使用）")]
    public Color illustColor30   = new Color(0.3f, 0.3f, 0.45f);
    public Color illustColor60   = new Color(0.5f, 0.3f, 0.55f);
    public Color illustColorFull = new Color(0.8f, 0.6f, 0.9f);

    // ============================================================
    // 裏ステージ（Boss 復活仕様） — Stage 5/10/15/20 のみ使用
    // ============================================================
    [Header("裏ステージ設定（Boss 復活）")]
    /// <summary>このステージに裏ステージが存在するか（Stage 5/10/15/20 で true）。</summary>
    public bool hasTrueStage = false;

    /// <summary>裏ステージでの Boss HP 倍率。</summary>
    public float trueBossHPMul = 2.0f;

    /// <summary>裏ステージでのボス攻撃倍率（追加 Block 数の倍率）。</summary>
    public float trueAttackMul = 2.0f;

    /// <summary>裏ステージでのパドル縮小時間倍率。</summary>
    public float truePaddleShrinkMul = 3.0f;

    /// <summary>裏ステージ用のブロック配置（本ステージとは別レイアウト）。</summary>
    public List<BlockPlacementData> trueBlocks = new List<BlockPlacementData>();

    [Header("裏ステージイラスト（Boss アイコンは共通）")]
    /// <summary>裏 0%イラスト。</summary>
    public Sprite trueIllustSprite0;
    /// <summary>裏 30%イラスト。</summary>
    public Sprite trueIllustSprite30;
    /// <summary>裏 60%イラスト。</summary>
    public Sprite trueIllustSprite60;
    /// <summary>裏 100%イラスト。</summary>
    public Sprite trueIllustSpriteFull;
}
