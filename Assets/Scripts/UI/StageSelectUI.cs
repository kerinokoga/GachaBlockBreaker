using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// ステージ選択画面（ScrollRect 対応・20ステージ）
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    List<RectTransform> particles = new List<RectTransform>();
    Transform canvasRoot;
    RectTransform stage1CardRT;  // 段階3チュートリアル用：ステージ1カードの実位置参照

    void Start()
    {
        BuildUI();
        // チュートリアル進捗：GuideStart 段階で到達したら StageSelect 段階へ
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.GuideStart)
        {
            TutorialManager.Instance.SetStep(TutorialManager.Step.StageSelect);
            Debug.Log("[Tutorial] StageSelect 段階へ進行");
            StartCoroutine(ShowStageSelectGuideAfterDelay(0.6f));
        }
    }

    // ============================================================
    // 段階3: ステージ選択画面のチュートリアル
    // ============================================================

    System.Collections.IEnumerator ShowStageSelectGuideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowStageSelectGuide_Page1();
    }

    /// <summary>
    /// 段階3-Page1: 立ち絵 + 各要素の概要解説（次へボタン）
    /// </summary>
    void ShowStageSelectGuide_Page1()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.SetCharacterByName("Rei", "レイ");
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessageFontSize(38);
        overlay.SetMessage(
            "ここがステージセレクト画面よ\n" +
            "ステージをクリアするごとに次のステージが開放されるわよ\n" +
            "5の倍数はボスステージだから気をつけなさい！\n" +
            "次にすることはわかる？？");

        // 専用ボイス（Tutorial/stage.wav）
        AudioClip stageVoice = Resources.Load<AudioClip>("Tutorial/stage");
        if (stageVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                stageVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        // 「君が邪魔で画面が見えない…」ボタンのみ → 押すとステージ1誘導へ
        overlay.ShowContinue("君が邪魔で画面が見えない…", () =>
        {
            overlay.Close();
            ShowStageSelectGuide_Page2();
        });

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
        });
    }

    /// <summary>
    /// 段階3-Page2: ステージ1カードをスポットライト＋矢印で誘導
    /// </summary>
    void ShowStageSelectGuide_Page2()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        // ステージ1カードの実 RectTransform から枠を算出（アスペクト比非依存）
        Vector2 cardCenter = new Vector2(0.5f, 0.6825f);
        if (stage1CardRT != null)
            cardCenter = overlay.HighlightTarget(stage1CardRT, 14f, new Color(1f, 0.9f, 0.2f));

        // 吹き出しはカード下に配置（カードと被らないように）
        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.40f),
            new Vector2(0.95f, 0.55f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "ぐだぐだうるさいわね…\n" +
            "実際にやって覚えなさい！\n" +
            "ほら、ステージ1を選びなさい！");

        // 専用ボイス（Tutorial/stage2.wav）
        AudioClip stage2Voice = Resources.Load<AudioClip>("Tutorial/stage2");
        if (stage2Voice != null)
        {
            AudioManager.Instance?.PlayVoice(
                stage2Voice, 1.5f, AudioManager.VoicePriority.High);
        }

        // 矢印をカード上端の真上に配置（正規化 y=0.82、カード TOP 0.79 より少し上）
        overlay.AddArrowAt(new Vector2(cardCenter.x, Mathf.Min(cardCenter.y + 0.11f, 0.95f)), "▼");

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
        });
    }

    void Update()
    {
        // 光の粒アニメーション
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            var p = particles[i];
            p.anchoredPosition += new Vector2(0f, 40f * Time.deltaTime);
            if (p.anchoredPosition.y > 1000f)
                p.anchoredPosition = new Vector2(Random.Range(-540f, 540f), -1000f);
            float x = p.anchoredPosition.x + Mathf.Sin(Time.time * 0.8f + i * 1.3f) * 15f * Time.deltaTime;
            p.anchoredPosition = new Vector2(x, p.anchoredPosition.y);
        }
    }

    void BuildUI()
    {
        // ステージセレクトに来た時点で通常モードに戻す
        // （エンドレスのフラグはエンドレスの「挑戦する」でのみ true になる）
        ResultData.IsEndless = false;

        var cGo = new GameObject("StageSelectCanvas");
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

        Transform root = cGo.transform;
        canvasRoot = root; // チュートリアルから参照

        // 背景（暗い紫/紺）
        MakeBg(root, new Color(0.03f, 0.02f, 0.1f));

        // 上部デコレーションバー
        var topBar = new GameObject("TopBar");
        topBar.transform.SetParent(root, false);
        topBar.AddComponent<Image>().color = new Color(0.3f, 0.1f, 0.5f, 0.3f);
        var topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0f, 0.92f);
        topBarRT.anchorMax = Vector2.one;
        topBarRT.offsetMin = topBarRT.offsetMax = Vector2.zero;

        // 下部デコレーションバー
        var botBar = new GameObject("BotBar");
        botBar.transform.SetParent(root, false);
        botBar.AddComponent<Image>().color = new Color(0.3f, 0.1f, 0.5f, 0.3f);
        var botBarRT = botBar.GetComponent<RectTransform>();
        botBarRT.anchorMin = Vector2.zero;
        botBarRT.anchorMax = new Vector2(1f, 0.08f);
        botBarRT.offsetMin = botBarRT.offsetMax = Vector2.zero;

        // 光の粒パーティクル
        CreateParticles(root, 15);

        // タイトル（Shadow + Outline 付き）
        var titleCherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        var titleT = MakeText(root, "\u2726 \u30b9\u30c6\u30fc\u30b8\u30bb\u30ec\u30af\u30c8 \u2726", 52, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.93f), new Vector2(900f, 80f));
        if (titleCherry != null) titleT.font = titleCherry;
        titleT.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleT.verticalOverflow = VerticalWrapMode.Overflow;
        var titleShadow = titleT.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.8f);
        titleShadow.effectDistance = new Vector2(3f, -3f);
        var titleOutline = titleT.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.8f, 0.2f, 0.4f, 0.9f);
        titleOutline.effectDistance = new Vector2(2f, -2f);

        // タイトル下の区切りライン
        MakeLine(root, new Color(1f, 0.85f, 0.1f, 0.4f), 0.915f, 2f);

        // BACK ボタン（装飾付き）
        MakeStyledButton(root, "ホーム",
            new Color(0.25f, 0.25f, 0.35f), new Color(0.45f, 0.45f, 0.6f),
            new Vector2(0.5f, 0.04f), new Vector2(360f, 80f),
            () => SceneManager.LoadScene("HomeScene"));

        // ScrollRect（y=0.10〜0.88）
        BuildScrollList(root);
    }

    void BuildScrollList(Transform root)
    {
        // ScrollView（ScrollRect + 透明Image でドラッグ入力を受け取る）
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(root, false);
        var scrollImg = scrollGo.AddComponent<Image>();
        scrollImg.color = new Color(0f, 0f, 0f, 0f); // 透明（レイキャスト用）
        var sr = scrollGo.AddComponent<ScrollRect>();
        var scrollRT = scrollGo.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.02f, 0.10f);
        scrollRT.anchorMax = new Vector2(0.98f, 0.88f);
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;

        // Viewport（Mask で領域外を非表示）
        var viewGo = new GameObject("Viewport");
        viewGo.transform.SetParent(scrollGo.transform, false);
        viewGo.AddComponent<Image>().color = Color.white;
        viewGo.AddComponent<Mask>().showMaskGraphic = false;
        var viewRT = viewGo.GetComponent<RectTransform>();
        viewRT.anchorMin = Vector2.zero;
        viewRT.anchorMax = Vector2.one;
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        // Content
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewGo.transform, false);
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        sr.content    = contentRT;
        sr.viewport   = viewRT;
        sr.horizontal = false;
        sr.vertical   = true;
        sr.scrollSensitivity = 30f;

        var allStages  = Resources.LoadAll<StageData>("Stages");
        int maxUnlocked = ProgressManager.GetMaxUnlocked();

        int   total  = ProgressManager.TotalStages;
        float cellW  = 950f;
        float cellH  = 360f;
        float padY   = 18f;

        float contentHeight = total * (cellH + padY) + padY + cellH * 0.5f;
        contentRT.sizeDelta = new Vector2(0f, contentHeight);

        for (int i = 0; i < total; i++)
        {
            int   stageNum = i + 1;
            float xAnchor  = 0.5f;
            float yPos     = -(padY + i * (cellH + padY) + cellH * 0.5f);

            StageData sd = null;
            foreach (var s in allStages)
                if (s.stageNumber == stageNum) { sd = s; break; }

            bool  unlocked = stageNum <= maxUnlocked;
            bool  cleared  = ProgressManager.IsCleared(stageNum);
            float rate     = ProgressManager.GetBestRate(stageNum);

            BuildStageCard(contentRT, stageNum, sd, unlocked, cleared, rate,
                           xAnchor, yPos, cellW, cellH);
        }
    }

    void BuildStageCard(Transform parent, int stageNum, StageData sd,
        bool unlocked, bool cleared, float rate,
        float anchorX, float yPos, float w, float h)
    {
        int capturedNum = stageNum;

        Color cardCol = unlocked
            ? new Color(0.12f, 0.12f, 0.28f)
            : new Color(0.10f, 0.10f, 0.12f);

        // カード外枠の色（ボス: 赤 / クリア済み: illustColorFull / ロック: 暗い灰色）
        bool isBoss = stageNum % 5 == 0;
        Color borderCol;
        if (!unlocked)
            borderCol = new Color(0.15f, 0.15f, 0.2f, 0.5f);
        else if (isBoss)
            borderCol = new Color(0.9f, 0.2f, 0.2f, 0.7f);
        else if (cleared && sd != null)
            borderCol = new Color(sd.illustColorFull.r, sd.illustColorFull.g, sd.illustColorFull.b, 0.5f);
        else
            borderCol = new Color(0.4f, 0.3f, 0.7f, 0.5f);

        // 外枠（ボーダー）
        var outerGo = new GameObject($"Stage{stageNum}Outer");
        outerGo.transform.SetParent(parent, false);
        outerGo.AddComponent<Image>().color = borderCol;
        var outerRT = outerGo.GetComponent<RectTransform>();
        outerRT.anchorMin = outerRT.anchorMax = new Vector2(anchorX, 1f);
        outerRT.pivot = new Vector2(0.5f, 1f);
        outerRT.anchoredPosition = new Vector2(0f, yPos);
        outerRT.sizeDelta = new Vector2(w, h);

        // 段階3チュートリアル用：ステージ1カードの参照を保持
        if (stageNum == 1) stage1CardRT = outerRT;

        // 外枠に Shadow（浮遊感）
        var outerShadow = outerGo.AddComponent<Shadow>();
        outerShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        outerShadow.effectDistance = new Vector2(3f, -3f);

        // 内側背景
        var cardGo = new GameObject($"Stage{stageNum}Card");
        cardGo.transform.SetParent(outerGo.transform, false);
        cardGo.AddComponent<Image>().color = new Color(cardCol.r, cardCol.g, cardCol.b, 0.93f);
        var rt = cardGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(3f, 3f); rt.offsetMax = new Vector2(-3f, -3f);

        // 上半分ハイライト（光沢感）
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(cardGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        Transform ct = cardGo.transform;

        // クリア済みなら上端カラーライン
        if (cleared && sd != null)
        {
            var lineGo = new GameObject("Line");
            lineGo.transform.SetParent(ct, false);
            lineGo.AddComponent<Image>().color = sd.illustColorFull;
            var lRT = lineGo.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0f, 1f);
            lRT.anchorMax = new Vector2(1f, 1f);
            lRT.pivot = new Vector2(0.5f, 1f);
            lRT.anchoredPosition = Vector2.zero;
            lRT.sizeDelta = new Vector2(0f, 6f);
        }

        // ステージ番号（Shadow 付き、CherryBombOne フォント）
        bool isBossStage = stageNum % 5 == 0;
        string stageLabel = isBossStage ? $"ステージ{stageNum}  ♡ボス♡" : $"ステージ{stageNum}";
        Color stageNumCol = unlocked
            ? (isBossStage ? new Color(1f, 0.4f, 0.4f) : Color.white)
            : new Color(0.5f, 0.5f, 0.5f);
        // ボスステージはイラストとの重なり回避のため右寄せに配置
        Vector2 stageLabelAnchor = isBossStage ? new Vector2(0.65f, 0.86f) : new Vector2(0.5f, 0.86f);
        float   stageLabelWidth  = isBossStage ? (w * 0.6f) : (w - 20f);
        var stageNumT = MakeCellText(ct, stageLabel, 56, stageNumCol,
            stageLabelAnchor, new Vector2(stageLabelWidth, 80f));
        var stageLabelCherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        if (stageLabelCherry != null) stageNumT.font = stageLabelCherry;
        stageNumT.horizontalOverflow = HorizontalWrapMode.Overflow;
        stageNumT.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(stageNumT.gameObject);
        // ステージ番号にもアウトラインで視認性UP
        var stageNumOutline = stageNumT.gameObject.AddComponent<Outline>();
        stageNumOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        stageNumOutline.effectDistance = new Vector2(2f, -2f);

        if (unlocked)
        {
            // 案A: クリア状態に応じてイラスト切替
            //   未クリア       → illustSprite0（シルエット）
            //   クリア済み     → illustSpriteFull（100%イラスト）
            //   裏クリア済み   → trueIllustSpriteFull（未設定なら illustSpriteFull にフォールバック）
            bool trueCleared = ProgressManager.IsTrueStageClear(stageNum);
            Sprite displaySprite = null;
            Color  fallbackCol   = new Color(0.4f, 0.4f, 0.6f);
            if (sd != null)
            {
                if (trueCleared)
                {
                    displaySprite = sd.trueIllustSpriteFull != null
                        ? sd.trueIllustSpriteFull
                        : sd.illustSpriteFull;
                    fallbackCol = sd.illustColorFull;
                }
                else if (cleared)
                {
                    displaySprite = sd.illustSpriteFull;
                    fallbackCol = sd.illustColorFull;
                }
                else
                {
                    // 未クリア: シルエット表示
                    displaySprite = sd.illustSprite0;
                    fallbackCol = new Color(0.15f, 0.12f, 0.2f); // シルエット風の暗紫
                }
            }

            var swatchGo = new GameObject("Swatch");
            swatchGo.transform.SetParent(ct, false);
            var swatchImg = swatchGo.AddComponent<Image>();
            if (displaySprite != null)
            {
                swatchImg.sprite = displaySprite;
                swatchImg.preserveAspect = true;
                swatchImg.color = Color.white;
            }
            else
            {
                // スプライトが用意されていない場合のフォールバック
                swatchImg.color = fallbackCol;
            }
            var sRT = swatchGo.GetComponent<RectTransform>();
            sRT.anchorMin = sRT.anchorMax = new Vector2(0.18f, 0.42f);
            sRT.sizeDelta = new Vector2(280f, 280f);

            // タイトル画面と同じフォント（CherryBombOne-Regular）を使用
            var cherryFont = Resources.Load<Font>("Fonts/CherryBombOne-Regular");

            string charName = (sd != null && !string.IsNullOrEmpty(sd.characterName))
                ? sd.characterName : "???";
            var charNameT = MakeCellText(ct, charName, 60, new Color(1f, 0.95f, 1f),
                new Vector2(0.65f, 0.55f), new Vector2(w * 0.55f, 80f));
            if (cherryFont != null) charNameT.font = cherryFont;
            charNameT.horizontalOverflow = HorizontalWrapMode.Overflow;
            charNameT.verticalOverflow = VerticalWrapMode.Overflow;
            AddShadow(charNameT.gameObject);
            // CherryBombOne は元から太字デザインのため、Outline で輪郭を強化
            var charOutline = charNameT.gameObject.AddComponent<Outline>();
            charOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            charOutline.effectDistance = new Vector2(2f, -2f);

            // クリア済みなら「ステージクリア済」を表示（未クリア時は何も表示しない）
            if (cleared)
            {
                var clearedT = MakeCellText(ct, "ステージクリア", 50,
                    new Color(1f, 0.85f, 0.3f),
                    new Vector2(0.65f, 0.26f), new Vector2(w * 0.6f, 70f));
                if (cherryFont != null) clearedT.font = cherryFont;
                clearedT.horizontalOverflow = HorizontalWrapMode.Overflow;
                clearedT.verticalOverflow = VerticalWrapMode.Overflow;
                AddShadow(clearedT.gameObject);
                // 「ステージクリア」にもアウトラインで視認性UP
                var clearedOutline = clearedT.gameObject.AddComponent<Outline>();
                clearedOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
                clearedOutline.effectDistance = new Vector2(2f, -2f);
            }

            var btn = outerGo.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() =>
            {
                ResultData.StageNumber = capturedNum;
                SceneManager.LoadScene("CharaSelectScene");
            });
        }
        else
        {
            var lockedT = MakeCellText(ct, "LOCKED", 28, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.5f, 0.50f), new Vector2(w - 20f, 40f));
            AddShadow(lockedT.gameObject);

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(ct, false);
            overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var ort = overlay.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;
        }
    }

    // ---- 装飾付きボタン ----

    void MakeStyledButton(Transform parent, string label, Color baseCol, Color highlightCol,
        Vector2 anchor, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        // 外枠（明るい縁取り）
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        var outerImg = go.AddComponent<Image>();
        outerImg.color = new Color(highlightCol.r, highlightCol.g, highlightCol.b, 0.6f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        btn.onClick.AddListener(onClick);

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.93f);
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

        // ラベルテキスト（Shadow + Outline 付き）
        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 36; t.color = Color.white;
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

        // ボタン全体に Shadow（浮遊感）
        var btnShadow = go.AddComponent<Shadow>();
        btnShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        btnShadow.effectDistance = new Vector2(4f, -4f);
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
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    Text MakeCellText(Transform parent, string txt, int size, Color col, Vector2 anchor, Vector2 sizeDelta)
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

    void AddShadow(GameObject go)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.6f);
        s.effectDistance = new Vector2(2f, -2f);
    }

    void MakeLine(Transform parent, Color col, float yAnchor, float height)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, yAnchor);
        rt.anchorMax = new Vector2(0.9f, yAnchor);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);
    }
}
