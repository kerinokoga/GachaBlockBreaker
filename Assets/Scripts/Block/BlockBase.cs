using UnityEngine;

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

    protected virtual void Start()
    {
        currentHP = maxHP;
        UpdateVisual();
    }

    /// <summary>
    /// ダメージを受ける。amount 分 HP を減らし、0 以下で破壊する
    /// </summary>
    public virtual void TakeDamage(int amount = 1)
    {
        if (currentHP <= 0) return; // 破壊済みなら無視

        currentHP -= amount;
        UpdateVisual();

        if (currentHP <= 0)
            DestroyBlock();
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
        OnBlockDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // ボールに当たったらダメージ1
        if (col.gameObject.CompareTag("Ball"))
            TakeDamage(1);
    }
}
