using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// キャラ管理画面：所持キャラ一覧・削除・合成強化
/// スターターキャラ（Luna/Aria/Sera）は削除不可
/// </summary>
public class CharaManageUI : MonoBehaviour
{
    static readonly string[] StarterNames = { "ルナ", "アリア", "セラ" };

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

    // フォントキャッシュ（毎回生成すると Destroy 時に巻き添え破棄される）
    Font cachedFont;
    Font btnFont;

    void Start()
    {
        cachedFont = UIFont.Main;
        btnFont = Resources.Load<Font>("Fonts/CherryBombOne-Regular") ?? cachedFont;
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
        cs.matchWidthOrHeight = 0.0f;
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
            new Vector2(0.5f, 0.95f), new Vector2(700f, 70f));

        // 所持数表示
        countText = MakeText(canvasRoot, "", 30, new Color(0.8f, 0.8f, 1f),
            new Vector2(0.5f, 0.90f), new Vector2(500f, 45f));

        // スクロールビュー
        BuildScrollView(canvasRoot);

        // ほーむボタン
        MakeButton(canvasRoot, "ホーム", new Color(0.25f, 0.25f, 0.35f),
            new Vector2(0.5f, 0.05f), new Vector2(360f, 70f),
            () => SceneManager.LoadScene("HomeScene"));

        // 説明ポップアップ（共通、初期非表示）
        var helpPopup = MakePanel(canvasRoot, new Color(0f, 0f, 0f, 0.92f));
        var helpPopupText = MakeText(helpPopup.transform, "", 30, Color.white,
            new Vector2(0.5f, 0.55f), new Vector2(800f, 600f));
        helpPopupText.alignment = TextAnchor.MiddleCenter;
        MakeButton(helpPopup.transform, "とじる", new Color(0.3f, 0.3f, 0.4f),
            new Vector2(0.5f, 0.25f), new Vector2(220f, 65f),
            () => helpPopup.SetActive(false));
        helpPopup.SetActive(false);

        // 説明ボタン（タイトル下、横4つ均等配置）
        MakeButton(canvasRoot, "強化とは？", new Color(0.2f, 0.5f, 0.2f, 0.9f),
            new Vector2(0.12f, 0.86f), new Vector2(170f, 50f),
            () => {
                helpPopupText.text =
                    "【強化】\n\n" +
                    "同じキャラを素材にして\n" +
                    "キャラを強化できます\n\n" +
                    "強化するとダメージ倍率が\n" +
                    "アップします！\n\n" +
                    "最大Lv5まで強化可能";
                helpPopup.SetActive(true);
            });

        MakeButton(canvasRoot, "覚醒とは？", new Color(0.6f, 0.45f, 0.1f, 0.9f),
            new Vector2(0.37f, 0.86f), new Vector2(170f, 50f),
            () => {
                helpPopupText.text =
                    "【覚醒】\n\n" +
                    "Lv5まで強化した\n" +
                    "キャラを覚醒できます\n\n" +
                    "覚醒するとステータスUP\n" +
                    "(ダメージ倍率 +0.5x)\n\n" +
                    "美少女コレクションに\n" +
                    "スペシャルイラストが\n" +
                    "解放されます\n\n" +
                    "覚醒済みキャラのアイコンに\n" +
                    "金の枠が付きます";
                helpPopup.SetActive(true);
            });

        MakeButton(canvasRoot, "変換とは？", new Color(0.2f, 0.3f, 0.6f, 0.9f),
            new Vector2(0.63f, 0.86f), new Vector2(170f, 50f),
            () => {
                helpPopupText.text =
                    "【変換】\n\n" +
                    "Lv5のキャラで\n" +
                    "所持数が2以上の場合\n" +
                    "余ったキャラをオーブに\n" +
                    "変換できます\n\n" +
                    "N=5  R=10\n" +
                    "SR=25  SSR=50 オーブ";
                helpPopup.SetActive(true);
            });

        MakeButton(canvasRoot, "削除とは？", new Color(0.6f, 0.2f, 0.2f, 0.9f),
            new Vector2(0.88f, 0.86f), new Vector2(170f, 50f),
            () => {
                helpPopupText.text =
                    "【削除】\n\n" +
                    "不要なキャラを\n" +
                    "削除できます\n\n" +
                    "※初期キャラ\n" +
                    "（ルナ・アリア・セラ）は\n" +
                    "削除できません";
                helpPopup.SetActive(true);
            });

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
        System.Array.Sort(owned, (a, b) => RarityOrder(a.rarity).CompareTo(RarityOrder(b.rarity)));

        // 所持済みだが Count=0 のキャラを補正（スターターキャラ等）
        foreach (var c in owned)
            if (OrbManager.GetCharCount(c.characterName) == 0)
                OrbManager.AddCharCount(c.characterName);

        int count = owned.Length;

        if (countText) countText.text = $"所持: {count} / {OrbManager.MaxOwnedCharacters}";

        // Content の高さを行数に合わせる
        const float rowH = 170f;
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

        // アイコン表示
        bool isAwakened = OrbManager.IsAwakened(cd.characterName);
        float nameAnchorX = 0.25f;
        if (cd.icon != null)
        {
            // 覚醒済みなら金枠
            if (isAwakened)
            {
                var goldFrame = new GameObject("GoldFrame");
                goldFrame.transform.SetParent(rowGo.transform, false);
                goldFrame.AddComponent<Image>().color = new Color(1f, 0.85f, 0.1f, 0.9f);
                var gfRt = goldFrame.GetComponent<RectTransform>();
                gfRt.anchorMin = gfRt.anchorMax = new Vector2(0.10f, 0.5f);
                gfRt.anchoredPosition = Vector2.zero;
                gfRt.sizeDelta = new Vector2(140f, 140f);
            }

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(rowGo.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = cd.icon;
            iconImg.preserveAspect = true;
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0.10f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(130f, 130f);
            nameAnchorX = 0.38f;
        }

        // キャラ名
        var nameT = MakeText(rowGo.transform,
            $"{cd.characterName}  [{cd.rarity}]", 36, rarCol,
            new Vector2(nameAnchorX, 0.7f), new Vector2(450f, 50f));
        nameT.alignment = TextAnchor.MiddleLeft;

        // 枚数・レベル・覚醒状態
        string lvlStr = isAwakened
            ? $"x{count}  Lv.{lvl}/5  ✦覚醒済"
            : $"x{count}  Lv.{lvl}/5";
        Color lvlCol = isAwakened ? new Color(1f, 0.85f, 0.1f) : new Color(0.75f, 0.75f, 0.75f);
        MakeText(rowGo.transform, lvlStr, 30, lvlCol,
            new Vector2(nameAnchorX, 0.25f), new Vector2(450f, 42f)).alignment = TextAnchor.MiddleLeft;

        // 行タップで詳細ダイアログ表示
        var rowBtn = rowGo.AddComponent<Button>();
        rowBtn.transition = Selectable.Transition.None;
        var cdRef = cd; // クロージャ用
        rowBtn.onClick.AddListener(() => ShowCharacterDetail(cdRef));

        // 強化 or 覚醒ボタン
        bool canEnhance = count >= 2 && lvl < 5;

        // 覚醒ボタン（Lv5かつ未覚醒時のみ有効）
        bool canAwaken = lvl >= 5 && !isAwakened;
        if (canAwaken)
        {
            var awBtn = MakeButton(rowGo.transform, "覚醒",
                new Color(0.7f, 0.55f, 0.1f),
                new Vector2(0.62f, 0.5f), new Vector2(120f, 74f),
                () => ShowAwakenConfirm(cd));
        }
        else if (!isAwakened)
        {
            // 強化ボタン（Lv未満 かつ 2枚以上）- 覚醒前のみ表示
            var enhBtn2 = MakeButton(rowGo.transform, "強化",
                canEnhance ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.2f, 0.2f, 0.2f),
                new Vector2(0.62f, 0.5f), new Vector2(120f, 74f),
                () =>
                {
                    OrbManager.TryEnhance(cd.characterName);
                    RefreshList();
                });
            enhBtn2.interactable = canEnhance;
        }

        // 変換ボタン（Lv5済 かつ 2枚以上 → 余りをオーブに変換）
        bool canConvert = count >= 2 && lvl >= 5;
        var convBtn = MakeButton(rowGo.transform, "変換",
            canConvert ? new Color(0.15f, 0.45f, 0.7f) : new Color(0.2f, 0.2f, 0.2f),
            new Vector2(0.76f, 0.5f), new Vector2(120f, 74f),
            () => ShowConvertConfirm(cd));
        convBtn.interactable = canConvert;

        // 削除ボタン（スターターは非表示）
        if (!isStarter)
        {
            var delBtn = MakeButton(rowGo.transform, "削除",
                new Color(0.6f, 0.15f, 0.15f),
                new Vector2(0.90f, 0.5f), new Vector2(120f, 74f),
                () => ShowDeleteConfirm(cd.characterName));
        }
    }

    // ---- 覚醒確認ダイアログ ----

    void ShowAwakenConfirm(CharacterData cd)
    {
        var overlay = new GameObject("AwakenOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var dialog = new GameObject("AwakenDialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.15f, 0.12f, 0.05f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(750f, 450f);

        // 金枠装飾
        var goldBorder = new GameObject("GoldBorder");
        goldBorder.transform.SetParent(dialog.transform, false);
        goldBorder.AddComponent<Image>().color = new Color(1f, 0.85f, 0.1f, 0.6f);
        var gbRt = goldBorder.GetComponent<RectTransform>();
        gbRt.anchorMin = Vector2.zero; gbRt.anchorMax = Vector2.one;
        gbRt.offsetMin = new Vector2(-4f, -4f); gbRt.offsetMax = new Vector2(4f, 4f);
        goldBorder.transform.SetAsFirstSibling();

        Transform dp = dialog.transform;

        MakeText(dp, "✦ 覚醒 ✦", 42, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f, 0.82f), new Vector2(600f, 55f));

        MakeText(dp,
            $"{cd.characterName} を覚醒しますか？\n\nステータスがアップし\n美少女コレクションに\n覚醒イラストが解放されます",
            28, Color.white,
            new Vector2(0.5f, 0.52f), new Vector2(680f, 200f));

        // 覚醒するボタン
        MakeButton(dp, "覚醒する",
            new Color(0.7f, 0.55f, 0.1f),
            new Vector2(0.3f, 0.12f), new Vector2(240f, 80f),
            () =>
            {
                OrbManager.TryAwaken(cd.characterName);
                Destroy(overlay);
                RefreshList();
            });

        // やめるボタン
        MakeButton(dp, "やめる",
            new Color(0.3f, 0.3f, 0.4f),
            new Vector2(0.7f, 0.12f), new Vector2(240f, 80f),
            () => Destroy(overlay));
    }

    // ---- 削除確認ダイアログ ----

    void ShowDeleteConfirm(string charName)
    {
        // オーバーレイ（半透明黒背景）
        var overlay = new GameObject("DeleteConfirmOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ背景
        var dialogGo = new GameObject("Dialog");
        dialogGo.transform.SetParent(overlay.transform, false);
        dialogGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.25f);
        var drt = dialogGo.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(700f, 300f);

        // メッセージ
        MakeText(dialogGo.transform,
            $"本当に {charName} を\n削除しますか？", 32, Color.white,
            new Vector2(0.5f, 0.65f), new Vector2(600f, 100f));

        // はいボタン
        MakeButton(dialogGo.transform, "はい",
            new Color(0.6f, 0.15f, 0.15f),
            new Vector2(0.3f, 0.22f), new Vector2(200f, 80f),
            () =>
            {
                OrbManager.RemoveOwned(charName);
                Destroy(overlay);
                RefreshList();
            });

        // いいえボタン
        MakeButton(dialogGo.transform, "いいえ",
            new Color(0.3f, 0.3f, 0.4f),
            new Vector2(0.7f, 0.22f), new Vector2(200f, 80f),
            () => Destroy(overlay));
    }

    // ---- オーブ変換ダイアログ ----

    static int OrbPerRarity(Rarity r)
    {
        switch (r)
        {
            case Rarity.SSR: return 50;
            case Rarity.SR:  return 25;
            case Rarity.R:   return 10;
            default:         return 5;
        }
    }

    void ShowConvertConfirm(CharacterData cd)
    {
        int count = OrbManager.GetCharCount(cd.characterName);
        int excess = count - 1; // 1枚残す
        if (excess <= 0) return;
        int orbPerCard = OrbPerRarity(cd.rarity);
        int totalOrb = excess * orbPerCard;

        // オーバーレイ
        var overlay = new GameObject("ConvertOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ背景
        var dialogGo = new GameObject("ConvertDialog");
        dialogGo.transform.SetParent(overlay.transform, false);
        dialogGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.22f);
        var drt = dialogGo.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(750f, 400f);

        Transform dp = dialogGo.transform;

        // メッセージ
        MakeText(dp,
            $"{cd.characterName}{excess}体を\n{totalOrb}オーブに変換しますか？",
            32, Color.white,
            new Vector2(0.5f, 0.6f), new Vector2(650f, 120f));

        // はいボタン
        MakeButton(dp, "はい",
            new Color(0.15f, 0.45f, 0.7f),
            new Vector2(0.3f, 0.2f), new Vector2(200f, 80f),
            () =>
            {
                OrbManager.SetCharCount(cd.characterName, 1);
                OrbManager.AddOrbs(totalOrb);
                Destroy(overlay);
                RefreshList();
            });

        // いいえボタン
        MakeButton(dp, "いいえ",
            new Color(0.3f, 0.3f, 0.4f),
            new Vector2(0.7f, 0.2f), new Vector2(200f, 80f),
            () => Destroy(overlay));
    }

    // ---- キャラ詳細ダイアログ ----

    void ShowCharacterDetail(CharacterData cd)
    {
        int lvl = OrbManager.GetEnhanceLevel(cd.characterName);
        bool awakened = OrbManager.IsAwakened(cd.characterName);
        Color rarCol = RarityColor(cd.rarity);

        // オーバーレイ
        var overlay = new GameObject("DetailOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ背景
        var dialog = new GameObject("DetailDialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.2f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(850f, 1100f);

        Transform dp = dialog.transform;

        // アイコン（大きく表示）+ 覚醒金枠
        if (cd.icon != null)
        {
            if (awakened)
            {
                var gf = new GameObject("GoldFrame");
                gf.transform.SetParent(dp, false);
                gf.AddComponent<Image>().color = new Color(1f, 0.85f, 0.1f, 0.9f);
                var gfRt = gf.GetComponent<RectTransform>();
                gfRt.anchorMin = gfRt.anchorMax = new Vector2(0.5f, 0.88f);
                gfRt.anchoredPosition = Vector2.zero;
                gfRt.sizeDelta = new Vector2(216f, 216f);
            }

            var iconGo = new GameObject("DetailIcon");
            iconGo.transform.SetParent(dp, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = cd.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.88f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(200f, 200f);
        }

        // ダイアログ幅 850px。テキスト幅は最大 780px に統一して左右に 35px の余白
        // overflow は Wrap で枠内に収め、長文は自動改行

        // 名前（二つ名付き。長い場合は自動縮小で1行維持）
        var nameTxt = MakeText(dp, cd.DisplayName, 48, rarCol,
            new Vector2(0.5f, 0.73f), new Vector2(780f, 70f));
        nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameTxt.verticalOverflow = VerticalWrapMode.Truncate;
        nameTxt.resizeTextForBestFit = true;
        nameTxt.resizeTextMinSize = 32;
        nameTxt.resizeTextMaxSize = 48;
        AddOutline(nameTxt.gameObject);

        // レアリティバッジ
        string rarStr = cd.rarity.ToString();
        string stars = cd.rarity == Rarity.SSR ? "★★★" :
                       cd.rarity == Rarity.SR  ? "★★" :
                       cd.rarity == Rarity.R   ? "★" : "";
        var rarTxt = MakeText(dp, $"[{rarStr}] {stars}", 36, rarCol,
            new Vector2(0.5f, 0.67f), new Vector2(780f, 48f));
        rarTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        rarTxt.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutline(rarTxt.gameObject);

        // ── ステータス区切り線 ──
        MakeLine(dp, new Vector2(0.5f, 0.625f), 780f, rarCol);

        // 強化レベル + 覚醒状態
        float charPower = 1.0f + 0.2f * lvl + (awakened ? OrbManager.AwakenBonusMultiplier : 0f);
        string awakenStr = awakened ? "  ✦覚醒済" : "";
        var lvlTxt = MakeText(dp, $"強化レベル: Lv.{lvl}/5{awakenStr}", 30,
            awakened ? new Color(1f, 0.85f, 0.1f) : Color.white,
            new Vector2(0.5f, 0.585f), new Vector2(780f, 44f));
        lvlTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        lvlTxt.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutline(lvlTxt.gameObject);

        // ダメージ（このキャラ単体の基礎値のみ表示）
        // ※パッシブは戦闘時に「編成3体のダメージ合計」へ適用されるため、
        //   自分の値にだけ掛けた擬似合計を出すと実際の効果と食い違う
        var pwTxt = MakeText(dp,
            $"ダメージ: {charPower:F1}\n※パッシブスキルは編成3体の合計ダメージに適用",
            24, new Color(1f, 0.7f, 0.3f),
            new Vector2(0.5f, 0.525f), new Vector2(780f, 70f));
        pwTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        pwTxt.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutline(pwTxt.gameObject);

        // ── パッシブ区切り線 ──
        MakeLine(dp, new Vector2(0.5f, 0.465f), 780f, new Color(0.4f, 0.4f, 0.6f));

        // パッシブスキル見出し
        var psHead = MakeText(dp, "◆ パッシブスキル", 32, new Color(0.4f, 0.95f, 1f),
            new Vector2(0.5f, 0.425f), new Vector2(780f, 44f));
        psHead.horizontalOverflow = HorizontalWrapMode.Wrap;
        psHead.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutline(psHead.gameObject);

        // パッシブスキル本文（複合は2行）
        string passiveDesc = GetPassiveDescription(cd.passiveType, cd.passiveValue);
        if (cd.passiveType2 != PassiveEffectType.None)
            passiveDesc += $"\n{GetPassiveDescription(cd.passiveType2, cd.passiveValue2)}";
        var psBody = MakeText(dp, passiveDesc, 26, Color.white,
            new Vector2(0.5f, 0.365f), new Vector2(780f, 80f));
        psBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        psBody.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutline(psBody.gameObject);

        // ── 奥義区切り線 ──
        MakeLine(dp, new Vector2(0.5f, 0.305f), 780f, new Color(0.4f, 0.4f, 0.6f));

        // 奥義見出し
        var ultHead = MakeText(dp, "✦ 奥義", 32, new Color(1f, 0.85f, 0.3f),
            new Vector2(0.5f, 0.265f), new Vector2(780f, 44f));
        ultHead.horizontalOverflow = HorizontalWrapMode.Wrap;
        ultHead.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutline(ultHead.gameObject);

        // 奥義本文（BallSplit など長文があるので2行想定。複合奥義は2行で表示）
        string ultDesc = GetUltimateDescription(cd.ultimateType, cd.ultimateValue, cd.ultimateDuration);
        if (cd.ultimateType2 != UltimateSkillType.None)
            ultDesc += $"\n＋ {GetUltimateDescription(cd.ultimateType2, cd.ultimateValue2, cd.ultimateDuration2)}";
        var ultText = MakeText(dp, ultDesc, 26, Color.white,
            new Vector2(0.5f, 0.205f), new Vector2(780f, 80f));
        ultText.horizontalOverflow = HorizontalWrapMode.Wrap;
        ultText.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutline(ultText.gameObject);

        // 説明文
        if (!string.IsNullOrEmpty(cd.description))
        {
            MakeLine(dp, new Vector2(0.5f, 0.145f), 780f, new Color(0.4f, 0.4f, 0.6f));
            var descHead = MakeText(dp, "▼ 説明", 28, new Color(0.85f, 0.85f, 1f),
                new Vector2(0.5f, 0.108f), new Vector2(780f, 40f));
            descHead.horizontalOverflow = HorizontalWrapMode.Wrap;
            descHead.verticalOverflow = VerticalWrapMode.Truncate;
            AddOutline(descHead.gameObject);
            var descText = MakeText(dp, cd.description, 22, new Color(0.85f, 0.85f, 0.9f),
                new Vector2(0.5f, 0.05f), new Vector2(780f, 80f));
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.verticalOverflow = VerticalWrapMode.Truncate;
            AddOutline(descText.gameObject);
        }

        // 閉じるボタン（ダイアログ外側・下方に配置して説明文との被りを回避）
        MakeButton(overlay.transform, "閉じる", new Color(0.3f, 0.3f, 0.45f),
            new Vector2(0.5f, 0.17f), new Vector2(320f, 90f),
            () => Destroy(overlay));
    }

    void MakeLine(Transform parent, Vector2 anchor, float width, Color col)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(col.r, col.g, col.b, 0.5f);
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, 2f);
    }

    string GetPassiveDescription(PassiveEffectType type, float value)
    {
        switch (type)
        {
            case PassiveEffectType.BallDamageUp:
                return $"ダメージ +{(value - 1f) * 100f:0}%";
            case PassiveEffectType.ExtraDamage:
                return $"追加ダメージ +{(int)value}";
            case PassiveEffectType.ExtraStock:
                return $"開始時ストック +{(int)value}";
            case PassiveEffectType.UltGaugeBoost:
                return $"奥義ゲージ増加量 +{(value - 1f) * 100f:0}%";
            case PassiveEffectType.CriticalRangeUp:
                return $"クリティカル範囲 +{(int)value}%";
            default:
                return "なし";
        }
    }

    string GetUltimateDescription(UltimateSkillType type, float value, float duration)
    {
        switch (type)
        {
            case UltimateSkillType.PowerBurst:
                return $"{duration:F0}秒間、ダメージ +{(value - 1f) * 100f:0}%";
            case UltimateSkillType.MassDestroy:
                return $"全ブロックに {(int)value} ダメージ";
            case UltimateSkillType.StockRecover:
                return $"ストック回復 +{(int)value}";
            case UltimateSkillType.BarrierShot:
                return "次の1ミスをキャンセル";
            case UltimateSkillType.Penetrate:
                return $"{duration:F0}秒間、ボールがブロックを貫通";
            case UltimateSkillType.BallSplit:
                return "ボールを2つに分裂（分裂したボールも再分裂可能）";
            case UltimateSkillType.GaugeCharge:
                return $"味方全員の奥義ゲージ +{(int)value}";
            default:
                return "なし";
        }
    }

    // ---- ファクトリーメソッド ----

    GameObject MakePanel(Transform parent, Color bgCol)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgCol;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

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
        t.font = cachedFont;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    /// <summary>テキストに黒アウトラインを追加（視認性向上）</summary>
    void AddOutline(GameObject go)
    {
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
        ol.effectDistance = new Vector2(2f, -2f);
    }

    Button MakeButton(Transform parent, string label, Color bgCol,
        Vector2 anchor, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        var btnImg = go.AddComponent<Image>(); btnImg.color = bgCol; UISprites.Button(btnImg);
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
        t.font = btnFont;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return btn;
    }

    static int RarityOrder(Rarity r)
    {
        switch (r)
        {
            case Rarity.SSR: return 0;
            case Rarity.SR:  return 1;
            case Rarity.R:   return 2;
            default:         return 3;
        }
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
