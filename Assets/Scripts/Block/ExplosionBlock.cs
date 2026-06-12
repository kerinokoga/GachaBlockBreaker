using UnityEngine;

/// <summary>
/// 爆発ブロック。破壊されると周囲のブロックにも範囲ダメージを与える
/// </summary>
public class ExplosionBlock : BlockBase
{
    [Header("爆発設定")]
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private LayerMask blockLayer;

    private TextMesh iconText;

    protected override void Start()
    {
        base.Start();
        CreateIconText("★", new Color(1f, 1f, 0.3f));
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

    protected override void DestroyBlock()
    {
        // 半径内の全ブロックにダメージ
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, blockLayer);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue; // 自分自身はスキップ
            BlockBase block = hit.GetComponent<BlockBase>();
            block?.TakeDamage(1);
        }

        NotifyAndDestroy();
    }

    // Scene ビューで爆発範囲を可視化
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
