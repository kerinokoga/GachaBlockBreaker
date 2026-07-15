using UnityEngine;

public enum Rarity { N, R, SR, SSR }

public enum PassiveEffectType
{
    None,
    BallDamageUp,     // ボールダメージ倍率 × passiveValue（旧 BallSpeedUp）
    ExtraDamage,      // ブロックへのダメージ + (int)passiveValue
    ExtraStock,       // 開始時ストック + (int)passiveValue
    UltGaugeBoost,    // ゲージ増加量 × passiveValue 倍
    CriticalRangeUp,  // クリティカル範囲 + passiveValue%（デフォルト3%に加算）
}

public enum UltimateSkillType
{
    None,
    PowerBurst,     // ultimateDuration 秒間 ダメージ × ultimateValue（旧 SpeedBurst）
    MassDestroy,    // 全ブロックに (int)ultimateValue ダメージ
    StockRecover,   // ストック +(int)ultimateValue
    BarrierShot,    // 次の1ミスをキャンセル
    Penetrate,      // ultimateDuration 秒間 ボールがブロックを貫通
    BallSplit,      // ボールを2つに分裂（各ボールがさらに分裂可能）
    GaugeCharge,    // 味方全員の奥義ゲージ +(int)ultimateValue（複合奥義のおまけ用）
}

[CreateAssetMenu(fileName = "CharaData", menuName = "GachaBlock/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("基本情報")]
    public string characterName;
    public Rarity rarity;
    public Color rarityColor = Color.white;
    /// <summary>キャラアイコン（ガチャ結果・管理画面等で使用）</summary>
    public Sprite icon;

    [Header("パッシブ効果")]
    public PassiveEffectType passiveType;
    public float passiveValue;

    [Header("パッシブ効果2（複合パッシブ用）")]
    public PassiveEffectType passiveType2;
    public float passiveValue2;

    [Header("奥義")]
    public UltimateSkillType ultimateType;
    public float ultimateValue = 1f;
    public float ultimateDuration = 3f;
    public int ultimateGaugeCost = 10;

    [Header("奥義の追加効果（複合奥義用。None なら単一効果）")]
    public UltimateSkillType ultimateType2 = UltimateSkillType.None;
    public float ultimateValue2 = 0f;
    public float ultimateDuration2 = 0f;

    [Header("ボイス共通設定")]
    /// <summary>このキャラのボイス全体の音量倍率（1.0 が標準）。録音音量が小さい場合は 1.5 等に上げる。</summary>
    [Range(0.1f, 3f)]
    public float voiceVolumeMultiplier = 1f;

    [Header("ボイス — タイトル画面用")]
    /// <summary>タイトル画面でランダム再生される「ぶろっくぶれいかー♡」のボイス。</summary>
    public AudioClip voiceTitle;

    [Header("ボイス — プレイヤーキャラ用")]
    public AudioClip voiceSelect;       // キャラ選択時
    public AudioClip voiceUlt;          // 奥義発動時
    public AudioClip voiceVictory;      // ステージクリア時（破壊率100%）
    public AudioClip voiceDefeat;       // ゲームオーバー時
    public AudioClip voiceStageStart;   // ステージ開始時
    public AudioClip voiceDestroy30;    // 破壊率30%時
    public AudioClip voiceDestroy60;    // 破壊率60%時

    [Header("ボイス — ボスキャラ用")]
    public AudioClip voiceBossDamaged;  // ボスHP減少時（50ごと）
    public AudioClip voiceBossAttack;   // ボス攻撃時
    [Header("説明")]
    [TextArea(2, 4)]
    public string description;
}
