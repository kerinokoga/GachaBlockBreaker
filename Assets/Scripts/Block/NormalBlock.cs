using UnityEngine;

/// <summary>
/// 通常ブロック。1発で破壊される
/// </summary>
public class NormalBlock : BlockBase
{
    protected override void DestroyBlock()
    {
        NotifyAndDestroy();
    }
}
