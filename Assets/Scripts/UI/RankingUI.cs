using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// ランキング画面：ステージ別クリア率ランキング表示
/// </summary>
public class RankingUI : MonoBehaviour
{
    Transform canvasRoot;
    Transform contentRoot;
    RectTransform contentRT;
    Text stageTitle;
    int currentStage = 1;

    // ステージタブボタン
    Button[] tabButtons = new Button[5];
    Image[]  tabImages  = new Image[5];

    void Start() => BuildUI();

    void BuildUI()
    {
        var cGo = new GameObject("RankingCanvas");
        var c = cGo.AddComponent<Canvas>();
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

        canvasRoot = cGo.transform;

        // 背景
        MakeBg(canvasRoot, new Color(0.05f, 0.03f, 0.12f));

        // タイトル
        MakeText(canvasRoot, "RANKING", 52, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f, 0.93f), new Vector2(500f, 65f));

        // ステージタブ
        BuildStageTabs(canvasRoot);

        // ステージ名
        stageTitle = MakeText(canvasRoot, "Stage 1", 28, new Color(0.7f, 0.7f, 0.85f),
            new Vector2(0.5f, 0.83f), new Vector2(400f, 36f));

        // スクロールビュー
        BuildScrollView(canvasRoot);

        // BACK ボタン
        MakeButton(canvasRoot, "BACK", new Color(0.25f, 0.25f, 0.35f),
            new Vector2(0.5f, 0.05f), new Vector2(360f, 70f),
            () => SceneManager.LoadScene("HomeScene"));

        RefreshRanking();
    }

    void BuildStageTabs(Transform parent)
    {
        float startX = 0.16f;
        float gap    = 0.17f;

        for (int i = 0; i < 5; i++)
        {
            int stageNum = i + 1;
            float x = startX + i * gap;

            var go = new GameObject($"Tab_{stageNum}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(x, 0.87f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(130f, 55f);

            int s = stageNum;
            btn.onClick.AddListener(() => OnTabClick(s));

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var t = txtGo.AddComponent<Text>();
            t.text = stageNum.ToString(); t.fontSize = 26; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Font.CreateDynamicFontFromOSFont("Arial", 26);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            tabButtons[i] = btn;
            tabImages[i]  = img;
        }
    }

    void OnTabClick(int stage)
    {
        currentStage = stage;
        RefreshRanking();
    }

    void BuildScrollView(Transform parent)
    {
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(parent, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.05f, 0.12f);
        scrollRT.anchorMax = new Vector2(0.95f, 0.80f);
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;
        var sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.scrollSensitivity = 40f;

        var vpGo = new GameObject("Viewport");
        vpGo.transform.SetParent(scrollGo.transform, false);
        var vpImg = vpGo.AddComponent<Image>();
        vpImg.color = Color.white;
        vpGo.AddComponent<Mask>().showMaskGraphic = false;
        var vpRT = vpGo.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRoot = contentGo.transform;

        sr.content  = contentRT;
        sr.viewport = vpRT;
    }

    void RefreshRanking()
    {
        // タブ色更新
        for (int i = 0; i < 5; i++)
        {
            bool active = (i + 1) == currentStage;
            tabImages[i].color = active
                ? new Color(0.3f, 0.5f, 0.8f)
                : new Color(0.15f, 0.15f, 0.25f);
        }

        stageTitle.text = $"Stage {currentStage}";

        // Content クリア
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        var ranking = RankingManager.GetTopRanking(currentStage, 20);
        const float rowH = 70f;
        contentRT.sizeDelta = new Vector2(0f, Mathf.Max(ranking.Count, 1) * rowH);

        if (ranking.Count == 0)
        {
            MakeText(contentRoot, "No data", 28, new Color(0.5f, 0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(300f, 40f));
            return;
        }

        string myName = AuthManager.GetName();

        for (int i = 0; i < ranking.Count; i++)
        {
            var entry = ranking[i];
            bool isMe = entry.name == myName;
            BuildRankRow(contentRoot, i, entry, rowH, isMe);
        }
    }

    void BuildRankRow(Transform parent, int index, RankingManager.RankEntry entry, float rowH, bool isMe)
    {
        Color bgCol = isMe
            ? new Color(0.2f, 0.3f, 0.5f)
            : (index % 2 == 0 ? new Color(0.1f, 0.1f, 0.18f) : new Color(0.12f, 0.12f, 0.22f));

        var rowGo = new GameObject($"Rank_{index}");
        rowGo.transform.SetParent(parent, false);
        rowGo.AddComponent<Image>().color = bgCol;
        var rowRT = rowGo.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot     = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, -index * rowH);
        rowRT.sizeDelta = new Vector2(0f, rowH - 2f);

        // 順位
        Color rankCol = index < 3 ? new Color(1f, 0.85f, 0.1f) : new Color(0.7f, 0.7f, 0.7f);
        MakeText(rowGo.transform, $"#{index + 1}", 26, rankCol,
            new Vector2(0.08f, 0.5f), new Vector2(80f, 34f));

        // 名前
        Color nameCol = isMe ? new Color(0.5f, 0.9f, 1f) : Color.white;
        var nameT = MakeText(rowGo.transform, entry.name, 26, nameCol,
            new Vector2(0.38f, 0.5f), new Vector2(350f, 34f));
        nameT.alignment = TextAnchor.MiddleLeft;

        // クリア率
        MakeText(rowGo.transform, $"{entry.rate:P0}", 26, new Color(0.4f, 1f, 0.4f),
            new Vector2(0.85f, 0.5f), new Vector2(150f, 34f));
    }

    // ---- ファクトリーメソッド ----

    void MakeBg(Transform parent, Color col)
    {
        var go = new GameObject("BG");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    Text MakeText(Transform parent, string txt, int size, Color col, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    Button MakeButton(Transform parent, string label, Color bgCol,
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

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 32; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return btn;
    }
}
