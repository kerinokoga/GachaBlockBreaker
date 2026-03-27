using UnityEngine;

/// <summary>
/// ボールの発射・速度維持・角度制限・ミス検知を担う
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BallController : MonoBehaviour
{
    [Header("ボール設定")]
    [SerializeField] public float speed = 12f;            // ボール速度（CharacterManager から変更可）
    [SerializeField] private float minAngleDeg = 20f;    // 水平に近すぎる角度を防ぐ最小角度(度)

    private Rigidbody2D rb;
    private bool isLaunched = false;
    private Transform paddleTransform;                   // 発射前にパドルに追従するため

    // GameManager から購読される「ミス」通知
    public System.Action OnMissed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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

    void FixedUpdate()
    {
        // 速度を一定に保つ（衝突で速度が変わるのを防ぐ）
        if (isLaunched && rb.velocity.magnitude > 0.1f)
        {
            rb.velocity = rb.velocity.normalized * speed;
        }
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
    }

    /// <summary>
    /// ミス後にボールをリセット（パドル上に戻す）
    /// </summary>
    public void ResetBall()
    {
        isLaunched = false;
        rb.velocity = Vector2.zero;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // 衝突後に角度補正（水平に近すぎる軌道を防ぐ）
        ClampAngle();
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
    /// DeathZone（画面下のトリガー）に触れたとき
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeathZone"))
        {
            isLaunched = false;
            rb.velocity = Vector2.zero;
            OnMissed?.Invoke(); // GameManager に通知
        }
    }
}
