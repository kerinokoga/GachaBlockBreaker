using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// キャラクター選択画面
/// スロット(0-2)をタップして選択 → キャラ一覧からタップして割り当て
/// </summary>
public class CharaSelectUI : MonoBehaviour
{
    private CharacterData[] allChars;           // ロード済みの全キャラ
    private CharacterData[] slotChars = new CharacterData[3]; // 各スロットに割り当てたキャラ
    private int activeSlot = 0;                 // 現在選択中のスロット番号

    // スロットカード UI
    private Image[]  slotBgs    = new Image[3];
    private Text[]   slotNames  = new Text[3];
    private Text[]   slotRarTxts= new Text[3];

    // 詳細パネル
    private Text detailName, detailRarity, detailPassive, detailUlt, detailDesc;

    // キャラ一覧ボタン
    private GameObject[] charListBtns;

    void Start()
    {
        allChars = Resources.LoadAll<CharacterData>("Characters");
        Debug.Log($"[CharaSelectUI] キャラ読込数: {allChars.Length}");
        foreach (var c in allChars)
            Debug.Log($"  - {c.characterName} ({c.rarity})");

        // スロット初期割り当て（キャラ数分だけ埋める）
        for (int i = 0; i < 3; i++)
            slotChars[i] = (i < allChars.Length) ? allChars[i] : null;

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
        MakeText(root, "CHARACTER SELECT", 48, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(900f, 64f));

        // ---- スロット3つ（上部）----
        float[] slotXs = { 0.18f, 0.50f, 0.82f };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var card = MakeRectImage(root, new Color(0.12f, 0.12f, 0.28f),
                new Vector2(slotXs[i], 0.76f), Vector2.zero, new Vector2(280f, 180f));
            slotBgs[i] = card;

            // スロット番号
            MakeText(card.transform, $"Slot {i + 1}", 22, new Color(0.6f, 0.6f, 0.8f),
                new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(240f, 32f));

            // キャラ名
            slotNames[i] = MakeText(card.transform, "---", 30, Color.white,
                new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(240f, 40f));

            // レアリティ
            slotRarTxts[i] = MakeText(card.transform, "", 22, Color.gray,
                new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(240f, 32f));

            // タップで選択
            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => SetActiveSlot(idx));
        }

        // ---- 詳細パネル（中段）----
        var dp = MakeRectImage(root, new Color(0.08f, 0.08f, 0.22f, 0.95f),
            new Vector2(0.5f, 0.535f), Vector2.zero, new Vector2(960f, 240f));

        detailName   = MakeText(dp.transform, "", 36, Color.white,
            new Vector2(0.5f, 0.83f), Vector2.zero, new Vector2(900f, 48f));
        detailRarity = MakeText(dp.transform, "", 26, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(900f, 36f));
        detailPassive= MakeText(dp.transform, "", 24, new Color(0.5f, 1f, 0.7f),
            new Vector2(0.5f, 0.47f), Vector2.zero, new Vector2(900f, 34f));
        detailUlt    = MakeText(dp.transform, "", 24, new Color(1f, 0.7f, 0.3f),
            new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(900f, 34f));
        detailDesc   = MakeText(dp.transform, "", 20, new Color(0.75f, 0.75f, 0.75f),
            new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(900f, 30f));

        // ---- キャラ一覧（中下段）----
        var listLabel = MakeText(root, "-- キャラを選んでスロットに割り当て --", 22,
            new Color(0.7f, 0.7f, 0.9f),
            new Vector2(0.5f, 0.385f), Vector2.zero, new Vector2(900f, 32f));

        charListBtns = new GameObject[allChars.Length];
        float[] listXs = { 0.20f, 0.50f, 0.80f };
        for (int i = 0; i < allChars.Length; i++)
        {
            int idx = i;
            var cd = allChars[i];
            Color rc = GetRarityColor(cd.rarity);
            float xPos = (allChars.Length == 1) ? 0.5f :
                         (allChars.Length == 2) ? (i == 0 ? 0.30f : 0.70f) :
                         listXs[i];

            var btn = MakeButton(root, cd.characterName, 28, new Color(rc.r * 0.5f, rc.g * 0.5f, rc.b * 0.5f),
                new Vector2(xPos, 0.29f), Vector2.zero, new Vector2(280f, 110f));

            // レアリティ帯
            var rarTxt = MakeText(btn.transform, cd.rarity.ToString(), 20, rc,
                new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(240f, 30f));

            btn.GetComponent<Button>().onClick.AddListener(() => AssignCharToActiveSlot(idx));
            charListBtns[i] = btn;
        }

        // ---- STARTボタン ----
        MakeButton(root, "START", 44, new Color(0.15f, 0.55f, 1f),
            new Vector2(0.5f, 0.09f), Vector2.zero, new Vector2(480f, 100f))
            .GetComponent<Button>().onClick.AddListener(OnStartClicked);
    }

    // スロットをアクティブにする
    void SetActiveSlot(int slot)
    {
        activeSlot = slot;
        RefreshAllSlots();
        RefreshDetail();
    }

    // 選択中スロットにキャラを割り当てる
    void AssignCharToActiveSlot(int charIdx)
    {
        slotChars[activeSlot] = allChars[charIdx];
        RefreshAllSlots();
        RefreshDetail();
    }

    void RefreshAllSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            var cd = slotChars[i];
            slotNames[i].text   = (cd != null) ? cd.characterName : "---";
            slotRarTxts[i].text = (cd != null) ? cd.rarity.ToString() : "";
            if (slotRarTxts[i] != null && cd != null)
                slotRarTxts[i].color = GetRarityColor(cd.rarity);

            // アクティブスロットをハイライト
            if (slotBgs[i] != null)
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
            ResultData.SelectedCharacterNames[i] = (slotChars[i] != null)
                ? slotChars[i].characterName : "";
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

    Text MakeText(Transform parent, string txt, int size, Color col,
        Vector2 anchor, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    GameObject MakeButton(Transform parent, string label, int fontSize, Color bgCol,
        Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgCol;
        go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
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
}
