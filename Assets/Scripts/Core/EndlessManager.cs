using UnityEngine;
using System;

/// <summary>
/// エンドレスモードの入口まわりの管理（解放条件・スタミナコスト・1日初回報酬）。
/// ウェーブ進行そのものは GameManager、ランキングは RankingManager が担う。
/// </summary>
public static class EndlessManager
{
    public const int DailyFirstReward = 100; // 1日初回チャレンジ報酬（オーブ）
    public const int StaminaCost = 5;        // 1回の挑戦で消費するスタミナ
    public const int UnlockStage = 5;        // このステージをクリアで解放

    const string KeyLastChallenge = "GachaBlock_EndlessLastChallenge";

    // ログインボーナス等と同じ午前4時リセット基準
    static string Today => DateTime.Now.AddHours(-4).ToString("yyyy-MM-dd");

    /// <summary>本日すでに挑戦済みか（初回報酬・ホームの告知表示の判定に使用）</summary>
    public static bool HasChallengedToday => PlayerPrefs.GetString(KeyLastChallenge, "") == Today;

    /// <summary>エンドレスが解放されているか（ステージ5クリア）</summary>
    public static bool IsUnlocked => ProgressManager.IsCleared(UnlockStage);

    // ---- 中断セーブ（撃破ごとの選択メニューから「中断」で保存） ----

    const string KeySuspendWave   = "GachaBlock_EndlessSuspendWave";
    const string KeySuspendScore  = "GachaBlock_EndlessSuspendScore";
    const string KeySuspendStock  = "GachaBlock_EndlessSuspendStock";
    const string KeySuspendParty  = "GachaBlock_EndlessSuspendParty";
    const string KeySuspendGauges = "GachaBlock_EndlessSuspendGauges";

    /// <summary>中断データが存在するか</summary>
    public static bool HasSuspendData => PlayerPrefs.HasKey(KeySuspendWave);

    /// <summary>中断時点の撃破数（再開確認ポップアップの表示用）</summary>
    public static int SuspendScore => PlayerPrefs.GetInt(KeySuspendScore, 0);

    public static void SaveSuspend(int wave, int score, int stock)
    {
        PlayerPrefs.SetInt(KeySuspendWave, wave);
        PlayerPrefs.SetInt(KeySuspendScore, score);
        PlayerPrefs.SetInt(KeySuspendStock, stock);
        // 挑戦中の編成も保存（再開時に固定復元。途中の編成替え・アプリ再起動後の編成消失を防ぐ）
        PlayerPrefs.SetString(KeySuspendParty,
            string.Join("\t", ResultData.SelectedCharacterNames));
        // 奥義ゲージも保存（中断した瞬間と同じ状態で再開できるように）
        var cm = CharacterManager.Instance;
        if (cm != null)
            PlayerPrefs.SetString(KeySuspendGauges,
                $"{cm.GetGaugeRaw(0):0.##},{cm.GetGaugeRaw(1):0.##},{cm.GetGaugeRaw(2):0.##}");
        PlayerPrefs.Save();
        Debug.Log($"[Endless] 中断セーブ: wave={wave} score={score} stock={stock}");
    }

    /// <summary>
    /// 中断時の奥義ゲージ（3スロット分）を読み出す。保存が無ければ null。
    /// ClearSuspend より先に呼ぶこと。
    /// </summary>
    public static float[] LoadSuspendGauges()
    {
        string s = PlayerPrefs.GetString(KeySuspendGauges, "");
        if (string.IsNullOrEmpty(s)) return null;
        var parts = s.Split(',');
        if (parts.Length < 3) return null;
        var g = new float[3];
        for (int i = 0; i < 3; i++)
            float.TryParse(parts[i], out g[i]);
        return g;
    }

    /// <summary>
    /// 中断時の編成を ResultData に復元する（再開時に LoadSuspend と合わせて呼ぶ）。
    /// これにより中断中に通常ステージを別編成でプレイしていても、
    /// エンドレスは中断時と同じ編成で再開される。
    /// </summary>
    public static void RestorePartyFromSuspend()
    {
        string s = PlayerPrefs.GetString(KeySuspendParty, "");
        if (string.IsNullOrEmpty(s)) return; // 旧バージョンの中断データには無い
        var parts = s.Split('\t');
        for (int i = 0; i < 3 && i < parts.Length; i++)
            ResultData.SelectedCharacterNames[i] = parts[i];
        Debug.Log($"[Endless] 中断時の編成を復元: {s.Replace("\t", " / ")}");
    }

    public static void LoadSuspend(out int wave, out int score, out int stock)
    {
        wave  = PlayerPrefs.GetInt(KeySuspendWave, 0);
        score = PlayerPrefs.GetInt(KeySuspendScore, 0);
        stock = PlayerPrefs.GetInt(KeySuspendStock, 3);
    }

    public static void ClearSuspend()
    {
        PlayerPrefs.DeleteKey(KeySuspendWave);
        PlayerPrefs.DeleteKey(KeySuspendScore);
        PlayerPrefs.DeleteKey(KeySuspendStock);
        PlayerPrefs.DeleteKey(KeySuspendParty);
        PlayerPrefs.DeleteKey(KeySuspendGauges);
        EndlessBuffManager.ClearSuspendData(); // 強化カードの中断データも一緒に消す
        PlayerPrefs.Save();
    }

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
