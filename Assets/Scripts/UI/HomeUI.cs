using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class HomeUI : MonoBehaviour
{
    Text staminaText;
    Transform canvasRoot;
    List<CanvasGroup> fadeButtons = new List<CanvasGroup>();
    List<RectTransform> particles = new List<RectTransform>();

    void Start()
    {
        // 初回起動時にスターターオーブ 1000 をプレゼントボックスへ付与
        PresentBoxManager.EnsureStarterOrbs();
        PresentBoxManager.CheckLoginBonus();
        // マンスリーパス有効中なら本日分の特典を付与
        MonthlyPassManager.CheckDailyGrant();
        // デイリーミッションの日付リセット判定
        DailyMissionManager.CheckReset();
        // 進行状況をクラウドへバックアップ（ホーム到達ごと・非同期）
        CloudSaveManager.Save();
        BuildUI();
        AudioManager.Instance?.PlayBGMForScene("HomeScene");
        StartCoroutine(FadeInButtons());

        // チュートリアル：**初回ホーム画面到達時のみ** ガイドキャラを表示
        // CurrentStep == None の時だけ intro を出す（中間ステップで戻ってきた時は出さない）
        TutorialManager.EnsureInstance();
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.ShouldShowTutorial
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.None)
        {
            StartCoroutine(ShowTutorialIntroAfterDelay(0.8f));
        }

        // 段階6 → 段階7: Result 段階でホームに戻ったら PresentBox 段階へ
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.Result)
        {
            TutorialManager.Instance.SetStep(TutorialManager.Step.PresentBox);
            Debug.Log("[Tutorial] PresentBox 段階へ進行");
        }

        // 段階7: PresentBox 段階ならプレゼントボックス誘導を起動
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.PresentBox)
        {
            StartCoroutine(ShowPresentBoxGuideAfterDelay(0.8f));
        }

        // 段階8（旧ガチャ誘導）→ チュートリアル完了通知ポップアップに変更
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.Gacha)
        {
            StartCoroutine(ShowTutorialCompletePopupAfterDelay(0.8f));
        }
    }

    // ============================================================
    // チュートリアル完了ポップアップ（PresentBox からホームに戻った時に表示）
    //   オーブ/ガチャの存在を告知 + チュートリアル終了を伝える
    // ============================================================

    System.Collections.IEnumerator ShowTutorialCompletePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowTutorialCompletePopup();
    }

    void ShowTutorialCompletePopup()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.SetCharacterByName("Rei", "レイ");
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "これでばっちりね\n" +
            "あとは自分でやりなさい");

        // 専用ボイス（Tutorial/end.wav）
        AudioClip endVoice = Resources.Load<AudioClip>("Tutorial/end");
        if (endVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                endVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowContinue("わかった", () =>
        {
            overlay.Close();
            // チュートリアル完了フラグを保存
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.Complete();
                Debug.Log("[Tutorial] チュートリアル完了！");
            }
        });

        // スキップは「わかった」と同じ動作（完了として扱う）
        overlay.ShowSkipButton(() =>
        {
            overlay.Close();
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.Complete();
        });
    }

    // ============================================================
    // 段階7: プレゼントボックスへの誘導
    // ============================================================

    System.Collections.IEnumerator ShowPresentBoxGuideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPresentBoxGuide();
    }

    /// <summary>
    /// プレゼントボックスボタンをスポットライト＋矢印で誘導。
    /// プレゼントボタン位置: anchor (0.12, 0.20), sizeDelta (260, 78)
    ///   x: (0.12*1080 ± 130)/1080 = -0.0004〜0.240
    ///   y: (0.20*1920 ± 39)/1920 = 0.180〜0.220
    /// </summary>
    void ShowPresentBoxGuide()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        // 視覚的余白を加えてプレゼントボタンを少し外側まで囲む
        // プレゼントボタン位置 (0.12, 0.29) size 260x78 に合わせた範囲（余白込み）
        Vector2 pMin = new Vector2(0.0f, 0.255f);
        Vector2 pMax = new Vector2(0.27f, 0.325f);

        // スポットライト（プレゼントボタンのみクリック可）
        overlay.ShowSpotlight(pMin, pMax);

        // 強調表示：脈動する黄金色フレーム
        overlay.AddHighlightFrame(pMin, pMax,
            new Color(1f, 0.9f, 0.2f), 10f);

        // 吹き出しは画面中央寄り
        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.50f),
            new Vector2(0.95f, 0.75f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "オーブが届いてるはずよ\n" +
            "受け取りなさい");

        // 矢印をプレゼントボタンの右に配置（◀ でボタンを指す）
        overlay.AddArrowAt(new Vector2(0.32f, 0.29f), "◀");

        // 専用ボイス（Tutorial/present.wav）
        AudioClip presentVoice = Resources.Load<AudioClip>("Tutorial/present");
        if (presentVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                presentVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
        });
    }

    // ============================================================
    // 段階8: ガチャ画面への誘導
    // ============================================================

    System.Collections.IEnumerator ShowGachaGuideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowGachaGuide();
    }

    /// <summary>
    /// ガチャボタンをスポットライト＋矢印で誘導。
    /// ガチャボタン位置: anchor (0.12, 0.65), sizeDelta (260, 78)
    ///   x: -0.0004〜0.240
    ///   y: 0.630〜0.670
    /// </summary>
    void ShowGachaGuide()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        // 視覚的余白を加えてガチャボタンを少し外側まで囲む
        Vector2 gMin = new Vector2(0.0f, 0.615f);
        Vector2 gMax = new Vector2(0.27f, 0.685f);

        // スポットライト（ガチャボタンのみクリック可）
        overlay.ShowSpotlight(gMin, gMax);

        // 強調表示：脈動する黄金色フレーム
        overlay.AddHighlightFrame(gMin, gMax,
            new Color(1f, 0.9f, 0.2f), 10f);

        // 吹き出しは画面中央上寄り
        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.30f),
            new Vector2(0.95f, 0.55f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "もらったオーブで\n" +
            "新しいキャラをゲットしなさい！\n" +
            "ガチャボタンをタップよ");

        // 矢印をガチャボタンの右に配置（◀ でボタンを指す）
        overlay.AddArrowAt(new Vector2(0.32f, 0.65f), "◀");

        // 専用ボイス（Tutorial/gacha.wav）
        AudioClip gachaVoice = Resources.Load<AudioClip>("Tutorial/gacha");
        if (gachaVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                gachaVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
        });
    }

    /// <summary>
    /// チュートリアル開始：レイのガイドキャラを表示して「はい/いいえ」を問う。
    /// </summary>
    System.Collections.IEnumerator ShowTutorialIntroAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // レイの CharacterData をボイス用に取得
        CharacterData reiData = null;
        var allChars = Resources.LoadAll<CharacterData>("Characters");
        foreach (var c in allChars)
        {
            if (c != null && c.characterName == "レイ")
            {
                reiData = c;
                break;
            }
        }

        // オーバーレイ生成
        // 立ち絵は Resources/Tutorial/Rei.png を優先、
        // なければ Stage10 の illustSpriteFull にフォールバック
        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.SetCharacterByName("Rei", "レイ");
        // レイらしいツンツン口調のチュートリアル導入
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessageFontSize(38);
        overlay.SetMessage(
            "ふんっ、あんたはじめてみたいね\n" +
            "しかたないわ、私が特別に\n" +
            "このゲームの遊び方を教えてあげてもいいわよ！");

        // チュートリアル専用ボイス（Resources/Tutorial/intro.wav）優先、
        // 無ければ既存の voiceSelect にフォールバック
        AudioClip tutorialVoice = Resources.Load<AudioClip>("Tutorial/intro");
        if (tutorialVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                tutorialVoice,
                1.5f,  // チュートリアルボイスはハッキリ聞こえるよう少し大きめ
                AudioManager.VoicePriority.High);
        }
        else if (reiData != null && reiData.voiceSelect != null)
        {
            AudioManager.Instance?.PlayVoice(
                reiData.voiceSelect,
                reiData.voiceVolumeMultiplier,
                AudioManager.VoicePriority.High);
        }

        // 「教えてほしい / わかるから大丈夫」選択
        TutorialManager.Instance.SetStep(TutorialManager.Step.Intro);
        overlay.ShowYesNo("教えてほしい", "わかるから大丈夫", (yes) =>
        {
            overlay.Close();
            if (yes)
            {
                TutorialManager.Instance.SetStep(TutorialManager.Step.GuideStart);
                Debug.Log("[Tutorial] ユーザーが「教えてほしい」を選択 → ガイド開始");
                // 段階2: スタートボタン誘導
                ShowStartButtonGuide();
            }
            else
            {
                // 永続スキップ
                TutorialManager.Instance.SkipAll();
                Debug.Log("[Tutorial] ユーザーが「わかるから大丈夫」を選択 → 永続スキップ");
            }
        });
    }

    /// <summary>
    /// 段階2：スタートボタンへの誘導
    /// 立ち絵非表示、ダイム無効、矢印＋吹き出し＋ボイスでスタートボタンを指す。
    /// </summary>
    void ShowStartButtonGuide()
    {
        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        // スタートボタン位置:
        //   anchor (0.12, 0.74), sizeDelta (260, 78), canvas 1080x1920
        //   x: -0.4 〜 259.6 px (= 0.000 〜 0.240 normalized)
        //   y: 1381.8 〜 1459.8 px (= 0.720 〜 0.760 normalized)
        // 視覚的余白を加えてスポットライト範囲を確保
        overlay.ShowSpotlight(
            new Vector2(0.0f,  0.705f),
            new Vector2(0.28f, 0.775f));

        // 強調表示：スポットライト境界に脈動する黄金色フレーム
        overlay.AddHighlightFrame(
            new Vector2(0.0f,  0.705f),
            new Vector2(0.28f, 0.775f),
            new Color(1f, 0.9f, 0.2f), // 黄金色
            10f);

        // 吹き出しを画面上部に配置
        overlay.SetBubbleAnchor(new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.92f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage("まずはスタートを押しなさいよ！");

        // 矢印をスタートボタン真上に配置（x=0.12, y=0.80）
        overlay.AddArrowAt(new Vector2(0.12f, 0.80f), "▼");

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
        });

        // ボイス再生（start.wav 優先）
        AudioClip startVoice = Resources.Load<AudioClip>("Tutorial/start");
        if (startVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                startVoice, 1.5f, AudioManager.VoicePriority.High);
        }
    }

    void Update()
    {
        // スタミナ表示更新（回復カウントダウン付き）
        if (staminaText != null)
        {
            int sta = StaminaManager.GetStamina();
            string staStr;
            if (sta >= StaminaManager.MaxStamina)
            {
                staStr = $"スタミナ: {sta}/{StaminaManager.MaxStamina}";
            }
            else
            {
                int sec = StaminaManager.GetSecondsUntilNext();
                int m = sec / 60;
                int s = sec % 60;
                staStr = $"スタミナ: {sta}/{StaminaManager.MaxStamina}  ({m:00}:{s:00})";
            }
            staminaText.text = staStr;
            staminaText.color = sta > 0 ? new Color(0.4f, 0.95f, 0.6f) : new Color(1f, 0.3f, 0.3f);
        }

        // 光の粒アニメーション
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            var p = particles[i];
            p.anchoredPosition += new Vector2(0f, 40f * Time.deltaTime);
            // 画面上端を超えたら下に戻す
            if (p.anchoredPosition.y > 1000f)
                p.anchoredPosition = new Vector2(Random.Range(-540f, 540f), -1000f);
            // ゆらゆら横移動
            float x = p.anchoredPosition.x + Mathf.Sin(Time.time * 0.8f + i * 1.3f) * 15f * Time.deltaTime;
            p.anchoredPosition = new Vector2(x, p.anchoredPosition.y);
        }
    }

    IEnumerator FadeInButtons()
    {
        // 最初は全ボタン非表示
        foreach (var cg in fadeButtons)
            cg.alpha = 0f;

        yield return new WaitForSeconds(0.15f); // 0.3 → 0.15（倍速）

        // 順番にフェードイン
        foreach (var cg in fadeButtons)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 6f; // 0.166秒でフェードイン（倍速）
                cg.alpha = Mathf.Clamp01(t);
                yield return null;
            }
            cg.alpha = 1f;
            yield return new WaitForSeconds(0.03f); // ボタン間の間隔（倍速）
        }
    }

    void BuildUI()
    {
        GameObject cGo = new GameObject("HomeCanvas");
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

        // ===== 1. 全画面背景（暗い紺色ベース） =====
        MakeImage(cGo.transform, new Color(0.02f, 0.02f, 0.08f), Vector2.zero, Vector2.one);

        // 背景イラスト（Resources/Home/bg に配置）
        var bgSprite = Resources.Load<Sprite>("Home/bg");
        if (bgSprite != null)
        {
            var bgGo = new GameObject("BGIllust");
            bgGo.transform.SetParent(cGo.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;
            bgImg.color = new Color(1f, 1f, 1f, 1f); // 背景イラスト完全表示
            var bgrt = bgGo.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
            bgrt.offsetMin = bgrt.offsetMax = Vector2.zero;
        }

        // 暗めオーバーレイ（グラデーション風に上下を暗く）
        // 全画面オーバーレイ（上下＋中央帯）
        var overlayTop = new GameObject("OverlayTop");
        overlayTop.transform.SetParent(cGo.transform, false);
        overlayTop.AddComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.35f);
        var otrt = overlayTop.GetComponent<RectTransform>();
        otrt.anchorMin = new Vector2(0f, 0.6f); otrt.anchorMax = Vector2.one;
        otrt.offsetMin = otrt.offsetMax = Vector2.zero;

        // 中央帯オーバーレイ（55%〜60%）
        var overlayMid = new GameObject("OverlayMid");
        overlayMid.transform.SetParent(cGo.transform, false);
        overlayMid.AddComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.35f);
        var omrt = overlayMid.GetComponent<RectTransform>();
        omrt.anchorMin = new Vector2(0f, 0.55f); omrt.anchorMax = new Vector2(1f, 0.6f);
        omrt.offsetMin = omrt.offsetMax = Vector2.zero;

        // 下部オーバーレイ
        var overlayBot = new GameObject("OverlayBot");
        overlayBot.transform.SetParent(cGo.transform, false);
        overlayBot.AddComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.4f);
        var obrt = overlayBot.GetComponent<RectTransform>();
        obrt.anchorMin = Vector2.zero; obrt.anchorMax = new Vector2(1f, 0.55f);
        obrt.offsetMin = obrt.offsetMax = Vector2.zero;

        // ===== 4. 光の粒パーティクル =====
        CreateParticles(cGo.transform, 18);

        // ユーザー情報（右上）
        string userName = AuthManager.GetName();
        if (!string.IsNullOrEmpty(userName))
        {
            var nameT = MakeText(cGo.transform, userName, 24,
                new Color(0.7f, 0.95f, 1f), new Vector2(0.72f, 0.96f), new Vector2(400f, 30f));
            AddShadow(nameT.gameObject);
        }

        // オーブ表示
        var orbT = MakeText(cGo.transform, $"◆ 所持オーブ: {OrbManager.GetOrbs()}", 32,
            new Color(0.5f, 0.9f, 0.7f), new Vector2(0.72f, 0.93f), new Vector2(500f, 40f));
        AddShadow(orbT.gameObject);

        // スタミナ表示（Update で毎フレーム更新）
        int sta = StaminaManager.GetStamina();
        string staStr = $"スタミナ: {sta}/{StaminaManager.MaxStamina}";
        staminaText = MakeText(cGo.transform, staStr, 32,
            sta > 0 ? new Color(0.4f, 0.95f, 0.6f) : new Color(1f, 0.3f, 0.3f),
            new Vector2(0.72f, 0.89f), new Vector2(500f, 40f));
        AddShadow(staminaText.gameObject);

        // 左右の美少女立ち絵
        var leftSprite = Resources.Load<Sprite>("Home/left");
        var rightSprite = Resources.Load<Sprite>("Home/right");
        if (leftSprite != null)
        {
            var lGo = new GameObject("LeftChara");
            lGo.transform.SetParent(cGo.transform, false);
            var lImg = lGo.AddComponent<Image>();
            lImg.sprite = leftSprite;
            lImg.preserveAspect = true;
            lImg.raycastTarget = false;
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.13f, 0.30f);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(550f, 1500f);
        }
        if (rightSprite != null)
        {
            var rGo = new GameObject("RightChara");
            rGo.transform.SetParent(cGo.transform, false);
            var rImg = rGo.AddComponent<Image>();
            rImg.sprite = rightSprite;
            rImg.preserveAspect = true;
            rImg.raycastTarget = false;
            var rrt = rGo.GetComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.87f, 0.30f);
            rrt.anchoredPosition = Vector2.zero;
            rrt.sizeDelta = new Vector2(550f, 1500f);
        }

        // ===== 3. ボタン（フェードイン付き） =====
        MakeMenuButton(cGo.transform, "スタート",
            new Color(0.1f, 0.4f, 0.8f), new Color(0.2f, 0.6f, 1f),
            0.74f, "♡", () => SceneManager.LoadScene("StageSelectScene"));

        MakeMenuButton(cGo.transform, "ガチャ",
            new Color(0.55f, 0.15f, 0.8f), new Color(0.75f, 0.35f, 1f),
            0.65f, "♡", () => SceneManager.LoadScene("GachaScene"));

        MakeMenuButton(cGo.transform, "オーブ購入",
            new Color(0.75f, 0.45f, 0.1f), new Color(0.95f, 0.65f, 0.2f),
            0.56f, "♡", () => SceneManager.LoadScene("ShopScene"));

        MakeMenuButton(cGo.transform, "キャラ管理",
            new Color(0.1f, 0.45f, 0.35f), new Color(0.2f, 0.65f, 0.5f),
            0.47f, "♡", () => SceneManager.LoadScene("CharaManageScene"));

        MakeMenuButton(cGo.transform, "コレクション",
            new Color(0.7f, 0.15f, 0.4f), new Color(0.9f, 0.35f, 0.55f),
            0.38f, "♡", () => SceneManager.LoadScene("CollectionScene"));

        // ランキング機能は一旦非公開（バックエンドのスコア送信は継続中）
        // 再公開する場合は以下のコメントを外すだけ
        // MakeMenuButton(cGo.transform, "ランキング",
        //     new Color(0.15f, 0.3f, 0.55f), new Color(0.3f, 0.5f, 0.8f),
        //     0.29f, "▷", () => SceneManager.LoadScene("RankingScene"));

        // プレゼントボックスボタン（バッジ付き）
        // ランキングボタン非公開に伴い 0.20 → 0.29 に詰めた
        MakePresentButton(cGo.transform, 0.29f);

        // デイリーミッションボタン
        MakeMenuButton(cGo.transform, "ミッション",
            new Color(0.15f, 0.5f, 0.6f), new Color(0.3f, 0.7f, 0.8f),
            0.20f, "♡", () => ShowMissionPopup());

        // アカウント連携ボタン（左上）
        var linkGo = new GameObject("LinkBtn");
        linkGo.transform.SetParent(cGo.transform, false);
        linkGo.AddComponent<Image>().color = new Color(0.15f, 0.25f, 0.45f, 0.7f);
        var linkB = linkGo.AddComponent<Button>();
        var linkRT = linkGo.GetComponent<RectTransform>();
        linkRT.anchorMin = linkRT.anchorMax = new Vector2(0.12f, 0.96f);
        linkRT.anchoredPosition = Vector2.zero;
        linkRT.sizeDelta = new Vector2(220f, 60f);
        linkB.onClick.AddListener(() => ShowAccountLinkPopup());
        var linkTxtGo = new GameObject("Txt");
        linkTxtGo.transform.SetParent(linkGo.transform, false);
        var linkT = linkTxtGo.AddComponent<Text>();
        linkT.text = "アカウント連携"; linkT.fontSize = 24; linkT.color = Color.white;
        linkT.alignment = TextAnchor.MiddleCenter;
        linkT.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        var linkTrt = linkTxtGo.GetComponent<RectTransform>();
        linkTrt.anchorMin = Vector2.zero; linkTrt.anchorMax = Vector2.one;
        linkTrt.offsetMin = linkTrt.offsetMax = Vector2.zero;

        // ===== 設定ボタン（右上） =====
        var setGo = new GameObject("SettingsBtn");
        setGo.transform.SetParent(cGo.transform, false);
        setGo.AddComponent<Image>().color = new Color(0.2f, 0.15f, 0.4f, 0.85f);
        var setBtn = setGo.AddComponent<Button>();
        var setRT = setGo.GetComponent<RectTransform>();
        setRT.anchorMin = setRT.anchorMax = new Vector2(0.92f, 0.96f);
        setRT.anchoredPosition = Vector2.zero;
        setRT.sizeDelta = new Vector2(130f, 60f);
        setBtn.onClick.AddListener(() => ShowSettingsPopup());
        var setTxtGo = new GameObject("Txt");
        setTxtGo.transform.SetParent(setGo.transform, false);
        var setT = setTxtGo.AddComponent<Text>();
        setT.text = "せってい"; setT.fontSize = 28; setT.color = Color.white;
        setT.alignment = TextAnchor.MiddleCenter;
        var setCherryFont = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        setT.font = setCherryFont != null ? setCherryFont : Font.CreateDynamicFontFromOSFont("Arial", 28);
        setT.horizontalOverflow = HorizontalWrapMode.Overflow;
        setT.verticalOverflow = VerticalWrapMode.Overflow;
        var setTrt = setTxtGo.GetComponent<RectTransform>();
        setTrt.anchorMin = Vector2.zero; setTrt.anchorMax = Vector2.one;
        setTrt.offsetMin = setTrt.offsetMax = Vector2.zero;
    }

    // ---- 設定ポップアップ ----

    void ShowSettingsPopup()
    {
        var overlay = new GameObject("SettingsOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ外枠
        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(800f, 620f);

        // ダイアログ内側
        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        // タイトル
        var titleT = MakeText(dialog.transform, "設定", 44,
            new Color(1f, 0.85f, 0.1f), new Vector2(0.5f, 0.92f), new Vector2(700f, 60f));
        AddShadow(titleT.gameObject);

        // 区切りライン
        var lineGo = new GameObject("Line");
        lineGo.transform.SetParent(dialog.transform, false);
        lineGo.AddComponent<Image>().color = new Color(1f, 0.85f, 0.3f, 0.4f);
        lineGo.GetComponent<Image>().raycastTarget = false;
        var lineRt = lineGo.GetComponent<RectTransform>();
        lineRt.anchorMin = lineRt.anchorMax = new Vector2(0.5f, 0.83f);
        lineRt.anchoredPosition = Vector2.zero;
        lineRt.sizeDelta = new Vector2(680f, 3f);

        // ---- 音量設定ボタン ----
        MakeSettingsItem(dialog.transform, "音量設定", 0.68f,
            new Color(0.2f, 0.35f, 0.6f), new Color(0.35f, 0.55f, 0.9f, 0.6f),
            () => { Destroy(overlay); ShowVolumePopup(); });

        // ---- あそびかたボタン ----
        MakeSettingsItem(dialog.transform, "あそびかた", 0.50f,
            new Color(0.15f, 0.5f, 0.35f), new Color(0.3f, 0.75f, 0.5f, 0.6f),
            () => { Destroy(overlay); ShowHowToPlayPopup(); });

        // ---- 利用規約・プライバシーポリシーボタン ----
        MakeSettingsItem(dialog.transform, "利用規約・プライバシーポリシー", 0.32f,
            new Color(0.45f, 0.2f, 0.25f), new Color(0.7f, 0.35f, 0.45f, 0.6f),
            () => Application.OpenURL("https://kerinokoga.github.io/GachaBlockBreaker/legal.html"));

        // 閉じるボタン
        var closeGo = new GameObject("CloseBtn");
        closeGo.transform.SetParent(dialog.transform, false);
        closeGo.AddComponent<Image>().color = new Color(0.6f, 0.25f, 0.8f, 0.6f);
        var closeBtn = closeGo.AddComponent<Button>();
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.06f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(260f, 70f);
        closeBtn.onClick.AddListener(() => Destroy(overlay));

        var closeInner = new GameObject("Inner");
        closeInner.transform.SetParent(closeGo.transform, false);
        closeInner.AddComponent<Image>().color = new Color(0.15f, 0.1f, 0.3f, 0.95f);
        var ciRt = closeInner.GetComponent<RectTransform>();
        ciRt.anchorMin = Vector2.zero; ciRt.anchorMax = Vector2.one;
        ciRt.offsetMin = new Vector2(3f, 3f); ciRt.offsetMax = new Vector2(-3f, -3f);

        var closeTxt = MakeText(closeGo.transform, "とじる", 34, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(240f, 60f));
        var cherryClose = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        if (cherryClose != null) closeTxt.font = cherryClose;
        closeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        closeTxt.verticalOverflow = VerticalWrapMode.Overflow;
        var clrt = closeTxt.GetComponent<RectTransform>();
        clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
        clrt.offsetMin = clrt.offsetMax = Vector2.zero;
    }

    void MakeSettingsItem(Transform parent, string label, float yAnchor,
        Color baseCol, Color highlightCol, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = highlightCol;
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, yAnchor);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(650f, 80f);
        btn.onClick.AddListener(onClick);

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.92f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 34; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 34);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var txtShadow = txtGo.AddComponent<Shadow>();
        txtShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        txtShadow.effectDistance = new Vector2(2f, -2f);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // 右矢印
        var arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(go.transform, false);
        var arrowT = arrowGo.AddComponent<Text>();
        arrowT.text = "▶"; arrowT.fontSize = 22;
        arrowT.color = new Color(1f, 1f, 1f, 0.5f);
        arrowT.alignment = TextAnchor.MiddleCenter;
        arrowT.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
        var arrowRt = arrowGo.GetComponent<RectTransform>();
        arrowRt.anchorMin = arrowRt.anchorMax = new Vector2(0.95f, 0.5f);
        arrowRt.anchoredPosition = Vector2.zero;
        arrowRt.sizeDelta = new Vector2(30f, 30f);
    }

    // ---- 遊び方ポップアップ（スクロール付き） ----

    void ShowHowToPlayPopup()
    {
        var overlay = new GameObject("HowToPlayOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ外枠
        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.15f, 0.4f, 0.6f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.03f, 0.04f);
        drt.anchorMax = new Vector2(0.97f, 0.96f);
        drt.offsetMin = drt.offsetMax = Vector2.zero;

        // ダイアログ内側
        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        // タイトル
        var titleT = MakeText(dialog.transform, "遊び方", 44,
            new Color(1f, 0.85f, 0.1f), new Vector2(0.5f, 0.95f), new Vector2(700f, 60f));
        AddShadow(titleT.gameObject);

        // ---- ScrollRect ----
        var scrollGo = new GameObject("HelpScroll");
        scrollGo.transform.SetParent(dialog.transform, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.03f, 0.10f);
        scrollRT.anchorMax = new Vector2(0.97f, 0.91f);
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;
        scrollGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);
        scrollGo.AddComponent<Mask>().showMaskGraphic = true;

        var vpGo = new GameObject("Viewport");
        vpGo.transform.SetParent(scrollGo.transform, false);
        var vpRT = vpGo.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        // テキスト(2500px)＋上マージン(20px)＋余白を確実に収める高さにする
        // （テキストより低いと末尾がスクロールできず、Elastic だと弾かれて戻る）
        contentRT.sizeDelta = new Vector2(0f, 2560f);

        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.content = contentRT;
        scrollRect.viewport = vpRT;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped; // 端で弾かれて戻らないように
        scrollRect.scrollSensitivity = 40f;

        // ---- 説明テキスト（GameUI と同じ内容） ----
        string helpContent =
            "スワイプでパドルを左右に動かし\n" +
            "ボールを落とさないようにしよう！\n" +
            "全てのブロックを壊せばクリア！\n\n" +
            "パドルの左右の端に当てると\n" +
            "ボールが斜めに飛ぶよ！\n\n" +
            "--- ブロックの種類 ---\n\n" +
            "通常ブロック（白）\n  1回当てると壊れる\n\n" +
            "耐久ブロック（黄〜紫）\n  HPが表示され、0になると壊れる\n  色が濃いほどHPが高い\n\n" +
            "爆発ブロック（赤）\n  壊すと周囲のブロックも破壊する\n\n" +
            "連鎖ブロック（緑）\n  壊すと隣接する同種ブロックが\n  連鎖して壊れる\n\n" +
            "速度ブロック（紫）\n  壊すとボールが加速する\n  速いほどダメージUp！注意！\n\n" +
            "ボスブロック（赤/大型）\n  各ステージに登場！\n  HPバー付きの大型ブロック\n  HPが減ると攻撃してくる！\n" +
            "  ・パドルが一時的に縮小\n  ・上からブロックが降ってくる\n  ・高難度ではスピードブロックも！\n\n" +
            "--- クリティカル ---\n\n" +
            "パドルのど真ん中にボールを\n" +
            "当てるとクリティカル発動！\n" +
            "ボールがブロックを貫通して\n" +
            "一直線に壊していくよ！\n" +
            "ダメージが2倍になる！\n" +
            "次にパドルに当たると解除\n\n" +
            "--- 奥義の使い方 ---\n\n" +
            "ブロックを壊すと奥義ゲージが溜まる\n" +
            "ゲージが満タンになると\n" +
            "キャラアイコンが点滅して発動可能！\n" +
            "タップで奥義を発動！\n\n" +
            "キャラごとに奥義が異なるよ\n" +
            "・パワーバースト: 一定時間ダメージUP\n" +
            "・全体攻撃: 全ブロックにダメージ\n" +
            "・ストック回復 +N\n" +
            "・バリア: 次の1ミスをキャンセル\n" +
            "・貫通: 一定時間ボールがブロックを貫通\n" +
            "・分裂: ボールを2つに分裂\n" +
            "  （分裂したボールも再分裂可能）\n\n" +
            "--- キャラ編成 ---\n\n" +
            "3人まで編成可能！\n" +
            "パッシブスキルで戦力アップ！\n" +
            "強化・覚醒でさらに強くなる！\n\n" +
            "--- ガチャ ---\n\n" +
            "オーブを使ってキャラを入手！\n" +
            "同じキャラが出ると強化素材に\n" +
            "10連で SR 以上1体確定！";

        var helpTxt = MakeText(contentGo.transform, helpContent,
            30, Color.white, new Vector2(0.5f, 1f), new Vector2(650f, 2500f));
        helpTxt.alignment = TextAnchor.UpperCenter;
        helpTxt.lineSpacing = 1.1f;
        var helpTxtRT = helpTxt.GetComponent<RectTransform>();
        helpTxtRT.anchorMin = new Vector2(0.5f, 1f);
        helpTxtRT.anchorMax = new Vector2(0.5f, 1f);
        helpTxtRT.pivot = new Vector2(0.5f, 1f);
        helpTxtRT.anchoredPosition = new Vector2(0f, -20f);

        // 閉じるボタン
        var closeGo = new GameObject("CloseBtn");
        closeGo.transform.SetParent(dialog.transform, false);
        closeGo.AddComponent<Image>().color = new Color(0.6f, 0.25f, 0.8f, 0.6f);
        var closeBtn = closeGo.AddComponent<Button>();
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.04f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(260f, 70f);
        closeBtn.onClick.AddListener(() => Destroy(overlay));

        var closeInner = new GameObject("Inner");
        closeInner.transform.SetParent(closeGo.transform, false);
        closeInner.AddComponent<Image>().color = new Color(0.15f, 0.1f, 0.3f, 0.95f);
        var ciRt = closeInner.GetComponent<RectTransform>();
        ciRt.anchorMin = Vector2.zero; ciRt.anchorMax = Vector2.one;
        ciRt.offsetMin = new Vector2(3f, 3f); ciRt.offsetMax = new Vector2(-3f, -3f);

        var closeTxt = MakeText(closeGo.transform, "とじる", 34, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(240f, 60f));
        var cherryClose = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        if (cherryClose != null) closeTxt.font = cherryClose;
        closeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        closeTxt.verticalOverflow = VerticalWrapMode.Overflow;
        var clrt = closeTxt.GetComponent<RectTransform>();
        clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
        clrt.offsetMin = clrt.offsetMax = Vector2.zero;
    }

    // ---- アカウント連携ポップアップ ----

    void ShowAccountLinkPopup()
    {
        var overlay = new GameObject("LinkOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ外枠
        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(800f, 780f);

        // ダイアログ内側
        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        // タイトル
        var titleT = MakeText(dialog.transform, "アカウント連携", 40,
            new Color(1f, 0.85f, 0.1f), new Vector2(0.5f, 0.92f), new Vector2(700f, 60f));
        AddShadow(titleT.gameObject);

        // 現在のアカウント情報
        string uid = AuthManager.GetUID();
        string shortId = uid.Length > 8 ? uid.Substring(0, 8) + "..." : uid;
        string accountType = AuthManager.IsGuest() ? "ゲスト" : "メール認証済み";
        var infoT = MakeText(dialog.transform,
            $"アカウント: {accountType}\nID: {shortId}",
            24, new Color(0.7f, 0.8f, 0.9f), new Vector2(0.5f, 0.83f), new Vector2(700f, 70f));
        infoT.lineSpacing = 1.3f;

        if (!AuthManager.IsGuest())
        {
            // ===== 連携済み表示（登録アドレスを見せて打ち間違いに気づけるように） =====
            string linkedEmail = AuthManager.GetEmail();
            if (string.IsNullOrEmpty(linkedEmail) && AuthManager.GetName().Contains("@"))
                linkedEmail = AuthManager.GetName();
            var doneT = MakeText(dialog.transform,
                $"✓ メール連携済み\n{linkedEmail}\n\n機種変更の際は、上記メールアドレスと\nパスワードでデータを引き継げます。",
                26, new Color(0.5f, 1f, 0.6f), new Vector2(0.5f, 0.54f), new Vector2(700f, 200f));
            doneT.lineSpacing = 1.3f;

            // 複数端末同時プレイの注意書き（問い合わせ予防）
            var cautionT = MakeText(dialog.transform,
                "※複数端末での同時プレイはサポート外です\n（最後にプレイした端末のデータが保存されます）",
                22, new Color(0.6f, 0.6f, 0.7f), new Vector2(0.5f, 0.33f), new Vector2(700f, 80f));
            cautionT.lineSpacing = 1.3f;
        }
        else
        {
            // ===== ゲスト: メール連携フォーム =====
            var descT = MakeText(dialog.transform,
                "メールアドレスを登録すると、機種変更時に\nデータを引き継げるようになります。\n（Google連携は今後のアップデートで対応予定）",
                24, new Color(0.6f, 0.6f, 0.7f), new Vector2(0.5f, 0.73f), new Vector2(700f, 90f));
            descT.lineSpacing = 1.3f;

            var emailInput = MakeLinkInputField(dialog.transform, "メールアドレス",
                new Vector2(0.5f, 0.615f), false);
            var passInput = MakeLinkInputField(dialog.transform, "パスワード（6文字以上）",
                new Vector2(0.5f, 0.505f), true);

            // 結果表示テキスト（初期表示は同時プレイの注意書き。操作すると結果に置き換わる）
            var statusT = MakeText(dialog.transform, "※複数端末での同時利用はサポート外です",
                22, new Color(0.6f, 0.6f, 0.7f), new Vector2(0.5f, 0.30f), new Vector2(700f, 44f));

            // 連携するボタン
            var linkGo2 = new GameObject("DoLinkBtn");
            linkGo2.transform.SetParent(dialog.transform, false);
            linkGo2.AddComponent<Image>().color = new Color(0.3f, 0.8f, 0.5f, 0.6f);
            var linkBtn2 = linkGo2.AddComponent<Button>();
            var lrt2 = linkGo2.GetComponent<RectTransform>();
            lrt2.anchorMin = lrt2.anchorMax = new Vector2(0.5f, 0.395f);
            lrt2.anchoredPosition = Vector2.zero;
            lrt2.sizeDelta = new Vector2(340f, 78f);

            var linkInner2 = new GameObject("Inner");
            linkInner2.transform.SetParent(linkGo2.transform, false);
            linkInner2.AddComponent<Image>().color = new Color(0.12f, 0.5f, 0.28f, 0.95f);
            var liRt2 = linkInner2.GetComponent<RectTransform>();
            liRt2.anchorMin = Vector2.zero; liRt2.anchorMax = Vector2.one;
            liRt2.offsetMin = new Vector2(3f, 3f); liRt2.offsetMax = new Vector2(-3f, -3f);

            var linkTxt2 = MakeText(linkGo2.transform, "連携する", 32, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(320f, 60f));
            var cherryLink = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
            if (cherryLink != null) linkTxt2.font = cherryLink;
            linkTxt2.horizontalOverflow = HorizontalWrapMode.Overflow;
            linkTxt2.verticalOverflow = VerticalWrapMode.Overflow;
            var ltrt2 = linkTxt2.GetComponent<RectTransform>();
            ltrt2.anchorMin = Vector2.zero; ltrt2.anchorMax = Vector2.one;
            ltrt2.offsetMin = ltrt2.offsetMax = Vector2.zero;

            linkBtn2.onClick.AddListener(() =>
            {
                statusT.color = new Color(0.8f, 0.8f, 1f);
                statusT.text = "連携中...";
                linkBtn2.interactable = false;

                AuthManager.LinkWithEmail(emailInput.text.Trim(), passInput.text,
                    onSuccess: () =>
                    {
                        if (overlay == null) return; // ポップアップが閉じられていたら無視
                        statusT.color = new Color(0.5f, 1f, 0.6f);
                        statusT.text = "連携しました！確認メールを送信しました";
                        infoT.text = $"アカウント: メール認証済み\nID: {shortId}";
                        emailInput.interactable = false;
                        passInput.interactable = false;
                        // 登録控えの確認メール（届かない場合はアドレス打ち間違いに気づける）
                        AuthManager.SendVerificationEmail();
                        // 連携直後の状態をクラウドへ即バックアップ
                        CloudSaveManager.Save();
                    },
                    onFailed: (error) =>
                    {
                        if (overlay == null) return;
                        statusT.color = new Color(1f, 0.4f, 0.4f);
                        statusT.text = error;
                        linkBtn2.interactable = true;
                    });
            });

            // ===== 引き継ぎログイン（機種変更で連携済みアカウントを受け取る側の入口） =====
            var recvGo = new GameObject("ReceiveBtn");
            recvGo.transform.SetParent(dialog.transform, false);
            var recvBg = recvGo.AddComponent<Image>();
            recvBg.color = new Color(0f, 0f, 0f, 0f); // 透明（テキストボタン）
            var recvBtn = recvGo.AddComponent<Button>();
            var rrt = recvGo.GetComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.225f);
            rrt.anchoredPosition = Vector2.zero;
            rrt.sizeDelta = new Vector2(700f, 44f);

            var recvTxt = MakeText(recvGo.transform, "▶ 機種変更の引き継ぎはこちら（登録済みメールでログイン）",
                22, new Color(0.5f, 0.8f, 1f), new Vector2(0.5f, 0.5f), new Vector2(700f, 40f));
            var rtrt = recvTxt.GetComponent<RectTransform>();
            rtrt.anchorMin = Vector2.zero; rtrt.anchorMax = Vector2.one;
            rtrt.offsetMin = rtrt.offsetMax = Vector2.zero;

            recvBtn.onClick.AddListener(() =>
            {
                string email = emailInput.text.Trim();
                string pass = passInput.text;
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
                {
                    statusT.color = new Color(1f, 0.8f, 0.4f);
                    statusT.text = "上の欄にメールとパスワードを入力してから押してください";
                    return;
                }

                statusT.color = new Color(0.8f, 0.8f, 1f);
                statusT.text = "ログイン中...";
                recvBtn.interactable = false;
                linkBtn2.interactable = false;

                AuthManager.Login(email, pass,
                    onSuccess: () =>
                    {
                        if (overlay == null) return;
                        statusT.text = "データを引き継いでいます...";
                        // ログインしたアカウントのクラウドデータで上書き復元 → ホーム再読込
                        CloudSaveManager.Load(_ =>
                        {
                            SceneManager.LoadScene("HomeScene");
                        });
                    },
                    onFailed: (error) =>
                    {
                        if (overlay == null) return;
                        statusT.color = new Color(1f, 0.4f, 0.4f);
                        statusT.text = error;
                        recvBtn.interactable = true;
                        linkBtn2.interactable = true;
                    });
            });

            // ===== パスワード再設定（忘れた場合の救済） =====
            var resetGo = new GameObject("ResetBtn");
            resetGo.transform.SetParent(dialog.transform, false);
            var resetBg = resetGo.AddComponent<Image>();
            resetBg.color = new Color(0f, 0f, 0f, 0f); // 透明（テキストボタン）
            var resetBtn = resetGo.AddComponent<Button>();
            var resetRt = resetGo.GetComponent<RectTransform>();
            resetRt.anchorMin = resetRt.anchorMax = new Vector2(0.5f, 0.163f);
            resetRt.anchoredPosition = Vector2.zero;
            resetRt.sizeDelta = new Vector2(700f, 40f);

            var resetTxt = MakeText(resetGo.transform, "パスワードを忘れた方はこちら（再設定メールを送信）",
                20, new Color(0.75f, 0.65f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(700f, 36f));
            var resetTrt = resetTxt.GetComponent<RectTransform>();
            resetTrt.anchorMin = Vector2.zero; resetTrt.anchorMax = Vector2.one;
            resetTrt.offsetMin = resetTrt.offsetMax = Vector2.zero;

            resetBtn.onClick.AddListener(() =>
            {
                string email = emailInput.text.Trim();
                if (string.IsNullOrEmpty(email))
                {
                    statusT.color = new Color(1f, 0.8f, 0.4f);
                    statusT.text = "上のメール欄に登録済みアドレスを入力してから押してください";
                    return;
                }

                statusT.color = new Color(0.8f, 0.8f, 1f);
                statusT.text = "送信中...";
                AuthManager.SendPasswordReset(email,
                    onSuccess: () =>
                    {
                        if (overlay == null) return;
                        statusT.color = new Color(0.5f, 1f, 0.6f);
                        statusT.text = "再設定メールを送信しました。メールをご確認ください";
                    },
                    onFailed: (error) =>
                    {
                        if (overlay == null) return;
                        statusT.color = new Color(1f, 0.4f, 0.4f);
                        statusT.text = error;
                    });
            });
        }

        // 閉じるボタン
        var closeGo = new GameObject("CloseBtn");
        closeGo.transform.SetParent(dialog.transform, false);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.6f, 0.25f, 0.8f, 0.6f);
        var closeBtn = closeGo.AddComponent<Button>();
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.085f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(260f, 70f);
        closeBtn.onClick.AddListener(() => Destroy(overlay));

        var closeInner = new GameObject("Inner");
        closeInner.transform.SetParent(closeGo.transform, false);
        closeInner.AddComponent<Image>().color = new Color(0.15f, 0.1f, 0.3f, 0.95f);
        var ciRt = closeInner.GetComponent<RectTransform>();
        ciRt.anchorMin = Vector2.zero; ciRt.anchorMax = Vector2.one;
        ciRt.offsetMin = new Vector2(3f, 3f); ciRt.offsetMax = new Vector2(-3f, -3f);

        var closeTxt = MakeText(closeGo.transform, "とじる", 34, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(240f, 60f));
        var cherryClose = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        if (cherryClose != null) closeTxt.font = cherryClose;
        closeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        closeTxt.verticalOverflow = VerticalWrapMode.Overflow;
        var clrt = closeTxt.GetComponent<RectTransform>();
        clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
        clrt.offsetMin = clrt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// デイリーミッション進捗ポップアップ。
    /// 報酬はミッション達成の瞬間にプレゼントボックスへ自動付与されるため、
    /// ここは進捗の確認のみ（受け取り操作はない）。
    /// </summary>
    void ShowMissionPopup()
    {
        var overlay = new GameObject("MissionOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ外枠
        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.15f, 0.5f, 0.6f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(800f, 640f);

        // ダイアログ内側
        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        var titleT = MakeText(dialog.transform, "デイリーミッション", 40,
            new Color(1f, 0.85f, 0.1f), new Vector2(0.5f, 0.90f), new Vector2(700f, 60f));
        AddShadow(titleT.gameObject);

        MakeText(dialog.transform, "毎日 午前4:00 リセット", 22,
            new Color(0.6f, 0.6f, 0.7f), new Vector2(0.5f, 0.81f), new Vector2(700f, 30f));

        // ミッション3行
        var missions = DailyMissionManager.GetMissions();
        float[] rowYs = { 0.70f, 0.58f, 0.46f };
        for (int i = 0; i < missions.Length && i < rowYs.Length; i++)
        {
            var m = missions[i];

            var rowGo = new GameObject($"MissionRow{i}");
            rowGo.transform.SetParent(dialog.transform, false);
            rowGo.AddComponent<Image>().color = m.granted
                ? new Color(0.1f, 0.35f, 0.2f, 0.85f)   // 達成済み: 緑背景
                : new Color(0.12f, 0.1f, 0.28f, 0.85f);
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, rowYs[i]);
            rowRt.anchoredPosition = Vector2.zero;
            rowRt.sizeDelta = new Vector2(700f, 64f);

            var tT = MakeText(rowGo.transform, m.title, 26, Color.white,
                new Vector2(0.32f, 0.5f), new Vector2(400f, 40f));
            tT.alignment = TextAnchor.MiddleLeft;

            string right = m.granted ? "達成 ✓" : $"{m.current}/{m.target}";
            MakeText(rowGo.transform, right, 26,
                m.granted ? new Color(0.5f, 1f, 0.6f) : new Color(0.8f, 0.85f, 1f),
                new Vector2(0.76f, 0.5f), new Vector2(160f, 40f));

            MakeText(rowGo.transform, $"+{m.reward}", 24, new Color(0.4f, 0.9f, 1f),
                new Vector2(0.92f, 0.5f), new Vector2(100f, 40f));
        }

        // 全達成ボーナス行
        bool allDone = DailyMissionManager.AllCompleted();
        var bonusT = MakeText(dialog.transform,
            allDone
                ? $"全達成ボーナス +{DailyMissionManager.RewardAll}オーブ  達成 ✓"
                : $"全達成ボーナス +{DailyMissionManager.RewardAll}オーブ",
            26, allDone ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.85f, 0.3f),
            new Vector2(0.5f, 0.34f), new Vector2(700f, 40f));
        AddShadow(bonusT.gameObject);

        MakeText(dialog.transform, "達成した報酬はプレゼントボックスに届きます", 22,
            new Color(0.6f, 0.6f, 0.7f), new Vector2(0.5f, 0.25f), new Vector2(700f, 30f));

        // とじるボタン
        var closeGo = new GameObject("CloseBtn");
        closeGo.transform.SetParent(dialog.transform, false);
        closeGo.AddComponent<Image>().color = new Color(0.6f, 0.25f, 0.8f, 0.6f);
        var closeBtn = closeGo.AddComponent<Button>();
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.11f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(260f, 70f);
        closeBtn.onClick.AddListener(() => Destroy(overlay));

        var closeInner = new GameObject("Inner");
        closeInner.transform.SetParent(closeGo.transform, false);
        closeInner.AddComponent<Image>().color = new Color(0.15f, 0.1f, 0.3f, 0.95f);
        var ciRt = closeInner.GetComponent<RectTransform>();
        ciRt.anchorMin = Vector2.zero; ciRt.anchorMax = Vector2.one;
        ciRt.offsetMin = new Vector2(3f, 3f); ciRt.offsetMax = new Vector2(-3f, -3f);

        var closeTxt = MakeText(closeGo.transform, "とじる", 34, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(240f, 60f));
        var cherryClose = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        if (cherryClose != null) closeTxt.font = cherryClose;
        closeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        closeTxt.verticalOverflow = VerticalWrapMode.Overflow;
        var clrt = closeTxt.GetComponent<RectTransform>();
        clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
        clrt.offsetMin = clrt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// アカウント連携用の入力フィールドを生成（メール / パスワード）。
    /// モバイルではタップでソフトキーボードが自動表示される。
    /// </summary>
    InputField MakeLinkInputField(Transform parent, string placeholder, Vector2 anchor, bool isPassword)
    {
        var go = new GameObject(isPassword ? "PasswordInput" : "EmailInput");
        go.transform.SetParent(parent, false);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.10f, 0.28f, 1f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(620f, 72f);

        var input = go.AddComponent<InputField>();

        // プレースホルダー
        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(go.transform, false);
        var ph = phGo.AddComponent<Text>();
        ph.text = placeholder;
        ph.fontSize = 26;
        ph.color = new Color(0.5f, 0.5f, 0.62f);
        ph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ph.alignment = TextAnchor.MiddleLeft;
        var phRt = phGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(20f, 6f); phRt.offsetMax = new Vector2(-20f, -6f);

        // 入力テキスト
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.fontSize = 28;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleLeft;
        txt.supportRichText = false;
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(20f, 6f); txtRt.offsetMax = new Vector2(-20f, -6f);

        input.textComponent = txt;
        input.placeholder = ph;
        input.characterLimit = 64;
        input.contentType = isPassword
            ? InputField.ContentType.Password
            : InputField.ContentType.EmailAddress;
        return input;
    }

    // ---- 音量設定ポップアップ ----

    void ShowVolumePopup()
    {
        var am = AudioManager.Instance;
        if (am == null) return;

        // オーバーレイ
        var overlay = new GameObject("VolumeOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ外枠
        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(800f, 620f);

        // ダイアログ内側
        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        // タイトル
        var titleT = MakeText(dialog.transform, "音量設定", 40,
            new Color(1f, 0.85f, 0.1f), new Vector2(0.5f, 0.88f), new Vector2(700f, 60f));
        AddShadow(titleT.gameObject);

        // BGM スライダー
        CreateVolumeSlider(dialog.transform, "BGM", 0.68f, am.BGMVolume,
            (val) => am.SetBGMVolume(val));

        // SE スライダー
        CreateVolumeSlider(dialog.transform, "SE", 0.48f, am.SEVolume,
            (val) => am.SetSEVolume(val));

        // ボイス スライダー
        CreateVolumeSlider(dialog.transform, "ボイス", 0.28f, am.VoiceVolume,
            (val) => am.SetVoiceVolume(val));

        // 閉じるボタン
        var closeGo = new GameObject("CloseBtn");
        closeGo.transform.SetParent(dialog.transform, false);
        var closeOuterImg = closeGo.AddComponent<Image>();
        closeOuterImg.color = new Color(0.6f, 0.25f, 0.8f, 0.6f);
        var closeBtn = closeGo.AddComponent<Button>();
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.08f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(260f, 70f);
        closeBtn.onClick.AddListener(() => Destroy(overlay));

        // 閉じるボタン内側
        var closeInner = new GameObject("Inner");
        closeInner.transform.SetParent(closeGo.transform, false);
        closeInner.AddComponent<Image>().color = new Color(0.15f, 0.1f, 0.3f, 0.95f);
        var ciRt = closeInner.GetComponent<RectTransform>();
        ciRt.anchorMin = Vector2.zero; ciRt.anchorMax = Vector2.one;
        ciRt.offsetMin = new Vector2(3f, 3f); ciRt.offsetMax = new Vector2(-3f, -3f);

        var closeTxt = MakeText(closeGo.transform, "とじる", 34, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(240f, 60f));
        var cherryClose2 = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        if (cherryClose2 != null) closeTxt.font = cherryClose2;
        closeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        closeTxt.verticalOverflow = VerticalWrapMode.Overflow;
        var closeTxtRt = closeTxt.GetComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero; closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.offsetMin = closeTxtRt.offsetMax = Vector2.zero;
    }

    void CreateVolumeSlider(Transform parent, string label, float yAnchor,
        float initialValue, UnityEngine.Events.UnityAction<float> onChanged)
    {
        // ラベル（左寄せ）
        var labelT = MakeText(parent, label, 30, new Color(0.7f, 0.9f, 1f),
            new Vector2(0.13f, yAnchor + 0.1f), new Vector2(160f, 40f));
        AddShadow(labelT.gameObject);

        // パーセント表示（右寄せ、ラベルと同じ行）
        var pctT = MakeText(parent, $"{Mathf.RoundToInt(initialValue * 100)}%", 28,
            new Color(0.4f, 0.95f, 0.6f),
            new Vector2(0.88f, yAnchor + 0.1f), new Vector2(120f, 40f));

        // スライダー（ラベル行の下に配置）
        var sliderGo = new GameObject(label + "Slider");
        sliderGo.transform.SetParent(parent, false);
        var sliderRT = sliderGo.GetComponent<RectTransform>();
        if (sliderRT == null) sliderRT = sliderGo.AddComponent<RectTransform>();
        sliderRT.anchorMin = sliderRT.anchorMax = new Vector2(0.5f, yAnchor - 0.02f);
        sliderRT.anchoredPosition = Vector2.zero;
        sliderRT.sizeDelta = new Vector2(620f, 24f);

        // Background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        bgGo.AddComponent<Image>().color = new Color(0.15f, 0.1f, 0.25f, 0.9f);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(5f, 5f); faRt.offsetMax = new Vector2(-5f, -5f);

        // Fill
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillArea.transform, false);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.2f, 0.8f, 0.9f);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        // Handle Slide Area
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGo.transform, false);
        var haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(10f, 0f); haRt.offsetMax = new Vector2(-10f, 0f);

        // Handle
        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleArea.transform, false);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = new Color(1f, 0.85f, 0.3f, 1f);
        var handleRt = handleGo.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(26f, 36f);

        // Slider コンポーネント
        var slider = sliderGo.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = initialValue;

        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;

        // 色変化（ホバー時）
        var colors = slider.colors;
        colors.normalColor = new Color(1f, 0.85f, 0.3f, 1f);
        colors.highlightedColor = new Color(1f, 0.95f, 0.5f, 1f);
        colors.pressedColor = new Color(1f, 0.7f, 0.1f, 1f);
        slider.colors = colors;

        slider.onValueChanged.AddListener((val) =>
        {
            onChanged?.Invoke(val);
            if (pctT != null) pctT.text = $"{Mathf.RoundToInt(val * 100)}%";
        });
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
            // 白〜淡いピンクの光の粒
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

    // ---- メニューボタン共通ヘルパー（装飾付き） ----

    void MakeMenuButton(Transform parent, string label, Color baseCol, Color highlightCol,
        float y, string icon, UnityEngine.Events.UnityAction onClick)
    {
        // 外枠（明るい縁取り）
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        var outerImg = go.AddComponent<Image>();
        outerImg.color = new Color(highlightCol.r, highlightCol.g, highlightCol.b, 0.6f);
        var btn = go.AddComponent<Button>();
        var cg = go.AddComponent<CanvasGroup>();
        fadeButtons.Add(cg);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.12f, y);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(260f, 78f);
        btn.onClick.AddListener(onClick);

        // 内側背景（メインカラー＋グラデーション風）
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
        liT.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
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
        riT.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        var riRt = rightIcon.GetComponent<RectTransform>();
        riRt.anchorMin = riRt.anchorMax = new Vector2(1f, 0.5f);
        riRt.anchoredPosition = new Vector2(-28f, 0f);
        riRt.sizeDelta = new Vector2(30f, 30f);

        // ラベルテキスト（Shadow＋Outline付き）
        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 28; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 28);
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
    }

    // ---- プレゼントボタン（バッジ付き） ----

    void MakePresentButton(Transform parent, float y)
    {
        // MakeMenuButton と同じスタイルで作成
        Color baseCol = new Color(0.7f, 0.3f, 0.1f);
        Color highlightCol = new Color(0.95f, 0.5f, 0.2f);

        var go = new GameObject("PresentBtn");
        go.transform.SetParent(parent, false);
        var outerImg = go.AddComponent<Image>();
        outerImg.color = new Color(highlightCol.r, highlightCol.g, highlightCol.b, 0.6f);
        var btn = go.AddComponent<Button>();
        var cg = go.AddComponent<CanvasGroup>();
        fadeButtons.Add(cg);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.12f, y);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(260f, 78f);
        btn.onClick.AddListener(() => SceneManager.LoadScene("PresentBoxScene"));

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.92f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        // 上半分ハイライト
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        // 左側アイコン（他のボタンと同じく♡で統一）
        var liGo = new GameObject("LIcon");
        liGo.transform.SetParent(go.transform, false);
        var liT = liGo.AddComponent<Text>();
        liT.text = "♡"; liT.fontSize = 24;
        liT.color = new Color(1f, 1f, 1f, 0.7f);
        liT.alignment = TextAnchor.MiddleCenter;
        liT.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        var liRt = liGo.GetComponent<RectTransform>();
        liRt.anchorMin = liRt.anchorMax = new Vector2(0f, 0.5f);
        liRt.anchoredPosition = new Vector2(28f, 0f);
        liRt.sizeDelta = new Vector2(30f, 30f);

        // 右側アイコン（他のボタンと同じく♡で統一）
        var riGo = new GameObject("RIcon");
        riGo.transform.SetParent(go.transform, false);
        var riT = riGo.AddComponent<Text>();
        riT.text = "♡"; riT.fontSize = 24;
        riT.color = new Color(1f, 1f, 1f, 0.7f);
        riT.alignment = TextAnchor.MiddleCenter;
        riT.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        var riRt = riGo.GetComponent<RectTransform>();
        riRt.anchorMin = riRt.anchorMax = new Vector2(1f, 0.5f);
        riRt.anchoredPosition = new Vector2(-28f, 0f);
        riRt.sizeDelta = new Vector2(30f, 30f);

        // ラベルテキスト
        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = "プレゼント"; t.fontSize = 28; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 28);
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

        // ボタンShadow
        var btnShadow = go.AddComponent<Shadow>();
        btnShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        btnShadow.effectDistance = new Vector2(4f, -4f);

        // ===== 未受取バッジ =====
        int pendingCount = PresentBoxManager.GetPendingCount();
        if (pendingCount > 0)
        {
            var badgeGo = new GameObject("Badge");
            badgeGo.transform.SetParent(go.transform, false);
            badgeGo.AddComponent<Image>().color = new Color(1f, 0.15f, 0.15f, 1f);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = new Vector2(-10f, -8f);
            badgeRt.sizeDelta = new Vector2(44f, 44f);

            var badgeTxt = new GameObject("Num");
            badgeTxt.transform.SetParent(badgeGo.transform, false);
            var bt = badgeTxt.AddComponent<Text>();
            bt.text = pendingCount > 99 ? "99+" : pendingCount.ToString();
            bt.fontSize = 22; bt.color = Color.white;
            bt.alignment = TextAnchor.MiddleCenter;
            bt.fontStyle = FontStyle.Bold;
            bt.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
            var btrt = badgeTxt.GetComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero; btrt.anchorMax = Vector2.one;
            btrt.offsetMin = btrt.offsetMax = Vector2.zero;
        }
    }

    // ---- ファクトリーメソッド ----

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
}
