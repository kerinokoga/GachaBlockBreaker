using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ブロックの種類
/// </summary>
public enum BlockType { Normal, Durable, Explosion, Chain }

/// <summary>
/// 1ブロックの配置データ
/// </summary>
[System.Serializable]
public class BlockPlacementData
{
    public BlockType blockType = BlockType.Normal;
    public Vector2Int gridPosition;  // グリッド上の位置（列, 行）
    [Range(1, 5)]
    public int hp = 1;               // DurableBlock 用
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
    public float cellWidth = 1.0f;   // 1セルの横幅
    public float cellHeight = 0.5f;  // 1セルの縦幅
    public Vector2 originOffset = new Vector2(-4.5f, 5f); // グリッド左上の起点
}
