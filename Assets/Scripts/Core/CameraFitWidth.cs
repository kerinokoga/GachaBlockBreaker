using UnityEngine;

/// <summary>
/// カメラの横幅を固定し、画面比率に応じて orthographicSize を自動調整する。
/// 縦長スマホでも壁・パドルが見切れなくなる。
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFitWidth : MonoBehaviour
{
    [SerializeField] private float targetWorldWidth = 11f; // 画面に収めたい横幅（壁間の距離）

    void Awake()
    {
        var cam = GetComponent<Camera>();
        float aspectRatio = (float)Screen.width / Screen.height;
        // orthographicSize = 横幅の半分 / アスペクト比
        cam.orthographicSize = (targetWorldWidth / 2f) / aspectRatio;
    }
}
