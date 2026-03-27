using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ステージ選択画面（ScrollRect 対応・20ステージ）
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    void Start() => BuildUI();

    void BuildUI()
    {
        var cGo = new GameObject("StageSelectCanvas");
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
        MakeBg(root, new Color(0.05f, 0.05f, 0.18f));

        // タイトル
        MakeText(root, "STAGE SELECT", 52, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.93f), new Vector2(800f, 70f));

        // BACK ボタン
        MakeButton(root, "BACK", new Color(0.25f, 0.25f, 0.35f),
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
        viewGo.AddComponent<Image>().color = Color.white;
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

        var allStages  = Resources.LoadAll<StageData>("Stages");
        int maxUnlocked = ProgressManager.GetMaxUnlocked();

        int   total  = ProgressManager.TotalStages;
        int   cols   = 2;
        int   rows   = Mathf.CeilToInt((float)total / cols);
        float cellW  = 460f;
        float cellH  = 220f;
        float padY   = 16f;

        float contentHeight = rows * (cellH + padY) + padY;
        contentRT.sizeDelta = new Vector2(0f, contentHeight);

        for (int i = 0; i < total; i++)
        {
            int   stageNum = i + 1;
            int   col      = i % cols;
            int   row      = i / cols;
            float xAnchor  = col == 0 ? 0.27f : 0.73f;
            float yPos     = -(padY + row * (cellH + padY) + cellH * 0.5f);

            StageData sd = null;
            foreach (var s in allStages)
                if (s.stageNumber == stageNum) { sd = s; break; }

            bool  unlocked = stageNum <= maxUnlocked;
            bool  cleared  = ProgressManager.IsCleared(stageNum);
            float rate     = ProgressManager.GetBestRate(stageNum);

            BuildStageCard(contentRT, stageNum, sd, unlocked, cleared, rate,
                           xAnchor, yPos, cellW, cellH);
        }
    }

    void BuildStageCard(Transform parent, int stageNum, StageData sd,
        bool unlocked, bool cleared, float rate,
        float anchorX, float yPos, float w, float h)
    {
        int capturedNum = stageNum;

        Color cardCol = unlocked
            ? new Color(0.12f, 0.12f, 0.28f)
            : new Color(0.10f, 0.10f, 0.12f);

        var cardGo = new GameObject($"Stage{stageNum}Card");
        cardGo.transform.SetParent(parent, false);
        cardGo.AddComponent<Image>().color = cardCol;
        var rt = cardGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(w, h);

        Transform ct = cardGo.transform;

        // クリア済みなら上端カラーライン
        if (cleared && sd != null)
        {
            var lineGo = new GameObject("Line");
            lineGo.transform.SetParent(ct, false);
            lineGo.AddComponent<Image>().color = sd.illustColorFull;
            var lRT = lineGo.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0f, 1f);
            lRT.anchorMax = new Vector2(1f, 1f);
            lRT.pivot = new Vector2(0.5f, 1f);
            lRT.anchoredPosition = Vector2.zero;
            lRT.sizeDelta = new Vector2(0f, 6f);
        }

        // ステージ番号
        MakeCellText(ct, $"STAGE {stageNum}", 32,
            unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f),
            new Vector2(0.5f, 0.80f), new Vector2(w - 20f, 44f));

        if (unlocked)
        {
            Color swatchCol = (sd != null) ? sd.illustColorFull : new Color(0.4f, 0.4f, 0.6f);
            var swatchGo = new GameObject("Swatch");
            swatchGo.transform.SetParent(ct, false);
            swatchGo.AddComponent<Image>().color = swatchCol;
            var sRT = swatchGo.GetComponent<RectTransform>();
            sRT.anchorMin = sRT.anchorMax = new Vector2(0.22f, 0.50f);
            sRT.sizeDelta = new Vector2(64f, 64f);

            string charName = (sd != null && !string.IsNullOrEmpty(sd.characterName))
                ? sd.characterName : "???";
            MakeCellText(ct, charName, 24, new Color(0.85f, 0.85f, 1f),
                new Vector2(0.65f, 0.52f), new Vector2(w * 0.6f, 32f));

            if (cleared)
            {
                MakeCellText(ct, $"★ {Mathf.FloorToInt(rate * 100)}%", 20,
                    new Color(1f, 0.9f, 0.2f),
                    new Vector2(0.65f, 0.28f), new Vector2(w * 0.5f, 28f));
            }

            var btn = cardGo.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() =>
            {
                ResultData.StageNumber = capturedNum;
                SceneManager.LoadScene("CharaSelectScene");
            });
        }
        else
        {
            MakeCellText(ct, "LOCKED", 28, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.5f, 0.50f), new Vector2(w - 20f, 40f));

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(ct, false);
            overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var ort = overlay.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;
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
