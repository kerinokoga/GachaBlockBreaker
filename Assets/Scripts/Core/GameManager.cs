using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

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

    // ---- 破壊率トリガー ----
    private bool triggered30 = false;
    private bool triggered60 = false;

    // ---- Phase 3: 美少女解放 ----
    private RevealUI revealUI;

    // ---- 外部公開 ----
    public int CurrentStock => currentStock;
    public int MaxStock => maxStock;

    // UI が購読するイベント
    public System.Action<int> OnStockChanged;       // 残りストック数
    public System.Action<float> OnDestroyRateChanged; // 破壊率 0.0〜1.0

    // ---- 初期化 ----

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        currentStock = maxStock;
        CurrentState = GameState.Ready;

        // StageManager の破壊イベントを購読
        if (stageManager != null)
            stageManager.OnBlockDestroyedCallback += CheckDestroyRate;

        // BallController のミスイベントを購読
        if (ball != null)
        {
            ball.OnMissed += OnMiss;
            ball.SetPaddle(paddle.transform);
        }

        // Phase 3: RevealUI を取得
        revealUI = FindObjectOfType<RevealUI>();

        // StageData を Resources からロード（なければテストステージ）
        if (ResultData.StageNumber <= 0) ResultData.StageNumber = 1; // 直接起動時のフォールバック
        if (stageManager != null)
        {
            var allStages = Resources.LoadAll<StageData>("Stages");
            StageData stageData = null;
            foreach (var s in allStages)
                if (s.stageNumber == ResultData.StageNumber) { stageData = s; break; }

            if (stageData != null)
            {
                stageManager.BuildStage(stageData);
                revealUI?.SetStageData(stageData.illustColor30, stageData.illustColor60,
                                       stageData.illustColorFull, stageData.characterName);
            }
            else
            {
                stageManager.BuildTestStage();
            }
        }

        // Phase 2: キャラクターマネージャー初期化（パッシブ適用）
        CharacterManager.Instance?.Initialize(ball, this);

        OnStockChanged?.Invoke(currentStock);

        // Phase 6: ゲーム BGM 再生
        AudioManager.Instance?.PlayBGMForScene("GameScene");
    }

    // ---- 入力 ----

    void Update()
    {
        // Ready 状態でタップ/クリックしたらボール発射
        if (CurrentState == GameState.Ready)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                StartGame();
        }
    }

    // ---- 状態遷移 ----

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        ball?.Launch();
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

    private void OnMiss()
    {
        if (CurrentState == GameState.Miss || CurrentState == GameState.GameOver) return;
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
            ball?.ResetBall();
            ball?.SetPaddle(paddle.transform);
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
            GameOver();
        }
        else
        {
            // ボールをリセットして Ready 状態に戻す
            ball?.ResetBall();
            ball?.SetPaddle(paddle.transform);
            paddle?.SetActive(true);
            CurrentState = GameState.Ready;
        }
    }

    private void GameOver()
    {
        CurrentState = GameState.GameOver;

        // Phase 6: ゲームオーバー SE + BGM 停止
        AudioManager.Instance?.PlaySE(AudioManager.Instance.seGameOver);
        AudioManager.Instance?.StopBGM();

        ResultData.IsClear = false;
        ResultData.DestroyRate = stageManager != null ? stageManager.GetDestroyRate() : 0f;
        ResultData.RemainingStock = 0;
        // StageNumber はそのまま保持

        StartCoroutine(LoadResultAfterDelay(1.5f));
    }

    private void StageClear()
    {
        CurrentState = GameState.Clear;
        paddle?.SetActive(false);

        // Phase 3: フル解放演出
        revealUI?.AdvanceToState(3);

        // Phase 6: クリア SE + 白フラッシュ + BGM 停止
        AudioManager.Instance?.PlaySE(AudioManager.Instance.seClear);
        AudioManager.Instance?.StopBGM();
        StartCoroutine(ScreenFlash(new Color(1f, 1f, 0.8f, 0.7f), 0.8f));

        ResultData.IsClear = true;
        ResultData.IsFirstClear = !ProgressManager.IsCleared(ResultData.StageNumber); // SaveClear より前に判定
        ResultData.DestroyRate = 1f;
        ResultData.RemainingStock = currentStock;
        // StageNumber はそのまま保持（変更しない）

        // Phase 3: 進行保存
        ProgressManager.SaveClear(ResultData.StageNumber, 1f);

        StartCoroutine(LoadResultAfterDelay(2.0f));
    }

    private IEnumerator LoadResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 1f;
        SceneManager.LoadScene("ResultScene");
    }

    // ---- 破壊率チェック ----

    /// <summary>
    /// ストックを加算する（ExtraStock パッシブ・StockRecover 奥義から呼ばれる）
    /// </summary>
    public void AddStock(int amount)
    {
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

        if (!triggered30 && rate >= 0.3f)
        {
            triggered30 = true;
            OnTrigger30Percent();
        }
        if (!triggered60 && rate >= 0.6f)
        {
            triggered60 = true;
            OnTrigger60Percent();
        }
        if (stageManager.IsAllCleared())
        {
            StageClear();
        }
    }

    private void OnTrigger30Percent()
    {
        Debug.Log("破壊率 30% 達成");
        revealUI?.AdvanceToState(1);
    }

    private void OnTrigger60Percent()
    {
        Debug.Log("破壊率 60% 達成");
        revealUI?.AdvanceToState(2);
    }

    // ---- リタイヤ（PauseMenu から呼ばれる） ----

    public void Retire()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("HomeScene");
    }

    // ---- Phase 6: 画面フラッシュ ----

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
