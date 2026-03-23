using UnityEngine;

/// <summary>
/// 連鎖ブロック。破壊されると上下左右に隣接する ChainBlock にも連鎖して破壊する
/// </summary>
public class ChainBlock : BlockBase
{
    [Header("連鎖設定")]
    [SerializeField] private float checkDistance = 1.1f; // 隣接チェックの距離

    protected override void DestroyBlock()
    {
        TriggerChain();
        NotifyAndDestroy();
    }

    private void TriggerChain()
    {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (var dir in directions)
        {
            // 自分の中心から checkDistance 先を小さい円でチェック
            Vector2 checkPos = (Vector2)transform.position + dir * checkDistance;
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, 0.3f);

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                ChainBlock neighbor = hit.GetComponent<ChainBlock>();
                if (neighbor != null)
                    neighbor.TakeDamage(99);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.6f);
        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (var d in dirs)
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(d * checkDistance));
    }
}
