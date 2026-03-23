using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ステージのブロック生成・破壊数追跡・破壊率計算を担う
/// </summary>
public class StageManager : MonoBehaviour
{
    [Header("ステージデータ")]
    [SerializeField] private StageData stageData;

    [Header("ブロック Prefab（0:Normal 1:Durable 2:Explosion 3:Chain）")]
    [SerializeField] private GameObject[] blockPrefabs;

    [Header("ブロックの親 Transform")]
    [SerializeField] private Transform blockParent;

    private int totalBlockCount = 0;
    private int destroyedBlockCount = 0;

    // GameManager が購読して破壊率チェックを行う
    public System.Action OnBlockDestroyedCallback;

    // ---- 外部公開 ----

    public float GetDestroyRate()
    {
        if (totalBlockCount == 0) return 0f;
        return (float)destroyedBlockCount / totalBlockCount;
    }

    public bool IsAllCleared() => destroyedBlockCount >= totalBlockCount && totalBlockCount > 0;

    // ---- ステージ構築 ----

    public void BuildStage(StageData data = null)
    {
        if (data != null) stageData = data;
        if (stageData == null)
        {
            Debug.LogWarning("StageData が設定されていません。");
            return;
        }

        // 既存ブロックを削除
        foreach (Transform child in blockParent)
            Destroy(child.gameObject);

        totalBlockCount = 0;
        destroyedBlockCount = 0;

        foreach (var blockData in stageData.blocks)
        {
            int typeIndex = (int)blockData.blockType;
            if (typeIndex >= blockPrefabs.Length || blockPrefabs[typeIndex] == null)
            {
                Debug.LogWarning($"BlockPrefab[{typeIndex}] が未設定です。スキップします。");
                continue;
            }

            // グリッド座標 → ワールド座標に変換
            Vector2 worldPos = new Vector2(
                stageData.originOffset.x + blockData.gridPosition.x * stageData.cellWidth,
                stageData.originOffset.y - blockData.gridPosition.y * stageData.cellHeight
            );

            GameObject blockGo = Instantiate(
                blockPrefabs[typeIndex],
                new Vector3(worldPos.x, worldPos.y, 0),
                Quaternion.identity,
                blockParent
            );

            // HP を設定
            BlockBase block = blockGo.GetComponent<BlockBase>();
            if (block != null)
            {
                // DurableBlock は HP を外部から設定できるよう公開している
                if (block is DurableBlock durable)
                    durable.SetHP(blockData.hp);

                // 破壊イベントを購読
                block.OnBlockDestroyed += HandleBlockDestroyed;
                totalBlockCount++;
            }
        }

        Debug.Log($"ステージ構築完了: {totalBlockCount} ブロック");
    }

    // ---- イベントハンドラ ----

    private void HandleBlockDestroyed(BlockBase block)
    {
        destroyedBlockCount++;
        OnBlockDestroyedCallback?.Invoke();
    }

    // ---- デバッグ用：テストステージを生成 ----

    public void BuildTestStage()
    {
        if (blockPrefabs == null || blockPrefabs.Length == 0 || blockPrefabs[0] == null)
        {
            Debug.LogWarning("NormalBlock Prefab が設定されていません。");
            return;
        }

        foreach (Transform child in blockParent)
            Destroy(child.gameObject);

        totalBlockCount = 0;
        destroyedBlockCount = 0;

        // 5列 × 3行 のテスト配置
        float startX = -2.0f;
        float startY = 4.0f;
        float spacingX = 1.05f;
        float spacingY = 0.55f;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                Vector3 pos = new Vector3(
                    startX + col * spacingX,
                    startY - row * spacingY,
                    0
                );
                GameObject blockGo = Instantiate(blockPrefabs[0], pos, Quaternion.identity, blockParent);
                BlockBase block = blockGo.GetComponent<BlockBase>();
                if (block != null)
                {
                    block.OnBlockDestroyed += HandleBlockDestroyed;
                    totalBlockCount++;
                }
            }
        }

        Debug.Log($"テストステージ構築完了: {totalBlockCount} ブロック");
    }

    /// <summary>
    /// 全ブロック種を混ぜたテストステージ
    /// </summary>
    public void BuildMixedTestStage()
    {
        if (blockPrefabs == null) return;

        foreach (Transform child in blockParent)
            Destroy(child.gameObject);

        totalBlockCount = 0;
        destroyedBlockCount = 0;

        float startX = -2.0f;
        float startY = 4.0f;
        float spacingX = 1.05f;
        float spacingY = 0.55f;

        // ブロック種別パターン（行ごと）
        // 0:Normal 1:Durable 2:Explosion 3:Chain
        int[,] pattern = {
            { 0, 0, 1, 0, 0 }, // 行1: 通常×4 + 耐久×1
            { 0, 2, 0, 2, 0 }, // 行2: 爆発ブロック2個
            { 3, 3, 3, 3, 3 }, // 行3: 連鎖ブロック5個
        };

        for (int row = 0; row < pattern.GetLength(0); row++)
        {
            for (int col = 0; col < pattern.GetLength(1); col++)
            {
                int typeIndex = pattern[row, col];
                if (typeIndex >= blockPrefabs.Length || blockPrefabs[typeIndex] == null) continue;

                Vector3 pos = new Vector3(
                    startX + col * spacingX,
                    startY - row * spacingY,
                    0
                );

                GameObject blockGo = Instantiate(blockPrefabs[typeIndex], pos, Quaternion.identity, blockParent);
                BlockBase block = blockGo.GetComponent<BlockBase>();
                if (block != null)
                {
                    if (block is DurableBlock d) d.SetHP(3);
                    block.OnBlockDestroyed += HandleBlockDestroyed;
                    totalBlockCount++;
                }
            }
        }

        Debug.Log($"混合テストステージ構築完了: {totalBlockCount} ブロック");
    }
}
