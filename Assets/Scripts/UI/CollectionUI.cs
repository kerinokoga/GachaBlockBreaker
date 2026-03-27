using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// コレクション画面：クリア済みステージの美少女解放状態を一覧表示（ScrollRect 対応）
/// </summary>
public class CollectionUI : MonoBehaviour
{
    void Start() => BuildUI();

    void BuildUI()
    {
        var cGo = new GameObject("CollectionCanvas");
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

        Transform root = cGo.transform;

        // 背景
        MakeBg(root, new Color(0.04f, 0.04f, 0.14f));

        // タイトル
        MakeText(root, "COLLECTION", 52, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.94f), new Vector2(800f, 70f));

        // HOME ボタン
        MakeButton(root, "HOME", new Color(0.25f, 0.25f, 0.35f),
            new Vector2(0.5f, 0.04f), new Vector2(360f, 80f),
            () => SceneManager.LoadScene("HomeScene"));

        // ScrollRect（y=0.10〜0.88）
        BuildScrollList(root);
    }

    void BuildScrollList(Transform root)
    {
        // Viewport
        var viewGo = new GameObject("Viewport");
        viewGo.transform.SetParent(root, false);
        var viewImg = viewGo.AddComponent<Image>();
        viewImg.color = Color.white;
        viewGo.AddComponent<Mask>().showMaskGraphic = false;
        var viewRT = viewGo.GetComponent<RectTransform>();
        viewRT.anchorMin = new Vector2(0.02f, 0.10f);
        viewRT.anchorMax = new Vector2(0.98f, 0.88f);
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        // Content
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewGo.transform, false);
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        // ScrollRect
        var srGo = new GameObject("ScrollRect");
        srGo.transform.SetParent(root, false);
        var sr = srGo.AddComponent<ScrollRect>();
        var srRT = srGo.GetComponent<RectTransform>();
        srRT.anchorMin = new Vector2(0.02f, 0.10f);
        srRT.anchorMax = new Vector2(0.98f, 0.88f);
        srRT.offsetMin = srRT.offsetMax = Vector2.zero;
        sr.content    = contentRT;
        sr.viewport   = viewRT;
        sr.horizontal = false;
        sr.vertical   = true;
        sr.scrollSensitivity = 30f;

        // ステージデータ
        var allStages = Resources.LoadAll<StageData>("Stages");

        // セル設定
        float cellH  = 200f;
        float cellW  = 480f;
        float padX   = 20f;
        float padY   = 16f;
        int   cols   = 2;
        int   total  = ProgressManager.TotalStages;
        int   rows   = Mathf.CeilToInt((float)total / cols);

        float contentHeight = rows * (cellH + padY) + padY;
        contentRT.sizeDelta = new Vector2(0f, contentHeight);

        for (int i = 0; i < total; i++)
        {
            int stageNum = i + 1;
            int col = i % cols;
            int row = i / cols;

            float xAnchor = col == 0 ? 0.27f : 0.73f;
            float yPos    = -(padY + row * (cellH + padY) + cellH * 0.5f);

            StageData sd = null;
            foreach (var s in allStages)
                if (s.stageNumber == stageNum) { sd = s; break; }

            bool  cleared = ProgressManager.IsCleared(stageNum);
            float rate    = ProgressManager.GetBestRate(stageNum);

            BuildCell(contentRT, stageNum, sd, cleared, rate, xAnchor, yPos, cellW, cellH);
        }
    }

    void BuildCell(Transform parent, int stageNum, StageData sd,
        bool cleared, float rate, float anchorX, float yPos, float w, float h)
    {
        Color bgCol = cleared
            ? new Color(0.12f, 0.10f, 0.22f)
            : new Color(0.08f, 0.08f, 0.12f);

        var cellGo = new GameObject($"Stage{stageNum}Cell");
        cellGo.transform.SetParent(parent, false);
        cellGo.AddComponent<Image>().color = bgCol;
        var rt = cellGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(w, h);

        Transform ct = cellGo.transform;

        // ステージ番号
        Color titleCol = cleared ? Color.white : new Color(0.4f, 0.4f, 0.4f);
        MakeCellText(ct, $"STAGE {stageNum}", 28, titleCol,
            new Vector2(0.5f, 0.85f), new Vector2(w - 20f, 36f));

        if (cleared && sd != null)
        {
            // カラースウォッチ
            var swatchGo = new GameObject("Swatch");
            swatchGo.transform.SetParent(ct, false);
            swatchGo.AddComponent<Image>().color = sd.illustColorFull;
            var sRT = swatchGo.GetComponent<RectTransform>();
            sRT.anchorMin = sRT.anchorMax = new Vector2(0.22f, 0.46f);
            sRT.sizeDelta = new Vector2(80f, 80f);

            string charName = !string.IsNullOrEmpty(sd.characterName) ? sd.characterName : "???";
            MakeCellText(ct, charName, 24, new Color(0.9f, 0.85f, 1f),
                new Vector2(0.65f, 0.56f), new Vector2(220f, 34f));

            MakeCellText(ct, $"{Mathf.FloorToInt(rate * 100)}%", 22,
                new Color(1f, 0.9f, 0.2f), new Vector2(0.65f, 0.26f), new Vector2(180f, 30f));

            // 上端カラーライン
            var lineGo = new GameObject("Line");
            lineGo.transform.SetParent(ct, false);
            lineGo.AddComponent<Image>().color = sd.illustColorFull;
            var lRT = lineGo.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0f, 1f);
            lRT.anchorMax = new Vector2(1f, 1f);
            lRT.pivot = new Vector2(0.5f, 1f);
            lRT.anchoredPosition = Vector2.zero;
            lRT.sizeDelta = new Vector2(0f, 5f);
        }
        else
        {
            MakeCellText(ct, "???", 36, new Color(0.3f, 0.3f, 0.3f),
                new Vector2(0.5f, 0.47f), new Vector2(w - 20f, 50f));
            MakeCellText(ct, "Not cleared", 20, new Color(0.3f, 0.3f, 0.3f),
                new Vector2(0.5f, 0.2f), new Vector2(w - 20f, 28f));
        }
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
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    Text MakeCellText(Transform parent, string txt, int size, Color col, Vector2 anchor, Vector2 sizeDelta)
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

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 36; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 36);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }
}
