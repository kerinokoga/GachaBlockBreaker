using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ガチャ画面のランタイムUI
/// </summary>
public class GachaUI : MonoBehaviour
{
    // レアリティカラー
    static readonly Color ColSSR = new Color(1.0f, 0.85f, 0.1f);  // 金
    static readonly Color ColSR  = new Color(0.8f, 0.3f, 1.0f);   // 紫
    static readonly Color ColR   = new Color(0.2f, 0.5f, 1.0f);   // 青
    static readonly Color ColN   = new Color(0.55f, 0.55f, 0.55f); // グレー

    Transform canvasRoot;
    Text orbText;
    Text pityText;
    Text ownedText;
    Text capacityText;
    GameObject resultPanel;
    Button btnSingle;
    Button btnTen;

    GachaPoolData pool;
    CharacterData[] allChars;

    void Start()
    {
        pool     = Resources.Load<GachaPoolData>("Gacha/GachaPool");
        allChars = Resources.LoadAll<CharacterData>("Characters");
        BuildUI();
    }

    void BuildUI()
    {
        var cGo = new GameObject("GachaCanvas");
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
        MakeText(canvasRoot, "GACHA", 60, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f, 0.93f), new Vector2(600f, 80f));

        // オーブ表示
        orbText = MakeText(canvasRoot, $"Orb: {OrbManager.GetOrbs()}", 36,
            new Color(0.4f, 0.9f, 1f), new Vector2(0.5f, 0.86f), new Vector2(500f, 50f));

        // 天井カウンター
        pityText = MakeText(canvasRoot, $"天井まで: {OrbManager.PityLimit - OrbManager.GetPityCount()}", 26,
            new Color(0.6f, 0.6f, 0.6f), new Vector2(0.5f, 0.82f), new Vector2(400f, 38f));

        // 所持数表示
        ownedText = MakeText(canvasRoot, $"所持: {OrbManager.GetOwnedCount()} / 50", 26,
            new Color(0.7f, 0.7f, 0.85f), new Vector2(0.5f, 0.78f), new Vector2(400f, 38f));

        // 所持上限警告テキスト（必要時のみ表示）
        capacityText = MakeText(canvasRoot, "", 24,
            new Color(1f, 0.4f, 0.4f), new Vector2(0.5f, 0.74f), new Vector2(700f, 36f));

        // 結果パネル（初期非表示）
        var rpGo = new GameObject("ResultPanel");
        rpGo.transform.SetParent(canvasRoot, false);
        rpGo.AddComponent<Image>().color = new Color(0.08f, 0.05f, 0.18f, 0.95f);
        var rpRT = rpGo.GetComponent<RectTransform>();
        rpRT.anchorMin = new Vector2(0.03f, 0.30f);
        rpRT.anchorMax = new Vector2(0.97f, 0.78f);
        rpRT.offsetMin = rpRT.offsetMax = Vector2.zero;
        resultPanel = rpGo;
        resultPanel.SetActive(false);

        // 1連ボタン
        btnSingle = MakeButton(canvasRoot, $"1連ガチャ  ({OrbManager.CostSingle} Orb)",
            new Color(0.85f, 0.45f, 0.1f),
            new Vector2(0.5f, 0.22f), new Vector2(600f, 100f),
            OnSingleDraw);

        // 10連ボタン
        btnTen = MakeButton(canvasRoot, $"10連ガチャ  ({OrbManager.CostTen} Orb)",
            new Color(0.5f, 0.1f, 0.85f),
            new Vector2(0.5f, 0.10f), new Vector2(600f, 100f),
            OnTenDraw);

        // HOMEボタン
        MakeButton(canvasRoot, "HOME", new Color(0.25f, 0.25f, 0.35f),
            new Vector2(0.5f, 0.02f), new Vector2(360f, 70f),
            () => SceneManager.LoadScene("HomeScene"));

        RefreshButtons();
    }

    // ---- ガチャ実行 ----

    void OnSingleDraw()
    {
        if (!OrbManager.SpendOrbs(OrbManager.CostSingle))
        {
            StartCoroutine(ShowInsufficientOrbs());
            return;
        }
        RefreshOrbDisplay();

        if (pool == null || allChars == null || allChars.Length == 0)
        {
            Debug.LogWarning("GachaPoolData または CharacterData が見つかりません");
            return;
        }

        GachaResult result = GachaEngine.DrawSingle(pool, allChars);
        RefreshOrbDisplay();
        StartCoroutine(ShowSingleResult(result));
    }

    void OnTenDraw()
    {
        if (!OrbManager.SpendOrbs(OrbManager.CostTen))
        {
            StartCoroutine(ShowInsufficientOrbs());
            return;
        }

        if (pool == null || allChars == null || allChars.Length == 0)
        {
            Debug.LogWarning("GachaPoolData または CharacterData が見つかりません");
            return;
        }

        GachaResult[] results = GachaEngine.DrawTen(pool, allChars);
        RefreshOrbDisplay();
        StartCoroutine(ShowTenResult(results));
    }

    // ---- 結果演出 ----

    IEnumerator ShowSingleResult(GachaResult result)
    {
        SetButtonsInteractable(false);
        ClearResultPanel();
        resultPanel.SetActive(true);

        // カード生成（フェードイン）
        var card = BuildResultCard(resultPanel.transform, result, new Vector2(0.5f, 0.5f), new Vector2(500f, 200f));
        var img = card.GetComponent<Image>();
        img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Clamp01(t / 0.4f));
            yield return null;
        }

        // タップで閉じるオーバーレイ
        AddCloseOverlay();
        SetButtonsInteractable(true);
    }

    IEnumerator ShowTenResult(GachaResult[] results)
    {
        SetButtonsInteractable(false);
        ClearResultPanel();
        resultPanel.SetActive(true);

        // 2列×5行グリッド
        float[] xs = { 0.27f, 0.73f };
        float[] ys = { 0.82f, 0.64f, 0.46f, 0.28f, 0.1f };

        for (int i = 0; i < results.Length; i++)
        {
            float ax = xs[i % 2];
            float ay = ys[i / 2];
            BuildResultCard(resultPanel.transform, results[i],
                new Vector2(ax, ay), new Vector2(420f, 120f));
            yield return new WaitForSeconds(0.08f);
        }

        AddCloseOverlay();
        SetButtonsInteractable(true);
    }

    IEnumerator ShowInsufficientOrbs()
    {
        SetButtonsInteractable(false);
        var warn = MakeText(canvasRoot, "オーブが足りません", 32,
            new Color(1f, 0.3f, 0.3f), new Vector2(0.5f, 0.28f), new Vector2(500f, 50f));
        yield return new WaitForSeconds(1.5f);
        Destroy(warn.gameObject);
        SetButtonsInteractable(true);
    }

    // ---- カード生成 ----

    GameObject BuildResultCard(Transform parent, GachaResult result, Vector2 anchor, Vector2 size)
    {
        Color rarityCol = RarityColor(result.chara.rarity);
        Color bgCol     = new Color(rarityCol.r * 0.25f, rarityCol.g * 0.25f, rarityCol.b * 0.25f);

        var cardGo = new GameObject("Card");
        cardGo.transform.SetParent(parent, false);
        cardGo.AddComponent<Image>().color = bgCol;
        var rt = cardGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        // 上部カラーバー（レアリティカラー）
        var barGo = new GameObject("RarityBar");
        barGo.transform.SetParent(cardGo.transform, false);
        barGo.AddComponent<Image>().color = rarityCol;
        var brt = barGo.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(0f, 6f);

        // キャラ名
        string nameLabel = result.chara.characterName;
        if (result.isNew) nameLabel += "  NEW!";
        MakeText(cardGo.transform, nameLabel, 28, Color.white,
            new Vector2(0.5f, 0.65f), new Vector2(size.x - 20f, 40f));

        // レアリティ文字
        MakeText(cardGo.transform, result.chara.rarity.ToString(), 22, rarityCol,
            new Vector2(0.5f, 0.28f), new Vector2(size.x - 20f, 32f));

        return cardGo;
    }

    void AddCloseOverlay()
    {
        var ov = new GameObject("CloseOverlay");
        ov.transform.SetParent(canvasRoot, false);
        ov.AddComponent<Image>().color = Color.clear;
        var rt = ov.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var btn = ov.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() =>
        {
            resultPanel.SetActive(false);
            Destroy(ov);
        });
    }

    void ClearResultPanel()
    {
        foreach (Transform child in resultPanel.transform)
            Destroy(child.gameObject);
    }

    // ---- 表示更新 ----

    void RefreshOrbDisplay()
    {
        if (orbText)   orbText.text   = $"Orb: {OrbManager.GetOrbs()}";
        if (pityText)  pityText.text  = $"天井まで: {OrbManager.PityLimit - OrbManager.GetPityCount()}";
        if (ownedText) ownedText.text = $"所持: {OrbManager.GetOwnedCount()} / 50";
        RefreshButtons();
    }

    void RefreshButtons()
    {
        bool canSingle = OrbManager.CanAfford(OrbManager.CostSingle) && OrbManager.CanDrawSingle();
        bool canTen    = OrbManager.CanAfford(OrbManager.CostTen)    && OrbManager.CanDrawTen();
        if (btnSingle) btnSingle.interactable = canSingle;
        if (btnTen)    btnTen.interactable    = canTen;

        // 所持上限メッセージ
        if (capacityText != null)
        {
            int owned = OrbManager.GetOwnedCount();
            if (owned >= 50)
                capacityText.text = "所持上限(50体)です。MANAGEでキャラを削除してください";
            else if (owned == 49)
                capacityText.text = "所持49体：1連のみ引けます";
            else
                capacityText.text = "";
        }
    }

    void SetButtonsInteractable(bool value)
    {
        if (btnSingle) btnSingle.interactable = value && OrbManager.CanAfford(OrbManager.CostSingle) && OrbManager.CanDrawSingle();
        if (btnTen)    btnTen.interactable    = value && OrbManager.CanAfford(OrbManager.CostTen)    && OrbManager.CanDrawTen();
    }

    // ---- レアリティカラー ----

    static Color RarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.SSR: return ColSSR;
            case Rarity.SR:  return ColSR;
            case Rarity.R:   return ColR;
            default:         return ColN;
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
