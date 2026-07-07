using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System;

/// <summary>
/// Firebase Authentication マネージャー
/// 匿名ログイン / メール＋パスワード認証
/// </summary>
public static class AuthManager
{
    static FirebaseAuth auth;
    static bool initialized;

    // PlayerPrefs キー（同期アクセス用キャッシュ）
    static string KeyUID => "GachaBlock_AuthUID";
    static string KeyName => "GachaBlock_AuthName";
    static string KeyIsGuest => "GachaBlock_AuthIsGuest";

    // ---- 状態取得（同期・キャッシュ） ----

    public static bool IsLoggedIn => !string.IsNullOrEmpty(GetUID());
    public static string GetUID() => PlayerPrefs.GetString(KeyUID, "");
    public static string GetName() => PlayerPrefs.GetString(KeyName, "");
    public static bool IsGuest() => PlayerPrefs.GetInt(KeyIsGuest, 0) == 1;

    // ---- Firebase 初期化 ----

    /// <summary>Firebase を初期化し、既存セッションを確認</summary>
    public static void Initialize(Action onReady, Action<string> onFailed)
    {
        if (initialized)
        {
            if (auth.CurrentUser != null)
                CacheUser(auth.CurrentUser);
            else
                ClearCache();
            onReady?.Invoke();
            return;
        }

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                initialized = true;

                if (auth.CurrentUser != null)
                    CacheUser(auth.CurrentUser);
                else
                    ClearCache();

                onReady?.Invoke();
            }
            else
            {
                onFailed?.Invoke($"Firebase 初期化失敗: {task.Result}");
            }
        });
    }

    // ---- ゲストログイン（Firebase Anonymous） ----

    public static void LoginAsGuest(Action onSuccess, Action<string> onFailed)
    {
        if (auth == null) { onFailed?.Invoke("Firebase 未初期化"); return; }

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(TranslateError(task.Exception));
                return;
            }
            CacheUser(task.Result.User);
            onSuccess?.Invoke();
        });
    }

    // ---- メール＋パスワード新規登録 ----

    public static void Register(string email, string password, Action onSuccess, Action<string> onFailed)
    {
        if (auth == null) { onFailed?.Invoke("Firebase 未初期化"); return; }

        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            onFailed?.Invoke("有効なメールアドレスを入力してください");
            return;
        }
        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            onFailed?.Invoke("パスワードは6文字以上で入力してください");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(TranslateError(task.Exception));
                return;
            }
            CacheUser(task.Result.User);
            onSuccess?.Invoke();
        });
    }

    // ---- ゲストアカウントへのメール連携（UID を維持したまま認証を紐付け） ----

    /// <summary>
    /// 現在のゲスト（匿名）アカウントにメール＋パスワード認証を紐付ける。
    /// UID が変わらないため、クラウドセーブ・ランキング等のデータがそのまま引き継がれる。
    /// （Register は新規アカウント作成＝UIDが変わるため、引き継ぎ用途にはこちらを使うこと）
    /// </summary>
    public static void LinkWithEmail(string email, string password, Action onSuccess, Action<string> onFailed)
    {
        if (auth == null || auth.CurrentUser == null)
        {
            onFailed?.Invoke("ログイン状態を確認できません。通信環境をご確認ください");
            return;
        }
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            onFailed?.Invoke("有効なメールアドレスを入力してください");
            return;
        }
        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            onFailed?.Invoke("パスワードは6文字以上で入力してください");
            return;
        }

        var credential = EmailAuthProvider.GetCredential(email, password);
        auth.CurrentUser.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(TranslateError(task.Exception));
                return;
            }
            CacheUser(task.Result.User);
            onSuccess?.Invoke();
        });
    }

    // ---- メール＋パスワードログイン ----

    public static void Login(string email, string password, Action onSuccess, Action<string> onFailed)
    {
        if (auth == null) { onFailed?.Invoke("Firebase 未初期化"); return; }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onFailed?.Invoke(TranslateError(task.Exception));
                return;
            }
            CacheUser(task.Result.User);
            onSuccess?.Invoke();
        });
    }

    // ---- ログアウト ----

    public static void Logout()
    {
        if (auth != null) auth.SignOut();
        ClearCache();
    }

    // ---- 内部ヘルパー ----

    static void CacheUser(FirebaseUser user)
    {
        PlayerPrefs.SetString(KeyUID, user.UserId);
        string displayName = user.DisplayName;
        if (string.IsNullOrEmpty(displayName))
            displayName = user.Email ?? "Guest_" + user.UserId.Substring(0, 6);
        PlayerPrefs.SetString(KeyName, displayName);
        PlayerPrefs.SetInt(KeyIsGuest, user.IsAnonymous ? 1 : 0);
        PlayerPrefs.Save();
    }

    static void ClearCache()
    {
        PlayerPrefs.DeleteKey(KeyUID);
        PlayerPrefs.DeleteKey(KeyName);
        PlayerPrefs.DeleteKey(KeyIsGuest);
        PlayerPrefs.Save();
    }

    static string TranslateError(AggregateException ex)
    {
        string msg = "";
        if (ex?.InnerExceptions != null && ex.InnerExceptions.Count > 0)
            msg = ex.InnerExceptions[0].Message;
        else
            return "不明なエラー";

        if (msg.Contains("badly formatted"))
            return "メールアドレスの形式が正しくありません";
        if (msg.Contains("already associated") || msg.Contains("already linked"))
            return "このメールアドレスは既に別のアカウントで使用されています";
        if (msg.Contains("operation is not allowed") || msg.Contains("OPERATION_NOT_ALLOWED"))
            return "メール認証が現在利用できません";
        if (msg.Contains("already in use"))
            return "このメールアドレスは既に使用されています";
        if (msg.Contains("wrong password") || msg.Contains("password is invalid"))
            return "パスワードが正しくありません";
        if (msg.Contains("no user record") || msg.Contains("user not found"))
            return "このメールアドレスは登録されていません";
        if (msg.Contains("too many requests"))
            return "リクエストが多すぎます。しばらく待ってください";
        if (msg.Contains("network"))
            return "ネットワークエラー。接続を確認してください";
        if (msg.Contains("weak password") || msg.Contains("6 characters"))
            return "パスワードは6文字以上で入力してください";

        return msg;
    }
}
