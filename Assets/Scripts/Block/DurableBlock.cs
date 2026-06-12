using UnityEngine;
using System.Collections;

/// <summary>
/// 耐久ブロック。HP が複数あり、ダメージを受けるたびにHPバーが減っていく
/// </summary>
public class DurableBlock : BlockBase
{
    [Header("HP カラー（自動グラデーション）")]
    [SerializeField] private Color lowHPColor  = new Color(1f, 1f, 0.3f);     // HP1: 黄
    [SerializeField] private Color highHPColor = new Color(0.15f, 0.0f, 0.3f); // HP100: 漆黒紫

    private SpriteRenderer sr;

    // HPバー用 SpriteRenderer
    private SpriteRenderer hpBarBg;   // 背景（黒枠）
    private SpriteRenderer hpBarFill; // 前景（緑→黄→赤）

    // HPバーのスムーズアニメーション用
    private float displayedRatio = 1f; // 現在表示中のHP割合
    private Coroutine barAnimCoroutine;

    // HPバー基準サイズ（1.0 のときのローカル座標での幅）
    private const float BarWidthWorld  = 0.8f;  // ブロック幅の80%
    private const float BarHeightWorld = 0.12f; // 細めのバー

    protected override void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        CreateHPBar();
        base.Start();
        displayedRatio = 1f;
    }

    void CreateHPBar()
    {
        // 親のスケール逆補正値を計算（ブロックPrefabは X=1.5, Y=0.8 等）
        Vector3 parentScale = transform.localScale;
        float invX = (parentScale.x != 0f) ? 1f / parentScale.x : 1f;
        float invY = (parentScale.y != 0f) ? 1f / parentScale.y : 1f;

        // 共通の1x1白スクエアスプライトを作成（InspectorでAssign不要）
        Sprite squareSprite = CreateWhiteSquare();

        // --- 背景（黒枠） ---
        var bgGo = new GameObject("HPBarBg");
        bgGo.transform.SetParent(transform, false);
        bgGo.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        hpBarBg = bgGo.AddComponent<SpriteRenderer>();
        hpBarBg.sprite = squareSprite;
        hpBarBg.color = new Color(0f, 0f, 0f, 0.85f);
        hpBarBg.sortingOrder = 10;
        // 逆スケール補正 + 実サイズ
        bgGo.transform.localScale = new Vector3(
            invX * (BarWidthWorld + 0.06f),
            invY * (BarHeightWorld + 0.04f),
            1f);

        // --- 前景（緑→黄→赤のHPフィル） ---
        // pivotを左端にしてスケールX変更で左から減るように
        var fillGo = new GameObject("HPBarFill");
        fillGo.transform.SetParent(transform, false);
        // ローカルで左端の位置（背景バー左端）へ
        float leftX = -(BarWidthWorld * 0.5f);
        fillGo.transform.localPosition = new Vector3(leftX * invX, 0f, -0.11f);

        hpBarFill = fillGo.AddComponent<SpriteRenderer>();
        hpBarFill.sprite = CreateLeftPivotSquare();
        hpBarFill.color = new Color(0.2f, 1f, 0.3f, 1f); // 初期：緑
        hpBarFill.sortingOrder = 11;
        fillGo.transform.localScale = new Vector3(
            invX * BarWidthWorld,
            invY * BarHeightWorld,
            1f);
    }

    // 中心pivot 1x1 白スクエア（背景用、共通可）
    static Sprite _centerSquare;
    static Sprite CreateWhiteSquare()
    {
        if (_centerSquare != null) return _centerSquare;
        var tex = new Texture2D(4, 4);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        _centerSquare = Sprite.Create(tex, new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f), 4);
        return _centerSquare;
    }

    // 左端pivot 1x1 白スクエア（HPフィル用。Xスケールで左から減っていく）
    static Sprite _leftSquare;
    static Sprite CreateLeftPivotSquare()
    {
        if (_leftSquare != null) return _leftSquare;
        var tex = new Texture2D(4, 4);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        _leftSquare = Sprite.Create(tex, new Rect(0, 0, 4, 4),
            new Vector2(0f, 0.5f), 4); // pivot.x = 0（左端）
        return _leftSquare;
    }

    /// <summary>
    /// StageManager から HP を外部設定するために使用
    /// </summary>
    public void SetHP(int hp)
    {
        maxHP = Mathf.Clamp(hp, 1, 100);
        currentHP = maxHP;
        displayedRatio = 1f;
        UpdateVisual();
    }

    protected override void UpdateVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // HP に応じてブロック色を黄→濃色にグラデーション（既存の仕様維持）
        float t = (maxHP > 1) ? (float)(currentHP - 1) / (maxHP - 1) : 0f;
        Color col = Color.Lerp(lowHPColor, highHPColor, t);
        col.a = blockAlpha;
        sr.color = col;

        // HPバー更新（HP2以上のとき表示、HP1以下は非表示）
        if (hpBarBg != null && hpBarFill != null)
        {
            bool show = (maxHP >= 2);
            hpBarBg.gameObject.SetActive(show);
            hpBarFill.gameObject.SetActive(show);

            if (show)
            {
                float targetRatio = Mathf.Clamp01((float)currentHP / maxHP);
                // スムーズアニメーションで現在値→目標値へ
                if (barAnimCoroutine != null) StopCoroutine(barAnimCoroutine);
                barAnimCoroutine = StartCoroutine(AnimateHPBar(targetRatio));
            }
        }
    }

    IEnumerator AnimateHPBar(float targetRatio)
    {
        float startRatio = displayedRatio;
        float duration = 0.3f; // スムーズさ
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            // イージング（ease-out）
            float eased = 1f - (1f - k) * (1f - k);
            displayedRatio = Mathf.Lerp(startRatio, targetRatio, eased);
            ApplyBarVisual(displayedRatio);
            yield return null;
        }
        displayedRatio = targetRatio;
        ApplyBarVisual(displayedRatio);
        barAnimCoroutine = null;
    }

    void ApplyBarVisual(float ratio)
    {
        if (hpBarFill == null) return;

        // スケールXを比率に合わせる（親スケールの逆補正も維持）
        Vector3 parentScale = transform.localScale;
        float invX = (parentScale.x != 0f) ? 1f / parentScale.x : 1f;
        float invY = (parentScale.y != 0f) ? 1f / parentScale.y : 1f;
        Vector3 scale = hpBarFill.transform.localScale;
        scale.x = invX * BarWidthWorld * ratio;
        scale.y = invY * BarHeightWorld;
        hpBarFill.transform.localScale = scale;

        // 色: 緑(>0.6) → 黄(0.3~0.6) → 赤(<0.3)
        Color c;
        if (ratio > 0.6f)
            c = Color.Lerp(new Color(1f, 0.95f, 0.2f), new Color(0.2f, 1f, 0.3f),
                           (ratio - 0.6f) / 0.4f);
        else if (ratio > 0.3f)
            c = Color.Lerp(new Color(1f, 0.4f, 0.2f), new Color(1f, 0.95f, 0.2f),
                           (ratio - 0.3f) / 0.3f);
        else
            c = Color.Lerp(new Color(1f, 0.1f, 0.1f), new Color(1f, 0.4f, 0.2f),
                           ratio / 0.3f);
        hpBarFill.color = c;
    }

    protected override void DestroyBlock()
    {
        NotifyAndDestroy();
    }
}
