using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// タッチ/マウス入力でパドルをX軸方向に移動させる
/// </summary>
public class PaddleController : MonoBehaviour
{
    [Header("パドル設定")]
    [SerializeField] private float halfWidth = 4.5f;    // 画面端の移動制限（カメラ幅に合わせる）
    [SerializeField] private float smoothSpeed = 0.4f; // 追従の滑らかさ（大きいほど素早い）

    private Camera mainCamera;
    private float targetX;
    private bool isActive = true;
    private Transform criticalZoneTransform; // クリティカルゾーンの参照（動的リサイズ用）
    private SpriteRenderer paddleSR;
    private Color originalColor;
    private static readonly Color PenetrateColor = new Color(0.2f, 0.6f, 1f); // 貫通時の青色

    // UI判定はタッチ/クリック開始時にだけ行い、セッション全体で維持する
    // （IsPointerOverGameObject(fingerId) を毎フレーム呼ぶとフレーム順序の関係で不安定になるため）
    private bool  pointerSessionOnUI = false;
    private int   trackedFingerId    = -1;

    void Start()
    {
        mainCamera = Camera.main;
        paddleSR = GetComponent<SpriteRenderer>();
        // カスタムパドル画像を適用
        var customSprite = Resources.Load<Sprite>("Game/paddle");
        if (customSprite != null && paddleSR != null)
        {
            paddleSR.sprite = customSprite;
            paddleSR.color = Color.white; // 画像の色をそのまま表示
        }
        if (paddleSR != null) originalColor = paddleSR.color;
        // パドルをULTゲージと被らない高さに調整
        Vector3 pos = transform.position;
        pos.y = -9.0f;
        transform.position = pos;
        targetX = transform.position.x;
        CreateCriticalZone();
    }

    void CreateCriticalZone()
    {
        var parentSR = GetComponent<SpriteRenderer>();
        if (parentSR == null || parentSR.sprite == null) return;

        var zoneGo = new GameObject("CriticalZone");
        zoneGo.transform.SetParent(transform, false);
        zoneGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        var sr = zoneGo.AddComponent<SpriteRenderer>();
        sr.sprite = parentSR.sprite; // 親と同じスプライトを使用
        sr.color = new Color(1f, 0.2f, 0.2f);
        sr.sortingOrder = parentSR.sortingOrder + 1;

        // クリティカル範囲に縮小（パッシブボーナス込み）
        float critRange = 0.03f;
        if (CharacterManager.Instance != null)
            critRange += CharacterManager.Instance.CriticalRangeBonus;
        zoneGo.transform.localScale = new Vector3(critRange, 1f, 1f);
        criticalZoneTransform = zoneGo.transform;
    }

    /// <summary>
    /// クリティカルゾーンのサイズをパッシブボーナスに合わせて更新
    /// </summary>
    public void UpdateCriticalZoneSize()
    {
        if (criticalZoneTransform == null) return;
        float critRange = 0.03f;
        if (CharacterManager.Instance != null)
            critRange += CharacterManager.Instance.CriticalRangeBonus;
        criticalZoneTransform.localScale = new Vector3(critRange, 1f, 1f);
    }

    /// <summary>
    /// 貫通状態に応じてパドルの色を切り替える
    /// </summary>
    public void SetPenetrateVisual(bool on)
    {
        if (paddleSR != null)
            paddleSR.color = on ? PenetrateColor : originalColor;
    }

    void Update()
    {
        if (!isActive) return;

        float inputX = GetInputX();
        if (inputX != float.MinValue)
        {
            // スクリーン座標 → ワールド座標に変換
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(inputX, 0, 0));
            targetX = Mathf.Clamp(worldPos.x, -halfWidth, halfWidth);
        }

        MovePaddle();
    }

    /// <summary>
    /// targetX に向けて移動（指にほぼ直接追従）
    /// </summary>
    private void MovePaddle()
    {
        Vector3 pos = transform.position;
        // deltaTime ベースの高速補間（フレームレート非依存でヌルヌル動く）
        float lerpSpeed = 20f; // 大きいほど即応性が高い
        pos.x = Mathf.Lerp(pos.x, targetX, lerpSpeed * Time.deltaTime);
        transform.position = pos;
    }

    /// <summary>
    /// タッチ・マウス入力を統一して X 座標を返す
    /// 入力がない場合は float.MinValue を返す
    ///
    /// モバイル対応: タッチ開始(Began)時点で IsPointerOverGameObject(fingerId) を
    /// 一度だけ評価し、その判定結果をタッチセッション全体で維持する。
    /// こうすることで EventSystem と Update のフレーム順序差異による
    /// 「UIボタン押下時にパドルが一瞬ジャンプする」問題を防ぐ。
    /// </summary>
    private float GetInputX()
    {
        // ==== モバイル（タッチ）====
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);

            // セッション開始時に UI 上かどうかをスナップショット
            if (touch.phase == TouchPhase.Began)
            {
                trackedFingerId = touch.fingerId;
                pointerSessionOnUI =
                    (EventSystem.current != null &&
                     EventSystem.current.IsPointerOverGameObject(touch.fingerId));
            }

            // タッチ終了でセッション状態をクリア（次のタッチに備える）
            bool sessionEnded = (touch.phase == TouchPhase.Ended ||
                                 touch.phase == TouchPhase.Canceled);

            if (pointerSessionOnUI)
            {
                if (sessionEnded)
                {
                    pointerSessionOnUI = false;
                    trackedFingerId    = -1;
                }
                return float.MinValue;
            }

            if (sessionEnded)
            {
                trackedFingerId = -1;
                return float.MinValue;
            }

            return touch.position.x;
        }

        // タッチが途切れたらセッション状態を念のためリセット
        if (trackedFingerId != -1)
        {
            pointerSessionOnUI = false;
            trackedFingerId    = -1;
        }

        // ==== PC/エディタ（マウス）====
        if (Input.GetMouseButtonDown(0))
        {
            pointerSessionOnUI =
                (EventSystem.current != null &&
                 EventSystem.current.IsPointerOverGameObject());
        }
        if (Input.GetMouseButtonUp(0))
        {
            pointerSessionOnUI = false;
        }
        if (Input.GetMouseButton(0))
        {
            if (pointerSessionOnUI) return float.MinValue;
            return Input.mousePosition.x;
        }

        return float.MinValue;
    }

    /// <summary>
    /// ポーズ中やミス後に入力を無効化する
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
    }
}
