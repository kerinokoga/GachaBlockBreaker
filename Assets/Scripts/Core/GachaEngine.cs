using UnityEngine;

/// <summary>
/// ガチャ抽選ロジック。UI に依存しない純粋な静的クラス。
/// OrbManager でオーブ消費・天井管理を行い、GachaEngine は抽選のみ担当。
/// </summary>
public static class GachaEngine
{
    // ---- 公開 API ----

    /// <summary>
    /// 1連ガチャ。オーブ消費と天井更新は呼び出し側（GachaUI）が行うこと。
    /// </summary>
    public static GachaResult DrawSingle(GachaPoolData pool, CharacterData[] allChars)
    {
        int pity = OrbManager.GetPityCount();
        Rarity rarity;

        // 天井チェック（SSR確定）
        if (pity >= pool.pityThreshold)
        {
            rarity = Rarity.SSR;
        }
        else
        {
            rarity = RollRarity(pool);
        }

        CharacterData chara = PickByRarity(allChars, rarity);
        bool isNew = !OrbManager.IsOwned(chara.characterName);

        // 天井カウント更新
        if (rarity == Rarity.SSR)
            OrbManager.ResetPity();
        else
            OrbManager.IncrementPity();

        // 所持フラグ登録
        OrbManager.SetOwned(chara.characterName);

        return new GachaResult(chara, isNew);
    }

    /// <summary>
    /// 10連ガチャ。10連中にSR以上が1枚もなければ10枚目を強制SR以上に変換する。
    /// </summary>
    public static GachaResult[] DrawTen(GachaPoolData pool, CharacterData[] allChars)
    {
        var results = new GachaResult[10];
        bool hasSROrAbove = false;

        for (int i = 0; i < 10; i++)
        {
            results[i] = DrawSingle(pool, allChars);
            if (results[i].chara.rarity >= Rarity.SR)
                hasSROrAbove = true;
        }

        // 10連SR保証：SR以上が1枚もなければ10枚目を強制SR以上に差し替え
        if (!hasSROrAbove)
        {
            CharacterData srChara = PickSROrAbove(allChars);
            bool isNew = !OrbManager.IsOwned(srChara.characterName);
            OrbManager.SetOwned(srChara.characterName);

            // 差し替えた分の天井カウントを1つ戻し（DrawSingleで加算済みのため補正）
            // SSRでなければ +1 してしまっているので ResetPity ではなく元の値に戻す
            // ここでは差し替えキャラがSSRかどうかで再調整
            if (srChara.rarity == Rarity.SSR)
                OrbManager.ResetPity();
            // SR の場合は天井カウントをそのまま維持（すでにIncrementされている）

            results[9] = new GachaResult(srChara, isNew);
        }

        return results;
    }

    // ---- 内部ロジック ----

    static Rarity RollRarity(GachaPoolData pool)
    {
        float roll = Random.value;
        if (roll < pool.rateSSR)               return Rarity.SSR;
        if (roll < pool.rateSSR + pool.rateSR) return Rarity.SR;
        if (roll < pool.rateSSR + pool.rateSR + pool.rateR) return Rarity.R;
        return Rarity.N;
    }

    static CharacterData PickByRarity(CharacterData[] all, Rarity rarity)
    {
        // 指定レアリティのキャラリストを作成
        var candidates = System.Array.FindAll(all, c => c.rarity == rarity);

        // 該当なしの場合は N にフォールバック
        if (candidates.Length == 0)
            candidates = System.Array.FindAll(all, c => c.rarity == Rarity.N);

        // それでも空の場合は全体からランダム
        if (candidates.Length == 0)
            return all[Random.Range(0, all.Length)];

        return candidates[Random.Range(0, candidates.Length)];
    }

    static CharacterData PickSROrAbove(CharacterData[] all)
    {
        var candidates = System.Array.FindAll(all, c => c.rarity >= Rarity.SR);
        if (candidates.Length == 0)
            candidates = all;
        return candidates[Random.Range(0, candidates.Length)];
    }
}

/// <summary>ガチャ1回の結果</summary>
public struct GachaResult
{
    public CharacterData chara;
    public bool isNew; // 今回初めて入手したキャラなら true

    public GachaResult(CharacterData chara, bool isNew)
    {
        this.chara = chara;
        this.isNew = isNew;
    }
}
