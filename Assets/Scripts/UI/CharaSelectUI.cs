using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// キャラクター選択画面のランタイムUI
/// Resources/Characters/ にある CharacterData を全ロードして表示する
/// </summary>
public class CharaSelectUI : MonoBehaviour
{
    private CharacterData[] allChars;
    private int selectedSlot = 0;     // 現在詳細を表示しているスロット

    // UI参照
    private Text detailName;
    private Text detailRarity;
    private Text detailPassive;
    private Text detailUlt;
    private Text detailDesc;
    private Image detailBg;
    private GameObject[] slotGos = new GameObject[3];
    private Image[] slotBgs       = new Image[3];

    void Start()
    {
        allChars = Resources.LoadAll<CharacterData>("Characters");

        // CharaSelectScene に最低3体分の名前を ResultData に設定（未設定ならデフォルト）
        for (int i = 0; i < 3; i++)
        {
            if (i < ResultData.SelectedCharacterNames.Length &&
                !string.IsNullOrEmpty(ResultData.SelectedCharacterNames[i]))
                continue;
            if (i < allChars.Length)
                ResultData.SelectedCharacterNames[i] = allChars[i].characterName;
        }

        BuildUI();
        RefreshDetail(0);
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
        MakeImage(root, new Color(0.05f, 0.05f, 0.18f), Vector2.zero, Vector2.one);

        // タイトル
        MakeText(root, "CHARACTER SELECT", 48, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(900f, 70f));

        // キャラスロット3つ（上部）
        Color[] rarityColors = { new Color(0.9f, 0.8f, 0.2f), new Color(0.6f, 0.4f, 1f), new Color(0.3f, 0.8f, 0.5f) };
        float[] slotX = { 0.18f, 0.50f, 0.82f };

        for (int i = 0; i < 3 && i < allChars.Length; i++)
        {
            int idx = i;
            var cd = allChars[i];
            Color slotCol = GetRarityColor(cd.rarity);

            // スロット背景
            var slotBg = MakeImage(root, new Color(0.12f, 0.12f, 0.25f),
                new Vector2(slotX[i], 0.62f), new Vector2(slotX[i], 0.62f),
                Vector2.zero, new Vector2(260f, 340f));
            slotBgs[i] = slotBg;
            slotGos[i] = slotBg.gameObject;

            // レアリティ枠色
            var border = MakeImage(slotBg.transform, slotCol,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, -4f), new Vector2(0f, 8f));
            border.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0f);

            // キャラカラーパネル（立ち絵の代わり）
            MakeImage(slotBg.transform, new Color(slotCol.r * 0.6f, slotCol.g * 0.6f, slotCol.b * 0.6f),
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.9f));

            // キャラ名
            MakeText(slotBg.transform, cd.characterName, 32, Color.white,
                new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(220f, 44f));

            // レアリティ表示
            MakeText(slotBg.transform, cd.rarity.ToString(), 26, slotCol,
                new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(220f, 36f));

            // タップで選択
            var btn = slotBg.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => {
                selectedSlot = idx;
                RefreshDetail(idx);
            });
        }

        // 詳細パネル（中段）
        var detailPanel = MakeImage(root, new Color(0.1f, 0.1f, 0.2f, 0.9f),
            new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f),
            Vector2.zero, new Vector2(920f, 380f));

        detailBg = detailPanel;

        detailName = MakeText(detailPanel.transform, "", 40, Color.white,
            new Vector2(0.5f, 0.87f), Vector2.zero, new Vector2(800f, 54f));

        detailRarity = MakeText(detailPanel.transform, "", 28, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.73f), Vector2.zero, new Vector2(800f, 40f));

        detailPassive = MakeText(detailPanel.transform, "", 26, new Color(0.6f, 1f, 0.8f),
            new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(860f, 60f));

        detailUlt = MakeText(detailPanel.transform, "", 26, new Color(1f, 0.7f, 0.4f),
            new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(860f, 60f));

        detailDesc = MakeText(detailPanel.transform, "", 22, new Color(0.8f, 0.8f, 0.8f),
            new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(860f, 60f));

        // STARTボタン（下部）
        MakeButton(root, "START", 44, new Color(0.2f, 0.6f, 1f),
            new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(480f, 100f))
            .GetComponent<Button>().onClick.AddListener(OnStartClicked);
    }

    void RefreshDetail(int slot)
    {
        if (slot >= allChars.Length) return;
        var cd = allChars[slot];

        detailName.text   = cd.characterName;
        detailRarity.text = $"Rarity: {cd.rarity}";
        detailPassive.text = $"[Passive] {PassiveDesc(cd)}";
        detailUlt.text    = $"[Ultimate] {UltDesc(cd)}";
        detailDesc.text   = cd.description;

        // 選択スロットのハイライト
        for (int i = 0; i < 3; i++)
            if (slotBgs[i] != null)
                slotBgs[i].color = (i == slot)
                    ? new Color(0.2f, 0.2f, 0.45f)
                    : new Color(0.12f, 0.12f, 0.25f);
    }

    string PassiveDesc(CharacterData cd)
    {
        switch (cd.passiveType)
        {
            case PassiveEffectType.BallSpeedUp:  return $"ボール速度 x{cd.passiveValue}";
            case PassiveEffectType.ExtraDamage:  return $"ブロックダメージ +{(int)cd.passiveValue}";
            case PassiveEffectType.ExtraStock:   return $"開始ストック +{(int)cd.passiveValue}";
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

    void OnStartClicked()
    {
        // 選択中キャラ名を ResultData に書き込んで GameScene へ
        for (int i = 0; i < 3 && i < allChars.Length; i++)
            ResultData.SelectedCharacterNames[i] = allChars[i].characterName;
        SceneManager.LoadScene("GameScene");
    }

    // ---- ファクトリーメソッド ----

    Image MakeImage(Transform parent, Color col, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pos = default, Vector2 size = default)
    {
        var go = new GameObject("Img");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
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
