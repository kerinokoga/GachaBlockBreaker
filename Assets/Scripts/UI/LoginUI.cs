using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
/// <summary>
/// スプラッシュ画面：ロゴ2秒表示 → 自動ゲストログイン → 利用規約 → 年齢確認 → ホーム
/// </summary>
public class LoginUI : MonoBehaviour
{
    Text statusText;
    Transform rootCanvas;
    Transform mainCanvas;
    bool loginReady;
    bool splashDone;

    void Start()
    {
        OrbManager.EnsureStarterCharacters();
        BuildSplashScreen();
        BuildMainCanvas();

        // スプラッシュ2秒 + Firebase初期化を並行
        StartCoroutine(SplashTimer());

        // Firebase 初期化（同期例外で止まらないようガード。
        // Android のネイティブ初期化失敗はここで throw されることがある）
        try
        {
            InitFirebase();
        }
        catch (System.Exception e)
        {
            // TypeInitializationException 等は InnerException に真因が入るため全体を出力
            Debug.LogWarning($"[Login] Firebase 初期化で例外: {e}");
        }

        // 認証が一定時間終わらなくてもオフラインで必ず先へ進むフォールバック
        StartCoroutine(LoginWatchdog());
    }

    /// <summary>
    /// 認証が終わらない端末でもゲームを止めないための監視。
    /// スプラッシュ後は進行状況を表示し、12秒で諦めてオフライン進行する。
    /// </summary>
    IEnumerator LoginWatchdog()
    {
        float elapsed = 0f;
        const float giveUp = 12f;

        while (!loginReady && elapsed < giveUp)
        {
            elapsed += Time.unscaledDeltaTime;

            // スプラッシュが明けても認証中なら状況を表示（無言のフリーズに見せない）
            if (splashDone && statusText != null && !loginReady)
            {
                ShowMainCanvas();
                statusText.color = new Color(0.7f, 0.8f, 1f);
                statusText.text = $"サーバー接続中... ({Mathf.CeilToInt(giveUp - elapsed)})";
            }
            yield return null;
        }

        if (!loginReady)
        {
            // オフライン進行（クラウドセーブ・ランキングは自動でスキップされる設計）
            Debug.LogWarning("[Login] 認証タイムアウト。オフラインで進行します");
            if (statusText != null)
            {
                statusText.color = new Color(1f, 0.8f, 0.4f);
                statusText.text = "オフラインで開始します";
            }
            loginReady = true;
            TryProceed();
        }
    }

    IEnumerator SplashTimer()
    {
        yield return new WaitForSeconds(3.5f);
        splashDone = true;
        TryProceed();
    }

    void InitFirebase()
    {
        AuthManager.Initialize(
            onReady: () =>
            {
                if (AuthManager.IsLoggedIn)
                {
                    loginReady = true;
                    TryProceed();
                }
                else
                {
                    AutoGuestLogin();
                }
            },
            onFailed: (error) =>
            {
                // スプラッシュが終わるまで待ってからエラー表示
                StartCoroutine(ShowErrorAfterSplash());
            }
        );
    }

    IEnumerator ShowErrorAfterSplash()
    {
        while (!splashDone) yield return null;
        ShowMainCanvas();
        statusText.color = new Color(1f, 0.4f, 0.4f);
        statusText.text = "サーバー接続に失敗しました";
    }

    void AutoGuestLogin()
    {
        AuthManager.LoginAsGuest(
            onSuccess: () =>
            {
                loginReady = true;
                TryProceed();
            },
            onFailed: (error) =>
            {
                StartCoroutine(ShowErrorAfterSplash());
            }
        );
    }

    bool proceeded = false; // 二重進行ガード

    void TryProceed()
    {
        // スプラッシュ2秒 + ログイン完了の両方が揃ったら進む
        if (!splashDone || !loginReady) return;
        if (proceeded) return;
        proceeded = true;
        StartCoroutine(ProceedWithRestore());
    }

    IEnumerator ProceedWithRestore()
    {
        // 機種変更・再インストール時のクラウド復元
        // （ローカルが初期状態のときだけ実行。通常起動では即コールバックされる）
        bool done = false;
        CloudSaveManager.LoadIfFreshDevice(restored =>
        {
            if (restored)
                Debug.Log("[CloudSave] クラウドセーブから進行状況を復元しました");
            done = true;
        });

        // 端末側の Firestore 不調で応答が無くても 6 秒で必ず先へ進む（タイトル画面での固まり防止）
        float timeout = 6f;
        while (!done && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (!done)
            Debug.LogWarning("[CloudSave] 復元チェックがタイムアウト。スキップして進行します");

        if (!AgeVerificationManager.IsSetupComplete)
        {
            ShowMainCanvas();
            ShowTermsAndAgePopup();
        }
        else
        {
            SceneManager.LoadScene("HomeScene");
        }
    }

    void ShowMainCanvas()
    {
        // スプラッシュを消してメインキャンバスを表示
        var splash = GameObject.Find("SplashCanvas");
        if (splash != null) splash.SetActive(false);
        mainCanvas.gameObject.SetActive(true);
    }

    // ===== スプラッシュ画面（白背景 + ロゴ中央） =====

    void BuildSplashScreen()
    {
        var cGo = new GameObject("SplashCanvas");
        var c = cGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0.0f;
        cGo.AddComponent<GraphicRaycaster>();

        var root = cGo.transform;

        // 白背景
        var bg = new GameObject("BG");
        bg.transform.SetParent(root, false);
        bg.AddComponent<Image>().color = Color.white;
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = bgrt.offsetMax = Vector2.zero;

        // タイトル（1文字ずつ上から落下）
        string titleStr = "ぶろっくぶれいかー♡";
        float titleY = 0.70f;
        float totalWidth = titleStr.Length * 75f;
        float startX = -totalWidth / 2f + 37.5f;
        var titleFont = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        if (titleFont != null)
            titleFont.RequestCharactersInTexture(titleStr, 72);
        else
            Debug.LogWarning("[Splash] フォント読み込み失敗: Fonts/CherryBombOne-Regular");
        for (int i = 0; i < titleStr.Length; i++)
        {
            var charGo = new GameObject("Char_" + i);
            charGo.transform.SetParent(root, false);
            var charT = charGo.AddComponent<Text>();
            charT.text = titleStr[i].ToString();
            bool isKanji = titleStr[i] >= '\u4E00' && titleStr[i] <= '\u9FFF';
            charT.fontSize = isKanji ? 64 : 82;
            charT.color = new Color(1f, 0.85f, 0.1f, 0f);
            charT.alignment = TextAnchor.MiddleCenter;
            charT.font = titleFont != null ? titleFont : Font.CreateDynamicFontFromOSFont("Arial", 72);
            charT.horizontalOverflow = HorizontalWrapMode.Overflow;
            charT.verticalOverflow = VerticalWrapMode.Overflow;
            charT.raycastTarget = false;
            var charShadow = charGo.AddComponent<Shadow>();
            charShadow.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.8f);
            charShadow.effectDistance = new Vector2(3f, -3f);
            var charOutline = charGo.AddComponent<Outline>();
            charOutline.effectColor = new Color(0.8f, 0.2f, 0.4f, 0.9f);
            charOutline.effectDistance = new Vector2(2f, -2f);
            var charRt = charGo.GetComponent<RectTransform>();
            charRt.anchorMin = charRt.anchorMax = new Vector2(0.5f, titleY);
            charRt.anchoredPosition = new Vector2(startX + i * 75f, 400f); // 上から
            charRt.sizeDelta = new Vector2(80f, 90f);
            StartCoroutine(DropInChar(charRt, charT, startX + i * 75f, i * 0.08f));
        }

        // タイトルボイス（ランダムキャラの voiceTitle を再生）
        // 1文字目が落下し始めるタイミング（0.2秒後）と同期
        StartCoroutine(PlayRandomTitleVoice(0.2f));

        // ロゴ（中央やや下・フェードイン 0.2秒後から）
        var logoSprite = Resources.Load<Sprite>("Home/logo");
        if (logoSprite != null)
        {
            var logoGo = new GameObject("Logo");
            logoGo.transform.SetParent(root, false);
            var logoImg = logoGo.AddComponent<Image>();
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;
            logoImg.color = new Color(1f, 1f, 1f, 0f);
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = logoRt.anchorMax = new Vector2(0.5f, 0.40f);
            logoRt.anchoredPosition = Vector2.zero;
            logoRt.sizeDelta = new Vector2(650f, 650f);
            StartCoroutine(FadeInLogo(logoImg));
        }

        // コピーライト
        var copyGo = new GameObject("Txt");
        copyGo.transform.SetParent(root, false);
        var copyT = copyGo.AddComponent<Text>();
        copyT.text = "© 2026 Kerino Game";
        copyT.fontSize = 48;
        copyT.color = new Color(0.5f, 0.5f, 0.5f, 0f); // 最初は透明
        copyT.alignment = TextAnchor.MiddleCenter;
        copyT.font = Font.CreateDynamicFontFromOSFont("Arial", 34);
        copyT.raycastTarget = false;
        var crt = copyGo.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.05f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(500f, 50f);
        StartCoroutine(FadeInText(copyT));
    }

    /// <summary>
    /// タイトル画面で全20キャラの voiceTitle からランダムに1つ選んで再生する。
    /// CharacterData.voiceVolumeMultiplier を反映。優先度Highで他に邪魔されない。
    /// </summary>
    IEnumerator PlayRandomTitleVoice(float delay)
    {
        yield return new WaitForSeconds(delay);

        var allChars = Resources.LoadAll<CharacterData>("Characters");
        if (allChars == null || allChars.Length == 0) yield break;

        // voiceTitle が設定されているキャラだけを候補に
        var candidates = new System.Collections.Generic.List<CharacterData>();
        foreach (var cd in allChars)
        {
            if (cd != null && cd.voiceTitle != null) candidates.Add(cd);
        }
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[Splash] voiceTitle が割当てられたキャラが1人もいません。");
            yield break;
        }

        var picked = candidates[Random.Range(0, candidates.Count)];
        AudioManager.Instance?.PlayVoice(
            picked.voiceTitle,
            picked.voiceVolumeMultiplier,
            AudioManager.VoicePriority.High);
        Debug.Log($"[Splash] タイトルボイス再生: {picked.characterName}");
    }

    IEnumerator DropInChar(RectTransform rt, Text txt, float targetX, float delay)
    {
        yield return new WaitForSeconds(0.2f + delay); // 0.2秒 + 文字ごとのディレイ
        float duration = 0.8f;
        float t = 0f;
        float startY = 400f;
        float endY = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // EaseOutCubic
            float bounceY = Mathf.Lerp(startY, endY, ease);
            // 着地時に小さくバウンド
            if (t > 0.8f)
            {
                float bounceT = (t - 0.8f) / 0.2f;
                bounceY += Mathf.Sin(bounceT * Mathf.PI) * 8f;
            }
            rt.anchoredPosition = new Vector2(targetX, bounceY);
            txt.color = new Color(1f, 0.85f, 0.1f, Mathf.Clamp01(t * 2f));
            yield return null;
        }
        rt.anchoredPosition = new Vector2(targetX, endY);
        txt.color = new Color(1f, 0.85f, 0.1f, 1f);
    }

    IEnumerator FadeInLogo(Image img)
    {
        yield return new WaitForSeconds(0.2f); // 0.2秒後から開始
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.2f;
            img.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t));
            yield return null;
        }
        img.color = new Color(1f, 1f, 1f, 1f);
    }

    IEnumerator FadeInText(Text txt)
    {
        yield return new WaitForSeconds(0.6f); // 文字が落ち終わった後
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            txt.color = new Color(0.5f, 0.5f, 0.5f, Mathf.Clamp01(t));
            yield return null;
        }
    }

    // ===== メインキャンバス（利用規約・年齢確認用 / エラー表示用） =====

    void BuildMainCanvas()
    {
        var cGo = new GameObject("MainCanvas");
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

        var root = cGo.transform;
        rootCanvas = root;
        mainCanvas = root;

        // 背景（暗い紫）
        MakeBg(root, new Color(0.03f, 0.02f, 0.1f));

        // ステータスメッセージ
        statusText = MakeText(root, "", 28, new Color(0.7f, 0.7f, 0.9f),
            new Vector2(0.5f, 0.5f), new Vector2(700f, 80f));

        // 初期状態は非表示
        cGo.SetActive(false);
    }

    // ===== 利用規約同意 + 年齢確認ポップアップ =====

    void ShowTermsAndAgePopup()
    {
        if (!AgeVerificationManager.HasAgreedToTerms)
            ShowTermsPopup();
        else
            ShowAgePopup();
    }

    void ShowTermsPopup()
    {
        var ov = new GameObject("TermsOverlay");
        ov.transform.SetParent(rootCanvas, false);
        ov.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        var ort = ov.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(ov.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(900f, 900f);

        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        var titleT = MakeText(dialog.transform, "利用規約・プライバシーポリシー", 36,
            new Color(1f, 0.85f, 0.1f), new Vector2(0.5f, 0.92f), new Vector2(850f, 50f));
        AddShadow(titleT.gameObject);

        var desc = MakeText(dialog.transform,
            "本アプリをご利用いただくには、\n利用規約およびプライバシーポリシーへの\n同意が必要です。\n\n以下のリンクから内容をご確認ください。",
            26, new Color(0.8f, 0.8f, 0.9f), new Vector2(0.5f, 0.72f), new Vector2(800f, 200f));
        desc.lineSpacing = 1.3f;

        // URLボタン（タップでブラウザを開く）
        var urlBtnGo = new GameObject("UrlBtn");
        urlBtnGo.transform.SetParent(dialog.transform, false);
        urlBtnGo.AddComponent<Image>().color = new Color(0.15f, 0.2f, 0.4f, 0.6f);
        var urlBtn = urlBtnGo.AddComponent<Button>();
        var urlBtnRt = urlBtnGo.GetComponent<RectTransform>();
        urlBtnRt.anchorMin = urlBtnRt.anchorMax = new Vector2(0.5f, 0.52f);
        urlBtnRt.anchoredPosition = Vector2.zero;
        urlBtnRt.sizeDelta = new Vector2(750f, 100f);
        urlBtn.onClick.AddListener(() => Application.OpenURL("https://kerinokoga.github.io/GachaBlockBreaker/legal.html"));
        var urlText = MakeText(urlBtnGo.transform, "▶ 利用規約を確認する", 30,
            new Color(0.5f, 0.8f, 1f), new Vector2(0.5f, 0.5f), new Vector2(700f, 60f));
        urlText.raycastTarget = false;
        var urlTrt = urlText.GetComponent<RectTransform>();
        urlTrt.anchorMin = Vector2.zero; urlTrt.anchorMax = Vector2.one;
        urlTrt.offsetMin = urlTrt.offsetMax = Vector2.zero;

        MakeText(dialog.transform,
            "※ 未成年の方は保護者の同意を得たうえで\n　 ご利用ください",
            24, new Color(1f, 0.6f, 0.4f), new Vector2(0.5f, 0.40f), new Vector2(800f, 70f));

        MakePopupButton(dialog.transform, "同意してつづける",
            new Color(0.15f, 0.5f, 0.2f), new Color(0.3f, 0.7f, 0.35f),
            new Vector2(0.5f, 0.22f), new Vector2(500f, 85f),
            () =>
            {
                AgeVerificationManager.HasAgreedToTerms = true;
                Destroy(ov);
                ShowAgePopup();
            });

        MakePopupButton(dialog.transform, "同意しない",
            new Color(0.4f, 0.15f, 0.15f), new Color(0.6f, 0.3f, 0.3f),
            new Vector2(0.5f, 0.10f), new Vector2(500f, 75f),
            () =>
            {
                AuthManager.Logout();
                Destroy(ov);
                statusText.color = new Color(1f, 0.6f, 0.4f);
                statusText.text = "利用規約への同意が必要です\nアプリを再起動してください";
            });
    }

    void ShowAgePopup()
    {
        var ov = new GameObject("AgeOverlay");
        ov.transform.SetParent(rootCanvas, false);
        ov.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        var ort = ov.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(ov.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(900f, 850f);

        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        var titleT = MakeText(dialog.transform, "年齢確認", 42,
            new Color(1f, 0.85f, 0.1f), new Vector2(0.5f, 0.92f), new Vector2(700f, 60f));
        AddShadow(titleT.gameObject);

        var desc = MakeText(dialog.transform,
            "本アプリにはアプリ内課金が含まれます。\n年齢に応じた課金制限を設定するため、\nあなたの年齢を選択してください。",
            26, new Color(0.8f, 0.8f, 0.9f), new Vector2(0.5f, 0.78f), new Vector2(800f, 130f));
        desc.lineSpacing = 1.3f;

        var limitDesc = MakeText(dialog.transform,
            "16歳未満：月額 ¥5,000 まで\n16〜19歳：月額 ¥20,000 まで\n20歳以上：制限なし",
            24, new Color(0.6f, 0.85f, 1f), new Vector2(0.5f, 0.62f), new Vector2(700f, 110f));
        limitDesc.lineSpacing = 1.4f;

        MakePopupButton(dialog.transform, "16歳未満",
            new Color(0.2f, 0.4f, 0.6f), new Color(0.3f, 0.55f, 0.8f),
            new Vector2(0.5f, 0.44f), new Vector2(500f, 85f),
            () => { AgeVerificationManager.AgeGroup = 1; Destroy(ov); SceneManager.LoadScene("HomeScene"); });

        MakePopupButton(dialog.transform, "16歳 〜 19歳",
            new Color(0.2f, 0.5f, 0.3f), new Color(0.35f, 0.7f, 0.4f),
            new Vector2(0.5f, 0.30f), new Vector2(500f, 85f),
            () => { AgeVerificationManager.AgeGroup = 2; Destroy(ov); SceneManager.LoadScene("HomeScene"); });

        MakePopupButton(dialog.transform, "20歳以上",
            new Color(0.5f, 0.2f, 0.5f), new Color(0.7f, 0.35f, 0.7f),
            new Vector2(0.5f, 0.16f), new Vector2(500f, 85f),
            () => { AgeVerificationManager.AgeGroup = 3; Destroy(ov); SceneManager.LoadScene("HomeScene"); });
    }

    // ---- ポップアップボタン ----

    void MakePopupButton(Transform parent, string label, Color baseCol, Color highlightCol,
        Vector2 anchor, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(highlightCol.r, highlightCol.g, highlightCol.b, 0.6f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        btn.onClick.AddListener(onClick);

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.92f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 34; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 34);
        var txtShadow = txtGo.AddComponent<Shadow>();
        txtShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        txtShadow.effectDistance = new Vector2(2f, -2f);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var btnShadow = go.AddComponent<Shadow>();
        btnShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        btnShadow.effectDistance = new Vector2(4f, -4f);
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

    void AddShadow(GameObject go)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.6f);
        s.effectDistance = new Vector2(2f, -2f);
    }
}
