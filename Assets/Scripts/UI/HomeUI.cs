using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    Text tapText;

    void Start() => BuildUI();

    void Update()
    {
        if (tapText != null)
            tapText.color = new Color(1f, 1f, 1f, Mathf.Abs(Mathf.Sin(Time.time * 1.5f)));
    }

    void BuildUI()
    {
        GameObject cGo = new GameObject("HomeCanvas");
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
        MakeText(cGo.transform, "GACHA BLOCK BREAKER", 52,
            new Color(1f, 0.9f, 0.2f), new Vector2(0.5f, 0.62f), new Vector2(900f, 80f));

        // サブタイトル
        MakeText(cGo.transform, "- Gacha x Block Breaker -", 30,
            new Color(0.8f, 0.8f, 1f), new Vector2(0.5f, 0.54f), new Vector2(700f, 50f));

        // タップテキスト（点滅）
        tapText = MakeText(cGo.transform, "TAP TO START", 40,
            Color.white, new Vector2(0.5f, 0.38f), new Vector2(500f, 60f));

        // 全画面透明ボタン
        var btn = new GameObject("StartBtn");
        btn.transform.SetParent(cGo.transform, false);
        btn.AddComponent<Image>().color = Color.clear;
        var b = btn.AddComponent<Button>();
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        b.onClick.AddListener(() => SceneManager.LoadScene("CharaSelectScene"));
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
}
