using UnityEngine;

/// <summary>
/// 年齢確認・課金制限マネージャー
/// 年齢区分: 0=未設定, 1=16歳未満, 2=16〜19歳, 3=20歳以上
/// </summary>
public static class AgeVerificationManager
{
    const string KEY_AGE_GROUP = "GachaBlock_AgeGroup";
    const string KEY_TERMS_AGREED = "GachaBlock_TermsAgreed";
    const string KEY_MONTHLY_SPENT = "GachaBlock_MonthlySpent";
    const string KEY_MONTH_KEY = "GachaBlock_SpentMonth";

    // 月額課金上限（円）
    public const int LIMIT_UNDER16 = 5000;
    public const int LIMIT_16TO19 = 20000;

    /// <summary>年齢区分 (0=未設定, 1=16歳未満, 2=16〜19歳, 3=20歳以上)</summary>
    public static int AgeGroup
    {
        get => PlayerPrefs.GetInt(KEY_AGE_GROUP, 0);
        set { PlayerPrefs.SetInt(KEY_AGE_GROUP, value); PlayerPrefs.Save(); }
    }

    /// <summary>利用規約に同意済みか</summary>
    public static bool HasAgreedToTerms
    {
        get => PlayerPrefs.GetInt(KEY_TERMS_AGREED, 0) == 1;
        set { PlayerPrefs.SetInt(KEY_TERMS_AGREED, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>年齢確認済みか</summary>
    public static bool IsAgeVerified => AgeGroup > 0;

    /// <summary>初回セットアップが完了しているか（規約同意 + 年齢確認）</summary>
    public static bool IsSetupComplete => HasAgreedToTerms && IsAgeVerified;

    /// <summary>年齢区分のラベル</summary>
    public static string AgeGroupLabel
    {
        get
        {
            switch (AgeGroup)
            {
                case 1: return "16歳未満";
                case 2: return "16〜19歳";
                case 3: return "20歳以上";
                default: return "未設定";
            }
        }
    }

    /// <summary>月額上限（円）。-1 = 制限なし</summary>
    public static int MonthlyLimit
    {
        get
        {
            switch (AgeGroup)
            {
                case 1: return LIMIT_UNDER16;
                case 2: return LIMIT_16TO19;
                default: return -1; // 制限なし
            }
        }
    }

    /// <summary>今月の使用額を取得</summary>
    public static int GetMonthlySpent()
    {
        ResetIfNewMonth();
        return PlayerPrefs.GetInt(KEY_MONTHLY_SPENT, 0);
    }

    /// <summary>購入額を加算（円）</summary>
    public static void AddSpent(int yen)
    {
        ResetIfNewMonth();
        int current = PlayerPrefs.GetInt(KEY_MONTHLY_SPENT, 0);
        PlayerPrefs.SetInt(KEY_MONTHLY_SPENT, current + yen);
        PlayerPrefs.Save();
    }

    /// <summary>購入可能か判定（金額指定）</summary>
    public static bool CanPurchase(int priceYen)
    {
        int limit = MonthlyLimit;
        if (limit < 0) return true; // 制限なし（20歳以上）

        int spent = GetMonthlySpent();
        return (spent + priceYen) <= limit;
    }

    /// <summary>残り購入可能額</summary>
    public static int RemainingLimit
    {
        get
        {
            int limit = MonthlyLimit;
            if (limit < 0) return -1;
            int spent = GetMonthlySpent();
            return Mathf.Max(0, limit - spent);
        }
    }

    /// <summary>月が変わったらリセット</summary>
    static void ResetIfNewMonth()
    {
        string currentMonth = System.DateTime.Now.ToString("yyyy-MM");
        string savedMonth = PlayerPrefs.GetString(KEY_MONTH_KEY, "");
        if (currentMonth != savedMonth)
        {
            PlayerPrefs.SetString(KEY_MONTH_KEY, currentMonth);
            PlayerPrefs.SetInt(KEY_MONTHLY_SPENT, 0);
            PlayerPrefs.Save();
        }
    }
}
