using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// キャラクター選択画面（装飾版）
/// スロット(0-2)をタップして選択 → 所持キャラ一覧からタップして割り当て
/// スクロール対応（最大50体）
/// </summary>
public class CharaSelectUI : MonoBehaviour
{
    static readonly string[] StarterNames = { "ルナ", "アリア", "セラ" };

    // 前回選択キャラ保存用 PlayerPrefs キー
    const string KeyLastSlot0 = "GachaBlock_LastSlot0";
    const string KeyLastSlot1 = "GachaBlock_LastSlot1";
    const string KeyLastSlot2 = "GachaBlock_LastSlot2";

    CharacterData[] allChars;    // Resources から全件ロード
    CharacterData[] ownedChars;  // 所持済みのみ
    CharacterData[] slotChars = new CharacterData[3];
    int activeSlot = 0;

    Image[] slotBgs     = new Image[3];
    Image[] slotFrames  = new Image[3];
    Text[]  slotNames   = new Text[3];
    Text[]  slotRarTxts = new Text[3];
    Image[] slotIcons   = new Image[3];
    Image[] slotIconBgs = new Image[3];
    Text staminaText;
    Text totalDamageText;
    Transform canvasRoot;
    ScrollRect charScrollRect;  // キャラ一覧のスクロール（ドラッグハンドラに渡す）
    Button backBtn;             // 「もどる」ボタン（チュートリアル中に無効化するため参照保持）

    Text detailName, detailRarity, detailPassive, detailUlt, detailDesc;

    List<RectTransform> particles = new List<RectTransform>();

    void Start()
    {
        // スターターキャラを確実に所持済みにする
        foreach (var name in StarterNames)
            if (!OrbManager.IsOwned(name)) OrbManager.SetOwned(name);

        allChars = Resources.LoadAll<CharacterData>("Characters");

        // 前回選択キャラ名を取得
        string[] lastNames = {
            PlayerPrefs.GetString(KeyLastSlot0, ""),
            PlayerPrefs.GetString(KeyLastSlot1, ""),
            PlayerPrefs.GetString(KeyLastSlot2, "")
        };

        // 所持済みキャラのみ抽出し、レア度順（SSR→SR→R→N）でソート
        var ownedList = new List<CharacterData>();
        foreach (var c in allChars)
        {
            if (!OrbManager.IsOwned(c.characterName))  continue;
            ownedList.Add(c);
        }
        ownedList.Sort((a, b) => RarityOrder(a.rarity).CompareTo(RarityOrder(b.rarity)));
        ownedChars = ownedList.ToArray();

        // スロット初期割り当て
        //   - 前回選択キャラ名が PlayerPrefs に保存されていればそれを復元
        //   - なければ空のまま（初回起動時は全スロット空。ドラッグ＆ドロップで自分で編成）
        for (int i = 0; i < 3; i++)
        {
            slotChars[i] = null;
            if (!string.IsNullOrEmpty(lastNames[i]))
            {
                foreach (var c in ownedChars)
                    if (c.characterName == lastNames[i]) { slotChars[i] = c; break; }
            }
        }

        BuildUI();
        SetActiveSlot(0);
        RefreshAllSlots();

        // チュートリアル進捗：StageSelect 段階で到達したら CharaSelect 段階へ
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.StageSelect)
        {
            TutorialManager.Instance.SetStep(TutorialManager.Step.CharaSelect);
            Debug.Log("[Tutorial] CharaSelect 段階へ進行");
            // 「もどる」ボタンを無効化（チュートリアル中の離脱を防止）
            SetBackButtonInteractable(false);
            StartCoroutine(ShowCharaSelectGuideAfterDelay(0.6f));
        }
    }

    /// <summary>
    /// 「もどる」ボタンの有効/無効を切替。
    /// 無効時は半透明にして "押せない" 感を出す。
    /// </summary>
    void SetBackButtonInteractable(bool enabled)
    {
        if (backBtn == null) return;
        backBtn.interactable = enabled;

        // 視覚的フィードバック：無効時は alpha 0.3 で薄く
        var outerImg = backBtn.GetComponent<Image>();
        if (outerImg != null)
        {
            var c = outerImg.color;
            c.a = enabled ? 0.6f : 0.3f;
            outerImg.color = c;
        }
        // 内側背景も合わせて薄く
        var inner = backBtn.transform.Find("Inner");
        if (inner != null)
        {
            var innerImg = inner.GetComponent<Image>();
            if (innerImg != null)
            {
                var ic = innerImg.color;
                ic.a = enabled ? 0.92f : 0.35f;
                innerImg.color = ic;
            }
        }
    }

    // ============================================================
    // 段階4: キャラ選択画面のチュートリアル
    // ============================================================

    System.Collections.IEnumerator ShowCharaSelectGuideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowCharaSelectGuide_Page1();
    }

    /// <summary>
    /// 段階4-Page1: 立ち絵 + 概要解説（「わからない」ボタン1個）
    /// </summary>
    void ShowCharaSelectGuide_Page1()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "ここがキャラ選択画面よ\n" +
            "下のキャラをタップすると\n" +
            "能力が表示されるから見ておきなさい\n" +
            "使うキャラは長押ししてから、\n" +
            "上のスロットにドラッグして入れるのよ\n" +
            "ちゃんと理解できた？？");

        // 専用ボイス（Tutorial/chara.wav）
        AudioClip charaVoice = Resources.Load<AudioClip>("Tutorial/chara");
        if (charaVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                charaVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowContinue("理解できた", () =>
        {
            overlay.Close();
            ShowCharaSelectGuide_Page2();
        });

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            // スキップ時は「もどる」ボタンを再有効化
            SetBackButtonInteractable(true);
        });
    }

    /// <summary>
    /// 段階4-Page2: スタートボタンをスポットライト＋矢印で誘導
    /// </summary>
    void ShowCharaSelectGuide_Page2()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();
        // キャラ選択・スロット・スタート全てをユーザに操作させる必要があるため
        // dim を無効化してクリックを背後の UI に通す
        overlay.HideDim();

        // スタートボタンの canvas 上の範囲（正規化、1080×1920 基準）
        //   anchor (0.75, 0.04), sizeDelta (340, 90), pivot 中央
        //   x: (0.75*1080 ± 170)/1080 = 0.593〜0.907
        //   y: (0.04*1920 ± 45)/1920 = 0.0166〜0.0635
        // 視覚的余白を加えて少し外側まで囲む
        Vector2 startMin = new Vector2(0.58f, 0.005f);
        Vector2 startMax = new Vector2(0.92f, 0.080f);

        // 強調表示：脈動する黄金色フレーム（クリックブロック無し）
        overlay.AddHighlightFrame(startMin, startMax,
            new Color(1f, 0.9f, 0.2f), 10f);

        // 吹き出しはボタン上部に配置
        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.10f),
            new Vector2(0.95f, 0.24f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "じゃあ実際にやってみなさい\n" +
            "キャラを長押し→ドラッグで3人スロットにセットして、\n" +
            "スタートボタンを押しなさい！");

        // 専用ボイス（Tutorial/chara2.wav）
        AudioClip chara2Voice = Resources.Load<AudioClip>("Tutorial/chara2");
        if (chara2Voice != null)
        {
            AudioManager.Instance?.PlayVoice(
                chara2Voice, 1.5f, AudioManager.VoicePriority.High);
        }

        // 矢印をスタートボタン上に配置（y=0.13、ボタン上端 0.08 のすぐ上）
        overlay.AddArrowAt(new Vector2(0.75f, 0.13f), "▼");

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            // スキップ時は「もどる」ボタンを再有効化
            SetBackButtonInteractable(true);
        });
    }

    void Update()
    {
        // 光の粒アニメーション
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            var p = particles[i];
            p.anchoredPosition += new Vector2(0f, 35f * Time.deltaTime);
            if (p.anchoredPosition.y > 1000f)
                p.anchoredPosition = new Vector2(Random.Range(-540f, 540f), -1000f);
            float x = p.anchoredPosition.x + Mathf.Sin(Time.time * 0.8f + i * 1.3f) * 15f * Time.deltaTime;
            p.anchoredPosition = new Vector2(x, p.anchoredPosition.y);
        }

        // スタミナ表示更新（回復タイマー用）
        if (staminaText != null) RefreshStaminaText();
    }

    void BuildUI()
    {
        var cGo = new GameObject("CharaSelectCanvas");
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

        // 長押し→ドラッグの成立率改善:
        // 既定のドラッグ判定しきい値(10px)は高DPI端末では約0.5mmしかなく、
        // 指の自然な揺れで「スクロール開始」と誤判定されて長押しが失敗する。
        // 約2mm相当（DPI×0.08インチ）まで許容する。
        var eventSystem = UnityEngine.EventSystems.EventSystem.current
            ?? FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null && Screen.dpi > 0f)
        {
            eventSystem.pixelDragThreshold = Mathf.Max(
                eventSystem.pixelDragThreshold,
                Mathf.RoundToInt(Screen.dpi * 0.08f));
        }

        Transform root = cGo.transform;

        // ===== 背景（深い紫/ネイビー） =====
        MakeBg(root, new Color(0.03f, 0.02f, 0.1f));

        // 上部装飾バー
        var topBar = new GameObject("TopBar");
        topBar.transform.SetParent(root, false);
        topBar.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.9f, 0.3f);
        var tbRt = topBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 0.96f); tbRt.anchorMax = Vector2.one;
        tbRt.offsetMin = tbRt.offsetMax = Vector2.zero;

        // 下部装飾バー
        var botBar = new GameObject("BotBar");
        botBar.transform.SetParent(root, false);
        botBar.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.9f, 0.3f);
        var bbRt = botBar.GetComponent<RectTransform>();
        bbRt.anchorMin = Vector2.zero; bbRt.anchorMax = new Vector2(1f, 0.02f);
        bbRt.offsetMin = bbRt.offsetMax = Vector2.zero;

        // ===== 光の粒パーティクル =====
        CreateParticles(root, 12);

        // ===== タイトル（Shadow + Outline 付き） =====
        var titleText = MakeText(root, "\u2726 \u9023\u308c\u3066\u3044\u304f\u30ad\u30e3\u30e9\u30923\u4f53\u3048\u3089\u3093\u3067\u266a \u2726", 38, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.93f), new Vector2(950f, 60f));
        var titleShadow = titleText.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.8f);
        titleShadow.effectDistance = new Vector2(3f, -3f);
        var titleOutline = titleText.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.8f, 0.2f, 0.4f, 0.9f);
        titleOutline.effectDistance = new Vector2(2f, -2f);

        // タイトル下の装飾ライン
        MakeLine(root, new Color(1f, 0.85f, 0.2f, 0.5f), new Vector2(0.5f, 0.905f), new Vector2(700f, 3f));

        // スタミナ表示（右上、タイトル下）
        canvasRoot = root;
        staminaText = MakeText(root, "", 28, new Color(0.4f, 0.95f, 0.6f),
            new Vector2(0.82f, 0.895f), new Vector2(320f, 36f));
        AddShadow(staminaText.gameObject);
        staminaText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);
        RefreshStaminaText();

        // 合計ヒットダメージ表示（左上、スタミナと対になる位置）
        totalDamageText = MakeText(root, "", 28, new Color(1f, 0.55f, 0.35f),
            new Vector2(0.18f, 0.895f), new Vector2(320f, 36f));
        AddShadow(totalDamageText.gameObject);
        totalDamageText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);

        // ---- キャラアイコンプレビュー（スロットの上） ----
        float[] slotXs = { 0.18f, 0.50f, 0.82f };
        for (int i = 0; i < 3; i++)
        {
            int slotIdxIcon = i;
            var iconBg = new GameObject($"SlotIconBg{i}");
            iconBg.transform.SetParent(root, false);
            var iconBgImg = iconBg.AddComponent<Image>();
            iconBgImg.color = new Color(0.1f, 0.06f, 0.25f, 0.8f);
            slotIconBgs[i] = iconBgImg;
            var ibRt = iconBg.GetComponent<RectTransform>();
            ibRt.anchorMin = ibRt.anchorMax = new Vector2(slotXs[i], 0.82f);
            ibRt.anchoredPosition = Vector2.zero;
            ibRt.sizeDelta = new Vector2(180f, 180f);
            // ドラッグドロップ用マーカー（アイコンプレビュー上でも受ける）
            var iconDrop = iconBg.AddComponent<SlotDropTarget>();
            iconDrop.slotIndex = slotIdxIcon;

            var iconGo = new GameObject($"SlotIcon{i}");
            iconGo.transform.SetParent(iconBg.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(1f, 1f, 1f, 0f);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(6f, 6f); irt.offsetMax = new Vector2(-6f, -6f);
            slotIcons[i] = iconImg;
        }

        // ---- スロット3つ ----
        for (int i = 0; i < 3; i++)
        {
            int idx = i;

            // 外枠（アクティブ時は明るい紫ボーダー）
            var frame = MakeRectImage(root, new Color(0.3f, 0.15f, 0.6f, 0.6f),
                new Vector2(slotXs[i], 0.71f), new Vector2(290f, 130f));
            slotFrames[i] = frame;

            // ドラッグドロップ用マーカー
            var dropTarget = frame.gameObject.AddComponent<SlotDropTarget>();
            dropTarget.slotIndex = idx;

            // 内側背景
            var inner = new GameObject("SlotInner");
            inner.transform.SetParent(frame.transform, false);
            var innerImg = inner.AddComponent<Image>();
            innerImg.color = new Color(0.08f, 0.06f, 0.2f, 0.95f);
            var innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

            slotBgs[i] = innerImg;

            // 上半分ハイライト（光沢）
            var shine = new GameObject("Shine");
            shine.transform.SetParent(inner.transform, false);
            shine.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var shineRt = shine.GetComponent<RectTransform>();
            shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
            shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

            MakeText(inner.transform, $"Slot {i + 1}", 20, new Color(0.6f, 0.6f, 0.8f),
                new Vector2(0.5f, 0.86f), new Vector2(240f, 28f));
            slotNames[i] = MakeText(inner.transform, "---", 28, Color.white,
                new Vector2(0.5f, 0.55f), new Vector2(240f, 38f));
            slotRarTxts[i] = MakeText(inner.transform, "", 20, Color.gray,
                new Vector2(0.5f, 0.22f), new Vector2(240f, 28f));

            var btn = frame.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => SetActiveSlot(idx));
        }

        // ---- 詳細パネル ----
        // 外枠（300→420 に拡張、5要素が完全に収まる）
        var dpFrame = MakeRectImage(root, new Color(0.4f, 0.2f, 0.7f, 0.5f),
            new Vector2(0.5f, 0.54f), new Vector2(970f, 420f));

        // 内側背景
        var dpInner = new GameObject("DetailInner");
        dpInner.transform.SetParent(dpFrame.transform, false);
        var dpInnerImg = dpInner.AddComponent<Image>();
        dpInnerImg.color = new Color(0.05f, 0.04f, 0.16f, 0.95f);
        var dpInnerRt = dpInner.GetComponent<RectTransform>();
        dpInnerRt.anchorMin = Vector2.zero; dpInnerRt.anchorMax = Vector2.one;
        dpInnerRt.offsetMin = new Vector2(3f, 3f); dpInnerRt.offsetMax = new Vector2(-3f, -3f);

        // パネル内側の有効幅 ≈ 940px。テキスト幅は 900px に統一して左右に 20px の余白
        // 縦方向：上から順に名前(60)→レアリティ(48)→パッシブ(90)→奥義(90)→説明(70)
        detailName    = MakeText(dpInner.transform, "", 46, Color.white,
            new Vector2(0.5f, 0.885f), new Vector2(900f, 60f));
        detailName.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailName.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutlineCS(detailName.gameObject);

        detailRarity  = MakeText(dpInner.transform, "", 34, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.755f), new Vector2(900f, 48f));
        detailRarity.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailRarity.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutlineCS(detailRarity.gameObject);

        // パッシブ：複合の場合は2行になりうるので Wrap で枠内に収める
        detailPassive = MakeText(dpInner.transform, "", 28, new Color(0.4f, 0.95f, 1f),
            new Vector2(0.5f, 0.575f), new Vector2(900f, 90f));
        detailPassive.alignment = TextAnchor.MiddleCenter;
        detailPassive.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailPassive.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutlineCS(detailPassive.gameObject);

        // 奥義：分裂など長文があるので Wrap で2行表示OK
        detailUlt     = MakeText(dpInner.transform, "", 28, new Color(1f, 0.85f, 0.3f),
            new Vector2(0.5f, 0.365f), new Vector2(900f, 90f));
        detailUlt.alignment = TextAnchor.MiddleCenter;
        detailUlt.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailUlt.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutlineCS(detailUlt.gameObject);

        // 説明：2-3行になりうる
        detailDesc    = MakeText(dpInner.transform, "", 22, new Color(0.85f, 0.85f, 0.9f),
            new Vector2(0.5f, 0.155f), new Vector2(900f, 70f));
        detailDesc.alignment = TextAnchor.MiddleCenter;
        detailDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailDesc.verticalOverflow = VerticalWrapMode.Truncate;
        AddOutlineCS(detailDesc.gameObject);

        // ---- 所持キャラ一覧ラベル ----
        var listLabel = MakeText(root, $"所持キャラ一覧  ({ownedChars.Length}/{OrbManager.MaxOwnedCharacters})", 30,
            new Color(0.7f, 0.7f, 0.9f),
            new Vector2(0.5f, 0.40f), new Vector2(900f, 40f));
        AddShadow(listLabel.gameObject);

        // ---- スクロール可能なキャラ一覧 ----
        BuildScrollList(root);

        // ---- ボタン行 ----
        // もどる ボタン（左、ステージセレクトへ戻る）
        backBtn = MakeStyledButton(root, "もどる", 36,
            new Color(0.2f, 0.15f, 0.35f), new Color(0.4f, 0.25f, 0.6f),
            new Vector2(0.25f, 0.04f), new Vector2(340f, 90f), "◁",
            () => SceneManager.LoadScene("StageSelectScene"));

        // スタート ボタン（右）
        MakeStyledButton(root, "スタート", 36,
            new Color(0.1f, 0.35f, 0.75f), new Color(0.2f, 0.55f, 1f),
            new Vector2(0.75f, 0.04f), new Vector2(340f, 90f), "▷",
            OnStartClicked);
    }

    void BuildScrollList(Transform root)
    {
        // Scroll 領域の RectTransform
        var scrollGo = new GameObject("ScrollArea");
        scrollGo.transform.SetParent(root, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.02f, 0.10f);
        scrollRT.anchorMax = new Vector2(0.98f, 0.38f);
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;
        scroll.scrollSensitivity = 30f;
        charScrollRect = scroll; // CharaDragHandler に渡す参照

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
        const float cardH  = 170f;
        const float padX   = 20f;
        const float padY   = 14f;

        int rows = Mathf.CeilToInt((float)ownedChars.Length / cols);
        float contentH = rows * (cardH + padY) + padY + cardH * 0.5f;
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

            // 外枠（レアリティカラー 0.5 alpha）
            var frameGo = new GameObject($"Frame_{cd.characterName}");
            frameGo.transform.SetParent(contentGo.transform, false);
            frameGo.AddComponent<Image>().color = new Color(rc.r, rc.g, rc.b, 0.5f);
            var frameRT = frameGo.GetComponent<RectTransform>();
            frameRT.anchorMin = frameRT.anchorMax = new Vector2(0.5f, 1f);
            frameRT.pivot      = new Vector2(0.5f, 0.5f);
            frameRT.anchoredPosition = new Vector2(xOffset, yOffset);
            frameRT.sizeDelta  = new Vector2(cardW, cardH);

            // 内側背景
            var cardGo = new GameObject($"Card_{cd.characterName}");
            cardGo.transform.SetParent(frameGo.transform, false);
            cardGo.AddComponent<Image>().color = bgC;
            var cardRT = cardGo.GetComponent<RectTransform>();
            cardRT.anchorMin = Vector2.zero; cardRT.anchorMax = Vector2.one;
            cardRT.offsetMin = new Vector2(3f, 3f); cardRT.offsetMax = new Vector2(-3f, -3f);

            // 上半分ハイライト（光沢）
            var shineGo = new GameObject("Shine");
            shineGo.transform.SetParent(cardGo.transform, false);
            shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var shineRt = shineGo.GetComponent<RectTransform>();
            shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
            shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

            // レアリティバー（上端）
            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(cardGo.transform, false);
            barGo.AddComponent<Image>().color = rc;
            var barRT = barGo.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0f, 1f); barRT.anchorMax = new Vector2(1f, 1f);
            barRT.pivot     = new Vector2(0.5f, 1f);
            barRT.anchoredPosition = Vector2.zero;
            barRT.sizeDelta = new Vector2(0f, 6f);

            // アイコン + キャラ名（覚醒済みは金枠）
            if (cd.icon != null)
            {
                if (OrbManager.IsAwakened(cd.characterName))
                {
                    var gf = new GameObject("GoldFrame");
                    gf.transform.SetParent(cardGo.transform, false);
                    gf.AddComponent<Image>().color = new Color(1f, 0.85f, 0.1f, 0.9f);
                    var gfRt = gf.GetComponent<RectTransform>();
                    gfRt.anchorMin = gfRt.anchorMax = new Vector2(0.20f, 0.48f);
                    gfRt.anchoredPosition = Vector2.zero;
                    gfRt.sizeDelta = new Vector2(120f, 120f);
                }

                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(cardGo.transform, false);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = cd.icon;
                iconImg.preserveAspect = true;
                var irt = iconGo.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = new Vector2(0.20f, 0.48f);
                irt.anchoredPosition = Vector2.zero;
                irt.sizeDelta = new Vector2(110f, 110f);

                MakeText(cardGo.transform, cd.characterName, 28, Color.white,
                    new Vector2(0.65f, 0.65f), new Vector2(cardW * 0.5f, 38f));
                MakeText(cardGo.transform, cd.rarity.ToString(), 22, rc,
                    new Vector2(0.65f, 0.25f), new Vector2(cardW * 0.5f, 30f));
            }
            else
            {
                MakeText(cardGo.transform, cd.characterName, 26, Color.white,
                    new Vector2(0.5f, 0.65f), new Vector2(cardW - 16f, 34f));
                MakeText(cardGo.transform, cd.rarity.ToString(), 20, rc,
                    new Vector2(0.5f, 0.22f), new Vector2(cardW - 16f, 28f));
            }

            // ロングプレス→ドラッグ操作（タップはハンドラ内部で OnCardTap として処理）
            var handler = frameGo.AddComponent<CharaDragHandler>();
            handler.charIndex   = idx;
            handler.ownerUI     = this;
            handler.parentScroll = charScrollRect;
            handler.canvasRect  = canvasRoot as RectTransform;
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
        var cd = ownedChars[charIdx];

        // 他のスロットで既に選択済みなら弾く
        for (int i = 0; i < 3; i++)
        {
            if (i == activeSlot) continue;
            if (slotChars[i] != null && slotChars[i].characterName == cd.characterName)
                return;
        }

        slotChars[activeSlot] = cd;

        // キャラ選択ボイス再生（Mid優先度：連打時は最新で上書き）
        if (cd.voiceSelect != null)
            AudioManager.Instance?.PlayVoice(cd.voiceSelect, cd.voiceVolumeMultiplier, AudioManager.VoicePriority.Mid);

        // 次のスロットへ自動移動（全スロット埋めやすくする）
        if (activeSlot < 2) SetActiveSlot(activeSlot + 1);
        else                { RefreshAllSlots(); RefreshDetail(); }
    }

    // ============================================================
    // ドラッグドロップ API（CharaDragHandler から呼び出される）
    // ============================================================

    /// <summary>
    /// 短いタップ時の動作。
    /// 仕様: タップでは割り当てず、詳細パネルにそのキャラの能力を表示する。
    /// スロットへの追加はロングプレス→ドラッグ専用（誤タップで編成が変わらない）。
    /// </summary>
    public void OnCardTap(int charIdx)
    {
        if (charIdx < 0 || charIdx >= ownedChars.Length) return;
        ShowDetailOf(ownedChars[charIdx]);
    }

    /// <summary>ロングプレス発火時：スロットを薄いハイライトで候補表示</summary>
    public void OnPickupStart(int charIdx)
    {
        for (int i = 0; i < 3; i++)
        {
            if (slotFrames[i] != null)
                slotFrames[i].color = new Color(0.6f, 0.4f, 0.9f, 0.75f);
        }
    }

    /// <summary>ピックアップ終了：ハイライトを通常状態に戻す</summary>
    public void OnPickupEnd()
    {
        RefreshAllSlots();
    }

    /// <summary>
    /// ドラッグ中、ポインタ下にあるスロットを明るく強調する。
    /// </summary>
    public void UpdateDropTargetHighlight(UnityEngine.EventSystems.PointerEventData ev)
    {
        int hoverSlot = FindSlotUnderPointer(ev);
        for (int i = 0; i < 3; i++)
        {
            if (slotFrames[i] == null) continue;
            if (i == hoverSlot)
                slotFrames[i].color = new Color(1f, 0.85f, 0.2f, 0.95f); // 強調イエロー
            else
                slotFrames[i].color = new Color(0.6f, 0.4f, 0.9f, 0.65f); // 候補紫
        }
    }

    /// <summary>
    /// ドロップ判定。スロット上で離せば割り当て、それ以外は何もしない（ゴーストは消滅）。
    /// </summary>
    public void TryDropOnSlot(int charIdx, UnityEngine.EventSystems.PointerEventData ev)
    {
        if (charIdx < 0 || charIdx >= ownedChars.Length) return;
        int targetSlot = FindSlotUnderPointer(ev);
        if (targetSlot < 0) return;
        DoAssignToSlot(charIdx, targetSlot);
    }

    int FindSlotUnderPointer(UnityEngine.EventSystems.PointerEventData ev)
    {
        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(ev, results);
        foreach (var r in results)
        {
            var t = r.gameObject.GetComponentInParent<SlotDropTarget>();
            if (t != null) return t.slotIndex;
        }
        return -1;
    }

    void DoAssignToSlot(int charIdx, int targetSlot)
    {
        var cd = ownedChars[charIdx];

        // 同キャラが他スロットにいるなら外す（同じキャラを2スロットに置かない）
        for (int i = 0; i < 3; i++)
        {
            if (i == targetSlot) continue;
            if (slotChars[i] != null && slotChars[i].characterName == cd.characterName)
                slotChars[i] = null;
        }

        // ターゲットスロットを上書き（元のキャラはリストに残り続けるので "戻す" 動作不要）
        slotChars[targetSlot] = cd;
        activeSlot = targetSlot;

        // キャラ選択ボイス
        if (cd.voiceSelect != null)
            AudioManager.Instance?.PlayVoice(cd.voiceSelect, cd.voiceVolumeMultiplier, AudioManager.VoicePriority.Mid);

        RefreshAllSlots();
        RefreshDetail();
    }

    /// <summary>
    /// ドラッグ中に指追従する半透明ゴースト（アイコン + レアリティ枠）を生成。
    /// </summary>
    public GameObject CreateDragGhost(int charIdx)
    {
        if (canvasRoot == null) return null;
        if (charIdx < 0 || charIdx >= ownedChars.Length) return null;
        var cd = ownedChars[charIdx];

        var ghost = new GameObject("DragGhost");
        ghost.transform.SetParent(canvasRoot, false);
        ghost.transform.SetAsLastSibling();

        // 外枠（レアリティ色）
        var frameImg = ghost.AddComponent<Image>();
        Color rc = GetRarityColor(cd.rarity);
        frameImg.color = new Color(rc.r, rc.g, rc.b, 0.9f);
        frameImg.raycastTarget = false;

        var grt = ghost.GetComponent<RectTransform>();
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(180f, 180f);

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(ghost.transform, false);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = new Color(rc.r * 0.25f, rc.g * 0.25f, rc.b * 0.25f, 0.95f);
        innerImg.raycastTarget = false;
        var inRt = innerGo.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero; inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(4f, 4f); inRt.offsetMax = new Vector2(-4f, -4f);

        // アイコン
        if (cd.icon != null)
        {
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(ghost.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = cd.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(12f, 12f); irt.offsetMax = new Vector2(-12f, -12f);
        }

        return ghost;
    }

    void RefreshAllSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            var cd = slotChars[i];
            slotNames[i].text   = (cd != null) ? cd.characterName : "---";
            slotRarTxts[i].text = (cd != null) ? cd.rarity.ToString() : "";
            if (cd != null) slotRarTxts[i].color = GetRarityColor(cd.rarity);

            // アイコンプレビュー更新
            if (slotIcons[i] != null)
            {
                if (cd != null && cd.icon != null)
                {
                    slotIcons[i].sprite = cd.icon;
                    slotIcons[i].color = Color.white;
                    // 覚醒済みなら背景を金枠に
                    if (slotIconBgs[i] != null)
                        slotIconBgs[i].color = OrbManager.IsAwakened(cd.characterName)
                            ? new Color(1f, 0.85f, 0.1f, 0.9f)
                            : new Color(0.1f, 0.06f, 0.25f, 0.8f);
                }
                else
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].color = (cd != null)
                        ? new Color(GetRarityColor(cd.rarity).r, GetRarityColor(cd.rarity).g, GetRarityColor(cd.rarity).b, 0.3f)
                        : new Color(1f, 1f, 1f, 0f);
                }
            }

            // 外枠のハイライト
            if (slotFrames[i])
                slotFrames[i].color = (i == activeSlot)
                    ? new Color(0.6f, 0.3f, 1f, 0.8f)
                    : new Color(0.2f, 0.1f, 0.35f, 0.5f);

            // 内側背景
            if (slotBgs[i])
                slotBgs[i].color = (i == activeSlot)
                    ? new Color(0.12f, 0.08f, 0.3f, 0.95f)
                    : new Color(0.08f, 0.06f, 0.2f, 0.95f);
        }

        RefreshTotalDamage();
    }

    /// <summary>
    /// スロット3体の合計ヒットダメージを表示。
    /// CharacterManager.Initialize / GetCurrentDamage と同じ式で、
    /// パワー合計（1 + 0.2×Lv + 覚醒ボーナス）＋ExtraDamage に BallDamageUp 倍率を掛けて切り上げ。
    /// </summary>
    void RefreshTotalDamage()
    {
        if (totalDamageText == null) return;

        float powerSum = 0f;
        int bonus = 0;
        float mult = 1f;
        foreach (var cd in slotChars)
        {
            if (cd == null) continue;
            int lvl = OrbManager.GetEnhanceLevel(cd.characterName);
            bool awakened = OrbManager.IsAwakened(cd.characterName);
            powerSum += 1f + 0.2f * lvl + (awakened ? OrbManager.AwakenBonusMultiplier : 0f);

            AccumulateDamagePassive(cd.passiveType, cd.passiveValue, ref bonus, ref mult);
            if (cd.passiveType2 != PassiveEffectType.None)
                AccumulateDamagePassive(cd.passiveType2, cd.passiveValue2, ref bonus, ref mult);
        }

        float baseDmg = Mathf.Max(powerSum, 1f) + bonus;
        int dmg = (int)System.Math.Ceiling(baseDmg * mult);
        totalDamageText.text = $"ヒットダメージ: {dmg}";
    }

    static void AccumulateDamagePassive(PassiveEffectType type, float value, ref int bonus, ref float mult)
    {
        if (type == PassiveEffectType.ExtraDamage)     bonus += (int)value;
        if (type == PassiveEffectType.BallDamageUp)    mult  *= value;
    }

    void RefreshDetail()
    {
        ShowDetailOf(slotChars[activeSlot]);
    }

    /// <summary>\u8a73\u7d30\u30d1\u30cd\u30eb\u306b\u6307\u5b9a\u30ad\u30e3\u30e9\u306e\u80fd\u529b\u3092\u8868\u793a\uff08null \u306a\u3089\u64cd\u4f5c\u30d2\u30f3\u30c8\u3092\u8868\u793a\uff09</summary>
    void ShowDetailOf(CharacterData cd)
    {
        if (cd == null)
        {
            detailName.text = "(\u672a\u9078\u629e)";
            detailRarity.text = detailPassive.text = detailUlt.text = "";
            detailDesc.text = "\u4e00\u89a7\u306e\u30ad\u30e3\u30e9\u3092\u30bf\u30c3\u30d7\u3067\u80fd\u529b\u3092\u78ba\u8a8d\n\u9577\u62bc\u3057\u2192\u30c9\u30e9\u30c3\u30b0\u3067\u30b9\u30ed\u30c3\u30c8\u306b\u30bb\u30c3\u30c8";
            return;
        }
        detailName.text    = cd.characterName;
        detailRarity.text  = $"レアリティ: {cd.rarity}";
        string p2 = (cd.passiveType2 != PassiveEffectType.None) ? $" / {PassiveDescSingle(cd.passiveType2, cd.passiveValue2)}" : "";
        detailPassive.text = $"[パッシブスキル] {PassiveDescSingle(cd.passiveType, cd.passiveValue)}{p2}";
        detailUlt.text     = $"[奥義] {UltDesc(cd)}";
        detailDesc.text    = cd.description;
    }

    void OnStartClicked()
    {
        int stage = ResultData.StageNumber;
        int cost = StaminaManager.GetCost(stage);

        // 全3スロット必須チェック
        int filled = 0;
        for (int i = 0; i < 3; i++) if (slotChars[i] != null) filled++;
        if (filled < 3)
        {
            ShowInfoPopup(
                "キャラを3人全員\nスロットに入れなさい！\n" +
                $"（現在 {filled} / 3 人）\n" +
                "長押しでドラッグできるわ");
            return;
        }

        if (!StaminaManager.HasStamina(stage))
        {
            ShowStaminaPopup();
            return;
        }

        // チュートリアル中（段階4）かどうか
        bool inTutorial = TutorialManager.Instance != null
                       && TutorialManager.Instance.CurrentStep == TutorialManager.Step.CharaSelect;

        // スタミナ消費確認ポップアップ
        ShowConfirmPopup($"スタミナを{cost}消費して開始します", "はい", "いいえ", () =>
        {
            StaminaManager.TryConsume(stage);
            RefreshStaminaText();

            for (int i = 0; i < 3; i++)
                ResultData.SelectedCharacterNames[i] =
                    (slotChars[i] != null) ? slotChars[i].characterName : "";

            PlayerPrefs.SetString(KeyLastSlot0, ResultData.SelectedCharacterNames[0]);
            PlayerPrefs.SetString(KeyLastSlot1, ResultData.SelectedCharacterNames[1]);
            PlayerPrefs.SetString(KeyLastSlot2, ResultData.SelectedCharacterNames[2]);
            PlayerPrefs.Save();

            SceneManager.LoadScene("GameScene");
        });

        // チュートリアル中なら「はい」誘導オーバーレイを追加表示
        if (inTutorial)
        {
            ShowStaminaConfirmGuide();
        }
    }

    /// <summary>
    /// 段階4 補足：スタミナ消費確認ポップアップ表示中に「はい」ボタンへ誘導するチュートリアル。
    /// ShowConfirmPopup の上に重ねて表示し、スポットライトで「はい」だけクリック可能にする。
    /// </summary>
    void ShowStaminaConfirmGuide()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        // 「はい」ボタンの canvas 上の範囲（正規化、1080×1920基準）
        //   Dialog center (0.5, 0.5), size 750×400
        //     → Dialog 範囲 x: 0.153〜0.847, y: 0.396〜0.604
        //   「はい」 anchor (0.3, 0.2) of dialog, size 240×70
        //     → canvas center (390, 840) = 正規化 (0.361, 0.4375)
        //     → 範囲 x: 0.250〜0.472, y: 0.419〜0.456
        // 視覚的余白を加えて少し広めに
        Vector2 yesMin = new Vector2(0.235f, 0.410f);
        Vector2 yesMax = new Vector2(0.485f, 0.470f);

        // スポットライト（「はい」以外をクリック禁止）
        overlay.ShowSpotlight(yesMin, yesMax);

        // 強調表示：脈動する黄金色フレーム
        overlay.AddHighlightFrame(yesMin, yesMax,
            new Color(1f, 0.9f, 0.2f), 10f);

        // 吹き出しはダイアログの上に配置
        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.66f),
            new Vector2(0.95f, 0.82f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "ゲーム開始にはスタミナを使うの\n" +
            "そのまま『はい』を押しなさい！");

        // 矢印はポップアップ本文と被って読めなくなるため表示しない
        // （「はい」はスポットライト＋黄金フレームで十分誘導できる）

        // 専用ボイス（Tutorial/stamina.wav）
        AudioClip staminaVoice = Resources.Load<AudioClip>("Tutorial/stamina");
        if (staminaVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                staminaVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            // スキップ時は「もどる」ボタンを再有効化
            SetBackButtonInteractable(true);
        });
    }

    void ShowConfirmPopup(string message, string yesLabel, string noLabel, System.Action onYes)
    {
        var overlay = new GameObject("ConfirmOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(750f, 400f);

        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        MakeText(dialog.transform, message, 34, Color.white,
            new Vector2(0.5f, 0.65f), new Vector2(680f, 80f));

        // はいボタン
        var yesGo = new GameObject("YesBtn");
        yesGo.transform.SetParent(dialog.transform, false);
        yesGo.AddComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f, 0.9f);
        var yesBtn = yesGo.AddComponent<Button>();
        var yrt = yesGo.GetComponent<RectTransform>();
        yrt.anchorMin = yrt.anchorMax = new Vector2(0.3f, 0.2f);
        yrt.anchoredPosition = Vector2.zero;
        yrt.sizeDelta = new Vector2(240f, 70f);
        var yesTxt = MakeText(yesGo.transform, yesLabel, 30, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(220f, 60f));
        { var cf = Resources.Load<Font>("Fonts/CherryBombOne-Regular"); if (cf != null) yesTxt.font = cf; }
        yesTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        yesTxt.verticalOverflow = VerticalWrapMode.Overflow;
        yesBtn.onClick.AddListener(() => { Destroy(overlay); onYes?.Invoke(); });

        // いいえボタン
        var noGo = new GameObject("NoBtn");
        noGo.transform.SetParent(dialog.transform, false);
        noGo.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 0.9f);
        var noBtn = noGo.AddComponent<Button>();
        var nrt = noGo.GetComponent<RectTransform>();
        nrt.anchorMin = nrt.anchorMax = new Vector2(0.7f, 0.2f);
        nrt.anchoredPosition = Vector2.zero;
        nrt.sizeDelta = new Vector2(240f, 70f);
        var noTxt = MakeText(noGo.transform, noLabel, 30, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(220f, 60f));
        { var cf = Resources.Load<Font>("Fonts/CherryBombOne-Regular"); if (cf != null) noTxt.font = cf; }
        noTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        noTxt.verticalOverflow = VerticalWrapMode.Overflow;
        noBtn.onClick.AddListener(() => Destroy(overlay));
    }

    /// <summary>
    /// OK ボタン1個だけの情報ポップアップ（編成エラー等のシンプル通知に）。
    /// ShowConfirmPopup と同レイアウト、ボタン1つ。
    /// </summary>
    void ShowInfoPopup(string message)
    {
        var overlay = new GameObject("InfoOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(750f, 400f);

        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        MakeText(dialog.transform, message, 32, Color.white,
            new Vector2(0.5f, 0.60f), new Vector2(680f, 200f));

        // OK ボタン
        var okGo = new GameObject("OkBtn");
        okGo.transform.SetParent(dialog.transform, false);
        okGo.AddComponent<Image>().color = new Color(0.2f, 0.55f, 0.95f, 0.9f);
        var okBtn = okGo.AddComponent<Button>();
        var okRt = okGo.GetComponent<RectTransform>();
        okRt.anchorMin = okRt.anchorMax = new Vector2(0.5f, 0.2f);
        okRt.anchoredPosition = Vector2.zero;
        okRt.sizeDelta = new Vector2(240f, 70f);
        var okTxt = MakeText(okGo.transform, "OK", 30, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(220f, 60f));
        { var cf = Resources.Load<Font>("Fonts/CherryBombOne-Regular"); if (cf != null) okTxt.font = cf; }
        okTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        okTxt.verticalOverflow = VerticalWrapMode.Overflow;
        okBtn.onClick.AddListener(() => Destroy(overlay));
    }

    void RefreshStaminaText()
    {
        int sta = StaminaManager.GetStamina();
        if (sta >= StaminaManager.MaxStamina)
            staminaText.text = $"スタミナ {sta}/{StaminaManager.MaxStamina}";
        else
        {
            int sec = StaminaManager.GetSecondsUntilNext();
            int m = sec / 60; int s = sec % 60;
            staminaText.text = $"スタミナ {sta}/{StaminaManager.MaxStamina}  ({m:D2}:{s:D2})";
        }

        staminaText.color = sta > 0
            ? new Color(0.4f, 0.95f, 0.6f)
            : new Color(1f, 0.3f, 0.3f);
    }

    void ShowStaminaPopup()
    {
        var overlay = new GameObject("StaminaOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ枠
        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(750f, 450f);

        // 内側
        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        // メッセージ
        MakeText(dialog.transform, "スタミナが足りません！", 36, new Color(1f, 0.4f, 0.4f),
            new Vector2(0.5f, 0.78f), new Vector2(680f, 50f));

        MakeText(dialog.transform,
            $"{StaminaManager.OrbCostFullRecover}オーブで\nスタミナを全回復しますか？",
            30, Color.white,
            new Vector2(0.5f, 0.55f), new Vector2(680f, 100f));

        MakeText(dialog.transform,
            $"◆ 所持オーブ: {OrbManager.GetOrbs()}",
            26, new Color(0.4f, 0.95f, 1f),
            new Vector2(0.5f, 0.38f), new Vector2(680f, 36f));

        // 回復ボタン
        var yesGo = new GameObject("RecoverBtn");
        yesGo.transform.SetParent(dialog.transform, false);
        yesGo.AddComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f, 0.9f);
        var yesBtn = yesGo.AddComponent<Button>();
        var yrt = yesGo.GetComponent<RectTransform>();
        yrt.anchorMin = yrt.anchorMax = new Vector2(0.3f, 0.15f);
        yrt.anchoredPosition = Vector2.zero;
        yrt.sizeDelta = new Vector2(240f, 70f);
        var recoverTxt = MakeText(yesGo.transform, "回復する", 28, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(220f, 60f));
        { var cf = Resources.Load<Font>("Fonts/CherryBombOne-Regular"); if (cf != null) recoverTxt.font = cf; }
        recoverTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        recoverTxt.verticalOverflow = VerticalWrapMode.Overflow;
        yesBtn.onClick.AddListener(() =>
        {
            if (StaminaManager.TryFullRecoverWithOrbs())
            {
                Destroy(overlay);
                RefreshStaminaText();
                ShowNoticePopup("スタミナが全回復しました！");
            }
        });

        // キャンセルボタン
        var noGo = new GameObject("CancelBtn");
        noGo.transform.SetParent(dialog.transform, false);
        noGo.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 0.9f);
        var noBtn = noGo.AddComponent<Button>();
        var nrt = noGo.GetComponent<RectTransform>();
        nrt.anchorMin = nrt.anchorMax = new Vector2(0.7f, 0.15f);
        nrt.anchoredPosition = Vector2.zero;
        nrt.sizeDelta = new Vector2(240f, 70f);
        var backTxt = MakeText(noGo.transform, "もどる", 28, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(220f, 60f));
        { var cf = Resources.Load<Font>("Fonts/CherryBombOne-Regular"); if (cf != null) backTxt.font = cf; }
        backTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        backTxt.verticalOverflow = VerticalWrapMode.Overflow;
        noBtn.onClick.AddListener(() => Destroy(overlay));
    }

    void ShowNoticePopup(string message)
    {
        var overlay = new GameObject("NoticeOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(700f, 300f);

        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        MakeText(dialog.transform, message, 36, new Color(0.4f, 1f, 0.6f),
            new Vector2(0.5f, 0.6f), new Vector2(650f, 60f));

        var okGo = new GameObject("OKBtn");
        okGo.transform.SetParent(dialog.transform, false);
        okGo.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f, 0.9f);
        var okBtn = okGo.AddComponent<Button>();
        var okRt = okGo.GetComponent<RectTransform>();
        okRt.anchorMin = okRt.anchorMax = new Vector2(0.5f, 0.2f);
        okRt.anchoredPosition = Vector2.zero;
        okRt.sizeDelta = new Vector2(240f, 70f);
        var okTxt = MakeText(okGo.transform, "OK", 30, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(220f, 60f));
        { var cf = Resources.Load<Font>("Fonts/CherryBombOne-Regular"); if (cf != null) okTxt.font = cf; }
        okTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        okTxt.verticalOverflow = VerticalWrapMode.Overflow;
        okBtn.onClick.AddListener(() => Destroy(overlay));
    }

    // ---- 説明文 ----

    string PassiveDesc(CharacterData cd)
    {
        string p1 = PassiveDescSingle(cd.passiveType, cd.passiveValue);
        if (cd.passiveType2 != PassiveEffectType.None)
            p1 += $" / {PassiveDescSingle(cd.passiveType2, cd.passiveValue2)}";
        return p1;
    }

    string PassiveDescSingle(PassiveEffectType type, float val)
    {
        switch (type)
        {
            case PassiveEffectType.BallDamageUp:    return $"ダメージ +{(val - 1f) * 100f:0}%";
            case PassiveEffectType.ExtraDamage:     return $"追加ダメージ +{(int)val}";
            case PassiveEffectType.ExtraStock:      return $"開始時ストック +{(int)val}";
            case PassiveEffectType.UltGaugeBoost:   return $"奥義ゲージ増加量 +{(val - 1f) * 100f:0}%";
            case PassiveEffectType.CriticalRangeUp: return $"クリティカル範囲 +{(int)val}%";
            default: return "なし";
        }
    }

    string UltDesc(CharacterData cd)
    {
        switch (cd.ultimateType)
        {
            case UltimateSkillType.PowerBurst:   return $"{cd.ultimateDuration:F0}\u79d2\u9593\u3001\u30c0\u30e1\u30fc\u30b8 +{(cd.ultimateValue - 1f) * 100f:0}%";
            case UltimateSkillType.MassDestroy:  return $"\u5168\u30d6\u30ed\u30c3\u30af\u306b {(int)cd.ultimateValue} \u30c0\u30e1\u30fc\u30b8";
            case UltimateSkillType.StockRecover: return $"\u30b9\u30c8\u30c3\u30af\u56de\u5fa9 +{(int)cd.ultimateValue}";
            case UltimateSkillType.BarrierShot:  return "\u6b21\u306e1\u30df\u30b9\u3092\u30ad\u30e3\u30f3\u30bb\u30eb";
            case UltimateSkillType.Penetrate:    return $"{cd.ultimateDuration:F0}\u79d2\u9593\u3001\u30dc\u30fc\u30eb\u304c\u30d6\u30ed\u30c3\u30af\u3092\u8cab\u901a";
            case UltimateSkillType.BallSplit:    return "\u30dc\u30fc\u30eb\u30922\u3064\u306b\u5206\u88c2\uff08\u5206\u88c2\u3057\u305f\u30dc\u30fc\u30eb\u3082\u518d\u5206\u88c2\u53ef\u80fd\uff09";
            default: return "\u306a\u3057";
        }
    }

    int RarityOrder(Rarity r)
    {
        switch (r)
        {
            case Rarity.SSR: return 0;
            case Rarity.SR:  return 1;
            case Rarity.R:   return 2;
            default:         return 3;
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

    // ---- 光の粒パーティクル生成 ----

    void CreateParticles(Transform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var pGo = new GameObject("Particle");
            pGo.transform.SetParent(parent, false);
            var img = pGo.AddComponent<Image>();
            img.raycastTarget = false;
            float r = Random.Range(0.85f, 1f);
            float g = Random.Range(0.7f, 0.95f);
            float bv = Random.Range(0.8f, 1f);
            float a = Random.Range(0.15f, 0.4f);
            img.color = new Color(r, g, bv, a);

            var prt = pGo.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            float startX = Random.Range(-540f, 540f);
            float startY = Random.Range(-960f, 960f);
            prt.anchoredPosition = new Vector2(startX, startY);
            float size = Random.Range(4f, 12f);
            prt.sizeDelta = new Vector2(size, size);

            particles.Add(prt);
        }
    }

    // ---- Shadow ヘルパー ----

    void AddShadow(GameObject go)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.6f);
        s.effectDistance = new Vector2(2f, -2f);
    }

    /// <summary>テキストに黒アウトラインを追加（視認性向上）</summary>
    void AddOutlineCS(GameObject go)
    {
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
        ol.effectDistance = new Vector2(2f, -2f);
    }

    // ---- 装飾ライン ヘルパー ----

    void MakeLine(Transform parent, Color col, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    // ---- スタイル付きボタン ヘルパー ----

    Button MakeStyledButton(Transform parent, string label, int fontSize,
        Color baseCol, Color highlightCol, Vector2 anchor, Vector2 size,
        string icon, UnityEngine.Events.UnityAction onClick)
    {
        // 外枠
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        var outerImg = go.AddComponent<Image>();
        outerImg.color = new Color(highlightCol.r, highlightCol.g, highlightCol.b, 0.6f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        btn.onClick.AddListener(onClick);

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.92f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        // 上半分ハイライト（光沢感）
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        // 左側装飾アイコン
        var leftIcon = new GameObject("LIcon");
        leftIcon.transform.SetParent(go.transform, false);
        var liT = leftIcon.AddComponent<Text>();
        liT.text = icon; liT.fontSize = 24;
        liT.color = new Color(1f, 1f, 1f, 0.7f);
        liT.alignment = TextAnchor.MiddleCenter;
        liT.font = UIFont.Main; liT.verticalOverflow = VerticalWrapMode.Overflow;
        var liRt = leftIcon.GetComponent<RectTransform>();
        liRt.anchorMin = liRt.anchorMax = new Vector2(0f, 0.5f);
        liRt.anchoredPosition = new Vector2(28f, 0f);
        liRt.sizeDelta = new Vector2(30f, 30f);

        // 右側装飾アイコン
        var rightIcon = new GameObject("RIcon");
        rightIcon.transform.SetParent(go.transform, false);
        var riT = rightIcon.AddComponent<Text>();
        riT.text = icon; riT.fontSize = 24;
        riT.color = new Color(1f, 1f, 1f, 0.7f);
        riT.alignment = TextAnchor.MiddleCenter;
        riT.font = UIFont.Main; riT.verticalOverflow = VerticalWrapMode.Overflow;
        var riRt = rightIcon.GetComponent<RectTransform>();
        riRt.anchorMin = riRt.anchorMax = new Vector2(1f, 0.5f);
        riRt.anchoredPosition = new Vector2(-28f, 0f);
        riRt.sizeDelta = new Vector2(30f, 30f);

        // ラベルテキスト（Shadow + Outline 付き）
        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = fontSize; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : UIFont.Main;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var txtShadow = txtGo.AddComponent<Shadow>();
        txtShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        txtShadow.effectDistance = new Vector2(2f, -2f);
        var txtOutline = txtGo.AddComponent<Outline>();
        txtOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
        txtOutline.effectDistance = new Vector2(1f, -1f);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // ボタン全体にShadow（浮遊感）
        var btnShadow = go.AddComponent<Shadow>();
        btnShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        btnShadow.effectDistance = new Vector2(4f, -4f);

        return btn;
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
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }
}
