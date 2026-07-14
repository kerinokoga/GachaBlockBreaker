using UnityEngine;
using System;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// プレイヤー名（ランキング表示名）の管理。
/// - 未設定時は「ゲスト+UID4桁」を返す
/// - 名前は Firestore の playerNames/{小文字化した名前} で一意予約（早い者勝ち）
/// - 変更時は旧予約を解放する
/// </summary>
public static class PlayerNameManager
{
    const string Key = "GachaBlock_PlayerName";
    public const int MaxLength = 8;

    static FirebaseFirestore Db => FirestoreProvider.Db;

    /// <summary>プレイヤーが自分で名前を設定済みか</summary>
    public static bool HasName => !string.IsNullOrEmpty(PlayerPrefs.GetString(Key, ""));

    /// <summary>表示名を返す（未設定ならゲスト+UID4桁）</summary>
    public static string GetName()
    {
        string n = PlayerPrefs.GetString(Key, "");
        if (!string.IsNullOrEmpty(n)) return n;

        string uid = AuthManager.GetUID();
        return "ゲスト" + (!string.IsNullOrEmpty(uid) && uid.Length >= 4 ? uid.Substring(0, 4) : "????");
    }

    /// <summary>ローカルでの形式チェック（長さ・使用不可文字）</summary>
    public static bool ValidateLocal(string name, out string error)
    {
        error = "";
        name = (name ?? "").Trim();

        if (string.IsNullOrEmpty(name))
        {
            error = "名前を入力してください";
            return false;
        }
        if (name.Length > MaxLength)
        {
            error = $"名前は{MaxLength}文字以内で入力してください";
            return false;
        }
        if (name.Contains("@") || name.Contains("\n") || name.Contains("/")
            || name == "." || name == "..")
        {
            error = "使用できない文字が含まれています";
            return false;
        }
        return true;
    }

    /// <summary>
    /// 名前をサーバーで一意予約して設定する（非同期）。
    /// 既に他プレイヤーが使用中なら失敗。成功時はローカル＋クラウドに保存される。
    /// </summary>
    public static void TrySetNameOnline(string name, Action onSuccess, Action<string> onFailed)
    {
        if (!ValidateLocal(name, out string error))
        {
            onFailed?.Invoke(error);
            return;
        }
        name = name.Trim();

        string uid = null;
        try
        {
            var user = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
            if (user != null) uid = user.UserId;
        }
        catch { }
        if (string.IsNullOrEmpty(uid))
        {
            onFailed?.Invoke("通信環境をご確認ください");
            return;
        }

        string newId = name.ToLowerInvariant();
        string oldRaw = GetRawName();
        string oldId = string.IsNullOrEmpty(oldRaw) ? "" : oldRaw.Trim().ToLowerInvariant();

        var db = Db;
        db.RunTransactionAsync(tx =>
        {
            var newRef = db.Collection("playerNames").Document(newId);
            return tx.GetSnapshotAsync(newRef).ContinueWith(t =>
            {
                var snap = t.Result;
                if (snap.Exists && snap.TryGetValue<string>("uid", out var owner) && owner != uid)
                    throw new InvalidOperationException("NAME_TAKEN");

                tx.Set(newRef, new Dictionary<string, object>
                {
                    { "uid",  uid },
                    { "name", name }
                });

                // 名前変更なら旧予約を解放
                if (!string.IsNullOrEmpty(oldId) && oldId != newId)
                    tx.Delete(db.Collection("playerNames").Document(oldId));

                return true;
            });
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string msg = task.Exception?.GetBaseException()?.Message ?? "";
                Debug.LogWarning($"[PlayerName] 名前予約失敗: {task.Exception?.Flatten()}");
                if (msg.Contains("NAME_TAKEN"))
                    onFailed?.Invoke("この名前は既に使用されています");
                else
                    onFailed?.Invoke("通信エラーが発生しました。しばらくしてからお試しください");
                return;
            }

            PlayerPrefs.SetString(Key, name);
            PlayerPrefs.Save();
            onSuccess?.Invoke();
        });
    }

    /// <summary>クラウドセーブ連携用</summary>
    public static string GetRawName() => PlayerPrefs.GetString(Key, "");
    public static void SetRawName(string name)
    {
        if (!string.IsNullOrEmpty(name)) PlayerPrefs.SetString(Key, name);
    }
}
