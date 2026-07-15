using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// ランキング画面：ステージ別クリア率ランキング表示（装飾付き）
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

    // 光の粒パーティクル
    List<RectTransform> particles = new List<RectTransform>();

    void Start() => BuildUI();

    void Update()
    {
        // 光の粒アニメーション
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            var p = particles[i];
            p.anchoredPosition += new Vector2(0f, 40f * Time.deltaTime);
            if (p.anchoredPosition.y > 1000f)
                p.anchoredPosition = new Vector2(Random.Range(-540f, 540f), -1000f);
            float x = p.anchoredPosition.x + Mathf.Sin(Time.time * 0.8f + i * 1.3f) * 15f * Time.deltaTime;
            p.anchoredPosition = new Vector2(x, p.anchoredPosition.y);
        }
    }

    void BuildUI()
    {
        var cGo = new GameObject("RankingCanvas");
        var c = cGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0.0f;
        cGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        canvasRoot = cGo.transform;

        // ===== 背景（ダークパープル/ネイビー） =====
        MakeBg(canvasRoot, new Color(0.03f, 0.02f, 0.1f));

        // ===== 上部デコレーションバー =====
        var topBar = new GameObject("TopBar");
        topBar.transform.SetParent(canvasRoot, false);
        topBar.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.8f, 0.5f);
        var topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0f, 0.97f);
        topBarRT.anchorMax = Vector2.one;
        topBarRT.offsetMin = topBarRT.offsetMax = Vector2.zero;

        // ===== 下部デコレーションバー =====
        var botBar = new GameObject("BotBar");
        botBar.transform.SetParent(canvasRoot, false);
        botBar.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.8f, 0.5f);
        var botBarRT = botBar.GetComponent<RectTransform>();
        botBarRT.anchorMin = Vector2.zero;
        botBarRT.anchorMax = new Vector2(1f, 0.015f);
        botBarRT.offsetMin = botBarRT.offsetMax = Vector2.zero;

        // ===== 光の粒パーティクル =====
        CreateParticles(canvasRoot, 12);

        // ===== タイトル（Shadow+Outline付き） =====
        var titleText = MakeText(canvasRoot, "\u2726 RANKING \u2726", 52, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f, 0.93f), new Vector2(600f, 65f));
        var titleShadow = titleText.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.8f);
        titleShadow.effectDistance = new Vector2(3f, -3f);
        var titleOutline = titleText.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.8f, 0.2f, 0.4f, 0.9f);
        titleOutline.effectDistance = new Vector2(2f, -2f);

        // ===== タイトル下の装飾ライン =====
        MakeLine(canvasRoot, new Color(1f, 0.85f, 0.3f, 0.6f),
            new Vector2(0.5f, 0.91f), new Vector2(500f, 3f));

        // ===== ステージタブ =====
        BuildStageTabs(canvasRoot);

        // ===== ステージ名 =====
        stageTitle = MakeText(canvasRoot, "Stage 1", 28, new Color(0.7f, 0.7f, 0.85f),
            new Vector2(0.5f, 0.83f), new Vector2(400f, 36f));
        AddShadow(stageTitle.gameObject);

        // ===== スクロールビュー =====
        BuildScrollView(canvasRoot);

        // ===== BACK ボタン（装飾付き） =====
        BuildBackButton(canvasRoot);

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

            // 外枠（明るい縁取り）
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

            // 内側背景
            var innerGo = new GameObject("Inner");
            innerGo.transform.SetParent(go.transform, false);
            innerGo.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.18f, 0.95f);
            var innerRt = innerGo.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2f, 2f); innerRt.offsetMax = new Vector2(-2f, -2f);

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var t = txtGo.AddComponent<Text>();
            t.text = stageNum.ToString(); t.fontSize = 26; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
            t.font = cherry != null ? cherry : UIFont.Main;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            AddShadow(txtGo);
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

    void BuildBackButton(Transform parent)
    {
        // 外枠
        var go = new GameObject("BACKBtn");
        go.transform.SetParent(parent, false);
        var outerImg = go.AddComponent<Image>();
        outerImg.color = new Color(0.4f, 0.4f, 0.6f, 0.6f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.05f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(360f, 70f);
        btn.onClick.AddListener(() => SceneManager.LoadScene("HomeScene"));

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(0.18f, 0.15f, 0.3f, 0.95f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        // 上半分ハイライト（光沢感）
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        // ラベル
        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = "ホーム"; t.fontSize = 32; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : UIFont.Main;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(txtGo);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // ボタン全体にShadow
        var btnShadow = go.AddComponent<Shadow>();
        btnShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        btnShadow.effectDistance = new Vector2(4f, -4f);
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

        // Content クリア + 読み込み中表示
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);
        contentRT.sizeDelta = new Vector2(0f, 70f);
        MakeText(contentRoot, "読み込み中...", 28, new Color(0.6f, 0.6f, 0.7f),
            new Vector2(0.5f, 0.5f), new Vector2(400f, 40f));

        // Firestore から非同期取得（取得中にステージ切替・シーン破棄されたら結果を破棄）
        int requestStage = currentStage;
        RankingManager.GetTopRanking(currentStage, 20, ranking =>
        {
            if (this == null || contentRoot == null) return;
            if (requestStage != currentStage) return;

            foreach (Transform child in contentRoot)
                Destroy(child.gameObject);

            const float rowH = 70f;
            contentRT.sizeDelta = new Vector2(0f, Mathf.Max(ranking.Count, 1) * rowH);

            if (ranking.Count == 0)
            {
                MakeText(contentRoot, "No data", 28, new Color(0.5f, 0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(300f, 40f));
                return;
            }

            string myUid = AuthManager.GetUID();
            for (int i = 0; i < ranking.Count; i++)
            {
                var entry = ranking[i];
                bool isMe = !string.IsNullOrEmpty(myUid) && entry.uid == myUid;
                BuildRankRow(contentRoot, i, entry, rowH, isMe);
            }
        });
    }

    void BuildRankRow(Transform parent, int index, RankingManager.RankEntry entry, float rowH, bool isMe)
    {
        bool isTop3 = index < 3;

        // 外枠（トップ3はゴールド枠）
        Color outerCol = isTop3
            ? new Color(1f, 0.85f, 0.2f, 0.6f)
            : (isMe ? new Color(0.3f, 0.6f, 1f, 0.4f) : new Color(0.3f, 0.25f, 0.5f, 0.3f));

        var rowGo = new GameObject($"Rank_{index}");
        rowGo.transform.SetParent(parent, false);
        rowGo.AddComponent<Image>().color = outerCol;
        var rowRT = rowGo.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot     = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, -index * rowH);
        rowRT.sizeDelta = new Vector2(0f, rowH - 2f);

        // 内側背景
        Color innerCol = isMe
            ? new Color(0.12f, 0.2f, 0.4f, 0.95f)
            : (index % 2 == 0 ? new Color(0.06f, 0.05f, 0.14f, 0.95f) : new Color(0.08f, 0.07f, 0.18f, 0.95f));

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(rowGo.transform, false);
        innerGo.AddComponent<Image>().color = innerCol;
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2f, 2f); innerRt.offsetMax = new Vector2(-2f, -2f);

        // 上半分ハイライト（トップ3のみ光沢）
        if (isTop3)
        {
            var shineGo = new GameObject("Shine");
            shineGo.transform.SetParent(innerGo.transform, false);
            shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var shineRt = shineGo.GetComponent<RectTransform>();
            shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
            shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;
        }

        // 順位
        Color rankCol = isTop3 ? new Color(1f, 0.85f, 0.1f) : new Color(0.7f, 0.7f, 0.7f);
        var rankText = MakeText(rowGo.transform, $"#{index + 1}", 26, rankCol,
            new Vector2(0.08f, 0.5f), new Vector2(80f, 34f));
        if (isTop3) AddShadow(rankText.gameObject);

        // 名前
        Color nameCol = isMe ? new Color(0.5f, 0.9f, 1f) : Color.white;
        var nameT = MakeText(rowGo.transform, entry.name, 26, nameCol,
            new Vector2(0.38f, 0.5f), new Vector2(350f, 34f));
        nameT.alignment = TextAnchor.MiddleLeft;

        // クリア率
        MakeText(rowGo.transform, $"{entry.rate:P0}", 26, new Color(0.4f, 1f, 0.4f),
            new Vector2(0.85f, 0.5f), new Vector2(150f, 34f));
    }

    // ---- 光の粒パーティクル生成 ----

    void CreateParticles(Transform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var pGo = new GameObject("Particle");
            pGo.transform.SetParent(parent, false);
            var img = pGo.AddComponent<Image>();
            img.raycastTarget = false;
            float r = Random.Range(0.85f, 1f);
            float g = Random.Range(0.7f, 0.95f);
            float bv = Random.Range(0.8f, 1f);
            float a = Random.Range(0.15f, 0.4f);
            img.color = new Color(r, g, bv, a);

            var prt = pGo.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            float startX = Random.Range(-540f, 540f);
            float startY = Random.Range(-960f, 960f);
            prt.anchoredPosition = new Vector2(startX, startY);
            float size = Random.Range(4f, 12f);
            prt.sizeDelta = new Vector2(size, size);

            particles.Add(prt);
        }
    }

    // ---- ヘルパーメソッド ----

    void AddShadow(GameObject go)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.6f);
        s.effectDistance = new Vector2(2f, -2f);
    }

    void MakeLine(Transform parent, Color col, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
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
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }
}
