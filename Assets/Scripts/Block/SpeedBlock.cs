using UnityEngine;

/// <summary>
/// 速度アップブロック。破壊時にボールの速度を speedMultiplier 倍にする
/// 見た目は紫〜マゼンタで警告色
/// </summary>
public class SpeedBlock : BlockBase
{
    [Header("速度倍率（破壊時にボール速度をこの倍率にする）")]
    [SerializeField] private float speedMultiplier = 1.1f;

    /// <summary>SpeedBlock破壊時の通知（GameUI が購読）</summary>
    public static System.Action OnSpeedUp;

    private SpriteRenderer sr;
    private TextMesh iconText;

    protected override void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        base.Start();
        CreateIconText("▲", new Color(1f, 0.7f, 1f));
    }

    void CreateIconText(string icon, Color col)
    {
        var go = new GameObject("Icon");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        iconText = go.AddComponent<TextMesh>();
        iconText.text = icon;
        iconText.alignment = TextAlignment.Center;
        iconText.anchor = TextAnchor.MiddleCenter;
        iconText.fontSize = 80;
        iconText.characterSize = 0.05f;
        iconText.color = col;
        iconText.fontStyle = FontStyle.Bold;

        Vector3 ps = transform.localScale;
        float invX = (ps.x != 0f) ? 1f / ps.x : 1f;
        float invY = (ps.y != 0f) ? 1f / ps.y : 1f;
        go.transform.localScale = new Vector3(invX, invY, 1f);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 10;
    }

    public void SetSpeedMultiplier(float mul)
    {
        speedMultiplier = mul;
    }

    protected override void UpdateVisual()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        // 紫〜マゼンタの警告色
        sr.color = new Color(0.85f, 0.15f, 0.85f, blockAlpha);
    }

    protected override void DestroyBlock()
    {
        // ボールの速度を上げる
        var ball = FindObjectOfType<BallController>();
        if (ball != null)
        {
            ball.speed *= speedMultiplier;
        }

        OnSpeedUp?.Invoke();
        NotifyAndDestroy();
    }
}
