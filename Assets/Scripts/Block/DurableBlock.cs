using UnityEngine;

/// <summary>
/// 耐久ブロック。HP が複数あり、ダメージを受けるたびに見た目が変化する
/// </summary>
public class DurableBlock : BlockBase
{
    [Header("HP 別スプライト（HP 高い順に設定）")]
    [SerializeField] private Color[] hpColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),   // HP3: 赤
        new Color(1f, 0.7f, 0.2f),   // HP2: オレンジ
        new Color(1f, 1f, 0.3f),     // HP1: 黄
    };

    private SpriteRenderer sr;

    protected override void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        base.Start();
    }

    /// <summary>
    /// StageManager から HP を外部設定するために使用
    /// </summary>
    public void SetHP(int hp)
    {
        maxHP = Mathf.Clamp(hp, 1, 5);
        currentHP = maxHP;
        UpdateVisual();
    }

    protected override void UpdateVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (hpColors == null || hpColors.Length == 0) return;

        // HP に応じて色を変える（配列の範囲内でクランプ）
        int index = Mathf.Clamp(currentHP - 1, 0, hpColors.Length - 1);
        sr.color = hpColors[index];
    }

    protected override void DestroyBlock()
    {
        NotifyAndDestroy();
    }
}
