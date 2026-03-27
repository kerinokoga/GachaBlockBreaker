using UnityEngine;

/// <summary>
/// モック認証マネージャー（PlayerPrefs ベース）
/// 後から Firebase Auth に差し替え可能な構造
/// </summary>
public static class AuthManager
{
    private static string KeyUID     => "GachaBlock_AuthUID";
    private static string KeyName    => "GachaBlock_AuthName";
    private static string KeyPass    => "GachaBlock_AuthPass";
    private static string KeyIsGuest => "GachaBlock_AuthIsGuest";

    // ---- 状態取得 ----

    public static bool IsLoggedIn => !string.IsNullOrEmpty(GetUID());
    public static string GetUID()  => PlayerPrefs.GetString(KeyUID, "");
    public static string GetName() => PlayerPrefs.GetString(KeyName, "");
    public static bool IsGuest()   => PlayerPrefs.GetInt(KeyIsGuest, 0) == 1;

    // ---- ゲストログイン ----

    public static string LoginAsGuest()
    {
        string uid = System.Guid.NewGuid().ToString("N").Substring(0, 12);
        string name = "Guest_" + uid.Substring(0, 6);
        PlayerPrefs.SetString(KeyUID, uid);
        PlayerPrefs.SetString(KeyName, name);
        PlayerPrefs.SetInt(KeyIsGuest, 1);
        PlayerPrefs.Save();
        Debug.Log($"[AuthManager] ゲストログイン: {name} ({uid})");
        return uid;
    }

    // ---- ユーザー登録 ----

    /// <summary>
    /// ユーザー名＋パスワードで登録。成功時 true。
    /// モック版: PlayerPrefs に保存するだけ。
    /// </summary>
    public static bool Register(string username, string password, out string errorMsg)
    {
        errorMsg = "";

        if (string.IsNullOrEmpty(username) || username.Length < 2)
        {
            errorMsg = "名前は2文字以上で入力してください";
            return false;
        }
        if (string.IsNullOrEmpty(password) || password.Length < 4)
        {
            errorMsg = "パスワードは4文字以上で入力してください";
            return false;
        }

        // モック版: 同名チェック（PlayerPrefs に名前→UIDマッピング保存）
        string existingUID = PlayerPrefs.GetString($"GachaBlock_UserMap_{username}", "");
        if (!string.IsNullOrEmpty(existingUID))
        {
            errorMsg = "この名前は既に使用されています";
            return false;
        }

        string uid = System.Guid.NewGuid().ToString("N").Substring(0, 12);
        string passHash = ComputeSimpleHash(password);

        // ユーザーデータ保存
        PlayerPrefs.SetString($"GachaBlock_UserMap_{username}", uid);
        PlayerPrefs.SetString($"GachaBlock_UserPass_{username}", passHash);

        // ログイン状態保存
        PlayerPrefs.SetString(KeyUID, uid);
        PlayerPrefs.SetString(KeyName, username);
        PlayerPrefs.SetString(KeyPass, passHash);
        PlayerPrefs.SetInt(KeyIsGuest, 0);
        PlayerPrefs.Save();

        Debug.Log($"[AuthManager] 登録完了: {username} ({uid})");
        return true;
    }

    // ---- ログイン ----

    public static bool Login(string username, string password, out string errorMsg)
    {
        errorMsg = "";

        string storedUID = PlayerPrefs.GetString($"GachaBlock_UserMap_{username}", "");
        if (string.IsNullOrEmpty(storedUID))
        {
            errorMsg = "ユーザーが見つかりません";
            return false;
        }

        string storedHash = PlayerPrefs.GetString($"GachaBlock_UserPass_{username}", "");
        string inputHash  = ComputeSimpleHash(password);
        if (storedHash != inputHash)
        {
            errorMsg = "パスワードが違います";
            return false;
        }

        PlayerPrefs.SetString(KeyUID, storedUID);
        PlayerPrefs.SetString(KeyName, username);
        PlayerPrefs.SetString(KeyPass, storedHash);
        PlayerPrefs.SetInt(KeyIsGuest, 0);
        PlayerPrefs.Save();

        Debug.Log($"[AuthManager] ログイン成功: {username}");
        return true;
    }

    // ---- ログアウト ----

    public static void Logout()
    {
        Debug.Log($"[AuthManager] ログアウト: {GetName()}");
        PlayerPrefs.DeleteKey(KeyUID);
        PlayerPrefs.DeleteKey(KeyName);
        PlayerPrefs.DeleteKey(KeyPass);
        PlayerPrefs.DeleteKey(KeyIsGuest);
        PlayerPrefs.Save();
    }

    // ---- 簡易ハッシュ（モック用、本番は Firebase Auth に差し替え） ----

    static string ComputeSimpleHash(string input)
    {
        int hash = 0;
        foreach (char c in input)
            hash = hash * 31 + c;
        return hash.ToString("X8");
    }
}
