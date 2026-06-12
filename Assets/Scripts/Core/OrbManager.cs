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

    // 初期所持オーブはプレゼントボックスで配布するためデフォルトは 0
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

    // ---- 所持枚数（重複含む。合成素材） ----

    private static string KeyCount(string n) => $"GachaBlock_Count_{n}";
    private static string KeyLevel(string n) => $"GachaBlock_Level_{n}";

    public static int GetCharCount(string charName) =>
        PlayerPrefs.GetInt(KeyCount(charName), 0);

    public static void AddCharCount(string charName)
    {
        PlayerPrefs.SetInt(KeyCount(charName), GetCharCount(charName) + 1);
        PlayerPrefs.Save();
    }

    public static void SetCharCount(string charName, int value)
    {
        PlayerPrefs.SetInt(KeyCount(charName), value);
        PlayerPrefs.Save();
    }

    // ---- 所持上限チェック（ユニーク数 50体まで） ----

    public static int GetOwnedCount()
    {
        var all = Resources.LoadAll<CharacterData>("Characters");
        int count = 0;
        foreach (var c in all)
            if (IsOwned(c.characterName)) count++;
        return count;
    }

    public static bool CanDrawSingle() => GetOwnedCount() < 50;
    public static bool CanDrawTen()    => GetOwnedCount() <= 48;

    // ---- キャラ削除 ----

    public static void RemoveOwned(string charName)
    {
        PlayerPrefs.SetInt(KeyOwned(charName), 0);
        PlayerPrefs.SetInt(KeyCount(charName), 0);
        PlayerPrefs.SetInt(KeyLevel(charName), 0);
        PlayerPrefs.Save();
    }

    // ---- 合成強化（同キャラ Count-1 → Level+1、最大10） ----

    public static int GetEnhanceLevel(string charName) =>
        PlayerPrefs.GetInt(KeyLevel(charName), 0);

    public static bool TryEnhance(string charName)
    {
        int count = GetCharCount(charName);
        int level = GetEnhanceLevel(charName);
        if (count < 2 || level >= 5) return false;
        PlayerPrefs.SetInt(KeyCount(charName), count - 1);
        PlayerPrefs.SetInt(KeyLevel(charName), level + 1);
        PlayerPrefs.Save();
        return true;
    }

    // ---- スターターキャラ保証 ----

    private static readonly string[] StarterNames = { "Luna", "Aria", "Sera" };

    /// <summary>
    /// スターターキャラを必ず所持済みにする。
    /// ファイル名で検索し、内部の characterName フィールド（日本語可）で PlayerPrefs に書き込む。
    /// アプリ起動時に呼ぶことで Reset 後も自動復元される。
    /// </summary>
    public static void EnsureStarterCharacters()
    {
        foreach (var fileName in StarterNames)
        {
            var asset = Resources.Load<CharacterData>($"Characters/{fileName}");
            if (asset == null) continue;
            string actualName = asset.characterName;
            if (string.IsNullOrEmpty(actualName)) continue;
            if (!IsOwned(actualName)) SetOwned(actualName);
            if (GetCharCount(actualName) == 0) AddCharCount(actualName);
        }
    }

    // ---- 覚醒（Lv5到達後に解放） ----

    private static string KeyAwakened(string n) => $"GachaBlock_Awakened_{n}";

    public static bool IsAwakened(string charName) =>
        PlayerPrefs.GetInt(KeyAwakened(charName), 0) == 1;

    /// <summary>覚醒を実行（Lv5必須）。成功で true。</summary>
    public static bool TryAwaken(string charName)
    {
        if (GetEnhanceLevel(charName) < 5) return false;
        if (IsAwakened(charName)) return false;
        PlayerPrefs.SetInt(KeyAwakened(charName), 1);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>覚醒後のダメージ倍率ボーナス（+0.5x）</summary>
    public const float AwakenBonusMultiplier = 0.5f;

    // ---- デバッグ ----

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KeyOrbs);
        PlayerPrefs.DeleteKey(KeyPityCount);
        PlayerPrefs.DeleteKey(KeySRDrawCount);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 全キャラの所持情報をリセット（所持フラグ・所持枚数・強化Lv・覚醒）。
    /// スターターキャラ復元は呼び出し側で EnsureStarterCharacters() を実行すること。
    /// </summary>
    public static void ResetAllCharacters()
    {
        var all = Resources.LoadAll<CharacterData>("Characters");
        foreach (var c in all)
        {
            if (c == null || string.IsNullOrEmpty(c.characterName)) continue;
            string n = c.characterName;
            PlayerPrefs.DeleteKey(KeyOwned(n));
            PlayerPrefs.DeleteKey(KeyCount(n));
            PlayerPrefs.DeleteKey(KeyLevel(n));
            PlayerPrefs.DeleteKey(KeyAwakened(n));
        }
        PlayerPrefs.Save();
    }
}
