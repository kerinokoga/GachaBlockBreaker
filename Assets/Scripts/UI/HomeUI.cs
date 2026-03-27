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

        // ユーザー情報（右上）
        string userName = AuthManager.GetName();
        if (!string.IsNullOrEmpty(userName))
        {
            MakeText(cGo.transform, userName, 24,
                new Color(0.5f, 0.9f, 1f), new Vector2(0.72f, 0.96f), new Vector2(400f, 30f));

            MakeText(cGo.transform, $"Orb: {OrbManager.GetOrbs()}", 22,
                new Color(0.4f, 0.8f, 0.6f), new Vector2(0.72f, 0.93f), new Vector2(400f, 28f));
        }

        // タイトル
        MakeText(cGo.transform, "GACHA BLOCK BREAKER", 48,
            new Color(1f, 0.9f, 0.2f), new Vector2(0.5f, 0.72f), new Vector2(900f, 70f));

        // サブタイトル
        MakeText(cGo.transform, "- Gacha x Block Breaker -", 26,
            new Color(0.8f, 0.8f, 1f), new Vector2(0.5f, 0.66f), new Vector2(700f, 40f));

        // タップテキスト（点滅）
        tapText = MakeText(cGo.transform, "TAP TO START", 38,
            Color.white, new Vector2(0.5f, 0.56f), new Vector2(500f, 55f));

        // 全画面透明ボタン（最背面）
        var btn = new GameObject("StartBtn");
        btn.transform.SetParent(cGo.transform, false);
        btn.AddComponent<Image>().color = Color.clear;
        var b = btn.AddComponent<Button>();
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        b.onClick.AddListener(() => SceneManager.LoadScene("StageSelectScene"));

        // GACHA ボタン
        MakeMenuButton(cGo.transform, "GACHA", new Color(0.5f, 0.1f, 0.75f, 0.9f),
            0.46f, () => SceneManager.LoadScene("GachaScene"));

        // SHOP ボタン
        MakeMenuButton(cGo.transform, "SHOP", new Color(0.7f, 0.4f, 0.1f, 0.9f),
            0.38f, () => SceneManager.LoadScene("ShopScene"));

        // MANAGE ボタン
        MakeMenuButton(cGo.transform, "MANAGE", new Color(0.1f, 0.4f, 0.3f, 0.9f),
            0.30f, () => SceneManager.LoadScene("CharaManageScene"));

        // COLLECTION ボタン
        MakeMenuButton(cGo.transform, "COLLECTION", new Color(0.2f, 0.2f, 0.3f, 0.9f),
            0.22f, () => SceneManager.LoadScene("CollectionScene"));

        // RANKING ボタン
        MakeMenuButton(cGo.transform, "RANKING", new Color(0.15f, 0.3f, 0.5f, 0.9f),
            0.14f, () => SceneManager.LoadScene("RankingScene"));

        // LOGOUT ボタン（左上小さめ）
        var logoutGo = new GameObject("LogoutBtn");
        logoutGo.transform.SetParent(cGo.transform, false);
        logoutGo.AddComponent<Image>().color = new Color(0.3f, 0.15f, 0.15f, 0.8f);
        var logoutB = logoutGo.AddComponent<Button>();
        var logoutRT = logoutGo.GetComponent<RectTransform>();
        logoutRT.anchorMin = logoutRT.anchorMax = new Vector2(0.12f, 0.96f);
        logoutRT.anchoredPosition = Vector2.zero;
        logoutRT.sizeDelta = new Vector2(160f, 45f);
        logoutB.onClick.AddListener(() =>
        {
            AuthManager.Logout();
            SceneManager.LoadScene("LoginScene");
        });
        var logTxtGo = new GameObject("Txt");
        logTxtGo.transform.SetParent(logoutGo.transform, false);
        var logT = logTxtGo.AddComponent<Text>();
        logT.text = "LOGOUT"; logT.fontSize = 20; logT.color = Color.white;
        logT.alignment = TextAnchor.MiddleCenter;
        logT.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        var logTrt = logTxtGo.GetComponent<RectTransform>();
        logTrt.anchorMin = Vector2.zero; logTrt.anchorMax = Vector2.one;
        logTrt.offsetMin = logTrt.offsetMax = Vector2.zero;
    }

    // ---- メニューボタン共通ヘルパー ----

    void MakeMenuButton(Transform parent, string label, Color bgCol, float y,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgCol;
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(420f, 68f);
        btn.onClick.AddListener(onClick);

        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 30; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 30);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    // ---- ファクトリーメソッド ----

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
