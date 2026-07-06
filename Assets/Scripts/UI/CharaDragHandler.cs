using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// キャラカードのロングプレス → ドラッグ操作。
///
/// 入力フロー:
///   - Pointer Down → 0.4秒の長押しタイマー開始（同時にScrollRect側もOnInitializePotentialDrag を受ける）
///   - 0.4秒経過まで動かなければ "Pickup" 発火（ゴースト生成、ScrollRectは抑止）
///   - 0.4秒前に動いた場合は通常スクロール（OnBeginDrag を ScrollRect に委譲）
///   - Pickup 中の OnDrag は ゴーストを指追従＋スロット ハイライト更新
///   - Pickup 中の OnEndDrag は スロット判定（成功で割り当て、外なら何もしない）
///   - 短いタップ（動かず即離す）は CharaSelectUI.OnCardTap を呼ぶ
/// </summary>
public class CharaDragHandler : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IInitializePotentialDragHandler
{
    public const float LongPressDuration = 0.3f;

    [HideInInspector] public int charIndex;
    [HideInInspector] public CharaSelectUI ownerUI;
    [HideInInspector] public ScrollRect parentScroll;
    [HideInInspector] public RectTransform canvasRect;

    Vector2 pointerDownPos;
    bool longPressFired;
    bool isDragging;
    bool tapEnabled;
    Coroutine longPressCo;
    GameObject ghost;

    // --- EventSystem コールバック ---

    public void OnInitializePotentialDrag(PointerEventData ev)
    {
        // ScrollRect が初期化されないとスクロール開始時にカクつくので必ず通す
        if (parentScroll != null) parentScroll.OnInitializePotentialDrag(ev);
    }

    public void OnPointerDown(PointerEventData ev)
    {
        pointerDownPos = ev.position;
        longPressFired = false;
        isDragging = false;
        tapEnabled = true;
        if (longPressCo != null) StopCoroutine(longPressCo);
        longPressCo = StartCoroutine(LongPressTimer());
    }

    IEnumerator LongPressTimer()
    {
        yield return new WaitForSecondsRealtime(LongPressDuration);
        // タイマー満了時にまだスクロールに移行していなければピックアップ
        if (!isDragging)
        {
            longPressFired = true;
            tapEnabled = false;
            BeginPickup();
        }
        longPressCo = null;
    }

    public void OnPointerUp(PointerEventData ev)
    {
        if (longPressCo != null) { StopCoroutine(longPressCo); longPressCo = null; }

        // 長押し発火後、動かずに離した場合は片付けだけ行う
        if (longPressFired && !isDragging)
        {
            CleanupPickup();
            longPressFired = false;
            return;
        }

        // 短いタップ → タップ動作
        if (tapEnabled && !isDragging && !longPressFired)
        {
            if (ownerUI != null) ownerUI.OnCardTap(charIndex);
        }
    }

    public void OnBeginDrag(PointerEventData ev)
    {
        if (longPressFired)
        {
            // Pickup モードで自前処理
            isDragging = true;
        }
        else
        {
            // 通常スクロール → ScrollRect に委譲
            if (longPressCo != null) { StopCoroutine(longPressCo); longPressCo = null; }
            tapEnabled = false;
            isDragging = true;  // スクロール中フラグ
            if (parentScroll != null) parentScroll.OnBeginDrag(ev);
        }
    }

    public void OnDrag(PointerEventData ev)
    {
        if (longPressFired && ghost != null)
        {
            // ゴーストを指追従
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, ev.position, null, out localPoint);
            var grt = ghost.GetComponent<RectTransform>();
            if (grt != null) grt.anchoredPosition = localPoint;
            if (ownerUI != null) ownerUI.UpdateDropTargetHighlight(ev);
        }
        else
        {
            // スクロール継続
            if (parentScroll != null) parentScroll.OnDrag(ev);
        }
    }

    public void OnEndDrag(PointerEventData ev)
    {
        if (longPressFired)
        {
            if (ownerUI != null) ownerUI.TryDropOnSlot(charIndex, ev);
            CleanupPickup();
        }
        else
        {
            if (parentScroll != null) parentScroll.OnEndDrag(ev);
        }
        isDragging = false;
        longPressFired = false;
    }

    // --- ピックアップ処理 ---

    void BeginPickup()
    {
        if (ownerUI == null) return;

        ghost = ownerUI.CreateDragGhost(charIndex);
        if (ghost != null && canvasRect != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, pointerDownPos, null, out localPoint);
            var grt = ghost.GetComponent<RectTransform>();
            if (grt != null) grt.anchoredPosition = localPoint;
        }

        // 元カードを少し縮小して "持ち上がった感"
        transform.localScale = new Vector3(0.92f, 0.92f, 1f);

        // モバイルではバイブで触感フィードバック
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        ownerUI.OnPickupStart(charIndex);
    }

    void CleanupPickup()
    {
        if (ghost != null) { Destroy(ghost); ghost = null; }
        transform.localScale = Vector3.one;
        if (ownerUI != null) ownerUI.OnPickupEnd();
    }
}
