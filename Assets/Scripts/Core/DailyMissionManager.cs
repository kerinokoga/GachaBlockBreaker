using UnityEngine;
using System;

/// <summary>
/// デイリーミッション管理（毎日3件、午前4時リセット＝ログインボーナスと同じ基準）。
/// 達成した瞬間に報酬をプレゼントボックスへ自動付与する（受け取りUIは既存の導線を再利用）。
/// </summary>
public static class DailyMissionManager
{
    // ミッション定義
    public const int ClearTarget = 1;   // ステージを1回クリア
    public const int GachaTarget = 1;   // ガチャを1回引く
    public const int BlockTarget = 100; // ブロックを100個破壊
    public const int RewardEach  = 30;  // 各ミッション報酬
    public const int RewardAll   = 60;  // 全達成ボーナス

    const string KeyDate    = "GachaBlock_MissionDate";
    const string KeyClear   = "GachaBlock_MissionClear";
    const string KeyGacha   = "GachaBlock_MissionGacha";
    const string KeyBlocks  = "GachaBlock_MissionBlocks";
    const string KeyGranted = "GachaBlock_MissionGranted"; // + 0..2（各ミッション）, 3（全達成）

    /// <summary>今日の基準日文字列（午前4時リセット）</summary>
    static string Today => DateTime.Now.AddHours(-4).ToString("yyyy-MM-dd");

    /// <summary>日付が変わっていたら進捗をリセットする（各 API の入口で自動的に呼ばれる）</summary>
    public static void CheckReset()
    {
        if (PlayerPrefs.GetString(KeyDate, "") == Today) return;
        PlayerPrefs.SetString(KeyDate, Today);
        PlayerPrefs.SetInt(KeyClear, 0);
        PlayerPrefs.SetInt(KeyGacha, 0);
        PlayerPrefs.SetInt(KeyBlocks, 0);
        for (int i = 0; i <= 3; i++) PlayerPrefs.SetInt(KeyGranted + i, 0);
        PlayerPrefs.Save();
        Debug.Log("[Mission] デイリーミッションをリセット");
    }

    // ---- 進捗報告（ゲーム側から呼ぶ） ----

    public static void ReportStageClear() => AddProgress(KeyClear, 0, ClearTarget, "ステージクリア");
    public static void ReportGachaDraw()  => AddProgress(KeyGacha, 1, GachaTarget, "ガチャを引く");

    /// <summary>ブロック破壊1個ごとに呼ぶ。頻度が高いため達成時以外は Save しない
    /// （未達成分の途中経過はアプリ終了時の自動 Save で保存される）</summary>
    public static void ReportBlockDestroyed() => AddProgress(KeyBlocks, 2, BlockTarget, "ブロック破壊", saveEveryTime: false);

    static void AddProgress(string key, int index, int target, string label, bool saveEveryTime = true)
    {
        CheckReset();
        int cur = PlayerPrefs.GetInt(key, 0);
        if (cur >= target) return; // 達成済み
        cur++;
        PlayerPrefs.SetInt(key, cur);
        if (cur >= target) Grant(index, label);
        else if (saveEveryTime) PlayerPrefs.Save();
    }

    static void Grant(int index, string label)
    {
        if (PlayerPrefs.GetInt(KeyGranted + index, 0) == 1) return;
        PlayerPrefs.SetInt(KeyGranted + index, 1);
        PresentBoxManager.AddOrbPresent(RewardEach, $"デイリーミッション：{label}", 7);
        Debug.Log($"[Mission] 達成: {label} (+{RewardEach}オーブ)");

        // 3件すべて達成なら全達成ボーナス
        if (PlayerPrefs.GetInt(KeyGranted + 0, 0) == 1
            && PlayerPrefs.GetInt(KeyGranted + 1, 0) == 1
            && PlayerPrefs.GetInt(KeyGranted + 2, 0) == 1
            && PlayerPrefs.GetInt(KeyGranted + 3, 0) == 0)
        {
            PlayerPrefs.SetInt(KeyGranted + 3, 1);
            PresentBoxManager.AddOrbPresent(RewardAll, "デイリーミッション全達成ボーナス", 7);
            Debug.Log($"[Mission] 全達成ボーナス (+{RewardAll}オーブ)");
        }
        PlayerPrefs.Save();
    }

    // ---- UI 用取得 ----

    public struct MissionInfo
    {
        public string title;
        public int current;
        public int target;
        public int reward;
        public bool granted;
    }

    public static MissionInfo[] GetMissions()
    {
        CheckReset();
        return new[]
        {
            Info("ステージを1回クリア", KeyClear,  ClearTarget, 0),
            Info("ガチャを1回引く",     KeyGacha,  GachaTarget, 1),
            Info("ブロックを100個破壊", KeyBlocks, BlockTarget, 2),
        };
    }

    static MissionInfo Info(string title, string key, int target, int index) => new MissionInfo
    {
        title   = title,
        current = Mathf.Min(PlayerPrefs.GetInt(key, 0), target),
        target  = target,
        reward  = RewardEach,
        granted = PlayerPrefs.GetInt(KeyGranted + index, 0) == 1,
    };

    /// <summary>全達成ボーナスまで受け取り済みか</summary>
    public static bool AllCompleted()
    {
        CheckReset();
        return PlayerPrefs.GetInt(KeyGranted + 3, 0) == 1;
    }

    /// <summary>未達成ミッション数（ホームのバッジ表示用）</summary>
    public static int RemainingCount()
    {
        CheckReset();
        int n = 0;
        for (int i = 0; i < 3; i++)
            if (PlayerPrefs.GetInt(KeyGranted + i, 0) == 0) n++;
        return n;
    }
}
