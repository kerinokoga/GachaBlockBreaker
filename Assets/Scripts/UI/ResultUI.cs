using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour
{
    void Start() => BuildUI();

    void BuildUI()
    {
        bool isClear = ResultData.IsClear;
        float rate = ResultData.DestroyRate;
        int stage = ResultData.StageNumber;

        var cGo = new GameObject("ResultCanvas");
        Canvas c = cGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0.5f;
        cGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 背景
        MakeImage(cGo.transform, new Color(0.05f, 0.05f, 0.15f), Vector2.zero, Vector2.one);

        // タイトル
        Color titleCol = isClear ? new Color(1f, 0.9f, 0.2f) : new Color(1f, 0.3f, 0.3f);
        MakeText(cGo.transform, isClear ? "STAGE CLEAR!" : "GAME OVER",
            72, titleCol, new Vector2(0.5f, 0.72f), new Vector2(800f, 100f));

        // ステージ番号
        MakeText(cGo.transform, $"STAGE {stage}",
            36, new Color(0.8f, 0.8f, 0.8f), new Vector2(0.5f, 0.62f), new Vector2(400f, 55f));

        // 破壊率
        MakeText(cGo.transform, $"DESTROY: {Mathf.FloorToInt(rate * 100)}%",
            44, Color.white, new Vector2(0.5f, 0.52f), new Vector2(500f, 65f));

        // RETRY ボタン
        MakeButton(cGo.transform, "RETRY", new Color(0.2f, 0.5f, 1f),
            new Vector2(0.5f, 0.36f), new Vector2(360f, 90f),
            () => SceneManager.LoadScene("GameScene"));

        // HOME ボタン
        MakeButton(cGo.transform, "HOME", new Color(0.3f, 0.3f, 0.35f),
            new Vector2(0.5f, 0.22f), new Vector2(360f, 90f),
            () => SceneManager.LoadScene("HomeScene"));
    }

    Text MakeText(Transform parent, string txt, int size, Color col, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    void MakeImage(Transform parent, Color col, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("BG");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void MakeButton(Transform parent, string label, Color bgCol,
        Vector2 anchor, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgCol;
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        btn.onClick.AddListener(onClick);

        // ボタンテキスト
        MakeText(go.transform, label, 44, Color.white, new Vector2(0.5f, 0.5f), sizeDelta);
        var txtRT = go.transform.GetChild(0).GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
    }
}
