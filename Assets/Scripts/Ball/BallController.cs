using UnityEngine;

/// <summary>
/// ボールの発射・速度維持・角度制限・ミス検知を担う
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BallController : MonoBehaviour
{
    [Header("ボール設定")]
    [SerializeField] public float speed = 8.5f;              // ボール速度
    [SerializeField] private float minAngleDeg = 20f;    // 水平に近すぎる角度を防ぐ最小角度(度)

    private float baseSpeed;                              // 初期速度（ダメージ倍率計算用）
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private SpriteRenderer sr;
    private Color originalColor;
    private bool isLaunched = false;
    private bool isCritical = false;                     // クリティカル貫通中フラグ
    public bool IsCritical => isCritical;               // 外部参照用
    public bool IsLaunched => isLaunched;               // 外部参照用
    private Transform paddleTransform;                   // 発射前にパドルに追従するため
    [HideInInspector] public bool isClone = false;       // 分裂で生成されたボールか
    private float prevBallY;                             // 前フレームのY座標（すり抜け検知用）
    private const float BaseCriticalRange = 0.03f;       // 基本クリティカル範囲（3%）

    // クリティカル表示用コールバック（GameUI が購読）
    public System.Action OnCriticalHit;

    /// <summary>現在速度 / 基準速度（1.0 以上なら速度ボーナス）</summary>
    public float SpeedDamageRatio => (baseSpeed > 0f) ? speed / baseSpeed : 1f;

    /// <summary>有効クリティカル範囲（パッシブボーナス込み）</summary>
    private float EffectiveCriticalRange
    {
        get
        {
            float bonus = (CharacterManager.Instance != null) ? CharacterManager.Instance.CriticalRangeBonus : 0f;
            return BaseCriticalRange + bonus;
        }
    }

    // GameManager から購読される「ミス」通知
    public System.Action OnMissed;

    // パドルヒット通知（ボス戦ターンカウント用）
    public System.Action OnPaddleHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        baseSpeed = speed; // 初期速度を記録
    }

    /// <summary>
    /// 貫通モードの切替（Collider の isTrigger を切り替える）
    /// ブロックのみ貫通し、壁・パドル・DeathZone は通常衝突を維持するため
    /// ブロックレイヤーに対してのみ Trigger として動作する
    /// </summary>
    public void SetPenetrate(bool on)
    {
        col.isTrigger = on;
        // パドルの色を貫通状態に合わせて変更
        if (paddleTransform != null)
        {
            var paddle = paddleTransform.GetComponent<PaddleController>();
            if (paddle != null) paddle.SetPenetrateVisual(on);
        }
    }

    void Update()
    {
        // 発射前はパドルの上に追従する
        if (!isLaunched && paddleTransform != null)
        {
            Vector3 pos = paddleTransform.position;
            pos.y += 0.7f; // パドルの少し上
            transform.position = pos;
        }
    }

    // 壁境界定数（SceneSetup の壁位置に合わせる）
    private const float WallLeft  = -5.0f;
    private const float WallRight =  5.0f;
    private const float WallTop   = 10.0f;

    void FixedUpdate()
    {
        // 速度を一定に保つ（衝突で速度が変わるのを防ぐ）
        if (isLaunched && rb.velocity.magnitude > 0.1f)
        {
            rb.velocity = rb.velocity.normalized * speed;
        }

        // 高速時の壁すり抜け防止（貫通モード中は物理衝突が無効なため手動で境界チェック）
        if (isLaunched && col.isTrigger)
        {
            Vector3 pos = transform.position;
            Vector2 vel = rb.velocity;
            bool clamped = false;
            float radius = col.radius * transform.localScale.x;

            if (pos.x - radius < WallLeft)
            {
                pos.x = WallLeft + radius;
                vel.x = Mathf.Abs(vel.x);
                clamped = true;
            }
            else if (pos.x + radius > WallRight)
            {
                pos.x = WallRight - radius;
                vel.x = -Mathf.Abs(vel.x);
                clamped = true;
            }

            if (pos.y + radius > WallTop)
            {
                pos.y = WallTop - radius;
                vel.y = -Mathf.Abs(vel.y);
                clamped = true;
            }

            if (clamped)
            {
                transform.position = pos;
                rb.velocity = vel.normalized * speed;
                ClampAngle();
            }
        }

        // クリティカル貫通中のパドルすり抜け防止（前フレーム位置とのクロス判定）
        if (isLaunched && isCritical && paddleTransform != null && rb.velocity.y < 0f)
        {
            float paddleY = paddleTransform.position.y;
            float ballY = transform.position.y;
            float paddleTop = paddleY + 0.4f;

            // 前フレームではパドル上、今フレームではパドル付近or以下 → すり抜け検知
            bool crossed = (prevBallY > paddleTop && ballY <= paddleTop);
            bool alreadyBelow = (ballY < paddleTop);

            if (crossed || alreadyBelow)
            {
                var paddleCol = paddleTransform.GetComponent<Collider2D>();
                if (paddleCol != null)
                {
                    float paddleX = paddleTransform.position.x;
                    float halfW = paddleCol.bounds.extents.x;
                    float ballX = transform.position.x;
                    if (Mathf.Abs(ballX - paddleX) <= halfW + 0.3f)
                    {
                        // パドル上に押し戻して反射処理
                        Vector3 pos = transform.position;
                        pos.y = paddleTop + 0.2f;
                        transform.position = pos;

                        EndCritical();
                        OnPaddleHit?.Invoke();
                        AudioManager.Instance?.PlayPaddleHitSE();

                        float offset = Mathf.Clamp((ballX - paddleX) / halfW, -1f, 1f);
                        if (Mathf.Abs(offset) <= EffectiveCriticalRange)
                        {
                            isCritical = true;
                            SetPenetrate(true);
                            if (sr != null) sr.color = Color.yellow;
                            OnCriticalHit?.Invoke();
                        }

                        float angleDeg = 90f - offset * 60f;
                        float rad = angleDeg * Mathf.Deg2Rad;
                        rb.velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
                        ClampAngle();
                    }
                }
            }
        }
        prevBallY = transform.position.y;
    }

    /// <summary>
    /// パドルを登録してボールをパドル上に配置する
    /// </summary>
    public void SetPaddle(Transform paddle)
    {
        paddleTransform = paddle;
        isLaunched = false;
        rb.velocity = Vector2.zero;
    }

    /// <summary>
    /// パドル参照のみ設定する（分裂ボール用、位置リセットなし）
    /// </summary>
    public void SetPaddleRef(Transform paddle)
    {
        paddleTransform = paddle;
    }

    /// <summary>
    /// ボールを発射する（GameManager から呼ばれる）
    /// </summary>
    public void Launch()
    {
        if (isLaunched) return;
        isLaunched = true;

        AudioManager.Instance?.PlaySE(AudioManager.Instance.seBallLaunch);

        // 斜め上に発射（少しランダムに）
        float angle = Random.Range(60f, 120f); // 60〜120度（上方向）
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        rb.velocity = dir * speed;

        ApplyUltPenetrateIfActive();
    }

    /// <summary>
    /// 指定角度で発射する（分裂ボール用）
    /// </summary>
    public void LaunchAt(float angleDeg)
    {
        isLaunched = true;
        float rad = angleDeg * Mathf.Deg2Rad;
        rb.velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
        ClampAngle();

        ApplyUltPenetrateIfActive();
    }

    /// <summary>
    /// 貫通奥義の効果中なら貫通状態を付け直す。
    /// 奥義発動時に存在しなかったボール（ミス後のリスポーン、効果中に生まれた分裂クローン）にも
    /// 発射の瞬間に貫通が付くように、発射処理の共通経路で呼ぶ。
    /// </summary>
    void ApplyUltPenetrateIfActive()
    {
        if (CharacterManager.Instance != null && CharacterManager.Instance.IsPenetrating)
            SetPenetrate(true);
    }

    /// <summary>
    /// ミス後にボールをリセット（パドル上に戻す）
    /// </summary>
    public void ResetBall()
    {
        isLaunched = false;
        rb.velocity = Vector2.zero;
        SetPenetrate(false);
        EndCritical();
        // 透明化パルスの「透明フェーズ」で待機・非表示にされると、
        // コルーチンが止まったまま alpha=0 が残り、復活時に見えなくなるため必ず戻す
        if (sr != null && sr.color.a < 1f)
        {
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    /// <summary>
    /// ボール速度を初期速度（baseSpeed）にリセットする。
    /// 裏ステージ突入時などに SpeedBlock による加速をリセットする用途。
    /// </summary>
    public void ResetSpeedToBase()
    {
        if (baseSpeed > 0f) speed = baseSpeed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // パドルに当たった場合、当たった位置に応じて反射角を制御
        if (collision.collider.GetComponent<PaddleController>() != null)
        {
            // クリティカル貫通中ならパドルに当たった時点で解除
            if (isCritical) EndCritical();

            // パドルヒット通知
            OnPaddleHit?.Invoke();
            AudioManager.Instance?.PlayPaddleHitSE();

            float paddleX = collision.collider.transform.position.x;
            float halfW = collision.collider.bounds.extents.x;
            float hitX = transform.position.x;
            // -1（左端）〜 +1（右端）に正規化
            float offset = Mathf.Clamp((hitX - paddleX) / halfW, -1f, 1f);

            // 中央クリティカル範囲でクリティカル貫通発動
            if (Mathf.Abs(offset) <= EffectiveCriticalRange)
            {
                isCritical = true;
                SetPenetrate(true);
                if (sr != null) sr.color = Color.yellow;
                OnCriticalHit?.Invoke();
            }

            // 中央=90°（真上）、左端=150°、右端=30°
            float angleDeg = 90f - offset * 60f;
            float rad = angleDeg * Mathf.Deg2Rad;
            rb.velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
            ClampAngle();
            return;
        }

        // 通常モード：物理反射後に角度補正
        ClampAngle();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // DeathZone 判定（貫通モード中でも有効）
        if (other.CompareTag("DeathZone"))
        {
            // 未発射（パドル追従中）は無視する。
            // 分裂中に落ちた本体は DeathZone 内の位置のまま非表示待機し、
            // ミス処理後の SetActive(true) で物理が再登録されて
            // OnTriggerEnter が再発火することがある（1ミスで2ストック減る原因）
            if (!isLaunched) return;

            isLaunched = false;
            rb.velocity = Vector2.zero;
            SetPenetrate(false);
            OnMissed?.Invoke();
            return;
        }

        // 貫通モード中の壁・パドル反射（isTrigger=true なので手動反射）
        if (col.isTrigger)
        {
            // ブロックは貫通（BlockBase 側の OnTriggerEnter2D でダメージ処理）
            if (other.GetComponent<BlockBase>() != null)
                return;

            // パドルに当たったらクリティカル解除＋反射角制御
            if (other.GetComponent<PaddleController>() != null)
            {
                EndCritical();
                OnPaddleHit?.Invoke();

                // ボールをパドル上面に強制移動（すり抜け防止）
                float paddleTopY = other.bounds.max.y + 0.3f;
                Vector3 safePos = transform.position;
                safePos.y = Mathf.Max(safePos.y, paddleTopY);
                transform.position = safePos;

                float paddleX = other.transform.position.x;
                float halfW = other.bounds.extents.x;
                float hitX = transform.position.x;
                float offset = Mathf.Clamp((hitX - paddleX) / halfW, -1f, 1f);

                // 先に速度を上方向に設定
                float angleDeg = 90f - offset * 60f;
                float rad = angleDeg * Mathf.Deg2Rad;
                rb.velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
                ClampAngle();

                // 再度クリティカル判定（1フレーム遅延で貫通有効化）
                if (Mathf.Abs(offset) <= EffectiveCriticalRange)
                {
                    StartCoroutine(DelayedPenetrate());
                    OnCriticalHit?.Invoke();
                }
                return;
            }

            // 他のボールとは干渉しない（分裂ボール同士が空中で跳ね返り合うのを防止）
            if (other.GetComponent<BallController>() != null)
                return;

            // 壁は反射させる
            Vector2 vel = rb.velocity;
            Vector2 cp = other.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - cp).normalized;
            if (normal.sqrMagnitude < 0.01f)
            {
                // ボール中心が壁コライダーに深くめり込むと ClosestPoint が自身の位置を返し
                // 法線がゼロになる。従来はここで「上向き」固定だったため、側壁でも上に
                // 反射してボールが永遠に落ちてこないバグがあった。
                // → 壁の中心からの相対位置で法線の向きを推定する
                Vector2 toBall = (Vector2)transform.position - (Vector2)other.bounds.center;
                Vector2 ext = other.bounds.extents;
                float nx = ext.x > 0.001f ? toBall.x / ext.x : 0f;
                float ny = ext.y > 0.001f ? toBall.y / ext.y : 0f;
                normal = Mathf.Abs(nx) >= Mathf.Abs(ny)
                    ? new Vector2(Mathf.Sign(nx), 0f)
                    : new Vector2(0f, Mathf.Sign(ny));
            }

            // 壁から離れる方向に動いている場合は反射しない（めり込み中の二重反射防止）
            if (Vector2.Dot(vel, normal) < 0f)
            {
                rb.velocity = Vector2.Reflect(vel, normal);
                ClampAngle();
            }
        }
    }

    /// <summary>
    /// ボールの速度ベクトルが水平に近すぎる場合に角度を補正する
    /// </summary>
    private void ClampAngle()
    {
        Vector2 vel = rb.velocity;
        float angle = Mathf.Abs(Vector2.Angle(vel, Vector2.right));

        // 水平に近い場合（角度が minAngleDeg 未満 or 180-minAngle 以上）
        if (angle < minAngleDeg || angle > 180f - minAngleDeg)
        {
            float signY = vel.y >= 0 ? 1f : -1f;
            float signX = vel.x >= 0 ? 1f : -1f;
            float rad = minAngleDeg * Mathf.Deg2Rad;
            vel = new Vector2(Mathf.Cos(rad) * signX, Mathf.Sin(rad) * signY);
            rb.velocity = vel.normalized * speed;
        }
    }

    /// <summary>
    /// 1フレーム遅延でクリティカル貫通を再有効化（すり抜け防止）
    /// </summary>
    private System.Collections.IEnumerator DelayedPenetrate()
    {
        yield return new WaitForFixedUpdate();
        isCritical = true;
        SetPenetrate(true);
        if (sr != null) sr.color = Color.yellow;
    }

    /// <summary>
    /// 外部から角度補正を適用する（分裂直後など、速度を直接書き換えた後に呼ぶ）
    /// </summary>
    public void ApplyClampAngle() => ClampAngle();

    /// <summary>
    /// originalColor の外部参照用（分裂クローンの状態継承に使用）
    /// </summary>
    public Color OriginalColor => originalColor;

    /// <summary>
    /// 分裂クローン作成時に、オリジナルのクリティカル状態と元色を引き継ぐ
    /// Instantiate 直後の Awake で sr.color（黄色）が originalColor に保存されるバグを防ぐ
    /// </summary>
    public void InheritStateFromSource(BallController source)
    {
        if (source == null) return;

        // 本来のボール色（クリティカル前の色）を引き継ぐ
        originalColor = source.OriginalColor;

        if (source.IsCritical)
        {
            // クリティカル状態も引き継ぐ：ダメージ2倍 + 貫通 + 黄色
            isCritical = true;
            SetPenetrate(true);
            if (sr != null) sr.color = Color.yellow;
        }
        else
        {
            // 通常状態：色を本来の色に戻す
            isCritical = false;
            if (sr != null) sr.color = originalColor;
        }

        // 透明化パルスの残り時間を引き継ぐ（分裂時に同時に点滅）
        if (source.IsTransparencyActive)
        {
            BeginTransparencyPulse(source.TransparencyRemaining, source.TransparencyInterval);
        }
    }

    // ============================================================
    // 裏ステージ専用：ボール透明化パルス
    // ============================================================
    private Coroutine transparencyCoroutine;
    private float transparencyRemaining = 0f; // 残り時間（継承判定用）
    private float transparencyInterval  = 1f;

    /// <summary>
    /// 非アクティブ化でコルーチンが止まっても透明のままにならないよう復帰させる。
    /// （オリジナルボールがクローン生存中に落ちて SetActive(false) される場合など）
    /// </summary>
    void OnDisable()
    {
        transparencyCoroutine = null;
        transparencyRemaining = 0f;
        if (sr != null && sr.color.a < 1f)
        {
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    /// <summary>外部参照用：現在アクティブな透明化パルスが残っていれば true</summary>
    public bool IsTransparencyActive => transparencyRemaining > 0f;
    public float TransparencyRemaining => transparencyRemaining;
    public float TransparencyInterval  => transparencyInterval;

    /// <summary>
    /// totalDuration 秒間、interval 秒間隔でボールの透明度を点滅させる。
    /// 裏ステージのボス攻撃で使用。
    /// </summary>
    public void BeginTransparencyPulse(float totalDuration, float interval)
    {
        if (transparencyCoroutine != null) StopCoroutine(transparencyCoroutine);
        transparencyInterval = (interval > 0f) ? interval : 1f;
        transparencyRemaining = Mathf.Max(0f, totalDuration);
        transparencyCoroutine = StartCoroutine(TransparencyPulseRoutine());
    }

    private System.Collections.IEnumerator TransparencyPulseRoutine()
    {
        if (sr == null) yield break;

        bool transparent = false;
        while (transparencyRemaining > 0f && this != null && sr != null)
        {
            transparent = !transparent;
            var c = sr.color;
            c.a = transparent ? 0f : 1f; // 完全透明 ↔ 不透明
            sr.color = c;

            float intervalLeft = Mathf.Min(transparencyInterval, transparencyRemaining);
            while (intervalLeft > 0f)
            {
                yield return null;
                if (this == null || sr == null) yield break;

                // 発射待機（Ready）中はタイマーを止め、一時的に不透明化（見やすく）
                if (IsReadyState())
                {
                    if (sr.color.a < 1f)
                    {
                        var vc = sr.color; vc.a = 1f; sr.color = vc;
                    }
                    continue; // 時間を進めない
                }

                // Playing 復帰時は意図したアルファを再適用
                float desired = transparent ? 0f : 1f;
                if (!Mathf.Approximately(sr.color.a, desired))
                {
                    var pc = sr.color; pc.a = desired; sr.color = pc;
                }

                float dt = Time.deltaTime;
                intervalLeft -= dt;
                transparencyRemaining -= dt;
                if (transparencyRemaining <= 0f) break;
            }
        }

        // 復帰：alpha を 1 に戻す
        if (sr != null)
        {
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
        transparencyRemaining = 0f;
        transparencyCoroutine = null;
    }

    /// <summary>
    /// 発射待機（Ready）状態かどうか。タイマー停止判定に使う。
    /// 奥義の WaitForPlaying と同じ基準。
    /// </summary>
    private bool IsReadyState()
    {
        return GameManager.Instance != null
            && GameManager.Instance.CurrentState == GameManager.GameState.Ready;
    }

    /// <summary>
    /// 進行中の透明化パルスを即時停止し、alpha を 1 に戻す。
    /// 裏ステージ突入時のリセットに使用。
    /// </summary>
    public void CancelTransparency()
    {
        if (transparencyCoroutine != null)
        {
            StopCoroutine(transparencyCoroutine);
            transparencyCoroutine = null;
        }
        transparencyRemaining = 0f;
        if (sr != null)
        {
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    /// <summary>
    /// クリティカル貫通を解除し、ボールの色を元に戻す
    /// </summary>
    private void EndCritical()
    {
        if (!isCritical) return;
        isCritical = false;
        // ULT貫通スキル中はクリティカル解除しても貫通を維持
        if (CharacterManager.Instance != null && CharacterManager.Instance.IsPenetrating)
        {
            // 色だけ元に戻す（貫通は維持）
            if (sr != null) sr.color = originalColor;
            return;
        }
        SetPenetrate(false);
        if (sr != null) sr.color = originalColor;
    }
}
