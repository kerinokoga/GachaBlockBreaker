using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// コレクション画面：クリア済みステージの美少女解放状態を一覧表示
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
            new Vector2(0.5f, 0.93f), new Vector2(800f, 70f));

        // StageData ロード
        var allStages = Resources.LoadAll<StageData>("Stages");

        float[] colXs = { 0.27f, 0.73f };
        float[] rowYs = { 0.82f, 0.65f, 0.48f };

        for (int i = 0; i < ProgressManager.TotalStages; i++)
        {
            int stageNum = i + 1;
            float xPos = colXs[i % 2];
            float yPos = rowYs[i / 2];

            StageData sd = null;
            foreach (var s in allStages)
                if (s.stageNumber == stageNum) { sd = s; break; }

            bool cleared = ProgressManager.IsCleared(stageNum);
            float rate   = ProgressManager.GetBestRate(stageNum);

            BuildCollectionCell(root, stageNum, sd, cleared, rate, xPos, yPos);
        }

        // BACK ボタン
        MakeButton(root, "BACK", new Color(0.25f, 0.25f, 0.35f),
            new Vector2(0.5f, 0.07f), new Vector2(360f, 90f),
            () => SceneManager.LoadScene("HomeScene"));
    }

    void BuildCollectionCell(Transform root, int stageNum, StageData sd,
        bool cleared, float rate, float anchorX, float anchorY)
    {
        Color bgCol = cleared
            ? new Color(0.12f, 0.1f, 0.2f)
            : new Color(0.08f, 0.08f, 0.1f);

        var cell = MakeRectImage(root, bgCol,
            new Vector2(anchorX, anchorY), Vector2.zero, new Vector2(460f, 200f));

        // ステージ番号
        Color titleCol = cleared ? Color.white : new Color(0.4f, 0.4f, 0.4f);
        MakeText(cell.transform, $"STAGE {stageNum}", 32, titleCol,
            new Vector2(0.5f, 0.82f), new Vector2(420f, 44f));

        if (cleared && sd != null)
        {
            // カラースウォッチ（解放カラー）
            MakeRectImage(cell.transform, sd.illustColorFull,
                new Vector2(0.25f, 0.47f), Vector2.zero, new Vector2(90f, 90f));

            // キャラ名
            string charName = !string.IsNullOrEmpty(sd.characterName) ? sd.characterName : "???";
            MakeText(cell.transform, charName, 28, new Color(0.9f, 0.85f, 1f),
                new Vector2(0.65f, 0.55f), new Vector2(220f, 40f));

            // 破壊率
            MakeText(cell.transform, $"{Mathf.FloorToInt(rate * 100)}%", 24,
                new Color(1f, 0.9f, 0.2f), new Vector2(0.65f, 0.25f), new Vector2(180f, 34f));

            // クリアマーク上枠
            var border = MakeRectImage(cell.transform, sd.illustColorFull,
                new Vector2(0f, 1f), Vector2.zero, new Vector2(460f, 6f));
            border.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        }
        else
        {
            // 未クリア
            MakeText(cell.transform, "???", 44, new Color(0.3f, 0.3f, 0.3f),
                new Vector2(0.5f, 0.47f), new Vector2(300f, 60f));
            MakeText(cell.transform, "Not cleared", 22, new Color(0.35f, 0.35f, 0.35f),
                new Vector2(0.5f, 0.2f), new Vector2(380f, 32f));
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
