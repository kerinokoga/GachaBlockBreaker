using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// キャラ管理画面：所持キャラ一覧・削除・合成強化
/// スターターキャラ（Luna/Aria/Sera）は削除不可
/// </summary>
public class CharaManageUI : MonoBehaviour
{
    static readonly string[] StarterNames = { "Luna", "Aria", "Sera" };

    // レアリティカラー
    static readonly Color ColSSR = new Color(1.0f, 0.85f, 0.1f);
    static readonly Color ColSR  = new Color(0.8f, 0.3f, 1.0f);
    static readonly Color ColR   = new Color(0.2f, 0.5f, 1.0f);
    static readonly Color ColN   = new Color(0.55f, 0.55f, 0.55f);

    Transform canvasRoot;
    Text countText;
    Transform contentRoot;
    RectTransform contentRT;

    CharacterData[] allChars;

    void Start()
    {
        allChars = Resources.LoadAll<CharacterData>("Characters");
        BuildUI();
    }

    void BuildUI()
    {
        var cGo = new GameObject("ManageCanvas");
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
        MakeBg(canvasRoot, new Color(0.05f, 0.05f, 0.15f));

        // タイトル
        MakeText(canvasRoot, "キャラ管理", 52, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.92f), new Vector2(700f, 70f));

        // 所持数表示
        countText = MakeText(canvasRoot, "", 30, new Color(0.8f, 0.8f, 1f),
            new Vector2(0.5f, 0.86f), new Vector2(500f, 45f));

        // スクロールビュー
        BuildScrollView(canvasRoot);

        // BACKボタン
        MakeButton(canvasRoot, "BACK", new Color(0.25f, 0.25f, 0.35f),
            new Vector2(0.5f, 0.05f), new Vector2(360f, 70f),
            () => SceneManager.LoadScene("HomeScene"));

        RefreshList();
    }

    void BuildScrollView(Transform parent)
    {
        // ScrollRect 外枠
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(parent, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0f, 0.12f);
        scrollRT.anchorMax = new Vector2(1f, 0.83f);
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;
        var sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.scrollSensitivity = 40f;

        // Viewport
        var vpGo = new GameObject("Viewport");
        vpGo.transform.SetParent(scrollGo.transform, false);
        var vpImg = vpGo.AddComponent<Image>();
        vpImg.color = Color.white; // Mask は alpha>0 が必要（showMaskGraphic=false で非表示）
        vpGo.AddComponent<Mask>().showMaskGraphic = false;
        var vpRT = vpGo.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;

        // Content
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

    // ---- リスト更新 ----

    void RefreshList()
    {
        // Content をクリア
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        var owned = System.Array.FindAll(allChars, c => OrbManager.IsOwned(c.characterName));

        // 所持済みだが Count=0 のキャラを補正（スターターキャラ等）
        foreach (var c in owned)
            if (OrbManager.GetCharCount(c.characterName) == 0)
                OrbManager.AddCharCount(c.characterName);

        int count = owned.Length;

        if (countText) countText.text = $"所持: {count} / 50";

        // Content の高さを行数に合わせる
        const float rowH = 130f;
        contentRT.sizeDelta = new Vector2(0f, count * rowH);

        for (int i = 0; i < owned.Length; i++)
        {
            int idx = i; // クロージャ用
            BuildRow(contentRoot, owned[i], idx, rowH);
        }
    }

    void BuildRow(Transform parent, CharacterData cd, int rowIndex, float rowH)
    {
        bool isStarter = System.Array.IndexOf(StarterNames, cd.characterName) >= 0;
        int lvl   = OrbManager.GetEnhanceLevel(cd.characterName);
        int count = OrbManager.GetCharCount(cd.characterName);
        Color rarCol = RarityColor(cd.rarity);

        // 行背景
        var rowGo = new GameObject($"Row_{cd.characterName}");
        rowGo.transform.SetParent(parent, false);
        var rowImg = rowGo.AddComponent<Image>();
        rowImg.color = new Color(0.12f, 0.12f, 0.22f, 1f);
        var rowRT = rowGo.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot     = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, -rowIndex * rowH);
        rowRT.sizeDelta = new Vector2(0f, rowH - 4f);

        // 左端レアリティバー
        var barGo = new GameObject("RarBar");
        barGo.transform.SetParent(rowGo.transform, false);
        barGo.AddComponent<Image>().color = rarCol;
        var barRT = barGo.GetComponent<RectTransform>();
        barRT.anchorMin = Vector2.zero; barRT.anchorMax = new Vector2(0f, 1f);
        barRT.pivot = new Vector2(0f, 0.5f);
        barRT.anchoredPosition = Vector2.zero;
        barRT.sizeDelta = new Vector2(8f, 0f);

        // キャラ名
        var nameT = MakeText(rowGo.transform,
            $"{cd.characterName}  [{cd.rarity}]", 28, rarCol,
            new Vector2(0.25f, 0.7f), new Vector2(400f, 40f));
        nameT.alignment = TextAnchor.MiddleLeft;

        // 枚数・レベル
        MakeText(rowGo.transform,
            $"x{count}  Lv.{lvl}/10", 24, new Color(0.75f, 0.75f, 0.75f),
            new Vector2(0.25f, 0.25f), new Vector2(300f, 34f)).alignment = TextAnchor.MiddleLeft;

        // 強化ボタン
        bool canEnhance = count >= 2 && lvl < 10;
        var enhBtn = MakeButton(rowGo.transform, "強化",
            canEnhance ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.2f, 0.2f, 0.2f),
            new Vector2(0.72f, 0.5f), new Vector2(150f, 74f),
            () =>
            {
                OrbManager.TryEnhance(cd.characterName);
                RefreshList();
            });
        enhBtn.interactable = canEnhance;

        // 削除ボタン（スターターは非表示）
        if (!isStarter)
        {
            var delBtn = MakeButton(rowGo.transform, "削除",
                new Color(0.6f, 0.15f, 0.15f),
                new Vector2(0.90f, 0.5f), new Vector2(140f, 74f),
                () =>
                {
                    OrbManager.RemoveOwned(cd.characterName);
                    RefreshList();
                });
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
        t.text = label; t.fontSize = 26; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 26);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return btn;
    }

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
}
