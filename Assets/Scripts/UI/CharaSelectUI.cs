using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// キャラクター選択画面
/// スロット(0-2)をタップして選択 → 所持キャラ一覧からタップして割り当て
/// スクロール対応（最大50体）
/// </summary>
public class CharaSelectUI : MonoBehaviour
{
    static readonly string[] StarterNames = { "Luna", "Aria", "Sera" };

    CharacterData[] allChars;    // Resources から全件ロード
    CharacterData[] ownedChars;  // 所持済みのみ
    CharacterData[] slotChars = new CharacterData[3];
    int activeSlot = 0;

    Image[] slotBgs     = new Image[3];
    Text[]  slotNames   = new Text[3];
    Text[]  slotRarTxts = new Text[3];

    Text detailName, detailRarity, detailPassive, detailUlt, detailDesc;

    void Start()
    {
        // スターターキャラを確実に所持済みにする
        foreach (var name in StarterNames)
            if (!OrbManager.IsOwned(name)) OrbManager.SetOwned(name);

        allChars = Resources.LoadAll<CharacterData>("Characters");

        // 所持済みキャラのみ抽出
        var owned = new System.Collections.Generic.List<CharacterData>();
        foreach (var c in allChars)
            if (OrbManager.IsOwned(c.characterName)) owned.Add(c);
        ownedChars = owned.ToArray();

        // スロット初期割り当て（所持キャラ先頭3体）
        for (int i = 0; i < 3; i++)
            slotChars[i] = (i < ownedChars.Length) ? ownedChars[i] : null;

        BuildUI();
        SetActiveSlot(0);
        RefreshAllSlots();
    }

    void BuildUI()
    {
        var cGo = new GameObject("CharaSelectCanvas");
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
        MakeText(root, "CHARACTER SELECT", 46, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.93f), new Vector2(900f, 60f));

        // ---- スロット3つ ----
        float[] slotXs = { 0.18f, 0.50f, 0.82f };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var card = MakeRectImage(root, new Color(0.12f, 0.12f, 0.28f),
                new Vector2(slotXs[i], 0.77f), new Vector2(280f, 170f));
            slotBgs[i] = card;

            MakeText(card.transform, $"Slot {i + 1}", 20, new Color(0.6f, 0.6f, 0.8f),
                new Vector2(0.5f, 0.86f), new Vector2(240f, 28f));
            slotNames[i] = MakeText(card.transform, "---", 28, Color.white,
                new Vector2(0.5f, 0.55f), new Vector2(240f, 38f));
            slotRarTxts[i] = MakeText(card.transform, "", 20, Color.gray,
                new Vector2(0.5f, 0.22f), new Vector2(240f, 28f));

            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => SetActiveSlot(idx));
        }

        // ---- 詳細パネル ----
        var dp = MakeRectImage(root, new Color(0.08f, 0.08f, 0.22f, 0.95f),
            new Vector2(0.5f, 0.54f), new Vector2(960f, 220f));

        detailName    = MakeText(dp.transform, "", 34, Color.white,
            new Vector2(0.5f, 0.83f), new Vector2(900f, 44f));
        detailRarity  = MakeText(dp.transform, "", 24, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.65f), new Vector2(900f, 34f));
        detailPassive = MakeText(dp.transform, "", 22, new Color(0.5f, 1f, 0.7f),
            new Vector2(0.5f, 0.47f), new Vector2(900f, 32f));
        detailUlt     = MakeText(dp.transform, "", 22, new Color(1f, 0.7f, 0.3f),
            new Vector2(0.5f, 0.30f), new Vector2(900f, 32f));
        detailDesc    = MakeText(dp.transform, "", 18, new Color(0.75f, 0.75f, 0.75f),
            new Vector2(0.5f, 0.12f), new Vector2(900f, 28f));

        // ---- 所持キャラ一覧ラベル ----
        MakeText(root, $"所持キャラ一覧  ({ownedChars.Length}/50)", 22,
            new Color(0.7f, 0.7f, 0.9f),
            new Vector2(0.5f, 0.41f), new Vector2(900f, 30f));

        // ---- スクロール可能なキャラ一覧 ----
        BuildScrollList(root);

        // ---- ボタン行 ----
        // HOME ボタン（左）
        MakeButton(root, "HOME", 36, new Color(0.25f, 0.25f, 0.35f),
            new Vector2(0.25f, 0.04f), new Vector2(340f, 90f))
            .GetComponent<Button>().onClick.AddListener(
                () => SceneManager.LoadScene("HomeScene"));

        // START ボタン（右）
        MakeButton(root, "START", 42, new Color(0.15f, 0.55f, 1f),
            new Vector2(0.75f, 0.04f), new Vector2(460f, 90f))
            .GetComponent<Button>().onClick.AddListener(OnStartClicked);
    }

    void BuildScrollList(Transform root)
    {
        // Scroll 領域の RectTransform
        var scrollGo = new GameObject("ScrollArea");
        scrollGo.transform.SetParent(root, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.02f, 0.10f);
        scrollRT.anchorMax = new Vector2(0.98f, 0.39f);
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;
        scroll.scrollSensitivity = 30f;

        // Viewport（マスク）
        var vpGo = new GameObject("Viewport");
        vpGo.transform.SetParent(scrollGo.transform, false);
        var vpImg = vpGo.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0.01f);
        vpGo.AddComponent<Mask>().showMaskGraphic = false;
        var vpRT = vpGo.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        scroll.viewport = vpRT;

        // Content（スクロールされる中身）
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        const int cols     = 3;
        const float cardW  = 310f;
        const float cardH  = 120f;
        const float padX   = 20f;
        const float padY   = 12f;

        int rows = Mathf.CeilToInt((float)ownedChars.Length / cols);
        float contentH = rows * (cardH + padY) + padY;
        contentRT.sizeDelta = new Vector2(0f, contentH);
        scroll.content = contentRT;

        // 各キャラカードを配置
        float totalWidth = cols * cardW + (cols - 1) * padX;

        for (int i = 0; i < ownedChars.Length; i++)
        {
            int idx = i;
            var cd  = ownedChars[i];
            Color rc  = GetRarityColor(cd.rarity);
            Color bgC = new Color(rc.r * 0.25f, rc.g * 0.25f, rc.b * 0.25f, 1f);

            int col = i % cols;
            int row = i / cols;

            float xOffset = -totalWidth / 2f + col * (cardW + padX) + cardW / 2f;
            float yOffset = -(padY + row * (cardH + padY) + cardH / 2f);

            // カード本体
            var cardGo = new GameObject($"Card_{cd.characterName}");
            cardGo.transform.SetParent(contentGo.transform, false);
            cardGo.AddComponent<Image>().color = bgC;
            var cardRT = cardGo.GetComponent<RectTransform>();
            cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 1f);
            cardRT.pivot      = new Vector2(0.5f, 0.5f);
            cardRT.anchoredPosition = new Vector2(xOffset, yOffset);
            cardRT.sizeDelta  = new Vector2(cardW, cardH);

            // レアリティバー（上端）
            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(cardGo.transform, false);
            barGo.AddComponent<Image>().color = rc;
            var barRT = barGo.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0f, 1f); barRT.anchorMax = new Vector2(1f, 1f);
            barRT.pivot     = new Vector2(0.5f, 1f);
            barRT.anchoredPosition = Vector2.zero;
            barRT.sizeDelta = new Vector2(0f, 6f);

            // キャラ名
            MakeText(cardGo.transform, cd.characterName, 26, Color.white,
                new Vector2(0.5f, 0.65f), new Vector2(cardW - 16f, 34f));
            // レアリティ文字
            MakeText(cardGo.transform, cd.rarity.ToString(), 20, rc,
                new Vector2(0.5f, 0.22f), new Vector2(cardW - 16f, 28f));

            // タップで割り当て
            var btn = cardGo.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() => AssignCharToActiveSlot(idx));
        }
    }

    // ---- ロジック ----

    void SetActiveSlot(int slot)
    {
        activeSlot = slot;
        RefreshAllSlots();
        RefreshDetail();
    }

    void AssignCharToActiveSlot(int charIdx)
    {
        slotChars[activeSlot] = ownedChars[charIdx];
        // 次のスロットへ自動移動（全スロット埋めやすくする）
        if (activeSlot < 2) SetActiveSlot(activeSlot + 1);
        else                { RefreshAllSlots(); RefreshDetail(); }
    }

    void RefreshAllSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            var cd = slotChars[i];
            slotNames[i].text   = (cd != null) ? cd.characterName : "---";
            slotRarTxts[i].text = (cd != null) ? cd.rarity.ToString() : "";
            if (cd != null) slotRarTxts[i].color = GetRarityColor(cd.rarity);
            if (slotBgs[i])
                slotBgs[i].color = (i == activeSlot)
                    ? new Color(0.2f, 0.2f, 0.5f)
                    : new Color(0.12f, 0.12f, 0.28f);
        }
    }

    void RefreshDetail()
    {
        var cd = slotChars[activeSlot];
        if (cd == null)
        {
            detailName.text = "(未選択)";
            detailRarity.text = detailPassive.text = detailUlt.text = detailDesc.text = "";
            return;
        }
        detailName.text    = cd.characterName;
        detailRarity.text  = $"Rarity: {cd.rarity}";
        detailPassive.text = $"[Passive] {PassiveDesc(cd)}";
        detailUlt.text     = $"[Ultimate] {UltDesc(cd)}";
        detailDesc.text    = cd.description;
    }

    void OnStartClicked()
    {
        for (int i = 0; i < 3; i++)
            ResultData.SelectedCharacterNames[i] =
                (slotChars[i] != null) ? slotChars[i].characterName : "";
        SceneManager.LoadScene("GameScene");
    }

    // ---- 説明文 ----

    string PassiveDesc(CharacterData cd)
    {
        switch (cd.passiveType)
        {
            case PassiveEffectType.BallSpeedUp:   return $"ボール速度 x{cd.passiveValue}";
            case PassiveEffectType.ExtraDamage:   return $"ブロックダメージ +{(int)cd.passiveValue}";
            case PassiveEffectType.ExtraStock:    return $"開始ストック +{(int)cd.passiveValue}";
            case PassiveEffectType.UltGaugeBoost: return $"奥義ゲージ x{cd.passiveValue}";
            default: return "なし";
        }
    }

    string UltDesc(CharacterData cd)
    {
        switch (cd.ultimateType)
        {
            case UltimateSkillType.SpeedBurst:   return $"速度 x{cd.ultimateValue} ({cd.ultimateDuration}秒)";
            case UltimateSkillType.MassDestroy:  return $"全ブロックに {(int)cd.ultimateValue} ダメージ";
            case UltimateSkillType.StockRecover: return "ストック +1 回復";
            case UltimateSkillType.BarrierShot:  return "次のミスをキャンセル";
            default: return "なし";
        }
    }

    Color GetRarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.SSR: return new Color(1f, 0.85f, 0.1f);
            case Rarity.SR:  return new Color(0.75f, 0.3f, 1f);
            case Rarity.R:   return new Color(0.3f, 0.5f, 1f);
            default:         return new Color(0.6f, 0.6f, 0.6f);
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

    Image MakeRectImage(Transform parent, Color col, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject("Img");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        return img;
    }

    Text MakeText(Transform parent, string txt, int size, Color col,
        Vector2 anchor, Vector2 sizeDelta)
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

    GameObject MakeButton(Transform parent, string label, int fontSize, Color bgCol,
        Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgCol;
        go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
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
}
