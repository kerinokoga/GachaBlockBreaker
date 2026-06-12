using UnityEngine;

/// <summary>
/// チュートリアルの状態を管理するシングルトン。
/// シーン跨ぎで永続化、PlayerPrefs で完了/スキップ状態を保存。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    /// <summary>チュートリアルの進行段階</summary>
    public enum Step
    {
        None,         // 未起動
        Intro,        // 初回問いかけ「はい/いいえ」
        GuideStart,   // ホーム画面でスタートボタンへ誘導
        StageSelect,  // ステージ選択画面の解説
        CharaSelect,  // キャラ選択画面の解説
        GameScreen,   // ゲーム画面解説
        GamePlay,     // 初回プレイ（ゲームオーバー無効）
        Result,       // クリア後解説
        PresentBox,   // プレゼント受取誘導
        Gacha,        // ガチャ画面誘導
        GachaPull,    // ガチャ1回引かせる
        Completed     // 完了
    }

    const string KeyCompleted   = "Tutorial_Completed";
    const string KeySkipped     = "Tutorial_Skipped";
    const string KeyCurrentStep = "Tutorial_CurrentStep";

    public Step CurrentStep { get; private set; } = Step.None;

    /// <summary>チュートリアル進行中（非None & 非Completed）</summary>
    public bool IsActive =>
        CurrentStep != Step.None && CurrentStep != Step.Completed;

    /// <summary>「いいえ」で永続スキップしたか</summary>
    public bool IsSkipped =>
        PlayerPrefs.GetInt(KeySkipped, 0) == 1;

    /// <summary>完了済みか</summary>
    public bool IsCompleted =>
        PlayerPrefs.GetInt(KeyCompleted, 0) == 1;

    /// <summary>初回起動時にチュートリアルを表示すべきか</summary>
    public bool ShouldShowTutorial =>
        !IsCompleted && !IsSkipped;

    /// <summary>シングルトン初期化＋シーン跨ぎ持続</summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>シングルトンが存在しなければ生成（HomeUI 等から呼び出し）</summary>
    public static void EnsureInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("TutorialManager");
        go.AddComponent<TutorialManager>();
    }

    /// <summary>現在ステップを設定</summary>
    public void SetStep(Step step)
    {
        CurrentStep = step;
        PlayerPrefs.SetInt(KeyCurrentStep, (int)step);
        PlayerPrefs.Save();
        Debug.Log($"[Tutorial] Step → {step}");
    }

    /// <summary>「いいえ」を選んだ → 永続スキップ</summary>
    public void SkipAll()
    {
        CurrentStep = Step.None;
        PlayerPrefs.SetInt(KeySkipped, 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] スキップされました（永続）");
    }

    /// <summary>全ステップ完了</summary>
    public void Complete()
    {
        CurrentStep = Step.Completed;
        PlayerPrefs.SetInt(KeyCompleted, 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] 完了しました");
    }

    /// <summary>デバッグ用：チュートリアル状態をリセット</summary>
    public void ResetProgress()
    {
        CurrentStep = Step.None;
        PlayerPrefs.DeleteKey(KeyCompleted);
        PlayerPrefs.DeleteKey(KeySkipped);
        PlayerPrefs.DeleteKey(KeyCurrentStep);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] リセット完了");
    }
}
