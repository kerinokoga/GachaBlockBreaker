using UnityEngine;

/// <summary>
/// ステージ進行データを PlayerPrefs に保存・読み込みする静的ヘルパー
/// </summary>
public static class ProgressManager
{
    public const int TotalStages = 20;

    private static string KeyMaxUnlocked => "GachaBlock_MaxUnlocked";
    private static string KeyCleared(int stage) => $"GachaBlock_Cleared_{stage}";
    private static string KeyRate(int stage)    => $"GachaBlock_Rate_{stage}";
    private static string KeyTrueCleared(int stage) => $"GachaBlock_TrueCleared_{stage}";

    public static int GetMaxUnlocked() =>
        PlayerPrefs.GetInt(KeyMaxUnlocked, 1);

    public static bool IsCleared(int stage) =>
        PlayerPrefs.GetInt(KeyCleared(stage), 0) == 1;

    public static float GetBestRate(int stage) =>
        PlayerPrefs.GetFloat(KeyRate(stage), 0f);

    /// <summary>
    /// ステージクリア時に呼ぶ。次ステージを解放する。
    /// </summary>
    public static void SaveClear(int stage, float rate)
    {
        bool wasAlreadyCleared = IsCleared(stage); // SetInt より前に判定

        PlayerPrefs.SetInt(KeyCleared(stage), 1);

        float prev = GetBestRate(stage);
        if (rate > prev) PlayerPrefs.SetFloat(KeyRate(stage), rate);

        int maxUnlocked = GetMaxUnlocked();
        if (stage + 1 > maxUnlocked && stage < TotalStages)
            PlayerPrefs.SetInt(KeyMaxUnlocked, stage + 1);

        // 初回クリアのみオーブ付与
        if (!wasAlreadyCleared)
            OrbManager.AddOrbs(OrbManager.StageClearReward);

        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        for (int i = 1; i <= TotalStages; i++)
        {
            PlayerPrefs.DeleteKey(KeyCleared(i));
            PlayerPrefs.DeleteKey(KeyRate(i));
            PlayerPrefs.DeleteKey(KeyTrueCleared(i));
        }
        PlayerPrefs.SetInt(KeyMaxUnlocked, 1);
        PlayerPrefs.Save();
    }

    // ============================================================
    // 裏ステージ（Boss 復活）クリア管理
    // ============================================================

    /// <summary>指定ステージの裏ステージをクリア済みか。</summary>
    public static bool IsTrueStageClear(int stage) =>
        PlayerPrefs.GetInt(KeyTrueCleared(stage), 0) == 1;

    /// <summary>裏ステージクリアを保存する。全イラスト（本＋裏）のコレクション解放を意味する。</summary>
    public static void SaveTrueStageClear(int stage)
    {
        bool wasAlreadyCleared = IsTrueStageClear(stage);

        PlayerPrefs.SetInt(KeyTrueCleared(stage), 1);

        // 初回クリアのみ報酬（通常のステージクリア報酬と同じオーブ量）
        if (!wasAlreadyCleared)
            OrbManager.AddOrbs(OrbManager.StageClearReward);

        PlayerPrefs.Save();
    }

    /// <summary>全ステージをアンロック（デバッグ用）</summary>
    public static void UnlockAll()
    {
        PlayerPrefs.SetInt(KeyMaxUnlocked, TotalStages);
        PlayerPrefs.Save();
    }
}
