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
            Color.white, new Vector2(0.5f, 0.50f), new Vector2(500f, 60f));

        // 全画面透明ボタン（先に追加して最背面に置く）
        var btn = new GameObject("StartBtn");
        btn.transform.SetParent(cGo.transform, false);
        btn.AddComponent<Image>().color = Color.clear;
        var b = btn.AddComponent<Button>();
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        b.onClick.AddListener(() => SceneManager.LoadScene("StageSelectScene"));

        // GACHA ボタン（StartBtn の後→前面）
        var gachaBtn = new GameObject("GachaBtn");
        gachaBtn.transform.SetParent(cGo.transform, false);
        gachaBtn.AddComponent<Image>().color = new Color(0.5f, 0.1f, 0.75f, 0.9f);
        var gachaB = gachaBtn.AddComponent<Button>();
        var gachaRT = gachaBtn.GetComponent<RectTransform>();
        gachaRT.anchorMin = gachaRT.anchorMax = new Vector2(0.5f, 0.42f);
        gachaRT.anchoredPosition = Vector2.zero;
        gachaRT.sizeDelta = new Vector2(420f, 75f);
        gachaB.onClick.AddListener(() => SceneManager.LoadScene("GachaScene"));
        var gachaTxtGo = new GameObject("Txt");
        gachaTxtGo.transform.SetParent(gachaBtn.transform, false);
        var gachaT = gachaTxtGo.AddComponent<Text>();
        gachaT.text = "GACHA"; gachaT.fontSize = 32; gachaT.color = Color.white;
        gachaT.alignment = TextAnchor.MiddleCenter;
        gachaT.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        var gachaTrt = gachaTxtGo.GetComponent<RectTransform>();
        gachaTrt.anchorMin = Vector2.zero; gachaTrt.anchorMax = Vector2.one;
        gachaTrt.offsetMin = gachaTrt.offsetMax = Vector2.zero;

        // MANAGE ボタン
        var manageBtn = new GameObject("ManageBtn");
        manageBtn.transform.SetParent(cGo.transform, false);
        manageBtn.AddComponent<Image>().color = new Color(0.1f, 0.4f, 0.3f, 0.9f);
        var manageB = manageBtn.AddComponent<Button>();
        var manageRT = manageBtn.GetComponent<RectTransform>();
        manageRT.anchorMin = manageRT.anchorMax = new Vector2(0.5f, 0.34f);
        manageRT.anchoredPosition = Vector2.zero;
        manageRT.sizeDelta = new Vector2(420f, 75f);
        manageB.onClick.AddListener(() => SceneManager.LoadScene("CharaManageScene"));
        var manageTxtGo = new GameObject("Txt");
        manageTxtGo.transform.SetParent(manageBtn.transform, false);
        var manageT = manageTxtGo.AddComponent<Text>();
        manageT.text = "MANAGE"; manageT.fontSize = 32; manageT.color = Color.white;
        manageT.alignment = TextAnchor.MiddleCenter;
        manageT.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        var manageTrt = manageTxtGo.GetComponent<RectTransform>();
        manageTrt.anchorMin = Vector2.zero; manageTrt.anchorMax = Vector2.one;
        manageTrt.offsetMin = manageTrt.offsetMax = Vector2.zero;

        // COLLECTION ボタン（MANAGE の後→最前面）
        var colBtn = new GameObject("CollectionBtn");
        colBtn.transform.SetParent(cGo.transform, false);
        colBtn.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
        var colB = colBtn.AddComponent<Button>();
        var colRT = colBtn.GetComponent<RectTransform>();
        colRT.anchorMin = colRT.anchorMax = new Vector2(0.5f, 0.26f);
        colRT.anchoredPosition = Vector2.zero;
        colRT.sizeDelta = new Vector2(420f, 75f);
        colB.onClick.AddListener(() => SceneManager.LoadScene("CollectionScene"));
        var colTxtGo = new GameObject("Txt");
        colTxtGo.transform.SetParent(colBtn.transform, false);
        var colT = colTxtGo.AddComponent<Text>();
        colT.text = "COLLECTION"; colT.fontSize = 32; colT.color = Color.white;
        colT.alignment = TextAnchor.MiddleCenter;
        colT.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        var colTrt = colTxtGo.GetComponent<RectTransform>();
        colTrt.anchorMin = Vector2.zero; colTrt.anchorMax = Vector2.one;
        colTrt.offsetMin = colTrt.offsetMax = Vector2.zero;
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
