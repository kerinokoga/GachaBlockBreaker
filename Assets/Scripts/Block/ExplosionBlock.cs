using UnityEngine;

/// <summary>
/// 爆発ブロック。破壊されると周囲のブロックにも範囲ダメージを与える
/// </summary>
public class ExplosionBlock : BlockBase
{
    [Header("爆発設定")]
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private LayerMask blockLayer;

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
