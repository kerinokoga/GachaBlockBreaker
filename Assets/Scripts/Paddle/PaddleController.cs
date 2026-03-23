using UnityEngine;

/// <summary>
/// タッチ/マウス入力でパドルをX軸方向に移動させる
/// </summary>
public class PaddleController : MonoBehaviour
{
    [Header("パドル設定")]
    [SerializeField] private float halfWidth = 4.5f;    // 画面端の移動制限（カメラ幅に合わせる）
    [SerializeField] private float smoothSpeed = 0.12f; // 追従の滑らかさ（小さいほど素早い）

    private Camera mainCamera;
    private float targetX;
    private bool isActive = true;

    void Start()
    {
        mainCamera = Camera.main;
        targetX = transform.position.x;
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
    /// targetX に向けて滑らかに移動
    /// </summary>
    private void MovePaddle()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, smoothSpeed);
        transform.position = pos;
    }

    /// <summary>
    /// タッチ・マウス入力を統一して X 座標を返す
    /// 入力がない場合は float.MinValue を返す
    /// </summary>
    private float GetInputX()
    {
        // モバイル（タッチ）
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position.x;
        }

        // エディタ・PC（マウス）
        if (Input.GetMouseButton(0))
        {
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
