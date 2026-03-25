using UnityEngine;

/// <summary>
/// ガチャの排出率とコストを保持する ScriptableObject
/// Resources/Gacha/GachaPool.asset に配置
/// </summary>
[CreateAssetMenu(fileName = "GachaPool", menuName = "GachaBlock/GachaPoolData")]
public class GachaPoolData : ScriptableObject
{
    [Header("排出率（合計が 1.0 になるようにすること）")]
    [Range(0f, 1f)] public float rateSSR = 0.03f;  // 3%
    [Range(0f, 1f)] public float rateSR  = 0.12f;  // 12%
    [Range(0f, 1f)] public float rateR   = 0.35f;  // 35%
    // N = 残り 50%（コードで計算）

    [Header("保証設定")]
    public int pityThreshold       = 100; // SSR天井（連続未排出数）
    public int srGuaranteeInterval = 10;  // 10連ごとにSR以上確定
}
