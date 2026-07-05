using UnityEngine;
using System;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// クラウドセーブ（Firebase Firestore 本実装）
/// ドキュメント: users/{uid}
/// - Save: 進行状況を Firestore にバックアップ（ホーム到達・クリア時に呼ぶ）
/// - LoadIfFreshDevice: ローカルが初期状態のときだけクラウドから復元
///   （機種変更・再インストール時の引き継ぎ用。プレイ中の端末は上書きしない）
/// ※匿名ログインは再インストールで UID が変わるため、確実な引き継ぎには
///   メール連携が必要。
/// </summary>
public static class CloudSaveManager
{
    static FirebaseFirestore Db => FirebaseFirestore.DefaultInstance;

    /// <summary>
    /// 現在の認証済み UID（Firebase Auth の実状態から取得）。
    /// PlayerPrefs キャッシュだと未認証セッションでも値が残っており、
    /// ルールで拒否される書き込みを投げてしまうため実状態を見る。
    /// </summary>
    static string CurrentUid
    {
        get
        {
            try
            {
                var user = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
                return user != null ? user.UserId : null;
            }
            catch { return null; }
        }
    }

    /// <summary>ログイン済みで Firestore を使える状態か</summary>
    static bool CanUseCloud => !string.IsNullOrEmpty(CurrentUid);

    // ============================================================
    // セーブ
    // ============================================================

    /// <summary>
    /// 進行状況を Firestore に保存（非同期・失敗してもゲームは継続）。
    /// </summary>
    public static void Save(Action<bool> onDone = null)
    {
        if (!CanUseCloud)
        {
            Debug.LogWarning("[CloudSave] 未ログインのため保存スキップ");
            onDone?.Invoke(false);
            return;
        }

        var data = CollectSaveData();
        string uid = CurrentUid;

        Db.Collection("users").Document(uid).SetAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("[CloudSave] Firestore 保存完了");
                    onDone?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"[CloudSave] 保存失敗: {task.Exception?.GetBaseException().Message}");
                    onDone?.Invoke(false);
                }
            });
    }

    /// <summary>現在のローカル進行状況を Firestore 用 Dictionary に集約</summary>
    static Dictionary<string, object> CollectSaveData()
    {
        var data = new Dictionary<string, object>();

        // オーブ・ガチャ状態
        data["orbs"]        = OrbManager.GetOrbs();
        data["pityCount"]   = OrbManager.GetPityCount();
        data["srDrawCount"] = OrbManager.GetSRDrawCount();

        // チュートリアル完了状態
        data["tutorialCompleted"] = PlayerPrefs.GetInt("Tutorial_Completed", 0);
        data["tutorialSkipped"]   = PlayerPrefs.GetInt("Tutorial_Skipped", 0);

        // ステージ進行
        data["maxUnlocked"] = ProgressManager.GetMaxUnlocked();
        var cleared     = new List<object>();
        var trueCleared = new List<object>();
        var bestRates   = new Dictionary<string, object>();
        for (int i = 1; i <= ProgressManager.TotalStages; i++)
        {
            if (ProgressManager.IsCleared(i)) cleared.Add(i);
            if (ProgressManager.IsTrueStageClear(i)) trueCleared.Add(i);
            float rate = ProgressManager.GetBestRate(i);
            if (rate > 0f) bestRates[i.ToString()] = rate;
        }
        data["clearedStages"]     = cleared;
        data["trueClearedStages"] = trueCleared;
        data["bestRates"]         = bestRates;

        // 所持キャラ（枚数・強化Lv・覚醒）
        var chars = new List<object>();
        var allChars = Resources.LoadAll<CharacterData>("Characters");
        foreach (var c in allChars)
        {
            if (c == null || !OrbManager.IsOwned(c.characterName)) continue;
            chars.Add(new Dictionary<string, object>
            {
                { "name",     c.characterName },
                { "count",    OrbManager.GetCharCount(c.characterName) },
                { "level",    OrbManager.GetEnhanceLevel(c.characterName) },
                { "awakened", OrbManager.IsAwakened(c.characterName) }
            });
        }
        data["ownedChars"] = chars;

        // 保存日時（サーバー時刻）
        data["updatedAt"] = FieldValue.ServerTimestamp;

        return data;
    }

    // ============================================================
    // ロード（復元）
    // ============================================================

    /// <summary>
    /// ローカルが初期状態（チュートリアル未完了かつステージ未進行）のときだけ
    /// クラウドから復元する。結果に関わらず必ず onDone を呼ぶ。
    /// </summary>
    public static void LoadIfFreshDevice(Action<bool> onDone)
    {
        bool isFresh = PlayerPrefs.GetInt("Tutorial_Completed", 0) == 0
                    && ProgressManager.GetMaxUnlocked() <= 1;
        if (!isFresh || !CanUseCloud)
        {
            onDone?.Invoke(false);
            return;
        }
        Load(onDone);
    }

    /// <summary>
    /// Firestore から進行状況を取得してローカル（PlayerPrefs）へ復元。
    /// ドキュメントが無い／通信失敗時は false。
    /// </summary>
    public static void Load(Action<bool> onDone)
    {
        if (!CanUseCloud)
        {
            onDone?.Invoke(false);
            return;
        }

        string uid = CurrentUid;
        Db.Collection("users").Document(uid).GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogWarning($"[CloudSave] 読込失敗: {task.Exception?.GetBaseException().Message}");
                    onDone?.Invoke(false);
                    return;
                }

                var snap = task.Result;
                if (!snap.Exists)
                {
                    Debug.Log("[CloudSave] クラウドセーブなし（新規ユーザー）");
                    onDone?.Invoke(false);
                    return;
                }

                try
                {
                    ApplySnapshot(snap);
                    Debug.Log("[CloudSave] Firestore から復元完了");
                    onDone?.Invoke(true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CloudSave] 復元中にエラー: {e.Message}");
                    onDone?.Invoke(false);
                }
            });
    }

    /// <summary>スナップショットの内容を PlayerPrefs に書き戻す</summary>
    static void ApplySnapshot(DocumentSnapshot snap)
    {
        // オーブ・ガチャ状態
        if (snap.TryGetValue<long>("orbs", out var orbs))
            PlayerPrefs.SetInt("GachaBlock_Orbs", (int)orbs);
        if (snap.TryGetValue<long>("pityCount", out var pity))
            PlayerPrefs.SetInt("GachaBlock_PityCount", (int)pity);
        if (snap.TryGetValue<long>("srDrawCount", out var srDraw))
            PlayerPrefs.SetInt("GachaBlock_SRDrawCount", (int)srDraw);

        // チュートリアル状態
        if (snap.TryGetValue<long>("tutorialCompleted", out var tuto))
            PlayerPrefs.SetInt("Tutorial_Completed", (int)tuto);
        if (snap.TryGetValue<long>("tutorialSkipped", out var skip))
            PlayerPrefs.SetInt("Tutorial_Skipped", (int)skip);

        // ステージ進行
        if (snap.TryGetValue<long>("maxUnlocked", out var maxUnlocked))
            PlayerPrefs.SetInt("GachaBlock_MaxUnlocked", (int)maxUnlocked);

        if (snap.TryGetValue<List<object>>("clearedStages", out var cleared))
            foreach (var s in cleared)
                PlayerPrefs.SetInt($"GachaBlock_Cleared_{(long)s}", 1);

        if (snap.TryGetValue<List<object>>("trueClearedStages", out var trueCleared))
            foreach (var s in trueCleared)
                PlayerPrefs.SetInt($"GachaBlock_TrueCleared_{(long)s}", 1);

        if (snap.TryGetValue<Dictionary<string, object>>("bestRates", out var rates))
            foreach (var kv in rates)
                PlayerPrefs.SetFloat($"GachaBlock_Rate_{kv.Key}", Convert.ToSingle(kv.Value));

        // 所持キャラ
        if (snap.TryGetValue<List<object>>("ownedChars", out var chars))
        {
            foreach (var entryObj in chars)
            {
                var entry = entryObj as Dictionary<string, object>;
                if (entry == null) continue;
                if (!entry.TryGetValue("name", out var nameObj)) continue;
                string name = nameObj as string;
                if (string.IsNullOrEmpty(name)) continue;

                PlayerPrefs.SetInt($"GachaBlock_Owned_{name}", 1);
                if (entry.TryGetValue("count", out var cnt))
                    PlayerPrefs.SetInt($"GachaBlock_Count_{name}", Convert.ToInt32(cnt));
                if (entry.TryGetValue("level", out var lvl))
                    PlayerPrefs.SetInt($"GachaBlock_Level_{name}", Convert.ToInt32(lvl));
                if (entry.TryGetValue("awakened", out var awk) && awk is bool b && b)
                    PlayerPrefs.SetInt($"GachaBlock_Awakened_{name}", 1);
            }
        }

        PlayerPrefs.Save();
    }
}
