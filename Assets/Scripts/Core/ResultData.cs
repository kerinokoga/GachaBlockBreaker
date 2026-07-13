/// <summary>
/// シーン間でリザルトデータを受け渡すための静的クラス
/// </summary>
public static class ResultData
{
    public static bool IsClear;
    public static bool IsFirstClear;   // 初回クリアかどうか（オーブ報酬表示に使用）
    public static bool IsTrueStageClear; // 裏ステージクリアかどうか（イラスト差し替えに使用）
    public static float DestroyRate;
    public static int RemainingStock;
    public static int StageNumber;

    // エンドレスモード
    public static bool IsEndless;      // エンドレスモードで開始するか（キャラ選択→GameScene へ引き継ぐ）
    public static int EndlessScore;    // 突破ウェーブ数（リザルト表示・ランキング送信に使用）

    // Phase 2: キャラ選択データの受け渡し（名前文字列で保持）
    public static string[] SelectedCharacterNames = new string[3];
}
