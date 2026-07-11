using UnityEngine;
using System;
using System.Globalization;

/// <summary>
/// マンスリーパス（買い切り30日パス）管理。
/// 購入で30日間有効になり、有効期間中は「ログインした日」に毎日80オーブを
/// プレゼントボックスへ付与する（未ログイン日の分は付与されない＝毎日開く動機）。
/// 自動更新サブスクリプションではないので、期限が切れたら再購入する方式。
/// </summary>
public static class MonthlyPassManager
{
    public const int DailyOrbs = 80;  // 毎日の付与量
    public const int PassDays  = 30;  // 有効日数

    const string KeyExpiry    = "GachaBlock_PassExpiry";    // yyyy-MM-dd HH:mm（不変カルチャ）
    const string KeyLastGrant = "GachaBlock_PassLastGrant"; // 付与済み基準日（午前4時リセット）
    const string DateFormat   = "yyyy-MM-dd HH:mm";

    static string Today => DateTime.Now.AddHours(-4).ToString("yyyy-MM-dd");

    /// <summary>パスが有効期間内か</summary>
    public static bool IsActive => GetExpiry() > DateTime.Now;

    /// <summary>残り日数（切り上げ。無効なら0）</summary>
    public static int RemainingDays
    {
        get
        {
            var span = GetExpiry() - DateTime.Now;
            return span.TotalSeconds <= 0 ? 0 : (int)Math.Ceiling(span.TotalDays);
        }
    }

    static DateTime GetExpiry()
    {
        string s = PlayerPrefs.GetString(KeyExpiry, "");
        if (DateTime.TryParseExact(s, DateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt))
            return dt;
        return DateTime.MinValue;
    }

    /// <summary>購入時に呼ぶ（IAPManager.GrantProduct から）。有効中の再購入は残り期間に加算。</summary>
    public static void Activate(int days)
    {
        var baseTime = IsActive ? GetExpiry() : DateTime.Now;
        var newExpiry = baseTime.AddDays(days);
        PlayerPrefs.SetString(KeyExpiry, newExpiry.ToString(DateFormat, CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        Debug.Log($"[Pass] マンスリーパス有効化 期限: {newExpiry}");

        // 購入当日分をその場で付与
        CheckDailyGrant();
    }

    /// <summary>ログイン時に呼ぶ。有効期間中なら1日1回、デイリー特典を付与。</summary>
    public static bool CheckDailyGrant()
    {
        if (!IsActive) return false;
        if (PlayerPrefs.GetString(KeyLastGrant, "") == Today) return false;

        PlayerPrefs.SetString(KeyLastGrant, Today);
        PlayerPrefs.Save();
        PresentBoxManager.AddOrbPresent(DailyOrbs, $"マンスリーパス特典（残り{RemainingDays}日）", 7);
        Debug.Log($"[Pass] デイリー特典付与 +{DailyOrbs}オーブ");
        return true;
    }

    // ---- クラウドセーブ連携（機種変更でパスが消えないように） ----

    public static string GetExpiryString() => PlayerPrefs.GetString(KeyExpiry, "");

    public static void SetExpiryString(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        PlayerPrefs.SetString(KeyExpiry, s);
    }
}
