using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour
{
    Transform canvasRoot;
    GameObject fullscreenPanel; // 全画面イラスト表示パネル
    Sprite clearIllustSprite;   // クリア時イラスト保持用

    void Start()
    {
        BuildUI();

        // チュートリアル進捗：GamePlay 段階でクリアしたら Result 段階へ
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.GamePlay
            && ResultData.IsClear)
        {
            TutorialManager.Instance.SetStep(TutorialManager.Step.Result);
            Debug.Log("[Tutorial] Result 段階へ進行");
            StartCoroutine(ShowResultGuideAfterDelay(1.0f));
        }
    }

    // ============================================================
    // 段階6: リザルト画面のチュートリアル
    // ============================================================

    System.Collections.IEnumerator ShowResultGuideAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ShowResultGuide_Page1();
    }

    /// <summary>
    /// 段階6-Page1: レイ立ち絵 + クリア祝福セリフ
    /// </summary>
    void ShowResultGuide_Page1()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.SetCharacterByName("Rei", "レイ");
        overlay.SetMessage(
            "ふんっ、やっとできたじゃない！\n" +
            "…でも、まあまあの腕前ね？\n" +
            "これからもっと修行しなさい！");

        // 専用ボイス（Tutorial/clear.wav）
        AudioClip clearVoice = Resources.Load<AudioClip>("Tutorial/clear");
        if (clearVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                clearVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowContinue("次へ", () =>
        {
            overlay.Close();
            ShowResultGuide_Page2();
        });

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
        });
    }

    /// <summary>
    /// 段階6-Page2: ホームボタンへの誘導（報酬通知）
    /// </summary>
    void ShowResultGuide_Page2()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        // ホームボタン位置（anchor 0.5, 0.10, sizeDelta 360×90、canvas 1080×1920）
        //   x: 540 ± 180 = 360〜720 → 正規化 0.333〜0.667
        //   y: 192 ± 45 = 147〜237 → 正規化 0.077〜0.123
        // 視覚的余白を加える
        Vector2 homeMin = new Vector2(0.32f, 0.065f);
        Vector2 homeMax = new Vector2(0.68f, 0.135f);

        // スポットライト（ホームボタンのみクリック可）
        overlay.ShowSpotlight(homeMin, homeMax);

        // 強調表示：脈動する黄金色フレーム
        overlay.AddHighlightFrame(homeMin, homeMax,
            new Color(1f, 0.9f, 0.2f), 10f);

        // 吹き出しは画面中央上寄りに配置
        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.40f),
            new Vector2(0.95f, 0.75f));
        overlay.SetMessage(
            "次のステージが解放されたよ！\n" +
            "\n" +
            "初回クリア報酬として\n" +
            "オーブが手に入ったよ！\n" +
            "\n" +
            "ホームに戻りなさい");

        // 矢印をホームボタンの上に配置（ボタン上端 0.135 のすぐ上）
        overlay.AddArrowAt(new Vector2(0.5f, 0.18f), "▼");

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
        });
    }

    void BuildUI()
    {
        bool isClear = ResultData.IsClear;
        float rate = ResultData.DestroyRate;
        int stage = ResultData.StageNumber;

        // クリア時にランキング送信
        if (isClear)
        {
            string playerName = AuthManager.GetName();
            if (!string.IsNullOrEmpty(playerName))
                RankingManager.SubmitScore(stage, playerName, rate);
        }

        var cGo = new GameObject("ResultCanvas");
        Canvas c = cGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0.0f;
        cGo.AddComponent<GraphicRaycaster>();
        canvasRoot = cGo.transform;

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 背景
        MakeImage(canvasRoot, new Color(0.05f, 0.05f, 0.15f), Vector2.zero, Vector2.one);

        // クリア時: 背景全面にイラスト（半透明オーバーレイ付き）
        StageData stageData = null;
        if (isClear)
        {
            var allStages = Resources.LoadAll<StageData>("Stages");
            foreach (var s in allStages)
                if (s.stageNumber == stage) { stageData = s; break; }

            // 裏ステージクリア時は _True_100.png を優先表示（未設定時は表の _100.png にフォールバック）
            Sprite fullSprite = null;
            if (stageData != null)
            {
                if (ResultData.IsTrueStageClear && stageData.trueIllustSpriteFull != null)
                    fullSprite = stageData.trueIllustSpriteFull;
                else
                    fullSprite = stageData.illustSpriteFull;
            }

            if (fullSprite != null)
            {
                clearIllustSprite = fullSprite;

                // 全面イラスト
                var illustGo = new GameObject("ClearIllust");
                illustGo.transform.SetParent(canvasRoot, false);
                var illustImg = illustGo.AddComponent<Image>();
                illustImg.sprite = clearIllustSprite;
                illustImg.preserveAspect = true;
                illustImg.color = Color.white;
                illustImg.raycastTarget = false;
                var irt = illustGo.GetComponent<RectTransform>();
                irt.anchorMin = Vector2.zero;
                irt.anchorMax = Vector2.one;
                irt.offsetMin = irt.offsetMax = Vector2.zero;

                // 半透明暗めオーバーレイ（テキストを読みやすくする）
                var overlayGo = new GameObject("DarkOverlay");
                overlayGo.transform.SetParent(canvasRoot, false);
                overlayGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
                var ort = overlayGo.GetComponent<RectTransform>();
                ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
                ort.offsetMin = ort.offsetMax = Vector2.zero;
                // オーバーレイ自体はクリックを遮らない
                overlayGo.GetComponent<Image>().raycastTarget = false;
            }
        }

        // タイトル
        Color titleCol = isClear ? new Color(1f, 0.9f, 0.2f) : new Color(1f, 0.3f, 0.3f);
        MakeText(canvasRoot, isClear ? "STAGE CLEAR!" : "GAME OVER",
            72, titleCol, new Vector2(0.5f, 0.72f), new Vector2(800f, 100f));

        // ステージ番号
        MakeText(canvasRoot, $"STAGE {stage}",
            52, new Color(0.8f, 0.8f, 0.8f), new Vector2(0.5f, 0.64f), new Vector2(500f, 70f));

        // 破壊率（ゲームオーバー時のみ表示）
        if (!isClear)
        {
            MakeText(canvasRoot, $"DESTROY: {Mathf.FloorToInt(rate * 100)}%",
                44, Color.white, new Vector2(0.5f, 0.56f), new Vector2(500f, 65f));
        }

        // 初回クリア報酬
        if (isClear && ResultData.IsFirstClear)
        {
            MakeText(canvasRoot,
                $"★ {OrbManager.StageClearReward} オーブ GET!",
                58, new Color(1f, 0.85f, 0.1f),
                new Vector2(0.5f, 0.48f), new Vector2(700f, 70f));

            MakeText(canvasRoot,
                "美少女コレクション♡Get!",
                58, new Color(1f, 0.5f, 0.8f),
                new Vector2(0.5f, 0.41f), new Vector2(700f, 65f));
        }

        // NEXT STAGE ボタン（クリア時のみ）
        int nextStage = stage + 1;
        if (isClear && nextStage <= ProgressManager.TotalStages)
        {
            MakeButton(canvasRoot, "ネクスト", new Color(0.15f, 0.7f, 0.3f),
                new Vector2(0.5f, 0.30f), new Vector2(400f, 90f),
                () => {
                    ResultData.StageNumber = nextStage;
                    SceneManager.LoadScene("StageSelectScene");
                });
        }
        else if (isClear)
        {
            MakeText(canvasRoot, "ALL STAGES CLEAR!", 36,
                new Color(1f, 0.9f, 0.2f), new Vector2(0.5f, 0.30f), new Vector2(600f, 55f));
        }

        // RETRY ボタン
        MakeButton(canvasRoot, "リトライ", new Color(0.2f, 0.5f, 1f),
            new Vector2(0.5f, 0.20f), new Vector2(360f, 90f),
            () => OnRetryClicked());

        // HOME ボタン
        MakeButton(canvasRoot, "ホーム", new Color(0.3f, 0.3f, 0.35f),
            new Vector2(0.5f, 0.10f), new Vector2(360f, 90f),
            () => SceneManager.LoadScene("HomeScene"));

        // 全画面表示ボタン（クリア＋イラストあり時のみ）
        if (isClear && clearIllustSprite != null)
        {
            MakeButton(canvasRoot, "全画面表示", new Color(1f, 1f, 1f, 0f),
                new Vector2(0.5f, 0.84f), new Vector2(360f, 72f),
                () => ShowFullscreenIllust());
        }

        // 全画面イラストパネル（初期非表示）
        BuildFullscreenPanel();
    }

    void BuildFullscreenPanel()
    {
        if (clearIllustSprite == null) return;

        fullscreenPanel = new GameObject("FullscreenPanel");
        fullscreenPanel.transform.SetParent(canvasRoot, false);

        // 黒背景
        var bgImg = fullscreenPanel.AddComponent<Image>();
        bgImg.color = Color.black;
        var rt = fullscreenPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // イラスト（全画面、アスペクト比維持）
        var illustGo = new GameObject("FullIllust");
        illustGo.transform.SetParent(fullscreenPanel.transform, false);
        var illustImg = illustGo.AddComponent<Image>();
        illustImg.sprite = clearIllustSprite;
        illustImg.preserveAspect = true;
        illustImg.color = Color.white;
        var irt = illustGo.GetComponent<RectTransform>();
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = irt.offsetMax = Vector2.zero;

        // 「タップで戻る」テキスト
        var hintGo = new GameObject("Hint");
        hintGo.transform.SetParent(fullscreenPanel.transform, false);
        var hint = hintGo.AddComponent<Text>();
        hint.text = "タップで戻る";
        hint.fontSize = 28;
        hint.color = new Color(1f, 1f, 1f, 0.6f);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        var hrt = hintGo.GetComponent<RectTransform>();
        hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.03f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(400f, 40f);

        // タップで閉じるボタン（全画面）
        var closeBtn = fullscreenPanel.AddComponent<Button>();
        closeBtn.transition = Selectable.Transition.None;
        closeBtn.onClick.AddListener(HideFullscreenIllust);

        fullscreenPanel.SetActive(false);
    }

    void ShowFullscreenIllust()
    {
        if (fullscreenPanel != null) fullscreenPanel.SetActive(true);
    }

    void HideFullscreenIllust()
    {
        if (fullscreenPanel != null) fullscreenPanel.SetActive(false);
    }

    void OnRetryClicked()
    {
        int stage = ResultData.StageNumber;
        int cost = StaminaManager.GetCost(stage);

        if (!StaminaManager.HasStamina(stage))
        {
            ShowStaminaPopup();
            return;
        }

        // スタミナ消費確認ポップアップ
        ShowConfirmPopup($"スタミナを{cost}消費して開始します", "はい", "いいえ", () =>
        {
            StaminaManager.TryConsume(stage);
            SceneManager.LoadScene("GameScene");
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

        MakeButton(dialog.transform, yesLabel, new Color(0.2f, 0.6f, 0.3f),
            new Vector2(0.3f, 0.2f), new Vector2(240f, 70f),
            () => { Destroy(overlay); onYes?.Invoke(); });

        MakeButton(dialog.transform, noLabel, new Color(0.5f, 0.2f, 0.2f),
            new Vector2(0.7f, 0.2f), new Vector2(240f, 70f),
            () => Destroy(overlay));
    }

    void ShowStaminaPopup()
    {
        var overlay = new GameObject("StaminaOverlay");
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
        drt.sizeDelta = new Vector2(750f, 450f);

        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        MakeText(dialog.transform, "スタミナが足りません！", 36, new Color(1f, 0.4f, 0.4f),
            new Vector2(0.5f, 0.78f), new Vector2(680f, 50f));
        MakeText(dialog.transform,
            $"{StaminaManager.OrbCostFullRecover}オーブで\nスタミナを全回復しますか？",
            30, Color.white, new Vector2(0.5f, 0.55f), new Vector2(680f, 100f));
        MakeText(dialog.transform,
            $"◆ 所持オーブ: {OrbManager.GetOrbs()}", 26, new Color(0.4f, 0.95f, 1f),
            new Vector2(0.5f, 0.38f), new Vector2(680f, 36f));

        // 回復ボタン
        MakeButton(dialog.transform, "回復する", new Color(0.2f, 0.6f, 0.3f),
            new Vector2(0.3f, 0.15f), new Vector2(240f, 70f), () =>
            {
                if (StaminaManager.TryFullRecoverWithOrbs())
                {
                    Destroy(overlay);
                    ShowNoticePopup("スタミナが全回復しました！");
                }
            });

        // キャンセルボタン
        MakeButton(dialog.transform, "もどる", new Color(0.5f, 0.2f, 0.2f),
            new Vector2(0.7f, 0.15f), new Vector2(240f, 70f), () => Destroy(overlay));
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

        MakeText(dialog.transform, message, 36, new Color(0.4f, 1f, 0.5f),
            new Vector2(0.5f, 0.65f), new Vector2(650f, 80f));

        MakeButton(dialog.transform, "OK", new Color(0.2f, 0.5f, 0.7f),
            new Vector2(0.5f, 0.2f), new Vector2(240f, 70f),
            () => Destroy(overlay));
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

    void MakeImage(Transform parent, Color col, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("BG");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
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

        // ボタンテキスト
        var btnTxt = MakeText(go.transform, label, 44, Color.white, new Vector2(0.5f, 0.5f), sizeDelta);
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        if (cherry != null) btnTxt.font = cherry;
        btnTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        btnTxt.verticalOverflow = VerticalWrapMode.Overflow;
        var txtRT = go.transform.GetChild(0).GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
    }
}
