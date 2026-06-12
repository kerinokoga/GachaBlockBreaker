using UnityEngine;
using System.Collections;

/// <summary>
/// 全ブロックの基底クラス。HP管理・ダメージ処理・破壊イベントを担う
/// </summary>
public abstract class BlockBase : MonoBehaviour
{
    [Header("ブロック設定")]
    [SerializeField] protected int maxHP = 1;

    protected int currentHP;

    // StageManager が購読して破壊数をカウントする
    public System.Action<BlockBase> OnBlockDestroyed;

    [Header("透過設定")]
    [SerializeField] protected float blockAlpha = 0.7f;

    protected SpriteRenderer baseSR;

    // パンチスケール制御用（連続ヒットで累積バグを防ぐ）
    private Coroutine punchCoroutine;
    private Vector3 baseScale = Vector3.one;
    private bool baseScaleRecorded = false;

    protected virtual void Start()
    {
        currentHP = maxHP;
        baseSR = GetComponent<SpriteRenderer>();
        // ブロックを半透明にして背面のイラストが透けて見えるようにする
        if (baseSR != null)
        {
            var c = baseSR.color;
            c.a = blockAlpha;
            baseSR.color = c;
        }
        // パンチ復帰用の基準スケールを記録
        if (!baseScaleRecorded)
        {
            baseScale = transform.localScale;
            baseScaleRecorded = true;
        }
        UpdateVisual();
    }

    /// <summary>
    /// ダメージを受ける。amount 分 HP を減らし、0 以下で破壊する
    /// </summary>
    public virtual void TakeDamage(int amount = 1)
    {
        if (currentHP <= 0) return; // 破壊済みなら無視

        int actualDamage = Mathf.Min(amount, currentHP);
        currentHP -= amount;
        AudioManager.Instance?.PlaySE(AudioManager.Instance?.seBlockBreak);

        // ダメージ数値ポップアップ（全ブロック共通）
        SpawnDamagePopup(actualDamage);

        // 被弾フラッシュ + パンチスケール
        if (currentHP > 0)
        {
            StartCoroutine(HitFlash());
            // 既存のパンチを停止してから新規開始（累積拡大バグ対策）
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            punchCoroutine = StartCoroutine(PunchScale());
        }

        UpdateVisual();

        if (currentHP <= 0)
            DestroyBlock();
    }

    /// <summary>
    /// 被弾時に一瞬拡大して元に戻る（punch効果、0.1秒）
    /// 基準スケール（baseScale）から開始/復帰するため、連続ヒットしても累積拡大しない
    /// </summary>
    IEnumerator PunchScale()
    {
        // 連続ヒット時は常に baseScale を基準にして 1.15 倍まで
        Vector3 origScale = baseScale;
        Vector3 maxScale = origScale * 1.15f;

        // 即座にスケールを基準値へ戻してから開始（中断直後の中途スケール対策）
        transform.localScale = origScale;

        // 0→0.05s で拡大
        float t = 0f;
        while (t < 0.05f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.05f);
            transform.localScale = Vector3.Lerp(origScale, maxScale, k);
            yield return null;
        }
        // 0.05→0.1s で元に戻す
        t = 0f;
        while (t < 0.05f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.05f);
            transform.localScale = Vector3.Lerp(maxScale, origScale, k);
            yield return null;
        }
        transform.localScale = origScale;
        punchCoroutine = null;
    }

    /// <summary>
    /// ダメージ数値を上方向に飛ばしながらフェードアウト
    /// ブロックが破壊されても動き続けるよう、ポップアップ自身で動く
    /// </summary>
    void SpawnDamagePopup(int dmg)
    {
        if (dmg <= 0) return;

        // クリティカル判定（BallController の状態を取得）
        var ball = FindObjectOfType<BallController>();
        bool isCrit = (ball != null && ball.IsCritical);

        var go = new GameObject("DmgPopup");
        go.transform.position = transform.position + new Vector3(0f, 0.2f, -1f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = $"-{dmg}";
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = isCrit ? 90 : 60;
        tm.characterSize = 0.08f;
        tm.fontStyle = FontStyle.Bold;
        tm.color = isCrit ? new Color(1f, 0.9f, 0.2f) : Color.white;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 30;

        // 自走するアニメーターを付与（ブロック破壊の影響を受けない）
        var anim = go.AddComponent<DamagePopupAnim>();
        anim.Init(tm, isCrit);
    }

    /// <summary>
    /// 被弾時に白くフラッシュする演出
    /// </summary>
    IEnumerator HitFlash()
    {
        if (baseSR == null) yield break;
        Color original = baseSR.color;
        baseSR.color = new Color(1f, 1f, 1f, 0.95f);
        yield return new WaitForSeconds(0.06f);
        if (baseSR != null) baseSR.color = original;
    }

    /// <summary>
    /// HP に応じてビジュアルを更新する（サブクラスでオーバーライド）
    /// </summary>
    protected virtual void UpdateVisual() { }

    /// <summary>
    /// ブロックを破壊する（サブクラスで実装必須）
    /// </summary>
    protected abstract void DestroyBlock();

    /// <summary>
    /// 破壊イベントを発火してから GameObject を削除する
    /// </summary>
    protected void NotifyAndDestroy()
    {
        SpawnBreakParticles();
        OnBlockDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    /// <summary>
    /// 破壊時にブロックの色で破片パーティクルを生成
    /// </summary>
    void SpawnBreakParticles()
    {
        Color col = (baseSR != null) ? baseSR.color : Color.white;
        col.a = 1f;
        int count = 6;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Particle");
            go.transform.position = transform.position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            // 破片の色にランダムなバリエーション
            float rv = Random.Range(-0.1f, 0.1f);
            sr.color = new Color(
                Mathf.Clamp01(col.r + rv),
                Mathf.Clamp01(col.g + rv),
                Mathf.Clamp01(col.b + rv), 1f);
            sr.sortingOrder = 20;
            go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = Random.Range(2f, 4f);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float force = Random.Range(3f, 6f);
            rb.velocity = new Vector2(Mathf.Cos(angle) * force, Mathf.Sin(angle) * force);
            rb.angularVelocity = Random.Range(-360f, 360f);

            Destroy(go, 0.6f);
        }
    }

    static Sprite _squareSprite;
    static Sprite CreateSquareSprite()
    {
        if (_squareSprite != null) return _squareSprite;
        var tex = new Texture2D(4, 4);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        _squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        return _squareSprite;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ball"))
            ApplyBallDamage();
    }

    /// <summary>
    /// 貫通モード時は OnTriggerEnter2D から呼ばれる
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ball"))
            ApplyBallDamage();
    }

    void ApplyBallDamage()
    {
        // 速度ダメージ倍率を取得（速度が上がるほどダメージUP）
        float speedRatio = 1f;
        var ball = FindObjectOfType<BallController>();
        if (ball != null) speedRatio = ball.SpeedDamageRatio;

        // クリティカル貫通中はダメージ2倍
        float criticalMul = (ball != null && ball.IsCritical) ? 2f : 1f;

        // ダメージ = (3キャラのパワー合計 + ExtraDamage) × パッシブ倍率 × 奥義倍率 × 速度倍率 × クリティカル倍率
        float baseDmg = (CharacterManager.Instance?.BasePower ?? 1f)
                      + (CharacterManager.Instance?.BonusDamage ?? 0);
        float mul   = (CharacterManager.Instance?.PassiveDamageMultiplier ?? 1f)
                    * (CharacterManager.Instance?.UltDamageMultiplier ?? 1f)
                    * speedRatio
                    * criticalMul;
        TakeDamage((int)System.Math.Ceiling(baseDmg * mul));
    }
}

/// <summary>
/// ダメージ数値ポップアップの自走アニメーター。
/// 親ブロックが Destroy されても、ポップアップは独立して最後まで動く
/// </summary>
public class DamagePopupAnim : MonoBehaviour
{
    TextMesh mainTM;
    TextMesh shadowTM;
    Color startCol;
    Vector3 startPos;
    const float Duration = 0.7f;
    float elapsed = 0f;

    public void Init(TextMesh tm, bool isCrit)
    {
        mainTM = tm;
        startCol = tm.color;
        startPos = transform.position;

        // 黒い影をつけてアウトラインの代わりに
        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(transform, false);
        shadow.transform.localPosition = new Vector3(0.05f, -0.05f, 0.01f);
        shadowTM = shadow.AddComponent<TextMesh>();
        shadowTM.text = tm.text;
        shadowTM.anchor = tm.anchor;
        shadowTM.alignment = tm.alignment;
        shadowTM.fontSize = tm.fontSize;
        shadowTM.characterSize = tm.characterSize;
        shadowTM.fontStyle = tm.fontStyle;
        shadowTM.color = new Color(0f, 0f, 0f, 0.8f);
        var smr = shadow.GetComponent<MeshRenderer>();
        if (smr != null) smr.sortingOrder = 29;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float k = elapsed / Duration;

        // 上に浮き上がる
        transform.position = startPos + new Vector3(0f, k * 0.8f, 0f);

        // フェード（後半）
        float alpha = (k < 0.5f) ? 1f : Mathf.Max(0f, 1f - (k - 0.5f) * 2f);
        if (mainTM != null)
            mainTM.color = new Color(startCol.r, startCol.g, startCol.b, alpha);
        if (shadowTM != null)
            shadowTM.color = new Color(0f, 0f, 0f, alpha * 0.8f);

        // 序盤のスケールバウンス
        float scale = (k < 0.15f) ? Mathf.Lerp(0.6f, 1.2f, k / 0.15f)
                    : (k < 0.3f)  ? Mathf.Lerp(1.2f, 1.0f, (k - 0.15f) / 0.15f)
                    : 1.0f;
        transform.localScale = Vector3.one * scale;

        if (elapsed >= Duration)
            Destroy(gameObject);
    }
}
