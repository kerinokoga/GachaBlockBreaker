using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体の状態管理・ストック管理・破壊率トリガーを担うシングルトン
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ---- ゲーム状態 ----
    public enum GameState { Ready, Playing, Paused, Miss, GameOver, Clear }
    public GameState CurrentState { get; private set; }

    // ---- ストック ----
    [Header("ストック設定")]
    [SerializeField] private int maxStock = 3;
    private int currentStock;

    // ---- 参照 ----
    [Header("参照")]
    [SerializeField] private BallController ball;
    [SerializeField] private PaddleController paddle;
    [SerializeField] private StageManager stageManager;

    // ---- マルチボール管理 ----
    private List<BallController> activeBalls = new List<BallController>();
    private int pendingSplitCount = 0; // Ready状態で使った分裂ULTの回数（発射時に 2^n 個発射）

    // ---- 破壊率トリガー ----
    private bool triggered30 = false;
    private bool triggered60 = false;

    // ---- ステージ敵キャラデータ（ボイス用）----
    private CharacterData stageCharData;

    // ---- 裏ステージ対応：StageData を保持しておく ----
    private StageData currentStageData;

    // ---- Phase 3: 美少女解放 ----
    private RevealUI revealUI;

    // ---- 外部公開 ----
    public int CurrentStock => currentStock;
    public int MaxStock => maxStock;

    // UI が購読するイベント
    public System.Action<int> OnStockChanged;       // 残りストック数
    public System.Action<float> OnDestroyRateChanged; // 破壊率 0.0〜1.0

    // チュートリアル用イベント
    public System.Action<int> OnTutorialRevive;     // 復活回数 (1=初回, 2+=以降)
    public System.Action      OnTutorialTurnsRefill; // 打数 +10 補充時

    // チュートリアル中の復活カウント
    private int tutorialReviveCount = 0;

    // ---- 初期化 ----

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // カメラに CameraFitWidth が無ければ自動追加（アスペクト比対応）
        var mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<CameraFitWidth>() == null)
            mainCam.gameObject.AddComponent<CameraFitWidth>();

        currentStock = maxStock;
        CurrentState = GameState.Ready;

        // StageManager の破壊イベントを購読
        if (stageManager != null)
            stageManager.OnBlockDestroyedCallback += CheckDestroyRate;

        // BallController のミスイベントを購読
        if (ball != null)
        {
            ball.speed = 8.5f; // Inspector値を上書きして確実に初期速度を設定
            ball.OnMissed += () => OnBallMissed(ball);
            ball.OnPaddleHit += OnPaddleHitForBoss;
            ball.SetPaddle(paddle.transform);
            activeBalls.Clear();
            activeBalls.Add(ball);
        }

        // Phase 3: RevealUI を取得
        revealUI = FindObjectOfType<RevealUI>();

        // StageData を Resources からロード（なければテストステージ）
        if (ResultData.StageNumber <= 0) ResultData.StageNumber = 1; // 直接起動時のフォールバック
        if (stageManager != null)
        {
            var allStages = Resources.LoadAll<StageData>("Stages");
            StageData stageData = null;
            if (ResultData.IsEndless)
            {
                // エンドレス: 1体目（レイアウト=ステージ10相当、キャラはランダム抽選）
                endlessWave = 0;
                ResultData.EndlessScore = 0;
                endlessRng = new System.Random();
                EndlessManager.OnChallengeStarted(); // 本日初回なら100オーブ付与
                stageData = PickEndlessLayout(0);
                endlessCharSource = PickEndlessCharSource(bossOnly: false); // 1体目はボスステージではない
                if (stageData != null)
                {
                    // BGM は抽選キャラのステージのものを使用（毎回変化して単調にならない）
                    ResultData.StageNumber = endlessCharSource != null
                        ? endlessCharSource.stageNumber : stageData.stageNumber;
                    stageManager.SetEndlessHPMultiplier(EndlessHPMulFor(0));
                    stageManager.SetEndlessCharSource(endlessCharSource);
                }
            }
            else
            {
                foreach (var s in allStages)
                    if (s.stageNumber == ResultData.StageNumber) { stageData = s; break; }
            }

            if (stageData != null)
            {
                currentStageData = stageData;
                // ボス攻撃用にパドル参照を渡す
                stageManager.paddleRef = paddle;
                stageManager.BuildStage(stageData);

                // ボスターン切れでゲームオーバー
                stageManager.OnBossTurnExpired += OnBossTurnExpired;

                // ボスHP変動でイラスト切替
                stageManager.OnBossHPRatioChanged += CheckBossHPForReveal;

                revealUI?.SetStageData(stageData.illustColor30, stageData.illustColor60,
                                       stageData.illustColorFull, stageData.characterName,
                                       stageData.illustSprite0, stageData.illustSprite30,
                                       stageData.illustSprite60, stageData.illustSpriteFull);

                // ステージ敵キャラデータを取得（ボイス再生用）
                if (!string.IsNullOrEmpty(stageData.characterName))
                {
                    var allChars = Resources.LoadAll<CharacterData>("Characters");
                    foreach (var cd in allChars)
                        if (cd.characterName == stageData.characterName) { stageCharData = cd; break; }
                }

                // パドルサイズ適用
                if (paddle != null && stageData.paddleScale > 0f)
                {
                    Vector3 ps = paddle.transform.localScale;
                    ps.x *= stageData.paddleScale;
                    paddle.transform.localScale = ps;
                }

                // パドルを少し上に移動（ULTゲージとの被り防止）
                if (paddle != null)
                {
                    Vector3 pp = paddle.transform.position;
                    pp.y += 0.5f;
                    paddle.transform.position = pp;
                }
            }
            else
            {
                stageManager.BuildTestStage();
            }
        }

        // エンドレス: 見た目（イラスト・敵ボイス）を抽選キャラへ差し替え
        if (ResultData.IsEndless) ApplyEndlessCharVisuals();

        // Phase 2: キャラクターマネージャー初期化（パッシブ適用）
        CharacterManager.Instance?.Initialize(ball, this);

        // クリティカルゾーンサイズをパッシブに合わせて更新
        if (paddle != null) paddle.UpdateCriticalZoneSize();

        OnStockChanged?.Invoke(currentStock);

        // Phase 6: ステージ別 BGM 再生
        AudioManager.Instance?.PlayStageBGM(ResultData.StageNumber);

        // ステージ開始ボイス（敵キャラ）— シーン遷移タイミングで再生
        PlayEnemyVoice(cd => cd.voiceStageStart, AudioManager.VoicePriority.Mid);
    }

    // ---- 入力 ----

    void Update()
    {
        // Ready 状態でタップ/クリックしたらボール発射（UI上のタップは除外）
        if (CurrentState == GameState.Ready)
        {
            bool tapped = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!overUI && Input.touchCount > 0)
                overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            if (tapped && !overUI)
                StartGame();
        }
    }

    // ---- 状態遷移 ----

    public void StartGame()
    {
        CurrentState = GameState.Playing;

        if (pendingSplitCount > 0)
        {
            // Ready状態で分裂ULTを使った場合：1回=2個、2回=4個（2^n 個）を同時発射
            int total = 1 << pendingSplitCount;
            pendingSplitCount = 0;

            ball?.Launch();
            float origAngle = Mathf.Atan2(ball.GetComponent<Rigidbody2D>().velocity.y,
                                           ball.GetComponent<Rigidbody2D>().velocity.x) * Mathf.Rad2Deg;
            for (int i = 1; i < total; i++)
            {
                var clone = CreateCloneBall(ball);
                if (clone == null) continue;
                // オリジナルを中心に左右交互 ±30°刻みの扇状に発射（重なり防止）
                float offset = Mathf.CeilToInt(i / 2f) * 30f * (i % 2 == 1 ? 1f : -1f);
                clone.LaunchAt(origAngle + offset);
            }
        }
        else
        {
            ball?.Launch();
        }
    }

    /// <summary>
    /// ボール分裂ULT：全アクティブボールをそれぞれ2つに分裂させる
    /// Ready状態の場合はフラグを立てて発射時に2個発射する
    /// </summary>
    public void TriggerBallSplit()
    {
        if (CurrentState == GameState.Ready)
        {
            pendingSplitCount++;
            return;
        }

        // Playing状態：全アクティブボールを分裂
        var currentBalls = new List<BallController>(activeBalls);
        foreach (var b in currentBalls)
        {
            if (b == null || !b.IsLaunched) continue;
            var clone = CreateCloneBall(b);
            if (clone != null)
            {
                // 元のボールの速度ベクトルを少し回転させた方向に飛ばす
                Vector2 vel = b.GetComponent<Rigidbody2D>().velocity;
                float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
                // 元ボールを+30°、クローンを-30°
                float rad1 = (angle + 30f) * Mathf.Deg2Rad;
                b.GetComponent<Rigidbody2D>().velocity = new Vector2(Mathf.Cos(rad1), Mathf.Sin(rad1)) * b.speed;
                b.ApplyClampAngle(); // 回転の結果が水平に近い角度にならないよう補正
                clone.LaunchAt(angle - 30f);
            }
        }
    }

    /// <summary>ボールのクローンを生成して管理リストに追加</summary>
    private BallController CreateCloneBall(BallController source)
    {
        var cloneGo = Instantiate(source.gameObject, source.transform.position, Quaternion.identity);
        var clone = cloneGo.GetComponent<BallController>();
        if (clone == null) { Destroy(cloneGo); return null; }

        clone.isClone = true;
        clone.speed = source.speed;

        // クリティカル状態・originalColor をオリジナルから引き継ぐ
        clone.InheritStateFromSource(source);

        // イベント購読
        clone.OnMissed = null; // Instantiate でコピーされたデリゲートをクリア
        clone.OnPaddleHit = null;
        clone.OnCriticalHit = null;
        clone.OnMissed += () => OnBallMissed(clone);
        clone.OnPaddleHit += OnPaddleHitForBoss;
        var gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null) clone.OnCriticalHit += gameUI.ShowCriticalText;

        // パドル参照を設定
        if (paddle != null)
            clone.SetPaddleRef(paddle.transform);

        activeBalls.Add(clone);
        return clone;
    }

    public void Pause()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        paddle?.SetActive(false);
    }

    public void Resume()
    {
        if (CurrentState != GameState.Paused) return;
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        paddle?.SetActive(true);
    }

    /// <summary>個別ボールのミス処理（クローンなら破棄、全滅なら本当のミス）</summary>
    private void OnBallMissed(BallController missedBall)
    {
        // Paused 中（ポーズメニュー、または裏ステージ突入演出中）は
        // ボール落下をミスとしてカウントしない
        if (CurrentState == GameState.Miss || CurrentState == GameState.GameOver
            || CurrentState == GameState.Clear || CurrentState == GameState.Paused) return;

        if (missedBall.isClone)
        {
            // クローンボールは破棄
            activeBalls.Remove(missedBall);
            Destroy(missedBall.gameObject);
            // まだ他のボールが残っていれば続行
            if (activeBalls.Count > 0) return;
        }
        else
        {
            // オリジナルボールが落ちた場合、クローンがまだあるか確認
            activeBalls.Remove(missedBall);
            if (activeBalls.Count > 0)
            {
                // オリジナルを非表示にして待機（クローンがまだ飛んでいる）
                missedBall.ResetBall();
                missedBall.gameObject.SetActive(false);
                return;
            }
        }

        // 全ボール消滅 → 本当のミス処理
        StartCoroutine(HandleMiss());
    }

    private IEnumerator HandleMiss()
    {
        CurrentState = GameState.Miss;
        paddle?.SetActive(false);

        // Phase 2: BarrierShot 奥義がアクティブならミスをキャンセル
        if (CharacterManager.Instance != null && CharacterManager.Instance.ConsumeBarrier())
        {
            yield return new WaitForSeconds(0.3f);
            ResetToSingleBall();
            paddle?.SetActive(true);
            CurrentState = GameState.Ready;
            yield break;
        }

        // Phase 6: ミス SE + 赤フラッシュ
        AudioManager.Instance?.PlaySE(AudioManager.Instance.seMiss);
        StartCoroutine(ScreenFlash(new Color(1f, 0.1f, 0.1f, 0.5f), 0.4f));

        currentStock--;
        OnStockChanged?.Invoke(currentStock);

        yield return new WaitForSeconds(0.8f);

        if (currentStock <= 0)
        {
            // 段階5-D: チュートリアル中 (GamePlay) なら自動復活
            bool inTutorial = TutorialManager.Instance != null
                           && TutorialManager.Instance.CurrentStep == TutorialManager.Step.GamePlay;
            if (inTutorial)
            {
                tutorialReviveCount++;
                AddStock(3);
                OnTutorialRevive?.Invoke(tutorialReviveCount);
                ResetToSingleBall();
                paddle?.SetActive(true);
                CurrentState = GameState.Ready;
                Debug.Log($"[Tutorial] 全ロスト復活 ({tutorialReviveCount}回目): AddStock(3)");
            }
            else
            {
                GameOver();
            }
        }
        else
        {
            ResetToSingleBall();
            paddle?.SetActive(true);
            CurrentState = GameState.Ready;
        }
    }

    /// <summary>
    /// 演出中にボールの物理（速度）を止める。
    /// EnterTrueStage の Phase A で呼び出し、ボールが画面下に落下するのを防ぐ。
    /// ボール本体は表示したまま（裏ステージ突入演出後に Destroy/Reset される）。
    /// </summary>
    private void FreezeAllBalls()
    {
        for (int i = 0; i < activeBalls.Count; i++)
        {
            var b = activeBalls[i];
            if (b == null) continue;
            var rb = b.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.simulated = false; // 物理シミュレーションも停止
            }
        }
    }

    /// <summary>全クローンを破棄してオリジナルボールのみに戻す</summary>
    private void ResetToSingleBall()
    {
        // クローンを全破棄
        for (int i = activeBalls.Count - 1; i >= 0; i--)
        {
            if (activeBalls[i] != null && activeBalls[i].isClone)
            {
                Destroy(activeBalls[i].gameObject);
                activeBalls.RemoveAt(i);
            }
        }
        // オリジナルボールをリセット
        ball.gameObject.SetActive(true);
        ball.ResetBall();
        ball.SetPaddle(paddle.transform);
        activeBalls.Clear();
        activeBalls.Add(ball);
        pendingSplitCount = 0;
    }

    /// <summary>パドルヒット時にボスターンを消費</summary>
    private void OnPaddleHitForBoss()
    {
        if (CurrentState != GameState.Playing)
        {
            Debug.Log($"[Tutorial Debug] OnPaddleHit 受信したが State={CurrentState} なので打数減らさず");
            return;
        }
        stageManager?.OnPaddleHit();
        Debug.Log($"[Tutorial Debug] パドルヒット → 残り打数={stageManager?.BossRemainingTurns}");
    }

    /// <summary>ボスターン切れ → ゲームオーバー</summary>
    private void OnBossTurnExpired()
    {
        if (CurrentState == GameState.GameOver || CurrentState == GameState.Clear) return;

        // 段階5-E: チュートリアル中 (GamePlay) なら打数 +10 を自動補充
        bool inTutorial = TutorialManager.Instance != null
                       && TutorialManager.Instance.CurrentStep == TutorialManager.Step.GamePlay;
        if (inTutorial && stageManager != null)
        {
            stageManager.RefillBossTurns(10);
            OnTutorialTurnsRefill?.Invoke();
            Debug.Log("[Tutorial] 打数ゼロ → +10 自動補充");
            return;
        }

        GameOver();
    }

    private void GameOver()
    {
        CurrentState = GameState.GameOver;

        // Phase 6: ゲームオーバー SE + BGM 停止
        AudioManager.Instance?.PlaySE(AudioManager.Instance.seGameOver);
        AudioManager.Instance?.StopBGM();

        // ゲームオーバー時ボイス（敵キャラ）
        PlayEnemyVoice(cd => cd.voiceDefeat, AudioManager.VoicePriority.High);

        ResultData.IsClear = false;
        ResultData.IsTrueStageClear = false;
        ResultData.DestroyRate = stageManager != null ? stageManager.GetDestroyRate() : 0f;
        ResultData.RemainingStock = 0;
        // StageNumber はそのまま保持

        StartCoroutine(LoadResultAfterDelay(1.5f));
    }

    private void StageClear()
    {
        // 裏ステージ未突入 + 裏ステージあり → 裏ステージへ自動突入
        // エンドレスでは「5の倍数体目」のみ裏ボスあり（レイアウトが常にステージ15相当のため制限する）
        bool allowTrueStage = !ResultData.IsEndless || ((endlessWave + 1) % 5 == 0);
        if (currentStageData != null && currentStageData.hasTrueStage && allowTrueStage
            && stageManager != null && !stageManager.IsTrueStage)
        {
            StartCoroutine(EnterTrueStage());
            return;
        }

        // エンドレス: リザルトへ行かず、次のウェーブを構築して続行
        if (ResultData.IsEndless)
        {
            StartCoroutine(EndlessNextWave());
            return;
        }

        CurrentState = GameState.Clear;
        paddle?.SetActive(false);

        // Phase 3: フル解放演出
        revealUI?.AdvanceToState(3);

        // Phase 6: クリア SE + 白フラッシュ + BGM 停止
        AudioManager.Instance?.PlaySE(AudioManager.Instance.seClear);
        AudioManager.Instance?.StopBGM();

        // クリア時ボイス（敵キャラ）
        PlayEnemyVoice(cd => cd.voiceVictory, AudioManager.VoicePriority.High);
        StartCoroutine(ScreenFlash(new Color(1f, 1f, 0.8f, 0.7f), 0.8f));

        ResultData.IsClear = true;
        ResultData.IsFirstClear = !ProgressManager.IsCleared(ResultData.StageNumber); // SaveClear より前に判定
        ResultData.IsTrueStageClear = (stageManager != null && stageManager.IsTrueStage); // 裏クリア判定（イラスト差し替えに使用）
        ResultData.DestroyRate = 1f;
        ResultData.RemainingStock = currentStock;
        // StageNumber はそのまま保持（変更しない）

        // Phase 3: 進行保存
        ProgressManager.SaveClear(ResultData.StageNumber, 1f);

        // デイリーミッション（ステージクリア）
        DailyMissionManager.ReportStageClear();

        // 裏ステージクリアなら全イラスト解放を保存
        if (stageManager != null && stageManager.IsTrueStage)
            ProgressManager.SaveTrueStageClear(ResultData.StageNumber);

        StartCoroutine(LoadResultAfterDelay(2.0f));
    }

    /// <summary>
    /// 裏ステージ突入コルーチン。
    /// - 全ボール消去（全ロスト扱い）
    /// - 赤フラッシュ×3（危険演出）
    /// - 裏配置で再構築
    /// - BGM ピッチ 1.2倍で再開
    /// </summary>
    private IEnumerator EnterTrueStage()
    {
        CurrentState = GameState.Paused;
        paddle?.SetActive(false);

        // 演出中はボールの物理を止める（落下によるミス誤判定 & 視覚ノイズを防止）
        FreezeAllBalls();

        // --- Phase A: ボス撃破クリア演出 + フル解放（_100.png 表示）---
        // ボス撃破 SE / 破壊エフェクトを見せるための短い間
        yield return new WaitForSeconds(0.6f);

        // フル解放（表ステージのイラスト _100.png を表示）
        // 裏ステージ突入前にプレイヤーに鑑賞させる
        revealUI?.AdvanceToState(3);

        // Phase 6: クリア SE + 白フラッシュ（表ステージ撃破の達成感）
        AudioManager.Instance?.PlaySE(AudioManager.Instance.seClear);
        StartCoroutine(ScreenFlash(new Color(1f, 1f, 0.8f, 0.7f), 0.8f));

        // フル解放イラストを鑑賞する尺
        yield return new WaitForSeconds(2.2f);

        // BGM 停止
        AudioManager.Instance?.StopBGM();

        // --- 一時効果を全リセット（奥義ゲージ以外） ---
        // 1) 奥義効果（PowerBurst のダメージ倍率 / Penetrate / Barrier）を解除
        CharacterManager.Instance?.ResetTemporaryEffects();
        // 1.5) 効果タイマー表示も消す（効果は消えたのに表示だけ残るのを防ぐ）
        FindObjectOfType<GameUI>()?.ClearUltEffectTimers();
        // 2) Boss 攻撃効果（パドル縮小・降下ブロック等）をキャンセル
        stageManager?.ResetBossAttackEffects();

        // 全ボール消去（裏ステージ突入でボール全ロスト）
        // 念のためクローンを画面外に飛ばしてから Destroy（凍結中にブロックと重なって
        // 破壊命令前に衝突判定が走るのを防ぐ）
        for (int i = activeBalls.Count - 1; i >= 0; i--)
        {
            var b = activeBalls[i];
            if (b != null)
            {
                // 透明化パルスも停止（念のため破棄前に）
                b.CancelTransparency();
                if (b.isClone)
                {
                    b.transform.position = new Vector3(9999f, 9999f, 0f);
                    Destroy(b.gameObject);
                }
            }
        }
        activeBalls.Clear();
        if (ball != null)
        {
            ball.CancelTransparency();
            // オリジナルボールも一旦画面外へ移動してから非表示化
            // （SetActive(false) では Transform.position は保持されるため、
            //  後の SetActive(true) で旧位置に出てブロックと重なる事故を予防）
            ball.transform.position = new Vector3(9999f, 9999f, 0f);
            ball.gameObject.SetActive(false);
            ball.ResetBall();
        }

        // デンジャー SE（赤点滅の冒頭で鳴らす）
        AudioManager.Instance?.PlayDangerSE();

        // --- Phase B: 危険演出（赤フラッシュ ×3、計3秒 + 「危険」テキスト）---
        float flashDur = 1.0f;
        GameObject dangerGO = CreateDangerText();
        for (int i = 0; i < 3; i++)
        {
            StartCoroutine(ScreenFlash(new Color(1f, 0f, 0f, 0.7f), flashDur));
            if (dangerGO != null)
                StartCoroutine(PunchDangerText(dangerGO.transform, flashDur));
            yield return new WaitForSeconds(flashDur);
        }
        if (dangerGO != null) Destroy(dangerGO);

        // --- Phase C: 点滅終了後の静止 ---
        yield return new WaitForSeconds(0.5f);

        // ステージ再構築（裏配置 + Boss HP×2）
        triggered30 = false;
        triggered60 = false;
        stageManager.paddleRef = paddle;
        stageManager.RebuildAsTrueStage();

        // 新しい Boss のイベント購読をやり直し
        stageManager.OnBossTurnExpired -= OnBossTurnExpired;
        stageManager.OnBossTurnExpired += OnBossTurnExpired;
        stageManager.OnBossHPRatioChanged -= CheckBossHPForReveal;
        stageManager.OnBossHPRatioChanged += CheckBossHPForReveal;

        // revealUI の差し替え（裏イラストへ、未設定時は本イラストを流用）
        // エンドレスでは抽選キャラのイラストを使用
        if (revealUI != null && currentStageData != null)
        {
            var src = (ResultData.IsEndless && endlessCharSource != null)
                ? endlessCharSource : currentStageData;
            Sprite s0   = src.trueIllustSprite0    ?? src.illustSprite0;
            Sprite s30  = src.trueIllustSprite30   ?? src.illustSprite30;
            Sprite s60  = src.trueIllustSprite60   ?? src.illustSprite60;
            Sprite sFul = src.trueIllustSpriteFull ?? src.illustSpriteFull;
            revealUI.SetStageData(src.illustColor30, src.illustColor60,
                                  src.illustColorFull, src.characterName,
                                  s0, s30, s60, sFul);
            revealUI.AdvanceToState(0);
        }

        // ボール復帰（1 つだけ、速度は初期値にリセット）
        if (ball != null)
        {
            // FreezeAllBalls で凍結時の位置がブロック内に重なる事故を防ぐため、
            // 物理シミュレーション再開より前にパドル位置へワープさせる。
            if (paddle != null)
            {
                Vector3 paddlePos = paddle.transform.position;
                ball.transform.position = new Vector3(paddlePos.x, paddlePos.y + 0.4f, 0f);
            }

            ball.gameObject.SetActive(true);
            // FreezeAllBalls で停止していた物理シミュレーションを再開
            var ballRb = ball.GetComponent<Rigidbody2D>();
            if (ballRb != null)
            {
                ballRb.velocity = Vector2.zero;
                ballRb.simulated = true;
            }
            ball.ResetBall();
            ball.ResetSpeedToBase(); // SpeedBlock による加速をリセット
            ball.SetPaddle(paddle.transform);
            activeBalls.Add(ball);
        }
        pendingSplitCount = 0;

        // BGM 再開（ピッチ 1.2 倍で緊迫感を演出）
        AudioManager.Instance?.PlayStageBGM(ResultData.StageNumber);
        AudioManager.Instance?.SetBGMPitch(1.2f);

        paddle?.SetActive(true);
        CurrentState = GameState.Ready;
    }


    private IEnumerator LoadResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 1f;
        SceneManager.LoadScene("ResultScene");
    }

    // ============================================================
    // エンドレスモード（ボスラッシュ）
    // ============================================================

    private int endlessWave = 0;             // 現在のステージ番号（0開始）
    private System.Random endlessRng;
    private StageData endlessCharSource;     // 出現キャラの抽選結果（イラスト・ボイス・ボスアイコン用）
    private int lastCharStageNum = -1;       // 直前のキャラ（連続同キャラ回避用）

    /// <summary>
    /// エンドレスの HP 倍率。1〜5体目は等倍（レイアウト自体の難易度で調整）、
    /// 6体目以降は 0.15 ずつ上昇していく。※数値は後で調整
    /// </summary>
    private float EndlessHPMulFor(int waveIndex)
        => waveIndex <= 4 ? 1f : 1f + 0.15f * (waveIndex - 4);

    /// <summary>
    /// エンドレスの難易度レイアウト（ブロック配置・HP・スピードブロック）を返す。
    /// 1体目=ステージ10相当、2体目=11、3体目=12、4体目=14、5体目以降=15相当で固定。
    /// 出現キャラは PickEndlessCharSource で別途ランダム抽選する。
    /// </summary>
    private StageData PickEndlessLayout(int waveIndex)
    {
        int[] earlyLayouts = { 10, 11, 12, 14 };
        int layoutNum = waveIndex < earlyLayouts.Length ? earlyLayouts[waveIndex] : 15;

        var all = Resources.LoadAll<StageData>("Stages");
        foreach (var s in all)
            if (s.stageNumber == layoutNum) return s;
        return null;
    }

    /// <summary>
    /// 出現キャラをランダム抽選する（等確率・直前と同じキャラは回避）。
    /// イラスト・敵ボイス・ボスアイコンがこのキャラに差し替わる。
    /// bossOnly=true（5の倍数ステージ）はボスステージ出身キャラ（5/10/15/20）限定＝裏イラスト持ち。
    /// </summary>
    private StageData PickEndlessCharSource(bool bossOnly)
    {
        var all = Resources.LoadAll<StageData>("Stages");
        var pool = new List<StageData>();
        foreach (var s in all)
        {
            if (bossOnly && s.stageNumber % 5 != 0) continue;
            pool.Add(s);
        }
        if (pool.Count == 0) return null;

        StageData pick = pool[endlessRng.Next(pool.Count)];
        for (int tries = 0; tries < 5 && pick.stageNumber == lastCharStageNum && pool.Count > 1; tries++)
            pick = pool[endlessRng.Next(pool.Count)];

        lastCharStageNum = pick.stageNumber;
        return pick;
    }

    /// <summary>
    /// 見た目（解放イラスト・敵ボイス）を抽選されたキャラへ差し替える。
    /// レイアウト（ブロック・ターン数・難度）は PickEndlessLayout のステージのまま。
    /// </summary>
    private void ApplyEndlessCharVisuals()
    {
        if (endlessCharSource == null) return;
        var src = endlessCharSource;

        revealUI?.SetStageData(src.illustColor30, src.illustColor60, src.illustColorFull,
                               src.characterName,
                               src.illustSprite0, src.illustSprite30,
                               src.illustSprite60, src.illustSpriteFull);

        stageCharData = null;
        if (!string.IsNullOrEmpty(src.characterName))
        {
            var allChars = Resources.LoadAll<CharacterData>("Characters");
            foreach (var cd in allChars)
                if (cd.characterName == src.characterName) { stageCharData = cd; break; }
        }
    }

    /// <summary>
    /// ウェーブクリア → スコア加算 → 次ウェーブを構築して続行。
    /// 構築手順は EnterTrueStage と同じ流儀（演出→効果リセット→ボール消去→再構築→復帰）。
    /// </summary>
    private IEnumerator EndlessNextWave()
    {
        // スコア加算（裏ボスまで倒したステージは +2）
        int gained = (stageManager != null && stageManager.IsTrueStage) ? 2 : 1;
        ResultData.EndlessScore += gained;
        HomeCharManager.AddEndlessKills(gained); // 累計撃破数（ホームきせかえのSSR解放条件）
        endlessWave++;
        Debug.Log($"[Endless] ステージ突破 スコア={ResultData.EndlessScore} 次={endlessWave + 1}体目");

        // デイリーミッション（エンドレスの1ステージ突破もクリア扱い）
        DailyMissionManager.ReportStageClear();

        CurrentState = GameState.Paused; // 演出中のミス誤判定防止
        paddle?.SetActive(false);
        FreezeAllBalls();

        // クリア演出（白フラッシュ + SE）
        AudioManager.Instance?.PlaySE(AudioManager.Instance.seClear);
        StartCoroutine(ScreenFlash(new Color(1f, 1f, 0.8f, 0.6f), 0.6f));
        yield return new WaitForSeconds(1.0f);

        // 一時効果リセット（裏ステージ突入時と同じ）
        CharacterManager.Instance?.ResetTemporaryEffects();
        FindObjectOfType<GameUI>()?.ClearUltEffectTimers();
        stageManager?.ResetBossAttackEffects();

        // 全ボール消去（クローン破棄 + オリジナル退避）
        for (int i = activeBalls.Count - 1; i >= 0; i--)
        {
            var b = activeBalls[i];
            if (b != null)
            {
                b.CancelTransparency();
                if (b.isClone)
                {
                    b.transform.position = new Vector3(9999f, 9999f, 0f);
                    Destroy(b.gameObject);
                }
            }
        }
        activeBalls.Clear();
        if (ball != null)
        {
            ball.CancelTransparency();
            ball.transform.position = new Vector3(9999f, 9999f, 0f);
            ball.gameObject.SetActive(false);
            ball.ResetBall();
        }

        // 次のレイアウトとキャラを選出（5の倍数ステージはボスキャラ4人から抽選）
        var next = PickEndlessLayout(endlessWave);
        endlessCharSource = PickEndlessCharSource(bossOnly: (endlessWave + 1) % 5 == 0);
        if (next == null)
        {
            Debug.LogWarning("[Endless] 次ステージの選出に失敗。リザルトへ");
            ResultData.IsClear = false;
            StartCoroutine(LoadResultAfterDelay(0.5f));
            yield break;
        }
        currentStageData = next;
        ResultData.StageNumber = endlessCharSource != null
            ? endlessCharSource.stageNumber : next.stageNumber; // BGM 用
        ResultData.IsTrueStageClear = false;

        // 再構築（HP倍率・キャラ差し替えを更新してから）
        triggered30 = false;
        triggered60 = false;
        stageManager.SetEndlessHPMultiplier(EndlessHPMulFor(endlessWave));
        stageManager.SetEndlessCharSource(endlessCharSource);
        stageManager.paddleRef = paddle;
        stageManager.BuildStage(next);

        // Boss イベントの購読やり直し
        stageManager.OnBossTurnExpired -= OnBossTurnExpired;
        stageManager.OnBossTurnExpired += OnBossTurnExpired;
        stageManager.OnBossHPRatioChanged -= CheckBossHPForReveal;
        stageManager.OnBossHPRatioChanged += CheckBossHPForReveal;

        // イラスト・敵ボイスを抽選キャラへ差し替え
        ApplyEndlessCharVisuals();
        revealUI?.AdvanceToState(0);

        // ボール復帰（1つだけ、速度リセット）
        if (ball != null)
        {
            if (paddle != null)
            {
                Vector3 paddlePos = paddle.transform.position;
                ball.transform.position = new Vector3(paddlePos.x, paddlePos.y + 0.4f, 0f);
            }
            ball.gameObject.SetActive(true);
            var ballRb = ball.GetComponent<Rigidbody2D>();
            if (ballRb != null)
            {
                ballRb.velocity = Vector2.zero;
                ballRb.simulated = true;
            }
            ball.ResetBall();
            ball.ResetSpeedToBase();
            ball.SetPaddle(paddle.transform);
            activeBalls.Add(ball);
        }
        pendingSplitCount = 0;

        // BGM・敵ボイスを新ステージのものへ
        AudioManager.Instance?.SetBGMPitch(1f);
        AudioManager.Instance?.PlayStageBGM(next.stageNumber);
        PlayEnemyVoice(cd => cd.voiceStageStart, AudioManager.VoicePriority.Mid);

        paddle?.SetActive(true);
        CurrentState = GameState.Ready;
    }

    // ---- 破壊率チェック ----

    /// <summary>
    /// ストックを加算する（ExtraStock パッシブ・StockRecover 奥義から呼ばれる）
    /// </summary>
    public void AddStock(int amount)
    {
        maxStock = Mathf.Max(maxStock, currentStock + amount);
        maxStock = Mathf.Min(maxStock, 7);
        currentStock = Mathf.Min(currentStock + amount, maxStock);
        OnStockChanged?.Invoke(currentStock);
    }

    private void CheckDestroyRate()
    {
        if (stageManager == null) return;

        float rate = stageManager.GetDestroyRate();
        OnDestroyRateChanged?.Invoke(rate);

        // Phase 2: 奥義ゲージ更新
        CharacterManager.Instance?.OnBlockDestroyed();

        // デイリーミッション（ブロック破壊カウント）
        DailyMissionManager.ReportBlockDestroyed();

        if (stageManager.IsAllCleared())
        {
            StageClear();
        }
    }

    /// <summary>
    /// ボスHP割合に応じてイラスト切替＋ボイス再生
    /// hpRatio: 1.0（満タン）→ 0.0（撃破）
    /// 70%以下 → 30%イラスト、40%以下 → 60%イラスト
    /// </summary>
    private void CheckBossHPForReveal(float hpRatio)
    {
        if (!triggered30 && hpRatio <= 0.7f)
        {
            triggered30 = true;
            Debug.Log("ボスHP 70%以下 → イラスト30%");
            revealUI?.AdvanceToState(1);
            PlayEnemyVoice(cd => cd.voiceDestroy30, AudioManager.VoicePriority.Mid);
        }
        if (!triggered60 && hpRatio <= 0.4f)
        {
            triggered60 = true;
            Debug.Log("ボスHP 40%以下 → イラスト60%");
            revealUI?.AdvanceToState(2);
            PlayEnemyVoice(cd => cd.voiceDestroy60, AudioManager.VoicePriority.Mid);
        }
    }

    // ---- キャラボイス再生ヘルパー ----

    /// <summary>ステージ敵キャラのボイスを再生する</summary>
    private void PlayEnemyVoice(System.Func<CharacterData, AudioClip> selector,
                                 AudioManager.VoicePriority priority = AudioManager.VoicePriority.Mid)
    {
        if (stageCharData == null) return;
        var clip = selector(stageCharData);
        AudioManager.Instance?.PlayVoice(clip, stageCharData.voiceVolumeMultiplier, priority);
    }

    /// <summary>味方キャラ（スロット0）のボイスを再生する</summary>
    private void PlayCharacterVoice(System.Func<CharacterData, AudioClip> selector,
                                     AudioManager.VoicePriority priority = AudioManager.VoicePriority.Mid)
    {
        if (CharacterManager.Instance == null) return;
        var chars = CharacterManager.Instance.GetSelectedCharacters();
        if (chars == null || chars.Length == 0 || chars[0] == null) return;
        var clip = selector(chars[0]);
        AudioManager.Instance?.PlayVoice(clip, chars[0].voiceVolumeMultiplier, priority);
    }

    // ---- リタイヤ（PauseMenu から呼ばれる） ----

    public void Retire()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("HomeScene");
    }

    // ---- Phase 6: 画面フラッシュ ----

    /// <summary>
    /// Phase B の赤フラッシュ中に表示する「危険」イラスト（または文字フォールバック）を生成する。
    /// 専用 Canvas（ScreenSpaceOverlay, sortingOrder=201）に直下配置。
    /// 画像は Resources/Game/danger.png（800×800 想定）。未配置なら従来の文字表示にフォールバック。
    /// </summary>
    private GameObject CreateDangerText()
    {
        var root = new GameObject("DangerTextCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 201; // ScreenFlash(200)より上
        var cs = root.AddComponent<UnityEngine.UI.CanvasScaler>();
        cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080f, 1920f);
        cs.matchWidthOrHeight = 0.5f; // 幅と高さの両方に追従
        root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 800×800 のイラスト（Resources/Game/danger.png）を優先表示
        var dangerSprite = Resources.Load<Sprite>("Game/danger");

        if (dangerSprite != null)
        {
            var imgGO = new GameObject("DangerImage");
            imgGO.transform.SetParent(root.transform, false);
            var img = imgGO.AddComponent<UnityEngine.UI.Image>();
            img.sprite = dangerSprite;
            img.preserveAspect = true; // 比率を保持
            img.raycastTarget = false;

            var rt = imgGO.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            // 参照解像度 1080×1920 に対して 800×800 で表示
            rt.sizeDelta = new Vector2(800f, 800f);
            rt.localScale = Vector3.zero; // 初期は非表示（パンチで拡大）
            return root;
        }

        // フォールバック：画像未配置時は従来の文字表示
        var txGO = new GameObject("DangerText");
        txGO.transform.SetParent(root.transform, false);
        var tx = txGO.AddComponent<UnityEngine.UI.Text>();
        tx.text = "Danger！";
        tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.fontSize = 140;
        tx.fontStyle = FontStyle.Bold;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = new Color(1f, 0.1f, 0.1f, 1f);
        tx.raycastTarget = false;
        tx.horizontalOverflow = HorizontalWrapMode.Overflow;
        tx.verticalOverflow = VerticalWrapMode.Overflow;

        // 黒アウトライン（±3px）
        var outline = txGO.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        var trt = txGO.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(1000f, 260f);
        trt.localScale = Vector3.zero; // 初期は非表示（パンチで拡大）

        return root;
    }

    /// <summary>
    /// 「危険」イラスト/テキストのパンチ拡大アニメ（0→1.2→1.0、duration 秒）
    /// フラッシュ1回ごとに呼び出す。子オブジェクト（Image または Text）に対してスケール適用。
    /// </summary>
    private IEnumerator PunchDangerText(Transform target, float duration)
    {
        if (target == null) yield break;
        var txTf = target.childCount > 0 ? target.GetChild(0) : target;

        float growEnd  = duration * 0.2f;
        float settleEnd = duration * 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (txTf == null) yield break;
            float s;
            if (elapsed < growEnd)
            {
                float k = elapsed / growEnd;
                s = Mathf.Lerp(0f, 1.2f, k);
            }
            else if (elapsed < settleEnd)
            {
                float k = (elapsed - growEnd) / (settleEnd - growEnd);
                s = Mathf.Lerp(1.2f, 1.0f, k);
            }
            else
            {
                s = 1.0f;
            }
            txTf.localScale = Vector3.one * s;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (txTf != null) txTf.localScale = Vector3.one;
    }

    private IEnumerator ScreenFlash(Color flashColor, float duration)
    {
        var go = new GameObject("ScreenFlash");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var img = go.AddComponent<Image>();
        img.color = flashColor;
        img.raycastTarget = false;

        float elapsed = 0f;
        Color endColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            img.color = Color.Lerp(flashColor, endColor, elapsed / duration);
            yield return null;
        }
        Destroy(go);
    }
}
