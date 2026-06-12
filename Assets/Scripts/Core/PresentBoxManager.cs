using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// プレゼントボックス管理
/// オーブ・キャラの配布と受け取り、期限管理
/// </summary>
public static class PresentBoxManager
{
    const string KEY_PRESENTS = "GachaBlock_PresentBox";

    // ---- プレゼントデータ ----

    [Serializable]
    public class Present
    {
        public string id;           // ユニークID
        public PresentType type;    // 種類
        public int orbAmount;       // オーブ数（type=Orb時）
        public string characterName;// キャラ名（type=Character時）
        public string message;      // 備考（「○○のお詫び」等）
        public string expireDate;   // 受取期限（yyyy-MM-dd HH:mm）
        public bool received;       // 受取済み
    }

    [Serializable]
    public enum PresentType { Orb, Character }

    [Serializable]
    class PresentList
    {
        public List<Present> presents = new List<Present>();
    }

    // ---- データ読み書き ----

    static PresentList LoadAll()
    {
        string json = PlayerPrefs.GetString(KEY_PRESENTS, "");
        if (string.IsNullOrEmpty(json))
            return new PresentList();
        return JsonUtility.FromJson<PresentList>(json) ?? new PresentList();
    }

    static void SaveAll(PresentList data)
    {
        PlayerPrefs.SetString(KEY_PRESENTS, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    // ---- プレゼント追加 ----

    /// <summary>オーブプレゼントを追加</summary>
    public static void AddOrbPresent(int amount, string message, int expireDays = 30)
    {
        var data = LoadAll();
        data.presents.Add(new Present
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 12),
            type = PresentType.Orb,
            orbAmount = amount,
            message = message,
            expireDate = DateTime.Now.AddDays(expireDays).ToString("yyyy-MM-dd HH:mm"),
            received = false
        });
        SaveAll(data);
    }

    /// <summary>キャラプレゼントを追加</summary>
    public static void AddCharacterPresent(string charName, string message, int expireDays = 30)
    {
        var data = LoadAll();
        data.presents.Add(new Present
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 12),
            type = PresentType.Character,
            characterName = charName,
            message = message,
            expireDate = DateTime.Now.AddDays(expireDays).ToString("yyyy-MM-dd HH:mm"),
            received = false
        });
        SaveAll(data);
    }

    // ---- 取得 ----

    /// <summary>未受取で期限内のプレゼント一覧を取得</summary>
    public static List<Present> GetPendingPresents()
    {
        var data = LoadAll();
        var result = new List<Present>();
        bool changed = false;

        foreach (var p in data.presents)
        {
            if (p.received) continue;

            // 期限チェック
            if (DateTime.TryParse(p.expireDate, out DateTime expire))
            {
                if (DateTime.Now > expire)
                {
                    p.received = true; // 期限切れ → 受取済み扱い
                    changed = true;
                    continue;
                }
            }
            result.Add(p);
        }

        if (changed) SaveAll(data);
        return result;
    }

    /// <summary>未受取プレゼント数</summary>
    public static int GetPendingCount()
    {
        return GetPendingPresents().Count;
    }

    // ---- 受け取り ----

    /// <summary>指定IDのプレゼントを受け取る</summary>
    public static bool Receive(string presentId)
    {
        var data = LoadAll();
        foreach (var p in data.presents)
        {
            if (p.id == presentId && !p.received)
            {
                // 期限チェック
                if (DateTime.TryParse(p.expireDate, out DateTime expire) && DateTime.Now > expire)
                {
                    p.received = true;
                    SaveAll(data);
                    return false;
                }

                // アイテム付与
                if (p.type == PresentType.Orb)
                {
                    OrbManager.AddOrbs(p.orbAmount);
                }
                else if (p.type == PresentType.Character)
                {
                    OrbManager.SetOwned(p.characterName);
                }

                p.received = true;
                SaveAll(data);
                return true;
            }
        }
        return false;
    }

    /// <summary>すべてのプレゼントを受け取る</summary>
    public static int ReceiveAll()
    {
        var pending = GetPendingPresents();
        int count = 0;
        foreach (var p in pending)
        {
            if (Receive(p.id)) count++;
        }
        return count;
    }

    // ---- スターターオーブ（初回起動時に1度だけ付与） ----

    const string KEY_STARTER_GRANTED = "GachaBlock_StarterOrbsGranted";
    public const int StarterOrbAmount = 1000;

    /// <summary>
    /// 初回起動時に「はじめましてプレゼント」として 1000 オーブを付与（プレゼントボックス経由）。
    /// 2回目以降は何もしない。
    /// </summary>
    public static bool EnsureStarterOrbs()
    {
        if (PlayerPrefs.GetInt(KEY_STARTER_GRANTED, 0) == 1) return false;
        AddOrbPresent(StarterOrbAmount, "はじめましてプレゼント", 30);
        PlayerPrefs.SetInt(KEY_STARTER_GRANTED, 1);
        PlayerPrefs.Save();
        Debug.Log($"[PresentBox] スターターオーブ付与: {StarterOrbAmount}オーブ");
        return true;
    }

    // ---- ログインボーナス ----

    const string KEY_LAST_LOGIN = "GachaBlock_LastLoginDate";

    /// <summary>1日1回のログインボーナスをチェックし、未付与ならプレゼントを追加（午前4時リセット）</summary>
    public static bool CheckLoginBonus()
    {
        // 午前4時リセット：4時間前の日付を基準にする
        string today = DateTime.Now.AddHours(-4).ToString("yyyy-MM-dd");
        string lastLogin = PlayerPrefs.GetString(KEY_LAST_LOGIN, "");

        if (lastLogin == today) return false; // 本日既に付与済み

        PlayerPrefs.SetString(KEY_LAST_LOGIN, today);
        PlayerPrefs.Save();

        AddOrbPresent(100, "ログインボーナス", 7);
        Debug.Log("[PresentBox] ログインボーナス付与: 100オーブ");
        return true;
    }

    // ---- 古いデータの掃除 ----

    /// <summary>受取済み・期限切れのデータを削除（データ肥大化防止）</summary>
    public static void CleanUp()
    {
        var data = LoadAll();
        data.presents.RemoveAll(p => p.received);
        SaveAll(data);
    }

    // ---- デバッグ用 ----

    /// <summary>テスト用プレゼントを追加</summary>
    public static void AddTestPresents()
    {
        AddOrbPresent(100, "ログインボーナス", 7);
        AddOrbPresent(500, "不具合のお詫び", 14);
        AddOrbPresent(50, "メンテナンス延長のお詫び", 3);
        AddCharacterPresent("ルナ", "事前登録特典", 30);
        Debug.Log("[PresentBox] テストプレゼント4件追加");
    }
}
