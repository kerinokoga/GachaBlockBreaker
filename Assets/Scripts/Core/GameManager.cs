using UnityEngine;
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

        // テストステージを自動生成（StageData 未設定時）
        if (stageManager != null)
            stageManager.BuildTestStage();

        OnStockChanged?.Invoke(currentStock);
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

        ResultData.IsClear = false;
        ResultData.DestroyRate = stageManager != null ? stageManager.GetDestroyRate() : 0f;
        ResultData.RemainingStock = 0;
        ResultData.StageNumber = 1;

        StartCoroutine(LoadResultAfterDelay(1.5f));
    }

    private void StageClear()
    {
        CurrentState = GameState.Clear;
        paddle?.SetActive(false);

        ResultData.IsClear = true;
        ResultData.DestroyRate = 1f;
        ResultData.RemainingStock = currentStock;
        ResultData.StageNumber = 1;

        StartCoroutine(LoadResultAfterDelay(1.5f));
    }

    private IEnumerator LoadResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 1f;
        SceneManager.LoadScene("ResultScene");
    }

    // ---- 破壊率チェック ----

    private void CheckDestroyRate()
    {
        if (stageManager == null) return;

        float rate = stageManager.GetDestroyRate();
        OnDestroyRateChanged?.Invoke(rate);

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

    // Phase 3 で美少女イラスト演出を追加する拡張ポイント
    private void OnTrigger30Percent()
    {
        Debug.Log("破壊率 30% 達成");
    }

    private void OnTrigger60Percent()
    {
        Debug.Log("破壊率 60% 達成");
    }

    // ---- リタイヤ（PauseMenu から呼ばれる） ----

    public void Retire()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("HomeScene");
    }
}
