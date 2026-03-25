using UnityEngine;

public enum Rarity { N, R, SR, SSR }

public enum PassiveEffectType
{
    None,
    BallSpeedUp,    // ball.speed *= passiveValue
    ExtraDamage,    // ブロックへのダメージ + (int)passiveValue
    ExtraStock,     // 開始時ストック + (int)passiveValue
    UltGaugeBoost,  // ゲージ増加量 × passiveValue 倍
}

public enum UltimateSkillType
{
    None,
    SpeedBurst,     // ultimateDuration 秒間 speed *= ultimateValue
    MassDestroy,    // 全ブロックに (int)ultimateValue ダメージ
    StockRecover,   // ストック +1
    BarrierShot,    // 次の1ミスをキャンセル
}

[CreateAssetMenu(fileName = "CharaData", menuName = "GachaBlock/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("基本情報")]
    public string characterName;
    public Rarity rarity;
    public Color rarityColor = Color.white;

    [Header("パッシブ効果")]
    public PassiveEffectType passiveType;
    public float passiveValue;

    [Header("奥義")]
    public UltimateSkillType ultimateType;
    public float ultimateValue = 1f;
    public float ultimateDuration = 3f;
    public int ultimateGaugeCost = 10;

    [Header("説明")]
    [TextArea(2, 4)]
    public string description;
}
