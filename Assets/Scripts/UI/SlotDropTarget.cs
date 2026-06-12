using UnityEngine;

/// <summary>
/// キャラ選択画面のスロットに付与するマーカーコンポーネント。
/// ドラッグドロップ時の RaycastAll でこの component を持つ GameObject を検出する。
/// </summary>
public class SlotDropTarget : MonoBehaviour
{
    public int slotIndex;
}
