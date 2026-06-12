using UnityEngine;

/// <summary>
/// ボスブロック。5ステージごとに登場する大型ブロック。
/// HPバー付き。HP減少の閾値（75%/50%/25%）で攻撃を発動する。
/// 攻撃パターン: パドル縮小、ブロック降下、スピードブロック降下
/// </summary>
public class BossBlock : BlockBase
{
    [Header("ボス設定")]
    [SerializeField] private Color bossColor = new Color(0.6f, 0.05f, 0.1f);
    [SerializeField] private Color bossDamagedColor = new Color(1f, 0.2f, 0.2f);

    private SpriteRenderer sr;
    private SpriteRenderer iconRenderer; // キャラアイコン表示用
    private TextMesh hpText;

    // HPバー用
    private SpriteRenderer hpBarBg;
    private SpriteRenderer hpBarFill;
    private float hpBarWidth = 2.5f;

    // 攻撃閾値トラッキング
    private bool attacked75 = false;
    private bool attacked50 = false;
    private bool attacked25 = false;

    // 攻撃コールバック（StageManager が設定）
    public System.Action<BossBlock, int> OnBossAttack; // int = 攻撃フェーズ(1,2,3)

    // HP50ごとのダメージ通知（StageManager が設定）
    public System.Action<BossBlock> OnBossHPMilestone;
    // HP変動通知（hpRatio 0.0〜1.0）— イラスト切替用
    public System.Action<float> OnBossHPChanged;
    private int lastMilestoneHP; // 前回通知したHP閾値

    // ステージ難度（StageManager から設定）
    public int difficulty = 1; // 1=Easy(Stage5), 2=Mid(10), 3=Hard(15), 4=VeryHard(20)

    // 制限ターン（パドルヒット回数）
    public int maxTurns = 30;  // StageManager から設定
    public bool IsAlive => currentHP > 0;

    protected override void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        // 赤い長方形ブロック本体を非表示にする（アイコンのみ表示）
        if (sr != null) sr.enabled = false;
        CreateHPText();
        CreateHPBar();
        base.Start();
    }

    /// <summary>
    /// キャラアイコンをボスブロック上に表示する
    /// </summary>
    public void SetIcon(Sprite icon)
    {
        if (icon == null) return;

        Vector3 ps = transform.localScale;
        float invX = (ps.x != 0f) ? 1f / ps.x : 1f;
        float invY = (ps.y != 0f) ? 1f / ps.y : 1f;
        float iconScale = 0.15f;
        float iconY = 0.35f; // 通常ブロックの上に余裕を持って配置

        // 赤い枠（アイコンと同じスプライトを少し大きく表示）
        var frameGo = new GameObject("BossFrame");
        frameGo.transform.SetParent(transform, false);
        var frameSR = frameGo.AddComponent<SpriteRenderer>();
        frameSR.sprite = icon; // アイコンと同じ形
        frameSR.color = new Color(0.9f, 0.1f, 0.1f, 1f);
        frameSR.sortingOrder = 3;
        float frameScale = iconScale * 1.12f;
        frameGo.transform.localScale = new Vector3(invX * frameScale, invY * frameScale, 1f);
        frameGo.transform.localPosition = new Vector3(0f, iconY, -0.04f);

        // アイコン本体
        var iconGo = new GameObject("BossIcon");
        iconGo.transform.SetParent(transform, false);
        iconRenderer = iconGo.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = icon;
        iconRenderer.sortingOrder = 4;
        iconGo.transform.localScale = new Vector3(invX * iconScale, invY * iconScale, 1f);
        iconGo.transform.localPosition = new Vector3(0f, iconY, -0.05f);

        // 当たり判定をアイコンの大きさに合わせる
        var col = GetComponent<BoxCollider2D>();
        if (col != null && iconRenderer.sprite != null)
        {
            float spriteW = iconRenderer.sprite.bounds.size.x * iconScale;
            float spriteH = iconRenderer.sprite.bounds.size.y * iconScale;
            col.size = new Vector2(spriteW * invX, spriteH * invY);
            col.offset = new Vector2(0f, iconY);
        }
    }

    /// <summary>
    /// StageManager から HP を外部設定
    /// </summary>
    public void SetHP(int hp)
    {
        maxHP = Mathf.Clamp(hp, 1, 9999);
        currentHP = maxHP;
        lastMilestoneHP = maxHP; // 初期HP = 最初のマイルストーン
        UpdateVisual();
    }

    void CreateHPText()
    {
        var textGo = new GameObject("BossHPText");
        textGo.transform.SetParent(transform, false);
        textGo.transform.localPosition = new Vector3(0f, -0.25f, -0.1f);

        hpText = textGo.AddComponent<TextMesh>();
        hpText.alignment = TextAlignment.Center;
        hpText.anchor = TextAnchor.MiddleCenter;
        hpText.fontSize = 60;
        hpText.characterSize = 0.06f;
        hpText.color = Color.white;
        hpText.fontStyle = FontStyle.Bold;

        // 親スケール逆補正
        Vector3 parentScale = transform.localScale;
        float invX = (parentScale.x != 0f) ? 1f / parentScale.x : 1f;
        float invY = (parentScale.y != 0f) ? 1f / parentScale.y : 1f;
        textGo.transform.localScale = new Vector3(invX, invY, 1f);

        var mr = textGo.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 5;
    }

    void CreateHPBar()
    {
        // HPバー背景（黒）
        var bgGo = new GameObject("HPBarBg");
        bgGo.transform.SetParent(transform, false);
        hpBarBg = bgGo.AddComponent<SpriteRenderer>();
        hpBarBg.sprite = MakeWhiteSprite();
        hpBarBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        hpBarBg.sortingOrder = 3;

        Vector3 parentScale = transform.localScale;
        float invX = (parentScale.x != 0f) ? 1f / parentScale.x : 1f;
        float invY = (parentScale.y != 0f) ? 1f / parentScale.y : 1f;

        bgGo.transform.localPosition = new Vector3(0f, -0.85f * invY, -0.05f);
        bgGo.transform.localScale = new Vector3(hpBarWidth * invX, 0.18f * invY, 1f);

        // HPバーFill（赤→黄グラデーション）
        var fillGo = new GameObject("HPBarFill");
        fillGo.transform.SetParent(transform, false);
        hpBarFill = fillGo.AddComponent<SpriteRenderer>();
        hpBarFill.sprite = MakeWhiteSprite();
        hpBarFill.color = new Color(0.9f, 0.15f, 0.15f);
        hpBarFill.sortingOrder = 4;

        fillGo.transform.localPosition = new Vector3(0f, -0.85f * invY, -0.06f);
        fillGo.transform.localScale = new Vector3((hpBarWidth - 0.08f) * invX, 0.13f * invY, 1f);
    }

    Sprite MakeWhiteSprite()
    {
        var tex = new Texture2D(4, 4);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    protected override void UpdateVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        float hpRatio = (maxHP > 0) ? (float)currentHP / maxHP : 0f;

        // ブロック本体が表示されている場合のみカラー変化
        if (sr != null && sr.enabled)
        {
            Color col = Color.Lerp(bossDamagedColor, bossColor, hpRatio);
            col.a = blockAlpha;
            sr.color = col;
        }

        // HPテキスト更新
        if (hpText != null)
        {
            hpText.text = currentHP.ToString();
            hpText.gameObject.SetActive(currentHP > 0);
        }

        // HPバー更新
        if (hpBarFill != null)
        {
            Vector3 parentScale = transform.localScale;
            float invX = (parentScale.x != 0f) ? 1f / parentScale.x : 1f;
            float invY = (parentScale.y != 0f) ? 1f / parentScale.y : 1f;

            float fillWidth = (hpBarWidth - 0.08f) * hpRatio;
            hpBarFill.transform.localScale = new Vector3(fillWidth * invX, 0.13f * invY, 1f);

            // 左寄せ（バーが左から減る）
            float offset = -(hpBarWidth - 0.08f) * (1f - hpRatio) * 0.5f;
            Vector3 pos = hpBarFill.transform.localPosition;
            pos.x = offset * invX;
            pos.y = -0.85f * invY;
            hpBarFill.transform.localPosition = pos;

            // 色変化（緑→黄→赤）
            if (hpRatio > 0.5f)
                hpBarFill.color = Color.Lerp(new Color(1f, 0.9f, 0.1f), new Color(0.2f, 0.9f, 0.2f), (hpRatio - 0.5f) * 2f);
            else
                hpBarFill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(1f, 0.9f, 0.1f), hpRatio * 2f);
        }

        // 攻撃閾値チェック
        CheckAttackThresholds(hpRatio);

        // HP50ごとのマイルストーン通知
        CheckHPMilestone();

        // HP変動通知（イラスト切替用）
        OnBossHPChanged?.Invoke(hpRatio);
    }

    void CheckHPMilestone()
    {
        if (currentHP <= 0) return;
        // 現在HPが前回マイルストーンから50以上減っていたら通知
        int nextMilestone = lastMilestoneHP - 50;
        if (currentHP <= nextMilestone)
        {
            lastMilestoneHP = (currentHP / 50) * 50 + 50; // 次の50区切りに更新
            OnBossHPMilestone?.Invoke(this);
        }
    }

    void CheckAttackThresholds(float hpRatio)
    {
        if (!attacked75 && hpRatio <= 0.75f && currentHP > 0)
        {
            attacked75 = true;
            OnBossAttack?.Invoke(this, 1);
        }
        if (!attacked50 && hpRatio <= 0.50f && currentHP > 0)
        {
            attacked50 = true;
            OnBossAttack?.Invoke(this, 2);
        }
        if (!attacked25 && hpRatio <= 0.25f && currentHP > 0)
        {
            attacked25 = true;
            OnBossAttack?.Invoke(this, 3);
        }
    }

    protected override void DestroyBlock()
    {
        // ボス撃破SE（通常と異なる派手な音）
        AudioManager.Instance?.PlaySE(AudioManager.Instance.seBlockBreak);
        NotifyAndDestroy();
    }
}
