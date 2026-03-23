using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム中の UI をランタイムで構築・管理する
/// </summary>
public class GameUI : MonoBehaviour
{
    // ランタイムで作成した参照
    private Image[] stockIcons = new Image[3];
    private Slider destroyRateSlider;
    private Text destroyRateText;
    private GameObject pauseMenuPanel;

    private Color activeColor   = new Color(1f, 0.9f, 0.2f);
    private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

    void Start()
    {
        BuildUI();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStockChanged    += UpdateStockDisplay;
            GameManager.Instance.OnDestroyRateChanged += UpdateDestroyRate;
            UpdateStockDisplay(GameManager.Instance.MaxStock);
            UpdateDestroyRate(0f);
        }
    }

    void BuildUI()
    {
        // Canvas
        var cGo = new GameObject("GameCanvas");
        Canvas c = cGo.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight  = 0.5f;
        cGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Transform root = cGo.transform;

        // ---- ストックアイコン（左上に黄色丸3個）----
        for (int i = 0; i < 3; i++)
        {
            var icon = MakeImage(root, activeColor,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f + i * 64f, -30f), new Vector2(52f, 52f),
                new Vector2(0f, 1f));
            stockIcons[i] = icon;
        }

        // ---- 破壊率（右上）----
        destroyRateText = MakeText(root, "0%", 28, Color.white,
            new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(130f, 48f),
            new Vector2(1f, 1f), TextAnchor.UpperRight);

        var sliderGo = new GameObject("DestroyRateSlider");
        sliderGo.transform.SetParent(root, false);
        destroyRateSlider = sliderGo.AddComponent<Slider>();
        SetRect(sliderGo.GetComponent<RectTransform>(),
            new Vector2(1f, 1f), new Vector2(-20f, -75f), new Vector2(200f, 18f), new Vector2(1f, 1f));
        BuildSlider(destroyRateSlider, sliderGo.transform);

        // ---- ポーズボタン（上部中央）----
        var pauseBtn = MakeButton(root, "||", 30, new Color(0.15f, 0.15f, 0.25f, 0.9f),
            new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(80f, 56f),
            new Vector2(0.5f, 1f));
        pauseBtn.GetComponent<Button>().onClick.AddListener(OnPauseButtonClicked);

        // ---- PauseMenu パネル----
        pauseMenuPanel = MakePanel(root, new Color(0f, 0f, 0f, 0.88f));

        MakeText(pauseMenuPanel.transform, "PAUSE", 60, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(400f, 80f));

        MakeButton(pauseMenuPanel.transform, "RESUME", 36, new Color(0.2f, 0.5f, 1f),
            new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(320f, 72f))
            .GetComponent<Button>().onClick.AddListener(OnResumeClicked);

        var retireGo = MakeButton(pauseMenuPanel.transform, "RETIRE", 36, new Color(0.85f, 0.2f, 0.2f),
            new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(320f, 72f));
        retireGo.GetComponent<Button>().onClick.AddListener(OnRetireClicked);

        MakeButton(pauseMenuPanel.transform, "HOW TO PLAY", 26, new Color(0.3f, 0.3f, 0.4f),
            new Vector2(0.5f, 0.40f), Vector2.zero, new Vector2(320f, 72f))
            .GetComponent<Button>().onClick.AddListener(OnHelpClicked);

        MakeText(pauseMenuPanel.transform, "BGM", 26, Color.white,
            new Vector2(0.28f, 0.30f), Vector2.zero, new Vector2(120f, 40f));
        var bgmSlGo = new GameObject("BGMSlider");
        bgmSlGo.transform.SetParent(pauseMenuPanel.transform, false);
        var bgmSl = bgmSlGo.AddComponent<Slider>();
        SetRect(bgmSlGo.GetComponent<RectTransform>(),
            new Vector2(0.63f, 0.30f), Vector2.zero, new Vector2(220f, 28f));
        BuildSlider(bgmSl, bgmSlGo.transform);
        bgmSl.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        bgmSl.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat("BGMVolume", v); AudioManager.Instance?.SetBGMVolume(v); });

        MakeText(pauseMenuPanel.transform, "SE", 26, Color.white,
            new Vector2(0.28f, 0.22f), Vector2.zero, new Vector2(120f, 40f));
        var seSlGo = new GameObject("SESlider");
        seSlGo.transform.SetParent(pauseMenuPanel.transform, false);
        var seSl = seSlGo.AddComponent<Slider>();
        SetRect(seSlGo.GetComponent<RectTransform>(),
            new Vector2(0.63f, 0.22f), Vector2.zero, new Vector2(220f, 28f));
        BuildSlider(seSl, seSlGo.transform);
        seSl.value = PlayerPrefs.GetFloat("SEVolume", 1f);
        seSl.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat("SEVolume", v); AudioManager.Instance?.SetSEVolume(v); });

        // リタイヤ確認ダイアログ
        var retireConfirm = MakePanel(root, new Color(0f, 0f, 0f, 0.92f));
        MakeText(retireConfirm.transform, "RETIRE?", 44, Color.white,
            new Vector2(0.5f, 0.57f), Vector2.zero, new Vector2(400f, 60f));
        MakeButton(retireConfirm.transform, "YES", 36, new Color(0.85f, 0.2f, 0.2f),
            new Vector2(0.32f, 0.45f), Vector2.zero, new Vector2(200f, 65f))
            .GetComponent<Button>().onClick.AddListener(() => GameManager.Instance?.Retire());
        MakeButton(retireConfirm.transform, "NO", 36, new Color(0.2f, 0.5f, 1f),
            new Vector2(0.68f, 0.45f), Vector2.zero, new Vector2(200f, 65f))
            .GetComponent<Button>().onClick.AddListener(() => retireConfirm.SetActive(false));
        retireConfirm.SetActive(false);
        retireGo.GetComponent<Button>().onClick.AddListener(() => retireConfirm.SetActive(true));

        // ヘルプパネル
        var helpPanel = MakePanel(root, new Color(0f, 0f, 0.1f, 0.96f));
        var helpTxt = MakeText(helpPanel.transform,
            "HOW TO PLAY\n\nSwipe left/right to move paddle\n\nDon't let the ball fall!\n\nDestroy all blocks to clear!",
            30, Color.white, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(700f, 500f));
        helpTxt.alignment = TextAnchor.MiddleCenter;
        MakeButton(helpPanel.transform, "CLOSE", 34, new Color(0.3f, 0.3f, 0.4f),
            new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(220f, 65f))
            .GetComponent<Button>().onClick.AddListener(() => helpPanel.SetActive(false));
        helpPanel.SetActive(false);

        // HelpClicked が helpPanel を開く
        pauseMenuPanel.GetComponentsInChildren<Button>(true)[2]
            .onClick.AddListener(() => helpPanel.SetActive(true));

        pauseMenuPanel.SetActive(false);
    }

    // ---- UI イベント ----

    public void OnPauseButtonClicked()
    {
        GameManager.Instance?.Pause();
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    void OnResumeClicked()
    {
        GameManager.Instance?.Resume();
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    void OnRetireClicked() { }   // retire confirm handled by lambda above
    void OnHelpClicked()  { }    // help panel handled by lambda above

    // ---- データ更新 ----

    public void UpdateStockDisplay(int remaining)
    {
        for (int i = 0; i < stockIcons.Length; i++)
            if (stockIcons[i] != null)
                stockIcons[i].color = i < remaining ? activeColor : inactiveColor;
    }

    public void UpdateDestroyRate(float rate)
    {
        if (destroyRateSlider != null) destroyRateSlider.value = rate;
        if (destroyRateText   != null) destroyRateText.text = $"{Mathf.FloorToInt(rate * 100)}%";
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStockChanged      -= UpdateStockDisplay;
            GameManager.Instance.OnDestroyRateChanged -= UpdateDestroyRate;
        }
    }

    // ---- ファクトリーメソッド ----

    Image MakeImage(Transform parent, Color col,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size, Vector2 pivot = default)
    {
        var go = new GameObject("Icon");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return img;
    }

    Text MakeText(Transform parent, string txt, int size, Color col,
        Vector2 anchor, Vector2 pos, Vector2 sizeDelta,
        Vector2 pivot = default, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.alignment = align;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    GameObject MakeButton(Transform parent, string label, int fontSize, Color bgCol,
        Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot = default)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgCol;
        go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = fontSize; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return go;
    }

    GameObject MakePanel(Transform parent, Color col)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    void BuildSlider(Slider slider, Transform parent)
    {
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

        var bg = new GameObject("BG"); bg.transform.SetParent(parent, false);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.2f, 0.2f, 0.2f);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var fa = new GameObject("FillArea"); fa.transform.SetParent(parent, false);
        var faRT = fa.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = faRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = new Color(0.3f, 0.8f, 1f);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        slider.fillRect = fillRT;
        slider.targetGraphic = bgImg;
        slider.direction = Slider.Direction.LeftToRight;
    }

    void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot = default)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
