using UnityEngine;
using System;

/// <summary>
/// エンドレスモードの入口まわりの管理（解放条件・スタミナコスト・1日初回報酬）。
/// ウェーブ進行そのものは GameManager、ランキングは RankingManager が担う。
/// </summary>
public static class EndlessManager
{
    public const int DailyFirstReward = 100; // 1日初回チャレンジ報酬（オーブ）
    public const int StaminaCost = 3;        // 1回の挑戦で消費するスタミナ
    public const int UnlockStage = 5;        // このステージをクリアで解放

    const string KeyLastChallenge = "GachaBlock_EndlessLastChallenge";

    // ログインボーナス等と同じ午前4時リセット基準
    static string Today => DateTime.Now.AddHours(-4).ToString("yyyy-MM-dd");

    /// <summary>本日すでに挑戦済みか（初回報酬・ホームの告知表示の判定に使用）</summary>
    public static bool HasChallengedToday => PlayerPrefs.GetString(KeyLastChallenge, "") == Today;

    /// <summary>エンドレスが解放されているか（ステージ5クリア）</summary>
    public static bool IsUnlocked => ProgressManager.IsCleared(UnlockStage);

    /// <summary>
    /// 挑戦開始時に呼ぶ（GameManager のエンドレス初期化から）。
    /// 本日初回ならプレゼントボックスへ100オーブを付与する。
    /// </summary>
    public static void OnChallengeStarted()
    {
        if (HasChallengedToday) return;
        PlayerPrefs.SetString(KeyLastChallenge, Today);
        PlayerPrefs.Save();
        PresentBoxManager.AddOrbPresent(DailyFirstReward, "エンドレス初回チャレンジ報酬", 7);
        Debug.Log($"[Endless] 本日初回チャレンジ報酬 +{DailyFirstReward}オーブ");
    }
}
