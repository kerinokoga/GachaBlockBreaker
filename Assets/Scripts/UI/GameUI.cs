using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム中の UI をランタイムで構築・管理する
/// </summary>
public class GameUI : MonoBehaviour
{
    // ランタイムで作成した参照
    private const int MaxStockDisplay = 7;
    private MaskableGraphic[] stockHearts = new MaskableGraphic[MaxStockDisplay];
    private static Sprite heartSprite;
    private Slider destroyRateSlider;
    private Text destroyRateText;
    private Text endlessScoreText;  // エンドレスの撃破数表示（エンドレス時のみ生成）
    private GameObject pauseMenuPanel;

    // Phase 2: 奥義UI（アイコンをタップで発動）
    private Slider[]     ultGaugeSliders = new Slider[3];     // 互換用・未使用
    private GameObject[] ultButtons      = new GameObject[3]; // 互換用・未使用
    private Image[]      ultIconImages   = new Image[3];      // アイコンの Image（alpha で進捗表示）
    private Button[]     ultIconButtons  = new Button[3];     // アイコン自体の Button
    private GameObject[] ultTapLabels    = new GameObject[3]; // 「タップ」ラベル

    private Color activeColor   = new Color(1f, 0.9f, 0.2f);
    private Text criticalText;
    private Text speedUpText;

    // ダメージ表示
    private Text damageText;

    // ボス攻撃通知表示
    private Text bossActionText;
    private GameObject bossActionPanel;
    private Coroutine bossActionCoroutine;

    // ボスターン表示
    private GameObject bossTurnPanel;
    private Text bossTurnText;
    private bool bossUIInitialized = false;
    private bool bossTurnBlink = false;
    private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

    // チュートリアル参照用
    private Transform canvasRoot;
    private bool ultTutorialShown = false;          // 段階5-C 起動済みフラグ
    private TutorialOverlay currentUltTutorialOverlay; // 段階5-C Part1 のオーバーレイ参照

    // 持続系奥義タイマーの縦積み管理（重複しても重ならないように）
    private readonly System.Collections.Generic.List<RectTransform> activeUltTimers
        = new System.Collections.Generic.List<RectTransform>();
    private const float UltTimerSpacing = 50f;       // 行間（px）

    void Start()
    {
        BuildUI();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStockChanged    += UpdateStockDisplay;
            GameManager.Instance.OnDestroyRateChanged += UpdateDestroyRate;
            // 段階5-D / 5-E: チュートリアル復活＆打数補充イベント
            GameManager.Instance.OnTutorialRevive      += OnTutorialReviveHandler;
            GameManager.Instance.OnTutorialTurnsRefill += OnTutorialTurnsRefillHandler;
            UpdateStockDisplay(GameManager.Instance.MaxStock);
            UpdateDestroyRate(0f);
        }

        // Phase 2: 奥義ゲージイベント購読
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnGaugeChanged += UpdateUltGauge;
            CharacterManager.Instance.OnUltReady     += ShowUltButton;
            // 段階5-C: 奥義チュートリアル用
            CharacterManager.Instance.OnUltReady     += TryShowUltTutorial;
            CharacterManager.Instance.OnUltUsed      += OnUltUsedForTutorial;
            // 奥義発動バナー（常時、チュートリアル外でも表示）
            CharacterManager.Instance.OnUltUsed      += ShowUltBanner;
        }

        // クリティカル表示購読
        var ball = FindObjectOfType<BallController>();
        if (ball != null)
            ball.OnCriticalHit += ShowCriticalText;

        // SpeedUp表示購読
        SpeedBlock.OnSpeedUp += ShowSpeedUpText;

        // チュートリアル進捗：CharaSelect 段階で到達したら GameScreen 段階へ
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.CharaSelect)
        {
            TutorialManager.Instance.SetStep(TutorialManager.Step.GameScreen);
            Debug.Log("[Tutorial] GameScreen 段階へ進行");
            // 段階5: ボスポップアップ完了後（約3.2秒）にチュートリアル開始
            StartCoroutine(ShowGameScreenGuideAfterDelay(3.2f));
        }
    }

    // ============================================================
    // 段階5: ゲーム画面のチュートリアル（残り打数 → 操作系総合）
    // ============================================================

    System.Collections.IEnumerator ShowGameScreenGuideAfterDelay(float delay)
    {
        // 実時間で待機（ボスポップアップは timeScale=1 で進行中）
        yield return new WaitForSecondsRealtime(delay);

        // ゲーム一時停止（spec通り）
        Time.timeScale = 0f;

        ShowGameScreenGuide_Page1();
    }

    /// <summary>
    /// 段階5-Page1: 残り打数の説明（bossTurnPanel をスポットライト）
    /// </summary>
    void ShowGameScreenGuide_Page1()
    {
        if (canvasRoot == null) { Time.timeScale = 1f; return; }

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        // 残り打数表示(bossTurnPanel)の実 RectTransform から枠を算出（アスペクト比非依存）
        Vector2 turnsCenter = new Vector2(0.5f, 0.8975f);
        var turnsRT = (bossTurnPanel != null && bossTurnPanel.activeInHierarchy)
            ? bossTurnPanel.GetComponent<RectTransform>() : null;
        if (turnsRT != null)
        {
            turnsCenter = overlay.HighlightTarget(turnsRT, 12f, new Color(1f, 0.9f, 0.2f));
        }
        else
        {
            // フォールバック：固定座標（パネル未活性時のみ）
            Vector2 turnsMin = new Vector2(0.18f, 0.870f);
            Vector2 turnsMax = new Vector2(0.82f, 0.925f);
            overlay.ShowSpotlight(turnsMin, turnsMax);
            overlay.AddHighlightFrame(turnsMin, turnsMax, new Color(1f, 0.9f, 0.2f), 10f);
        }

        // 吹き出しは画面中央に配置
        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.45f),
            new Vector2(0.95f, 0.65f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "画面の『残り打数』を見なさい！\n" +
            "これがあんたがパドルで打てる回数よ\n" +
            "ゼロになる前にブロック全部壊しなさい！");

        // 矢印を残り打数表示の下に配置
        overlay.AddArrowAt(new Vector2(turnsCenter.x, Mathf.Max(turnsCenter.y - 0.07f, 0.05f)), "▲");

        // 専用ボイス（Tutorial/turns.wav）
        AudioClip turnsVoice = Resources.Load<AudioClip>("Tutorial/turns");
        if (turnsVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                turnsVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowContinue("次へ", () =>
        {
            overlay.Close();
            ShowGameScreenGuide_Page2();
        });

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            Time.timeScale = 1f;
        });
    }

    /// <summary>
    /// 段階5-Page2: 操作系の総合解説（フルダム、テキストのみ）
    /// </summary>
    void ShowGameScreenGuide_Page2()
    {
        if (canvasRoot == null) { Time.timeScale = 1f; return; }

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();
        // 全画面 dim（スポットライト無し）

        // 吹き出しは長文に合わせて広めに配置（左寄せで読みやすく）
        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.16f),
            new Vector2(0.95f, 0.84f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "操作箇所の説明をまとめといてあげたわ\n" +
            "感謝しなさい\n" +
            "\n" +
            "・ボール発射\n" +
            "　まずはタップしてボールを発射しなさい\n" +
            "\n" +
            "・パドル\n" +
            "　ドラッグしてボールを打つのよ\n" +
            "　パドルの真ん中でボールを打つと\n" +
            "　いいことあるわよ\n" +
            "\n" +
            "・左上のハート\n" +
            "　大事な命よ\n" +
            "　ボールが落下したら1つ減るの\n" +
            "　ハートがなくなったらゲームオーバーよ\n" +
            "\n" +
            "・左上のヒットダメージ\n" +
            "　ブロックに与えるダメージ量よ\n" +
            "　キャラの能力で増えるわ\n" +
            "\n" +
            "・左下のキャラアイコン\n" +
            "　光ったらタップして奥義を使うのよ\n" +
            "\n" +
            "集中して全部壊しなさいよ！");

        // 専用ボイス（Tutorial/game.wav）
        AudioClip gameVoice = Resources.Load<AudioClip>("Tutorial/game");
        if (gameVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                gameVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowContinue("わかった", () =>
        {
            overlay.Close();
            // ゲーム再開
            Time.timeScale = 1f;
            // 段階を GamePlay へ進める（5-C/5-D/5-E のトリガ待ち）
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.SetStep(TutorialManager.Step.GamePlay);
            Debug.Log("[Tutorial] GamePlay 段階へ進行");

            // 段階5-C 視覚デモ用：「わかった」押下から 5秒後にアリアの奥義ゲージを強制満タン
            // ユーザーがプレイ感覚を掴むための猶予を与える
            StartCoroutine(ForceFillAriaGaugeAfterDelay(5.0f));
        });

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            Time.timeScale = 1f;
        });
    }

    /// <summary>
    /// 段階5-C 視覚デモ用：「わかった」押下から指定秒数後にアリアの奥義ゲージを満タンにする。
    /// ユーザーが少しプレイした後に発動するので操作感を掴みやすい。
    /// </summary>
    System.Collections.IEnumerator ForceFillAriaGaugeAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (CharacterManager.Instance == null) yield break;

        int ariaSlot = CharacterManager.Instance.FindSlotByCharacterName("アリア");
        if (ariaSlot >= 0)
        {
            CharacterManager.Instance.ForceFillGauge(ariaSlot);
            Debug.Log($"[Tutorial] アリアの奥義ゲージを強制満タン (slot={ariaSlot})");
        }
        else
        {
            Debug.Log("[Tutorial] アリア未装備のため段階5-C は通常通り (誰か満タンになるまで待機)");
        }
    }

    // ============================================================
    // 奥義発動バナー（何が起きたかを画面上部に短時間表示）
    // ============================================================

    /// <summary>
    /// OnUltUsed から呼ばれる。発動キャラ名 + 効果説明をバナー表示する。
    /// ボールの邪魔をしないよう、上部配置・背景無し・短時間・クリック透過。
    /// </summary>
    void ShowUltBanner(int slot)
    {
        if (canvasRoot == null || CharacterManager.Instance == null) return;
        var cd = CharacterManager.Instance.GetSelectedChar(slot);
        if (cd == null) return;

        string effectLine = GetUltEffectText(cd);
        StartCoroutine(UltBannerCoroutine($"{cd.characterName}の奥義！", effectLine));

        // 持続系奥義は効果内容 + 残り時間も表示（案D）
        if (cd.ultimateType == UltimateSkillType.PowerBurst)
        {
            StartCoroutine(UltEffectTimerCoroutine(
                $"ダメージ+{(cd.ultimateValue - 1f) * 100f:0}%", cd.ultimateDuration));
        }
        else if (cd.ultimateType == UltimateSkillType.Penetrate)
        {
            StartCoroutine(UltEffectTimerCoroutine(
                "ボール貫通", cd.ultimateDuration));
        }
    }

    /// <summary>奥義タイプから効果説明文を生成</summary>
    string GetUltEffectText(CharacterData cd)
    {
        switch (cd.ultimateType)
        {
            case UltimateSkillType.PowerBurst:
                return $"{cd.ultimateDuration:0.#}秒間 ダメージ+{(cd.ultimateValue - 1f) * 100f:0}%！";
            case UltimateSkillType.MassDestroy:
                return $"全ブロックに {cd.ultimateValue:0.#} ダメージ！";
            case UltimateSkillType.StockRecover:
                return $"ストック +{cd.ultimateValue:0.#} 回復！";
            case UltimateSkillType.BarrierShot:
                return "次のミスを1回防ぐ！";
            case UltimateSkillType.Penetrate:
                return $"{cd.ultimateDuration:0.#}秒間 ボール貫通！";
            case UltimateSkillType.BallSplit:
                return "ボールが2つに分裂！";
            default:
                return "";
        }
    }

    System.Collections.IEnumerator UltBannerCoroutine(string title, string effect)
    {
        // 親（2行まとめ）
        var go = new GameObject("UltBanner");
        go.transform.SetParent(canvasRoot, false);
        go.transform.SetAsLastSibling();
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.80f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900f, 160f);

        // タイトル行（キャラ名の奥義！）
        var titleT = MakeBannerText(go.transform, title, 60,
            new Color(1f, 0.85f, 0.25f), new Vector2(0.5f, 0.72f));
        // 効果行
        var effectT = MakeBannerText(go.transform, effect, 44,
            Color.white, new Vector2(0.5f, 0.26f));

        // フェードイン 0.25s → 静止 1.2s → フェードアウト 0.3s（実時間）
        float fadeIn = 0.25f, hold = 1.2f, fadeOut = 0.3f;
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeIn);
            SetBannerAlpha(titleT, effectT, k);
            rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k);
            yield return null;
        }
        yield return new WaitForSecondsRealtime(hold);
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            SetBannerAlpha(titleT, effectT, 1f - Mathf.Clamp01(t / fadeOut));
            yield return null;
        }
        Destroy(go);
    }

    Text MakeBannerText(Transform parent, string text, int fontSize, Color color, Vector2 anchor)
    {
        var go = new GameObject("BannerTxt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = new Color(color.r, color.g, color.b, 0f); // 初期透明
        t.alignment = TextAnchor.MiddleCenter;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false; // 操作を妨げない
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
        ol.effectDistance = new Vector2(3f, -3f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900f, 70f);
        return t;
    }

    void SetBannerAlpha(Text a, Text b, float alpha)
    {
        if (a != null) { var c = a.color; c.a = alpha; a.color = c; }
        if (b != null) { var c = b.color; c.a = alpha; b.color = c; }
    }

    /// <summary>
    /// 持続系奥義（PowerBurst / Penetrate）の効果内容 + 残り時間を表示。
    /// ボスアイコン（中央上部）と被らないよう右側に配置。
    /// ゲーム内時間で減少（一時停止中は止まる）。
    /// </summary>
    // 効果タイマーの世代番号。ClearUltEffectTimers で進めると、
    // 古い世代のタイマー表示は次のフレームで自動的に消える
    int ultTimerGeneration = 0;

    /// <summary>効果タイマー表示を全て消す（裏ステージ突入など、効果の強制リセット時に呼ぶ）</summary>
    public void ClearUltEffectTimers()
    {
        ultTimerGeneration++;
    }

    System.Collections.IEnumerator UltEffectTimerCoroutine(string effectLabel, float duration)
    {
        int gen = ultTimerGeneration;
        var go = new GameObject("UltEffectTimer");
        go.transform.SetParent(canvasRoot, false);
        go.transform.SetAsLastSibling();
        var t = go.AddComponent<Text>();
        t.fontSize = 30;
        t.color = new Color(1f, 0.75f, 0.2f);
        t.alignment = TextAnchor.MiddleRight;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.85f);
        ol.effectDistance = new Vector2(2f, -2f);
        // 右端寄せ（ボスアイコンは中央上部のため右サイドへ退避）
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.745f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 44f);

        // 縦積みリストに登録して再レイアウト（重複しても重ならない）
        activeUltTimers.Add(rt);
        RelayoutUltTimers();

        // 発射前（Ready中）に発動した場合、効果はボール発射までカウントが始まらないため
        // 表示のカウントダウンも発射まで待つ（CharacterManager.WaitForPlaying と同期）
        while (gen == ultTimerGeneration
               && GameManager.Instance != null
               && GameManager.Instance.CurrentState == GameManager.GameState.Ready)
        {
            t.text = $"{effectLabel}　残り {duration:0.0}秒";
            yield return null;
        }

        // ゲーム内時間でカウントダウン（奥義効果の進行と同期、pause 中は停止）
        // 世代が進んだら（＝効果の強制リセット）即座に表示を終了する
        float remaining = duration;
        while (remaining > 0f && gen == ultTimerGeneration)
        {
            remaining -= Time.deltaTime;
            t.text = $"{effectLabel}　残り {Mathf.Max(remaining, 0f):0.0}秒";
            yield return null;
        }

        // リストから外して残りを上に詰める
        activeUltTimers.Remove(rt);
        Destroy(go);
        RelayoutUltTimers();
    }

    /// <summary>
    /// アクティブな奥義タイマーを上から順に縦積み配置し直す。
    /// 個数に上限はなく、3つ以上でも自動で間隔を空けて並ぶ。
    /// </summary>
    void RelayoutUltTimers()
    {
        activeUltTimers.RemoveAll(r => r == null); // 破棄済みを掃除
        for (int i = 0; i < activeUltTimers.Count; i++)
        {
            activeUltTimers[i].anchoredPosition = new Vector2(-20f, -i * UltTimerSpacing);
        }
    }

    // ============================================================
    // 段階5-C: 奥義チュートリアル（ゲージ満タン → 発動 → フィードバック）
    // ============================================================

    /// <summary>
    /// OnUltReady から呼ばれる。チュートリアル中なら初回のみ発動する。
    /// </summary>
    void TryShowUltTutorial(int slot)
    {
        if (ultTutorialShown) return;
        if (TutorialManager.Instance == null) return;
        if (TutorialManager.Instance.CurrentStep != TutorialManager.Step.GamePlay) return;

        ultTutorialShown = true;
        StartCoroutine(ShowUltTutorialPart1Coroutine(slot));
    }

    /// <summary>
    /// 段階5-C Part1：満タンになった奥義アイコンをスポットライト＋矢印で誘導。
    /// </summary>
    System.Collections.IEnumerator ShowUltTutorialPart1Coroutine(int slot)
    {
        // 1フレーム待ってから（OnGaugeChanged 等の処理を済ませる）
        yield return null;
        Time.timeScale = 0f;

        if (canvasRoot == null) { Time.timeScale = 1f; yield break; }

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        // 奥義アイコンの実 RectTransform から枠を算出（アスペクト比非依存）
        Vector2 iconCenter = new Vector2(0.1f, 0.5f);
        if (ultIconImages != null && slot < ultIconImages.Length && ultIconImages[slot] != null)
            iconCenter = overlay.HighlightTarget(
                ultIconImages[slot].rectTransform, 12f, new Color(1f, 0.9f, 0.2f));

        // 吹き出しは画面中央付近に配置
        overlay.SetBubbleAnchor(
            new Vector2(0.20f, 0.45f),
            new Vector2(0.95f, 0.70f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "奥義ゲージが満タンよ！\n" +
            "左下のキャラアイコンが\n" +
            "光ってるでしょ？\n" +
            "タップして奥義を発動しなさい！");

        // 矢印：アイコンの右側に ◀ を配置してアイコンを指す
        float arrowX = Mathf.Min(iconCenter.x + 0.10f, 0.95f);
        overlay.AddArrowAt(new Vector2(arrowX, iconCenter.y), "◀");

        // 専用ボイス（Tutorial/ult_ready.wav）
        AudioClip ultReadyVoice = Resources.Load<AudioClip>("Tutorial/ult_ready");
        if (ultReadyVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                ultReadyVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            currentUltTutorialOverlay = null;
            Time.timeScale = 1f;
        });

        // Part2 から参照するため保存
        currentUltTutorialOverlay = overlay;
    }

    /// <summary>
    /// OnUltUsed から呼ばれる。Part1 を閉じてゲームを再開し、1.5秒後に Part2 を表示する。
    /// </summary>
    void OnUltUsedForTutorial(int slot)
    {
        if (TutorialManager.Instance == null) return;
        if (TutorialManager.Instance.CurrentStep != TutorialManager.Step.GamePlay) return;
        if (currentUltTutorialOverlay == null) return; // Part1 が無ければスキップ

        // Part1 を閉じてゲーム再開
        currentUltTutorialOverlay.Close();
        currentUltTutorialOverlay = null;
        Time.timeScale = 1f;

        // 2.5 秒待ってから Part2（奥義演出の時間を確保）
        StartCoroutine(ShowUltTutorialPart2AfterDelay(2.5f));
    }

    System.Collections.IEnumerator ShowUltTutorialPart2AfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (canvasRoot == null) yield break;

        Time.timeScale = 0f;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.35f),
            new Vector2(0.95f, 0.65f));
        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(
            "ふんっ、それが奥義よ\n" +
            "キャラごとに違う効果があるから\n" +
            "強いキャラを編成しなさい！");

        // 専用ボイス（Tutorial/ult_used.wav）
        AudioClip ultUsedVoice = Resources.Load<AudioClip>("Tutorial/ult_used");
        if (ultUsedVoice != null)
        {
            AudioManager.Instance?.PlayVoice(
                ultUsedVoice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowContinue("わかった", () =>
        {
            overlay.Close();
            Time.timeScale = 1f;
        });

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            Time.timeScale = 1f;
        });
    }

    // ============================================================
    // 段階5-D: 全ロスト時の自動復活フィードバック
    // ============================================================

    /// <summary>
    /// GameManager.OnTutorialRevive から呼ばれる。
    /// 1回目: miss_recover.wav / 2回目以降: miss_second.wav
    /// </summary>
    void OnTutorialReviveHandler(int reviveCount)
    {
        if (canvasRoot == null) return;

        Time.timeScale = 0f;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.32f),
            new Vector2(0.95f, 0.68f));

        string message;
        string voiceKey;
        if (reviveCount <= 1)
        {
            message =
                "…ふん、しょうがないわね\n" +
                "特別にストック回復してあげる\n" +
                "今度こそしっかりやりなさいよ！";
            voiceKey = "Tutorial/miss_recover";
        }
        else
        {
            message =
                "あんた、不器用すぎない？\n" +
                "こんどこそしっかりやりなさいよ！";
            voiceKey = "Tutorial/miss_second";
        }

        overlay.SetMessageAlignment(TextAnchor.MiddleLeft);
        overlay.SetMessage(message);

        AudioClip voice = Resources.Load<AudioClip>(voiceKey);
        if (voice != null)
        {
            AudioManager.Instance?.PlayVoice(
                voice, 1.5f, AudioManager.VoicePriority.High);
        }

        overlay.ShowContinue("わかった", () =>
        {
            overlay.Close();
            Time.timeScale = 1f;
        });

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            Time.timeScale = 1f;
        });
    }

    // ============================================================
    // 段階5-E: 打数ゼロ → +10 自動補充 演出（ボイス無し / 吹き出し無し）
    // ============================================================

    /// <summary>
    /// GameManager.OnTutorialTurnsRefill から呼ばれる。
    /// 「打数 +10」テキストを画面中央にフェードイン → 静止 → フェードアウトで表示。
    /// ゲームは止めない。
    /// </summary>
    void OnTutorialTurnsRefillHandler()
    {
        if (canvasRoot == null) return;
        StartCoroutine(ShowTurnsRefillEffect());
    }

    System.Collections.IEnumerator ShowTurnsRefillEffect()
    {
        // テキスト GameObject 生成
        var go = new GameObject("TurnsRefillEffect");
        go.transform.SetParent(canvasRoot, false);
        go.transform.SetAsLastSibling();

        var t = go.AddComponent<Text>();
        t.text = "打数 +10";
        t.fontSize = 96;
        t.color = new Color(1f, 0.9f, 0.3f, 0f); // 初期 alpha 0
        t.alignment = TextAnchor.MiddleCenter;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        // Outline + Shadow（視認性UP）
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
        ol.effectDistance = new Vector2(3f, -3f);
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.7f);
        sh.effectDistance = new Vector2(4f, -4f);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600f, 150f);

        // フェードイン 0.6s → 静止 1.0s → フェードアウト 0.4s
        float fadeIn = 0.6f;
        float hold = 1.0f;
        float fadeOut = 0.4f;

        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / fadeIn);
            var c = t.color; c.a = k; t.color = c;
            rt.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, k); // 軽い拡大演出
            yield return null;
        }
        // 静止
        yield return new WaitForSecondsRealtime(hold);
        // フェードアウト
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(elapsed / fadeOut);
            var c = t.color; c.a = k; t.color = c;
            yield return null;
        }
        Destroy(go);
    }

    void Update()
    {
        // ボスUI初期化（GameManager.Start後に1回だけ実行）
        if (!bossUIInitialized)
        {
            var sm = FindObjectOfType<StageManager>();
            if (sm != null && sm.HasBoss)
            {
                bossUIInitialized = true;
                sm.OnBossTurnChanged += UpdateBossTurn;
                if (bossTurnPanel != null) bossTurnPanel.SetActive(true);
                UpdateBossTurn(sm.BossRemainingTurns, sm.BossMaxTurns);
                StartCoroutine(ShowBossPopup(sm.BossMaxTurns));
            }
        }

        // ダメージ表示更新（速度・クリティカル含むリアルタイム）
        if (damageText != null && CharacterManager.Instance != null)
        {
            float speedRatio = 1f;
            float criticalMul = 1f;
            var ball = FindObjectOfType<BallController>();
            if (ball != null)
            {
                speedRatio = ball.SpeedDamageRatio;
                criticalMul = ball.IsCritical ? 2f : 1f;
            }
            float baseDmg = CharacterManager.Instance.BasePower
                          + CharacterManager.Instance.BonusDamage;
            float mul = CharacterManager.Instance.PassiveDamageMultiplier
                      * CharacterManager.Instance.UltDamageMultiplier
                      * speedRatio * criticalMul;
            int dmg = (int)System.Math.Ceiling(baseDmg * mul);
            damageText.text = $"ヒットダメージ：{dmg}";
        }

        // エンドレスの撃破数を更新
        if (endlessScoreText != null)
            endlessScoreText.text = $"撃破: {ResultData.EndlessScore}体";

        // 赤色時の点滅
        if (bossTurnBlink && bossTurnText != null)
        {
            float a = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.7f + 0.3f;
            bossTurnText.color = new Color(1f, 0.3f, 0.3f, a);
        }
    }

    void BuildUI()
    {
        // Canvas
        var cGo = new GameObject("GameCanvas");
        Canvas c = cGo.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight  = 0.0f;
        cGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Transform root = cGo.transform;
        canvasRoot = root; // チュートリアルから参照（全画面のまま）

        // ===== セーフエリア対応 =====
        // ノッチ搭載スマホでは画面最上部が切り欠きに隠れるため、
        // HUD（ハート・ポーズ・破壊率など）は Screen.safeArea 内のコンテナに配置する。
        // PC/エディタでは safeArea = 全画面なので影響なし。
        var safeGo = new GameObject("SafeArea");
        safeGo.transform.SetParent(root, false);
        var safeRt = safeGo.AddComponent<RectTransform>();
        var sa = Screen.safeArea;
        safeRt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        safeRt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        safeRt.offsetMin = safeRt.offsetMax = Vector2.zero;
        root = safeGo.transform; // 以降の HUD はセーフエリア内に生成

        // ---- ライフ表示（左上：コード生成ハートスプライト）----
        // Unicode「♥」は Android のフォントに字形が無く表示されないため、
        // フォント非依存のスプライト方式（上下反転バグは修正済み）を使用。
        if (heartSprite == null) heartSprite = CreateHeartSprite();
        for (int i = 0; i < MaxStockDisplay; i++)
        {
            var heart = MakeImage(root, new Color(1f, 0.4f, 0.6f),
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f + i * 48f, -42f), new Vector2(40f, 40f),
                new Vector2(0f, 1f));
            heart.sprite = heartSprite;
            heart.preserveAspect = true;
            stockHearts[i] = heart;
            heart.gameObject.SetActive(false);
        }

        // ---- ダメージ表示（ライフの下）----
        damageText = MakeText(root, "ヒットダメージ：1", 44, new Color(1f, 0.7f, 0.3f),
            new Vector2(0f, 1f), new Vector2(20f, -84f), new Vector2(550f, 60f),
            new Vector2(0f, 1f), TextAnchor.UpperLeft);

        // ---- 破壊率（右上）----
        destroyRateText = MakeText(root, "0%", 28, Color.white,
            new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(130f, 48f),
            new Vector2(1f, 1f), TextAnchor.UpperRight);

        var sliderGo = new GameObject("DestroyRateSlider");
        sliderGo.transform.SetParent(root, false);
        destroyRateSlider = sliderGo.AddComponent<Slider>();
        SetRect(sliderGo.GetComponent<RectTransform>(),
            new Vector2(1f, 1f), new Vector2(-20f, -75f), new Vector2(200f, 18f), new Vector2(1f, 1f));
        BuildSlider(destroyRateSlider, sliderGo.transform);

        // ---- エンドレス撃破数（右上・破壊率バーの下）----
        if (ResultData.IsEndless)
        {
            endlessScoreText = MakeText(root, "撃破: 0体", 30, new Color(1f, 0.75f, 0.2f),
                new Vector2(1f, 1f), new Vector2(-20f, -100f), new Vector2(260f, 44f),
                new Vector2(1f, 1f), TextAnchor.UpperRight);
            var esOl = endlessScoreText.gameObject.AddComponent<Outline>();
            esOl.effectColor = new Color(0f, 0f, 0f, 0.85f);
            esOl.effectDistance = new Vector2(2f, -2f);
        }

        // ---- ポーズボタン（右上寄り）----
        var pauseBtn = MakeButton(root, "ポーズ", 28, new Color(0.15f, 0.15f, 0.25f, 0.9f),
            new Vector2(1f, 1f), new Vector2(-300f, -50f), new Vector2(120f, 56f),
            new Vector2(1f, 1f));
        pauseBtn.GetComponent<Button>().onClick.AddListener(OnPauseButtonClicked);

        // ---- PauseMenu パネル----
        pauseMenuPanel = MakePanel(root, new Color(0f, 0f, 0f, 0.88f));

        MakeText(pauseMenuPanel.transform, "PAUSE", 60, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(400f, 80f));

        MakeButton(pauseMenuPanel.transform, "つづける", 36, new Color(0.2f, 0.5f, 1f),
            new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(320f, 72f))
            .GetComponent<Button>().onClick.AddListener(OnResumeClicked);

        var retireGo = MakeButton(pauseMenuPanel.transform, "あきらめる", 36, new Color(0.85f, 0.2f, 0.2f),
            new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(320f, 72f));
        retireGo.GetComponent<Button>().onClick.AddListener(OnRetireClicked);

        MakeButton(pauseMenuPanel.transform, "あそびかた", 36, new Color(0.3f, 0.3f, 0.4f),
            new Vector2(0.5f, 0.40f), Vector2.zero, new Vector2(320f, 72f))
            .GetComponent<Button>().onClick.AddListener(OnHelpClicked);

        // BGM スライダー
        CreateVolumeSlider(pauseMenuPanel.transform, "BGM", 0.32f,
            PlayerPrefs.GetFloat("BGMVolume", 1f),
            v => { PlayerPrefs.SetFloat("BGMVolume", v); AudioManager.Instance?.SetBGMVolume(v); });

        // SE スライダー
        CreateVolumeSlider(pauseMenuPanel.transform, "SE", 0.22f,
            PlayerPrefs.GetFloat("SEVolume", 1f),
            v => { PlayerPrefs.SetFloat("SEVolume", v); AudioManager.Instance?.SetSEVolume(v); });

        // ボイス スライダー
        CreateVolumeSlider(pauseMenuPanel.transform, "ボイス", 0.12f,
            PlayerPrefs.GetFloat("VoiceVolume", 1f),
            v => { PlayerPrefs.SetFloat("VoiceVolume", v); AudioManager.Instance?.SetVoiceVolume(v); });

        // 奥義アニメ ON/OFF トグル
        // 注意: この下の [2] インデックス参照（あそびかたボタン）より後に追加すること
        var ultAnimGo = MakeButton(pauseMenuPanel.transform,
            $"奥義アニメ: {(UltAnimationManager.Enabled ? "ON" : "OFF")}", 30,
            new Color(0.4f, 0.3f, 0.65f),
            new Vector2(0.5f, 0.04f), Vector2.zero, new Vector2(360f, 64f));
        var ultAnimTxt = ultAnimGo.GetComponentInChildren<Text>();
        ultAnimGo.GetComponent<Button>().onClick.AddListener(() =>
        {
            UltAnimationManager.Enabled = !UltAnimationManager.Enabled;
            if (ultAnimTxt != null)
                ultAnimTxt.text = $"奥義アニメ: {(UltAnimationManager.Enabled ? "ON" : "OFF")}";
        });

        // リタイヤ確認ダイアログ
        var retireConfirm = MakePanel(root, new Color(0f, 0f, 0f, 0.92f));
        MakeText(retireConfirm.transform, "あきらめますか？", 40, Color.white,
            new Vector2(0.5f, 0.57f), Vector2.zero, new Vector2(500f, 60f));
        MakeButton(retireConfirm.transform, "はい", 36, new Color(0.85f, 0.2f, 0.2f),
            new Vector2(0.32f, 0.45f), Vector2.zero, new Vector2(200f, 65f))
            .GetComponent<Button>().onClick.AddListener(() => GameManager.Instance?.Retire());
        MakeButton(retireConfirm.transform, "いいえ", 36, new Color(0.2f, 0.5f, 1f),
            new Vector2(0.68f, 0.45f), Vector2.zero, new Vector2(200f, 65f))
            .GetComponent<Button>().onClick.AddListener(() => retireConfirm.SetActive(false));
        retireConfirm.SetActive(false);
        retireGo.GetComponent<Button>().onClick.AddListener(() => retireConfirm.SetActive(true));

        // ヘルプパネル
        var helpPanel = MakePanel(root, new Color(0f, 0f, 0.1f, 0.96f));

        // タイトル
        MakeText(helpPanel.transform, "あそびかた", 44, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(500f, 60f));

        // ScrollRect 構築
        var scrollGo = new GameObject("HelpScroll");
        scrollGo.transform.SetParent(helpPanel.transform, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.05f, 0.14f);
        scrollRT.anchorMax = new Vector2(0.95f, 0.88f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.3f);
        scrollGo.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;

        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var vpRT = viewportGo.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 2200f);

        scrollRect.content = contentRT;
        scrollRect.viewport = vpRT;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

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
            30, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(650f, 2500f));
        helpTxt.alignment = TextAnchor.UpperCenter;
        var helpTxtRT = helpTxt.GetComponent<RectTransform>();
        helpTxtRT.anchorMin = new Vector2(0.5f, 1f);
        helpTxtRT.anchorMax = new Vector2(0.5f, 1f);
        helpTxtRT.pivot = new Vector2(0.5f, 1f);

        MakeButton(helpPanel.transform, "とじる", 34, new Color(0.3f, 0.3f, 0.4f),
            new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(220f, 65f))
            .GetComponent<Button>().onClick.AddListener(() => helpPanel.SetActive(false));
        helpPanel.SetActive(false);

        // HelpClicked が helpPanel を開く
        pauseMenuPanel.GetComponentsInChildren<Button>(true)[2]
            .onClick.AddListener(() => helpPanel.SetActive(true));

        pauseMenuPanel.SetActive(false);

        // ---- クリティカル表示テキスト（画面中央、初期非表示）----
        criticalText = MakeText(root, "クリティカル!", 60, Color.yellow,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 80f));
        criticalText.gameObject.SetActive(false);

        // ---- Speed Up 表示テキスト（画面中央やや下、初期非表示）----
        speedUpText = MakeText(root, "Speed Up!", 50, new Color(0.85f, 0.15f, 0.85f),
            new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(600f, 80f));
        speedUpText.gameObject.SetActive(false);

        // ---- 奥義ゲージ（左下：スロット0〜2 縦並び）----
        BuildUltGaugeUI(root);

        // ---- ボスターン表示（上部中央）----
        BuildBossTurnUI(root);

        // ---- ボス攻撃通知表示（画面中央やや上、初期非表示）----
        BuildBossActionNotif(root);
    }

    /// <summary>
    /// ボス攻撃通知パネルを構築（ShowBossAction時にボスアイコン上に配置）
    /// </summary>
    void BuildBossActionNotif(Transform root)
    {
        bossActionPanel = new GameObject("BossActionNotif");
        bossActionPanel.transform.SetParent(root, false);
        var prt = bossActionPanel.AddComponent<RectTransform>();
        // Canvas の中心をアンカーに（RectTransformUtilityのlocal座標と一致させる）
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0f); // 下端基準：パネル下端がボスアイコン上辺にくる
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(780f, 200f);

        // 半透明の赤い背景（警告色）
        var bg = bossActionPanel.AddComponent<Image>();
        bg.color = new Color(0.7f, 0f, 0.1f, 0.85f);

        // 外枠
        var frameGo = new GameObject("Frame");
        frameGo.transform.SetParent(bossActionPanel.transform, false);
        var frameImg = frameGo.AddComponent<Image>();
        frameImg.color = new Color(1f, 0.9f, 0.2f, 1f);
        var fRT = frameGo.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
        fRT.offsetMin = new Vector2(-6f, -6f);
        fRT.offsetMax = new Vector2(6f, 6f);
        frameGo.transform.SetSiblingIndex(0); // 背景の後ろに

        // テキスト（アクション最大3行）
        bossActionText = MakeText(bossActionPanel.transform, "", 52, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 180f));
        bossActionText.fontStyle = FontStyle.Bold;
        bossActionText.horizontalOverflow = HorizontalWrapMode.Overflow;
        bossActionText.verticalOverflow = VerticalWrapMode.Overflow;

        // Outline で視認性UP
        var outline = bossActionText.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);

        bossActionPanel.SetActive(false);
    }

    /// <summary>
    /// ボス攻撃通知を3秒表示する（StageManagerから呼ばれる）
    /// キャラアイコン（ボス）の上に配置する
    /// </summary>
    public void ShowBossAction(string message)
    {
        if (bossActionPanel == null || bossActionText == null) return;

        // ボスアイコンのワールド位置を取得 → スクリーン座標 → Canvas ローカル座標に変換
        var sm = FindObjectOfType<StageManager>();
        var cam = Camera.main;
        var canvasRT = bossActionPanel.transform.parent as RectTransform;
        if (sm != null && sm.ActiveBoss != null && cam != null && canvasRT != null)
        {
            // ボスアイコン上辺のワールド位置
            var bossT = sm.ActiveBoss.transform;
            float topY = bossT.position.y + 0.6f;
            Vector3 worldTop = new Vector3(bossT.position.x, topY, 0f);
            Vector2 screenPos = cam.WorldToScreenPoint(worldTop);

            // ScreenSpaceOverlay の場合 camera=null で変換
            // canvas pivot=(0.5,0.5) なので中心原点のローカル座標が返る
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, screenPos, null, out localPos);

            var prt = bossActionPanel.GetComponent<RectTransform>();
            // パネル下端をボスアイコン上辺+30px に配置
            Vector2 targetPos = localPos + new Vector2(0f, 30f);

            // 画面からはみ出さないよう clamp（canvas中心原点）
            float halfW = canvasRT.rect.width  * 0.5f;
            float halfH = canvasRT.rect.height * 0.5f;
            float panelW = prt.sizeDelta.x;
            float panelH = prt.sizeDelta.y;

            // 横: pivot=0.5 → 左右端は targetPos.x ± panelW/2
            float minX = -halfW + panelW * 0.5f + 20f;
            float maxX =  halfW - panelW * 0.5f - 20f;
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);

            // 縦: pivot.y=0 → パネル上端 = targetPos.y + panelH、下端 = targetPos.y
            float minY = -halfH + 20f;
            float maxY =  halfH - panelH - 20f;
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

            prt.anchoredPosition = targetPos;
        }

        if (bossActionCoroutine != null) StopCoroutine(bossActionCoroutine);
        bossActionCoroutine = StartCoroutine(BossActionRoutine(message));
    }

    System.Collections.IEnumerator BossActionRoutine(string message)
    {
        bossActionText.text = message;
        bossActionPanel.SetActive(true);

        // フェードイン（0.15秒）
        float t = 0f;
        var bgImg = bossActionPanel.GetComponent<Image>();
        Color bgBase = new Color(0.7f, 0f, 0.1f, 0.85f);
        Color txtBase = Color.white;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / 0.15f);
            bgImg.color = new Color(bgBase.r, bgBase.g, bgBase.b, bgBase.a * a);
            bossActionText.color = new Color(txtBase.r, txtBase.g, txtBase.b, a);
            yield return null;
        }

        // 表示維持（2.55秒）
        yield return new WaitForSeconds(2.55f);

        // フェードアウト（0.3秒）
        t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / 0.3f);
            bgImg.color = new Color(bgBase.r, bgBase.g, bgBase.b, bgBase.a * a);
            bossActionText.color = new Color(txtBase.r, txtBase.g, txtBase.b, a);
            yield return null;
        }

        bossActionPanel.SetActive(false);
        bossActionCoroutine = null;
    }

    void BuildUltGaugeUI(Transform root)
    {
        Color[] slotColors = {
            new Color(0.4f, 0.7f, 1f),   // スロット0 青
            new Color(1f, 0.6f, 0.3f),    // スロット1 橙
            new Color(0.5f, 1f, 0.5f),    // スロット2 緑
        };

        // 選択キャラのアイコンを検索するための参照ロード（Resources）
        var allChars = Resources.LoadAll<CharacterData>("Characters");

        // --- 配置パラメータ ---
        // ゲージバー・ULT ボタンを廃止し、アイコン自体をタップで Ult 発動させる。
        // パドル（world y=-9 ≒ screen y=96）との被りを避けるため baseY=140 だけ底上げ。
        const float baseY       = 140f;
        const float slotSpacing = 160f;   // スロット間隔
        const float iconSize    = 130f;
        const float nameHeight  = 28f;
        // アイコン中心 x=95 / 名前中心 x=100（中心ピボット基準で座標を書く）

        for (int i = 0; i < 3; i++)
        {
            int idx = i; // クロージャ用

            string charName = (idx < ResultData.SelectedCharacterNames.Length)
                ? ResultData.SelectedCharacterNames[idx] : "?";
            if (string.IsNullOrEmpty(charName)) charName = "?";

            float slotBottom = baseY + i * slotSpacing;     // スロット下端 Y
            float iconCenterY = slotBottom + iconSize * 0.5f + 4f; // 下から 4px マージン
            float nameCenterY = slotBottom + iconSize + nameHeight * 0.5f + 4f;

            // キャラ名（アイコンの上）
            MakeText(root, charName, 24, slotColors[i],
                new Vector2(0f, 0f),
                new Vector2(100f, nameCenterY),
                new Vector2(160f, nameHeight));

            // キャラアイコン（Button 化してタップで Ult 発動）
            Sprite iconSprite = null;
            foreach (var cd in allChars)
            {
                if (cd != null && cd.characterName == charName)
                {
                    iconSprite = cd.icon;
                    break;
                }
            }

            var iconGo = new GameObject($"UltCharIcon{i}");
            iconGo.transform.SetParent(root, false);
            var iconImg = iconGo.AddComponent<Image>();
            if (iconSprite != null) iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = true;            // タップ判定を受け付け
            iconImg.color = new Color(1f, 1f, 1f, 0.4f); // 初期：チャージ中は半透明
            SetRect(iconGo.GetComponent<RectTransform>(),
                new Vector2(0f, 0f),
                new Vector2(95f, iconCenterY),
                new Vector2(iconSize, iconSize));

            var iconBtn = iconGo.AddComponent<Button>();
            iconBtn.targetGraphic = iconImg;
            iconBtn.interactable = false;            // 初期：未満タンなのでタップ不可
            iconBtn.onClick.AddListener(() =>
            {
                CharacterManager.Instance?.TriggerUltimate(idx);
                // 発動直後に即タップ表示を消す（OnGaugeChanged もトリガされる）
                if (ultTapLabels[idx] != null)  ultTapLabels[idx].SetActive(false);
                if (ultIconButtons[idx] != null) ultIconButtons[idx].interactable = false;
            });
            ultIconImages[i]  = iconImg;
            ultIconButtons[i] = iconBtn;

            // 「タップ」ラベル（アイコン中央にオーバーレイ、初期非表示）
            var tapGo = new GameObject($"UltTapLabel{i}");
            tapGo.transform.SetParent(iconGo.transform, false);
            var tapText = tapGo.AddComponent<Text>();
            tapText.text = "タップ";
            tapText.fontSize = 36;
            tapText.fontStyle = FontStyle.Normal;                    // 細字で視認性UP
            tapText.color = new Color(1f, 0.95f, 0.2f);              // 黄色
            tapText.alignment = TextAnchor.MiddleCenter;
            tapText.raycastTarget = false;
            tapText.horizontalOverflow = HorizontalWrapMode.Overflow;
            tapText.verticalOverflow   = VerticalWrapMode.Overflow;
            // 本文用の丸ゴシック（CherryBombOne は太字の飾りフォントのため避ける）
            tapText.font = UIFont.Main; tapText.verticalOverflow = VerticalWrapMode.Overflow;
            // タップラベル用の黒アウトライン（薄め・細め）
            var tapOutline = tapGo.AddComponent<Outline>();
            tapOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            tapOutline.effectDistance = new Vector2(1f, -1f);
            // アイコン全面にフィット
            var tapRT = tapGo.GetComponent<RectTransform>();
            tapRT.anchorMin = Vector2.zero; tapRT.anchorMax = Vector2.one;
            tapRT.offsetMin = tapRT.offsetMax = Vector2.zero;
            tapGo.SetActive(false);
            ultTapLabels[i] = tapGo;
        }

        // 「タップ」ラベルの点滅アニメーションを起動（常駐コルーチン）
        StartCoroutine(BlinkTapLabels());
    }

    /// <summary>
    /// 「タップ」ラベルの alpha を 0.3〜1.0 で PingPong させて点滅表示する。
    /// アクティブなラベルのみ更新する（非表示中は何もしない）。
    /// </summary>
    System.Collections.IEnumerator BlinkTapLabels()
    {
        while (true)
        {
            // 2Hz 程度の点滅（Sin 波で滑らかに明滅）
            float alpha = 0.3f + Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * 2f)) * 0.7f;
            for (int i = 0; i < 3; i++)
            {
                var go = ultTapLabels[i];
                if (go == null || !go.activeSelf) continue;

                var t = go.GetComponent<Text>();
                if (t != null)
                {
                    var c = t.color;
                    c.a = alpha;
                    t.color = c;
                }
                var o = go.GetComponent<Outline>();
                if (o != null)
                {
                    var oc = o.effectColor;
                    oc.a = 0.75f * alpha;
                    o.effectColor = oc;
                }
            }
            yield return null;
        }
    }

    System.Collections.IEnumerator ShowBossPopup(int maxTurns)
    {
        // キャンバスのルートを取得
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) yield break;
        Transform root = canvas.transform;

        // 暗めオーバーレイ
        var overlay = new GameObject("BossPopupOverlay");
        overlay.transform.SetParent(root, false);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0f);
        overlayImg.raycastTarget = false;
        var ovRT = overlay.GetComponent<RectTransform>();
        ovRT.anchorMin = Vector2.zero; ovRT.anchorMax = Vector2.one;
        ovRT.offsetMin = ovRT.offsetMax = Vector2.zero;

        // ポップアップパネル
        var popup = new GameObject("BossPopup");
        popup.transform.SetParent(overlay.transform, false);
        popup.AddComponent<Image>().color = new Color(0.5f, 0.05f, 0.1f, 0.9f);
        var prt = popup.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(700f, 200f);

        // 内枠
        var inner = new GameObject("Inner");
        inner.transform.SetParent(popup.transform, false);
        inner.AddComponent<Image>().color = new Color(0.08f, 0.02f, 0.05f, 0.95f);
        var iRT = inner.GetComponent<RectTransform>();
        iRT.anchorMin = Vector2.zero; iRT.anchorMax = Vector2.one;
        iRT.offsetMin = new Vector2(4f, 4f); iRT.offsetMax = new Vector2(-4f, -4f);

        // テキスト
        var txtGo = new GameObject("PopupTxt");
        txtGo.transform.SetParent(popup.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.text = $"♡{maxTurns}打以内に倒してね♡";
        txt.fontSize = 46;
        txt.color = new Color(1f, 0.85f, 0.9f);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = UIFont.Main; txt.verticalOverflow = VerticalWrapMode.Overflow;
        var shadow = txtGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        var tRT = txtGo.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        // フェードイン
        float fadeIn = 0.4f;
        float elapsed = 0f;
        Vector2 startSize = new Vector2(700f, 0f);
        Vector2 endSize = new Vector2(700f, 200f);
        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeIn;
            prt.sizeDelta = Vector2.Lerp(startSize, endSize, Mathf.SmoothStep(0f, 1f, t));
            overlayImg.color = new Color(0f, 0f, 0f, 0.4f * t);
            yield return null;
        }

        // 2秒表示
        yield return new WaitForSeconds(2f);

        // フェードアウト
        float fadeOut = 0.5f;
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            float t = 1f - elapsed / fadeOut;
            overlayImg.color = new Color(0f, 0f, 0f, 0.4f * t);
            txt.color = new Color(1f, 0.85f, 0.9f, t);
            popup.GetComponent<Image>().color = new Color(0.5f, 0.05f, 0.1f, 0.9f * t);
            inner.GetComponent<Image>().color = new Color(0.08f, 0.02f, 0.05f, 0.95f * t);
            yield return null;
        }

        Destroy(overlay);
    }

    void BuildBossTurnUI(Transform root)
    {
        // ボスターン表示パネル（上部中央、初期非表示）
        bossTurnPanel = new GameObject("BossTurnPanel");
        bossTurnPanel.transform.SetParent(root, false);
        var panelImg = bossTurnPanel.AddComponent<Image>();
        panelImg.color = new Color(0.4f, 0.05f, 0.05f, 0.85f);
        panelImg.raycastTarget = false;
        var prt = bossTurnPanel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -160f);
        prt.sizeDelta = new Vector2(600f, 70f);

        bossTurnText = MakeText(bossTurnPanel.transform, "", 38, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(580f, 64f));
        bossTurnText.alignment = TextAnchor.MiddleCenter;

        // Shadow
        var shadow = bossTurnText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(2f, -2f);

        bossTurnPanel.SetActive(false); // ボスステージ以外は非表示
    }

    void UpdateBossTurn(int remaining, int max)
    {
        if (bossTurnText == null) return;

        bossTurnText.text = $"♡あと{remaining}打で負けちゃう・・・♡";

        // 残り少ないと警告（5以下で赤点滅）
        if (remaining <= 5)
        {
            bossTurnBlink = true;
        }
        else if (remaining <= 10)
        {
            bossTurnBlink = false;
            bossTurnText.color = new Color(1f, 0.8f, 0.2f);
        }
        else
        {
            bossTurnBlink = false;
            bossTurnText.color = Color.white;
        }

        // ボス撃破後は非表示
        var sm = FindObjectOfType<StageManager>();
        if (sm != null && !sm.HasBoss && bossTurnPanel != null)
            bossTurnPanel.SetActive(false);
    }

    void BuildUltSlider(Slider slider, Transform parent, Color fillColor)
    {
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0f;

        var bg = new GameObject("BG"); bg.transform.SetParent(parent, false);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var fa = new GameObject("FillArea"); fa.transform.SetParent(parent, false);
        var faRT = fa.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = faRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = fillColor;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        slider.fillRect = fillRT;
        slider.targetGraphic = bgImg;
        slider.direction = Slider.Direction.LeftToRight;
    }

    // ---- UI イベント ----

    public void OnPauseButtonClicked()
    {
        GameManager.Instance?.Pause();
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    void OnResumeClicked()
    {
        GameManager.Instance?.Resume();
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    void OnRetireClicked() { }   // retire confirm handled by lambda above
    void OnHelpClicked()  { }    // help panel handled by lambda above

    public void ShowCriticalText()
    {
        if (criticalText != null)
            StartCoroutine(CriticalTextCoroutine());
    }

    void ShowSpeedUpText()
    {
        if (speedUpText != null)
            StartCoroutine(SpeedUpTextCoroutine());
    }

    System.Collections.IEnumerator SpeedUpTextCoroutine()
    {
        Color baseColor = new Color(0.85f, 0.15f, 0.85f);
        speedUpText.color = baseColor;
        speedUpText.gameObject.SetActive(true);
        float duration = 1.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            speedUpText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }
        speedUpText.gameObject.SetActive(false);
        speedUpText.color = baseColor;
    }

    System.Collections.IEnumerator CriticalTextCoroutine()
    {
        criticalText.gameObject.SetActive(true);
        // 1秒間表示してフェードアウト
        float duration = 1.0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            criticalText.color = new Color(1f, 1f, 0f, alpha);
            yield return null;
        }
        criticalText.gameObject.SetActive(false);
        criticalText.color = Color.yellow;
    }

    // ---- データ更新 ----

    public void UpdateStockDisplay(int remaining)
    {
        int max = GameManager.Instance != null ? GameManager.Instance.MaxStock : 3;
        Color heartActive = new Color(1f, 0.4f, 0.6f);      // ピンク
        Color heartInactive = new Color(0.3f, 0.15f, 0.2f);  // 暗いピンク（消費済み）
        for (int i = 0; i < stockHearts.Length; i++)
        {
            if (stockHearts[i] == null) continue;
            stockHearts[i].gameObject.SetActive(i < max);
            stockHearts[i].color = i < remaining ? heartActive : heartInactive;
        }
    }

    /// <summary>コードでハート型スプライトを生成する</summary>
    static Sprite CreateHeartSprite()
    {
        // 「上部の円2つ + 下部の逆三角形」による古典的なハート構成。
        // 数式カーブより形が安定し、誰が見てもハートに見える。
        // 2x2 スーパーサンプリングで縁を滑らかにする。
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Vector2 c1 = new Vector2(0.32f, 0.62f);   // 左の円（正規化 0-1、y は上向き）
        Vector2 c2 = new Vector2(0.68f, 0.62f);   // 右の円
        float r = 0.235f;
        Vector2 tipA = new Vector2(0.5f, 0.04f);  // 下の尖り
        Vector2 tipB = new Vector2(0.085f, 0.62f);
        Vector2 tipC = new Vector2(0.915f, 0.62f);

        bool InHeart(float px, float py)
        {
            // 円判定
            if ((px - c1.x) * (px - c1.x) + (py - c1.y) * (py - c1.y) <= r * r) return true;
            if ((px - c2.x) * (px - c2.x) + (py - c2.y) * (py - c2.y) <= r * r) return true;
            // 三角形判定（符号付き面積の向きが3辺で一致するか）
            float d1 = (px - tipB.x) * (tipA.y - tipB.y) - (tipA.x - tipB.x) * (py - tipB.y);
            float d2 = (px - tipA.x) * (tipC.y - tipA.y) - (tipC.x - tipA.x) * (py - tipA.y);
            float d3 = (px - tipC.x) * (tipB.y - tipC.y) - (tipB.x - tipC.x) * (py - tipC.y);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 2x2 サブサンプルで滑らかなアルファを作る
                int hit = 0;
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                {
                    float px = (x + 0.25f + sx * 0.5f) / size;
                    float py = (y + 0.25f + sy * 0.5f) / size;
                    if (InHeart(px, py)) hit++;
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, hit / 4f));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public void UpdateDestroyRate(float rate)
    {
        if (destroyRateSlider != null) destroyRateSlider.value = rate;
        if (destroyRateText   != null) destroyRateText.text = $"{Mathf.FloorToInt(rate * 100)}%";
    }

    /// <summary>
    /// 奥義ゲージの増減を受けてアイコンの表示を更新する。
    /// - アイコン alpha を 0.4（空）〜1.0（満タン）に線形補間
    /// - 満タンから減少した瞬間（= Ult 消費）にはタップラベルを隠す
    /// </summary>
    void UpdateUltGauge(int slot, float ratio)
    {
        if (slot < 0 || slot >= 3) return;
        if (ultIconImages[slot] != null)
        {
            float alpha = Mathf.Lerp(0.4f, 1.0f, Mathf.Clamp01(ratio));
            var c = ultIconImages[slot].color;
            c.a = alpha;
            ultIconImages[slot].color = c;
        }
        // 満タン未満ならタップ表示とボタンを無効化
        if (ratio < 1.0f)
        {
            if (ultTapLabels[slot] != null)   ultTapLabels[slot].SetActive(false);
            if (ultIconButtons[slot] != null) ultIconButtons[slot].interactable = false;
        }
    }

    /// <summary>
    /// 奥義ゲージが満タンになったときに呼ばれる。
    /// アイコンを「タップ」ラベル付きで点灯させ、タップ受付を有効化する。
    /// </summary>
    void ShowUltButton(int slot)
    {
        if (slot < 0 || slot >= 3) return;
        if (ultIconImages[slot] != null)
        {
            var c = ultIconImages[slot].color;
            c.a = 1f;
            ultIconImages[slot].color = c;
        }
        if (ultTapLabels[slot] != null)   ultTapLabels[slot].SetActive(true);
        if (ultIconButtons[slot] != null) ultIconButtons[slot].interactable = true;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStockChanged      -= UpdateStockDisplay;
            GameManager.Instance.OnDestroyRateChanged -= UpdateDestroyRate;
            GameManager.Instance.OnTutorialRevive       -= OnTutorialReviveHandler;
            GameManager.Instance.OnTutorialTurnsRefill  -= OnTutorialTurnsRefillHandler;
        }
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnGaugeChanged -= UpdateUltGauge;
            CharacterManager.Instance.OnUltReady     -= ShowUltButton;
            CharacterManager.Instance.OnUltReady     -= TryShowUltTutorial;
            CharacterManager.Instance.OnUltUsed      -= OnUltUsedForTutorial;
            CharacterManager.Instance.OnUltUsed      -= ShowUltBanner;
        }
        var sm = FindObjectOfType<StageManager>();
        if (sm != null) sm.OnBossTurnChanged -= UpdateBossTurn;
        SpeedBlock.OnSpeedUp -= ShowSpeedUpText;
    }

    // ---- ファクトリーメソッド ----

    Image MakeImage(Transform parent, Color col,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size, Vector2 pivot = default)
    {
        var go = new GameObject("Icon");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return img;
    }

    Text MakeText(Transform parent, string txt, int size, Color col,
        Vector2 anchor, Vector2 pos, Vector2 sizeDelta,
        Vector2 pivot = default, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.alignment = align;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    GameObject MakeButton(Transform parent, string label, int fontSize, Color bgCol,
        Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot = default)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        var btnImg = go.AddComponent<Image>(); btnImg.color = bgCol; UISprites.Button(btnImg);
        var btn = go.AddComponent<Button>();
        // ボタンSEはAudioManagerの自動付与で鳴る
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = fontSize; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : UIFont.Main;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return go;
    }

    GameObject MakePanel(Transform parent, Color col)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    void BuildSlider(Slider slider, Transform parent)
    {
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

        var bg = new GameObject("BG"); bg.transform.SetParent(parent, false);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.2f, 0.2f, 0.2f);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var fa = new GameObject("FillArea"); fa.transform.SetParent(parent, false);
        var faRT = fa.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = faRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = new Color(0.3f, 0.8f, 1f);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        slider.fillRect = fillRT;
        slider.targetGraphic = bgImg;
        slider.direction = Slider.Direction.LeftToRight;
    }

    void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot = default)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    // ---- 音量スライダー（ホーム画面と統一デザイン） ----

    void CreateVolumeSlider(Transform parent, string label, float yAnchor,
        float initialValue, UnityEngine.Events.UnityAction<float> onChanged)
    {
        // ラベル（左寄せ）
        var labelT = MakeText(parent, label, 30, new Color(0.7f, 0.9f, 1f),
            new Vector2(0.15f, yAnchor + 0.04f), Vector2.zero, new Vector2(160f, 40f));

        // パーセント表示（右寄せ、ラベルと同じ行）
        var pctT = MakeText(parent, $"{Mathf.RoundToInt(initialValue * 100)}%", 28,
            new Color(0.4f, 0.95f, 0.6f),
            new Vector2(0.88f, yAnchor + 0.04f), Vector2.zero, new Vector2(120f, 40f));

        // スライダー（ラベル行の下）
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
}
