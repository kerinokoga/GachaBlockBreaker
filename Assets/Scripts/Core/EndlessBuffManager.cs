using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// エンドレスモード限定の3択強化カードシステム。
/// - カード定義（22種）とレア度抽選（R87% / SR10% / SSR3%）
/// - 永続効果（ヒットダメージ+% / クリ範囲+% / 奥義延長+秒）の累積
/// - 次ステージ限定効果（集中打 / 弱体 / 強化 / 不死身 / 覚醒）の管理
/// - 中断セーブへの保存・復元（永続効果の合計値＋提示中の3枚）
///
/// 通常ステージへの漏れ防止:
/// 全ての参照プロパティは Active=false のとき中立値（倍率1・ボーナス0）を返す。
/// Active はエンドレス開始時のみ true になり、通常ステージ開始時に必ず Reset される。
/// </summary>
public static class EndlessBuffManager
{
    // ============================================================
    // カード定義
    // ============================================================

    public enum Rarity { R, SR, SSR }
    public enum CardKind { Instant, Permanent, NextWave }

    public class Card
    {
        public string id;          // セーブ用ID（改名禁止）
        public string cardName;    // カード名（1行・5文字目安）
        public string effectText;  // 効果説明文
        public Rarity rarity;
        public CardKind kind;
        public int value;          // 効果量（%・秒・個数・倍率・分母）
    }

    static readonly List<Card> catalog = new List<Card>
    {
        // 永続強化 I〜IV: ヒットダメージ+%（加算合算）
        new Card{ id="dmg3",   cardName="永続強化 I",   effectText="ヒットダメージ +3%",       rarity=Rarity.R,   kind=CardKind.Permanent, value=3 },
        new Card{ id="dmg5",   cardName="永続強化 II",  effectText="ヒットダメージ +5%",       rarity=Rarity.R,   kind=CardKind.Permanent, value=5 },
        new Card{ id="dmg10",  cardName="永続強化 III", effectText="ヒットダメージ +10%",      rarity=Rarity.SR,  kind=CardKind.Permanent, value=10 },
        new Card{ id="dmg15",  cardName="永続強化 IV",  effectText="ヒットダメージ +15%",      rarity=Rarity.SSR, kind=CardKind.Permanent, value=15 },
        // 範囲拡大 I〜III: クリティカル範囲+%（加算・上限なし）
        new Card{ id="crit1",  cardName="範囲拡大 I",   effectText="パドルのクリティカル範囲 +1%", rarity=Rarity.SR,  kind=CardKind.Permanent, value=1 },
        new Card{ id="crit2",  cardName="範囲拡大 II",  effectText="パドルのクリティカル範囲 +2%", rarity=Rarity.SR,  kind=CardKind.Permanent, value=2 },
        new Card{ id="crit3",  cardName="範囲拡大 III", effectText="パドルのクリティカル範囲 +3%", rarity=Rarity.SSR, kind=CardKind.Permanent, value=3 },
        // 奥義延長 I〜III: 奥義効果時間+秒
        new Card{ id="ult1",   cardName="奥義延長 I",   effectText="奥義の効果時間 +1秒",      rarity=Rarity.R,   kind=CardKind.Permanent, value=1 },
        new Card{ id="ult2",   cardName="奥義延長 II",  effectText="奥義の効果時間 +2秒",      rarity=Rarity.R,   kind=CardKind.Permanent, value=2 },
        new Card{ id="ult3",   cardName="奥義延長 III", effectText="奥義の効果時間 +3秒",      rarity=Rarity.SR,  kind=CardKind.Permanent, value=3 },
        // 回復 I〜III: ストック+（即時・上限7・満タン時は抽選除外）
        new Card{ id="heal1",  cardName="回復 I",       effectText="ストック +1",             rarity=Rarity.R,   kind=CardKind.Instant,   value=1 },
        new Card{ id="heal2",  cardName="回復 II",      effectText="ストック +2",             rarity=Rarity.R,   kind=CardKind.Instant,   value=2 },
        new Card{ id="heal3",  cardName="回復 III",     effectText="ストック +3",             rarity=Rarity.R,   kind=CardKind.Instant,   value=3 },
        // 次ステージ限定
        new Card{ id="turns20", cardName="集中打",      effectText="打数 +20",                rarity=Rarity.R,   kind=CardKind.NextWave,  value=20 },
        new Card{ id="weak2",  cardName="弱体 I",       effectText="ボスのHPが 1/2 になる",    rarity=Rarity.R,   kind=CardKind.NextWave,  value=2 },
        new Card{ id="weak3",  cardName="弱体 II",      effectText="ボスのHPが 1/3 になる",    rarity=Rarity.SR,  kind=CardKind.NextWave,  value=3 },
        new Card{ id="weak4",  cardName="弱体 III",     effectText="ボスのHPが 1/4 になる",    rarity=Rarity.SR,  kind=CardKind.NextWave,  value=4 },
        new Card{ id="mult2",  cardName="強化 I",       effectText="ヒットダメージ ×2",        rarity=Rarity.R,   kind=CardKind.NextWave,  value=2 },
        new Card{ id="mult3",  cardName="強化 II",      effectText="ヒットダメージ ×3",        rarity=Rarity.SR,  kind=CardKind.NextWave,  value=3 },
        new Card{ id="mult4",  cardName="強化 III",     effectText="ヒットダメージ ×4",        rarity=Rarity.SR,  kind=CardKind.NextWave,  value=4 },
        new Card{ id="immortal", cardName="不死身",     effectText="ボールを落としてもストックが減らない", rarity=Rarity.SR,  kind=CardKind.NextWave, value=0 },
        new Card{ id="fullcrit", cardName="覚醒",       effectText="パドル全体がクリティカル範囲になる", rarity=Rarity.SSR, kind=CardKind.NextWave, value=0 },
    };

    public static Card FindCard(string id) => catalog.Find(c => c.id == id);

    /// <summary>デバッグメニュー用: 全カード一覧</summary>
    public static IReadOnlyList<Card> Catalog => catalog;

    /// <summary>カード種別の表示タグ（即時／永続／次だけ）</summary>
    public static string KindTag(CardKind kind) => kind switch
    {
        CardKind.Instant => "即時",
        CardKind.Permanent => "永続",
        _ => "次だけ",
    };

    /// <summary>カードのアイコン名（Resources/CardIcons/ 配下のスプライト名）</summary>
    public static string IconKey(string id)
    {
        if (id.StartsWith("dmg")) return "icon_power";
        if (id.StartsWith("crit")) return "icon_target";
        if (id.StartsWith("ult")) return "icon_clock";
        if (id.StartsWith("heal")) return "icon_heal";
        if (id == "turns20") return "icon_turns";
        if (id.StartsWith("weak")) return "icon_weak";
        if (id.StartsWith("mult")) return "icon_sword";
        if (id == "immortal") return "icon_shield";
        return "icon_flame"; // fullcrit（覚醒）
    }

    // ============================================================
    // ラン状態
    // ============================================================

    /// <summary>エンドレスのラン中のみ true。false の間は全効果が中立値になる。</summary>
    public static bool Active { get; private set; }

    // 永続効果の累積
    static int permDamagePct;      // ヒットダメージ+%（加算合算）
    static float permCritPct;      // クリティカル範囲+%（加算・上限なし）
    static float permUltSec;       // 奥義効果時間+秒

    // 次ステージ限定効果（ウェーブ完全終了＝裏ボス込みでクリア）
    static int waveTurnsBonus;     // 集中打: 打数+N（表・裏それぞれの構築時に適用）
    static int waveBossHPDiv = 1;  // 弱体: ボスHP 1/N
    static int waveDamageMul = 1;  // 強化: ヒットダメージ×N
    static bool waveImmortal;      // 不死身
    static bool waveFullCrit;      // 覚醒

    // 提示中の3枚（中断セーブ対象。選択確定で消える）
    static List<string> presentedIds = new List<string>();

    // デバッグ用: 次回抽選を全カード順繰りにする
    public static bool DebugDrawAll;
    static int debugDrawCursor;

    /// <summary>エンドレスのラン開始時に呼ぶ（新規・再開どちらも）。状態を全クリアして有効化する。</summary>
    public static void StartRun()
    {
        ResetAll();
        Active = true;
    }

    /// <summary>通常ステージ開始時・ホーム復帰時に呼ぶ。エンドレス外への効果漏れを防ぐ。</summary>
    public static void ResetAll()
    {
        Active = false;
        permDamagePct = 0;
        permCritPct = 0f;
        permUltSec = 0f;
        ClearWaveEffects();
        presentedIds.Clear();
    }

    /// <summary>次ステージ限定効果をクリアする（ウェーブ完全終了時＝スコア加算直後に呼ぶ。裏突入では呼ばない）。</summary>
    public static void ClearWaveEffects()
    {
        waveTurnsBonus = 0;
        waveBossHPDiv = 1;
        waveDamageMul = 1;
        waveImmortal = false;
        waveFullCrit = false;
    }

    // ============================================================
    // 効果の参照（Active=false なら常に中立値）
    // ============================================================

    /// <summary>ヒットダメージ倍率（永続+% と 次ステージ×N の合成）。ダメージ計算の最後に掛ける。</summary>
    public static float DamageMultiplier
        => Active ? (1f + permDamagePct / 100f) * waveDamageMul : 1f;

    /// <summary>クリティカル範囲への加算値（0.01 = 1%）。BallController.EffectiveCriticalRange が参照。</summary>
    public static float CritRangeBonus
        => Active ? permCritPct / 100f : 0f;

    /// <summary>覚醒（パドル全面クリティカル）が有効か。</summary>
    public static bool FullCritActive => Active && waveFullCrit;

    /// <summary>不死身（ストック減無効）が有効か。</summary>
    public static bool ImmortalActive => Active && waveImmortal;

    /// <summary>集中打の打数ボーナス（表・裏それぞれの構築後に RefillBossTurns で加算する）。</summary>
    public static int WaveTurnsBonus => Active ? waveTurnsBonus : 0;

    /// <summary>弱体: ボスHPに 1/N を適用する（最低1保証）。StageManager のボスHP設定から呼ぶ。</summary>
    public static int ApplyBossWeaken(int bossHP)
        => (Active && waveBossHPDiv > 1) ? Mathf.Max(1, bossHP / waveBossHPDiv) : bossHP;

    /// <summary>奥義延長: 持続時間のある奥義（duration>0）に永続ボーナス秒を足す。</summary>
    public static float ExtendUltDuration(float duration)
        => (Active && duration > 0f) ? duration + permUltSec : duration;

    /// <summary>「取得した永続効果」表示用（0のものは非表示にする）。</summary>
    public static int PermanentDamagePct => Active ? permDamagePct : 0;
    public static float PermanentCritPct => Active ? permCritPct : 0f;
    public static float PermanentUltSec => Active ? permUltSec : 0f;

    // ============================================================
    // カード適用
    // ============================================================

    /// <summary>選択確定したカードの効果を適用する。次ステージ効果は「これから構築するウェーブ」に効く。</summary>
    public static void ApplyCard(Card card)
    {
        if (card == null || !Active) return;
        switch (card.id)
        {
            case "dmg3": case "dmg5": case "dmg10": case "dmg15":
                permDamagePct += card.value; break;
            case "crit1": case "crit2": case "crit3":
                permCritPct += card.value; break;
            case "ult1": case "ult2": case "ult3":
                permUltSec += card.value; break;
            case "heal1": case "heal2": case "heal3":
                GameManager.Instance?.AddStock(card.value); break;
            case "turns20":
                waveTurnsBonus += card.value; break;
            case "weak2": case "weak3": case "weak4":
                waveBossHPDiv = card.value; break;
            case "mult2": case "mult3": case "mult4":
                waveDamageMul = card.value; break;
            case "immortal":
                waveImmortal = true; break;
            case "fullcrit":
                waveFullCrit = true; break;
        }
        presentedIds.Clear();
        Debug.Log($"[EndlessBuff] カード適用: {card.cardName} " +
            $"(永続 dmg+{permDamagePct}% crit+{permCritPct}% ult+{permUltSec}s / " +
            $"次だけ turns+{waveTurnsBonus} bossHP 1/{waveBossHPDiv} ×{waveDamageMul} " +
            $"immortal={waveImmortal} fullcrit={waveFullCrit})");
    }

    // ============================================================
    // 抽選
    // ============================================================

    const float RateSSR = 0.03f;
    const float RateSR = 0.10f;

    /// <summary>
    /// 提示する3枚を返す。中断からの再開などで保持中の3枚があればそれを返し、
    /// なければ新規に抽選して保持する（中断セーブで同じ3枚を復元するため）。
    /// </summary>
    public static List<Card> GetOrDrawPresentedCards(int currentStock)
    {
        var cards = new List<Card>();
        foreach (var id in presentedIds)
        {
            var c = FindCard(id);
            if (c != null) cards.Add(c);
        }
        if (cards.Count == 3) return cards;

        cards = DrawCards(currentStock);
        presentedIds.Clear();
        foreach (var c in cards) presentedIds.Add(c.id);
        return cards;
    }

    static List<Card> DrawCards(int currentStock)
    {
        // デバッグ: カタログを3枚ずつ順繰りに提示（全カードの表示確認用）
        if (DebugDrawAll)
        {
            var dbg = new List<Card>();
            for (int i = 0; i < 3; i++)
                dbg.Add(catalog[(debugDrawCursor + i) % catalog.Count]);
            debugDrawCursor = (debugDrawCursor + 3) % catalog.Count;
            return dbg;
        }

        bool stockFull = currentStock >= 7;
        var picked = new List<Card>();
        for (int slot = 0; slot < 3; slot++)
        {
            float roll = Random.value;
            Rarity rarity = roll < RateSSR ? Rarity.SSR
                          : roll < RateSSR + RateSR ? Rarity.SR : Rarity.R;

            var pool = catalog.FindAll(c =>
                c.rarity == rarity
                && !picked.Contains(c)
                && !(stockFull && c.id.StartsWith("heal")));

            // 同レア度の残りが尽きたら R にフォールバック（通常は起こらない保険）
            if (pool.Count == 0)
                pool = catalog.FindAll(c =>
                    c.rarity == Rarity.R && !picked.Contains(c)
                    && !(stockFull && c.id.StartsWith("heal")));

            if (pool.Count > 0)
                picked.Add(pool[Random.Range(0, pool.Count)]);
        }
        return picked;
    }

    // ============================================================
    // 中断セーブ（EndlessManager の Suspend と対で使う）
    // ============================================================

    const string KeySuspendPerm = "GachaBlock_EndlessSuspendPerm";   // "dmg,crit,ult"
    const string KeySuspendCards = "GachaBlock_EndlessSuspendCards"; // "id1,id2,id3"

    /// <summary>中断時に永続効果の合計と提示中の3枚を保存する（EndlessManager.SaveSuspend と同時に呼ぶ）。</summary>
    public static void SaveSuspend()
    {
        PlayerPrefs.SetString(KeySuspendPerm,
            $"{permDamagePct},{permCritPct:0.##},{permUltSec:0.##}");
        PlayerPrefs.SetString(KeySuspendCards, string.Join(",", presentedIds));
        PlayerPrefs.Save();
    }

    /// <summary>再開時に呼ぶ（StartRun の後）。永続効果と提示中カードを復元する。</summary>
    public static void LoadSuspend()
    {
        var perm = PlayerPrefs.GetString(KeySuspendPerm, "");
        var parts = perm.Split(',');
        if (parts.Length == 3)
        {
            int.TryParse(parts[0], out permDamagePct);
            float.TryParse(parts[1], out permCritPct);
            float.TryParse(parts[2], out permUltSec);
        }
        presentedIds.Clear();
        var cardsStr = PlayerPrefs.GetString(KeySuspendCards, "");
        if (!string.IsNullOrEmpty(cardsStr))
            foreach (var id in cardsStr.Split(','))
                if (FindCard(id) != null) presentedIds.Add(id);
        Debug.Log($"[EndlessBuff] 中断データ復元: dmg+{permDamagePct}% crit+{permCritPct}% " +
            $"ult+{permUltSec}s cards=[{cardsStr}]");
    }

    /// <summary>中断データ削除時に合わせて呼ぶ（EndlessManager.ClearSuspend から）。</summary>
    public static void ClearSuspendData()
    {
        PlayerPrefs.DeleteKey(KeySuspendPerm);
        PlayerPrefs.DeleteKey(KeySuspendCards);
    }

    /// <summary>再開時に提示中カードが保存されているか（旧バージョンの中断データには無い）。</summary>
    public static bool HasPresentedCards => presentedIds.Count == 3;
}
