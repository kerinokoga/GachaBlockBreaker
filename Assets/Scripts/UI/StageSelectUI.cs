using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ステージ選択画面のランタイムUI
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
            new Vector2(0.5f, 0.92f), new Vector2(800f, 70f));
        MakeText(root, "Select a stage", 28, new Color(0.7f, 0.7f, 0.9f),
            new Vector2(0.5f, 0.86f), new Vector2(700f, 42f));

        // ステージデータをロード
        var allStages = Resources.LoadAll<StageData>("Stages");
        int maxUnlocked = ProgressManager.GetMaxUnlocked();

        // 2列レイアウト
        float[] colXs = { 0.28f, 0.72f };
        float[] rowYs = { 0.74f, 0.55f, 0.36f };

        for (int i = 0; i < ProgressManager.TotalStages; i++)
        {
            int stageNum = i + 1;
            float xPos = colXs[i % 2];
            float yPos = rowYs[i / 2];

            StageData sd = null;
            foreach (var s in allStages)
                if (s.stageNumber == stageNum) { sd = s; break; }

            bool unlocked = stageNum <= maxUnlocked;
            bool cleared  = ProgressManager.IsCleared(stageNum);
            float rate    = ProgressManager.GetBestRate(stageNum);

            BuildStageCard(root, stageNum, sd, unlocked, cleared, rate, xPos, yPos);
        }

        // BACK ボタン
        MakeButton(root, "BACK", new Color(0.25f, 0.25f, 0.35f),
            new Vector2(0.5f, 0.08f), new Vector2(360f, 90f),
            () => SceneManager.LoadScene("HomeScene"));
    }

    void BuildStageCard(Transform root, int stageNum, StageData sd,
        bool unlocked, bool cleared, float rate,
        float anchorX, float anchorY)
    {
        int capturedNum = stageNum;

        // カード背景
        Color cardCol = unlocked
            ? new Color(0.12f, 0.12f, 0.28f)
            : new Color(0.1f, 0.1f, 0.1f);

        var card = MakeRectImage(root, cardCol,
            new Vector2(anchorX, anchorY), Vector2.zero, new Vector2(320f, 220f));

        // クリア済みなら上枠を色付き
        if (cleared && sd != null)
        {
            var border = MakeRectImage(card.transform, sd.illustColorFull,
                new Vector2(0f, 1f), Vector2.zero, new Vector2(320f, 8f));
            border.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        }

        // ステージ番号
        MakeText(card.transform, $"STAGE {stageNum}", 34,
            unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f),
            new Vector2(0.5f, 0.78f), new Vector2(280f, 48f));

        if (unlocked)
        {
            // カラースウォッチ（解放カラーのプレビュー）
            Color swatchCol = (sd != null) ? sd.illustColorFull : new Color(0.4f, 0.4f, 0.6f);
            MakeRectImage(card.transform, swatchCol,
                new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(60f, 60f));

            // キャラ名
            string charName = (sd != null && !string.IsNullOrEmpty(sd.characterName))
                ? sd.characterName : "???";
            MakeText(card.transform, charName, 26, new Color(0.85f, 0.85f, 1f),
                new Vector2(0.5f, 0.28f), new Vector2(280f, 36f));

            // クリア状態
            if (cleared)
            {
                MakeText(card.transform, $"★ {Mathf.FloorToInt(rate * 100)}%", 22,
                    new Color(1f, 0.9f, 0.2f), new Vector2(0.5f, 0.1f), new Vector2(280f, 30f));
            }

            // タップで選択
            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() =>
            {
                ResultData.StageNumber = capturedNum;
                SceneManager.LoadScene("CharaSelectScene");
            });
        }
        else
        {
            // ロック表示
            MakeText(card.transform, "LOCKED", 30, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.5f, 0.5f), new Vector2(280f, 44f));

            // 暗いオーバーレイ
            var overlay = MakeRectImage(card.transform, new Color(0f, 0f, 0f, 0.55f),
                new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
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

    Image MakeRectImage(Transform parent, Color col, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Img");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
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
        t.text = label; t.fontSize = 38; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 38);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }
}
