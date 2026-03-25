using UnityEngine;

/// <summary>
/// オーブ（ガチャ通貨）・天井カウント・キャラ所持フラグを PlayerPrefs で管理する静的ヘルパー
/// </summary>
public static class OrbManager
{
    public const int StageClearReward = 100; // ステージ初回クリア報酬
    public const int CostSingle       = 100; // 1連コスト
    public const int CostTen          = 1000; // 10連コスト
    public const int PityLimit        = 100; // SSR天井
    public const int SRGuaranteeEvery = 10;  // 10連SR確定間隔

    // ---- PlayerPrefs キー ----
    private static string KeyOrbs           => "GachaBlock_Orbs";
    private static string KeyPityCount      => "GachaBlock_PityCount";
    private static string KeySRDrawCount    => "GachaBlock_SRDrawCount"; // 天井内の連続単発カウント
    private static string KeyOwned(string n) => $"GachaBlock_Owned_{n}";

    // ---- オーブ操作 ----

    public static int GetOrbs() =>
        PlayerPrefs.GetInt(KeyOrbs, 0);

    public static void AddOrbs(int amount)
    {
        PlayerPrefs.SetInt(KeyOrbs, GetOrbs() + amount);
        PlayerPrefs.Save();
    }

    public static bool CanAfford(int cost) => GetOrbs() >= cost;

    /// <summary>オーブを消費する。残高不足時は false を返す。</summary>
    public static bool SpendOrbs(int cost)
    {
        int current = GetOrbs();
        if (current < cost) return false;
        PlayerPrefs.SetInt(KeyOrbs, current - cost);
        PlayerPrefs.Save();
        return true;
    }

    // ---- 天井カウント（SSR未排出の通算連数） ----

    public static int GetPityCount() =>
        PlayerPrefs.GetInt(KeyPityCount, 0);

    public static void IncrementPity()
    {
        PlayerPrefs.SetInt(KeyPityCount, GetPityCount() + 1);
        PlayerPrefs.Save();
    }

    public static void ResetPity()
    {
        PlayerPrefs.SetInt(KeyPityCount, 0);
        PlayerPrefs.Save();
    }

    // ---- SR保証用カウント（10連内のカウント） ----

    public static int GetSRDrawCount() =>
        PlayerPrefs.GetInt(KeySRDrawCount, 0);

    public static void AddSRDrawCount(int amount)
    {
        PlayerPrefs.SetInt(KeySRDrawCount, GetSRDrawCount() + amount);
        PlayerPrefs.Save();
    }

    public static void ResetSRDrawCount()
    {
        PlayerPrefs.SetInt(KeySRDrawCount, 0);
        PlayerPrefs.Save();
    }

    // ---- キャラ所持フラグ ----

    public static bool IsOwned(string charName) =>
        PlayerPrefs.GetInt(KeyOwned(charName), 0) == 1;

    public static void SetOwned(string charName)
    {
        PlayerPrefs.SetInt(KeyOwned(charName), 1);
        PlayerPrefs.Save();
    }

    // ---- デバッグ ----

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KeyOrbs);
        PlayerPrefs.DeleteKey(KeyPityCount);
        PlayerPrefs.DeleteKey(KeySRDrawCount);
        PlayerPrefs.Save();
    }
}
