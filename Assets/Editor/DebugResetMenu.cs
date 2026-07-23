using UnityEngine;
using UnityEditor;

/// <summary>
/// デバッグ用: セーブデータ初期化メニュー
/// Tools > デバッグ > セーブデータを完全初期化
/// </summary>
public static class DebugResetMenu
{
    [MenuItem("Tools/デバッグ/全解放（全ステージクリア＋全キャラ覚醒＋エンドレス500体）")]
    public static void UnlockAll()
    {
        if (!EditorUtility.DisplayDialog("全解放デバッグ",
            "全ステージクリア・全キャラ所持＋覚醒・エンドレス累計500体＆自己ベスト100体 の状態にします。\n" +
            "（ギャラリー・きせかえ等の解放条件テスト用）実行しますか？",
            "実行する", "キャンセル"))
            return;

        DebugUnlock.UnlockAll();
        EditorUtility.DisplayDialog("全解放デバッグ",
            "完了しました。Play中の場合はシーンを読み込み直すと反映されます。", "OK");
    }

    [MenuItem("Tools/デバッグ/ランキング送信の停止を切替")]
    public static void ToggleRankingSubmission()
    {
        bool next = !RankingManager.SubmissionBlocked;
        RankingManager.SetSubmissionBlocked(next);
        EditorUtility.DisplayDialog("ランキング送信",
            next
                ? "停止しました。デバッグプレイのスコアは全国ランキングへ送信されません。\n（全解放デバッグの実行時も自動で停止になります）"
                : "有効にしました。スコアが全国ランキングへ送信されます。",
            "OK");
    }

    [MenuItem("Tools/デバッグ/エンドレス強化カード全種テストを切替")]
    public static void ToggleEndlessCardDebug()
    {
        EndlessBuffManager.DebugDrawAll = !EndlessBuffManager.DebugDrawAll;
        Debug.Log($"[Debug] エンドレス強化カード全種テスト: {(EndlessBuffManager.DebugDrawAll ? "ON（カタログ順に3枚ずつ提示）" : "OFF（通常抽選）")}");
        EditorUtility.DisplayDialog("エンドレス強化カード",
            EndlessBuffManager.DebugDrawAll
                ? "ON: 撃破ごとにカタログ順で3枚ずつ提示されます（全22種の表示確認用）。"
                : "OFF: 通常のレア度抽選（R87% / SR10% / SSR3%）に戻しました。",
            "OK");
    }

    [MenuItem("Tools/デバッグ/エンドレス実績をリセット（ギャラリー解放テスト用）")]
    public static void ResetEndless()
    {
        if (!EditorUtility.DisplayDialog("エンドレス実績リセット",
            "エンドレスの累計撃破・自己ベスト・ギャラリー確認状態をリセットします。\n" +
            "（リザルトの解放演出やバッジを最初から確認できます）実行しますか？",
            "実行する", "キャンセル"))
            return;

        PlayerPrefs.DeleteKey("GachaBlock_EndlessTotalKills");
        PlayerPrefs.DeleteKey("GachaBlock_EndlessBest");
        PlayerPrefs.DeleteKey("GachaBlock_GallerySeenTotal");
        PlayerPrefs.DeleteKey("GachaBlock_GallerySeenBest");
        PlayerPrefs.DeleteKey("GachaBlock_GalleryViewTotal");
        PlayerPrefs.DeleteKey("GachaBlock_GalleryViewBest");
        PlayerPrefs.Save();
        Debug.Log("[Debug] エンドレス実績をリセットしました");
        EditorUtility.DisplayDialog("デバッグ",
            "リセットしました。エンドレスを1回プレイすると\n累計1体などの解放演出がリザルトで確認できます。", "OK");
    }

    [MenuItem("Tools/デバッグ/ステージ20を未クリアに戻す（初回報酬テスト用）")]
    public static void UnclearStage20()
    {
        PlayerPrefs.DeleteKey("GachaBlock_Cleared_20");
        PlayerPrefs.DeleteKey("GachaBlock_Rate_20");
        PlayerPrefs.DeleteKey("GachaBlock_TrueCleared_20");
        PlayerPrefs.Save();
        Debug.Log("[Debug] ステージ20を未クリア状態に戻しました（挑戦は可能なまま）");
        EditorUtility.DisplayDialog("デバッグ",
            "ステージ20を未クリアに戻しました。\nクリアすると初回報酬（オーブ＋コレクション）の表記が確認できます。", "OK");
    }

    [MenuItem("Tools/デバッグ/セーブデータを完全初期化")]
    public static void ResetAll()
    {
        bool playing = Application.isPlaying;
        string detail = playing
            ? "ローカルセーブを全削除し、Firebase からログアウトします。\n（次回 Play 時に新しいゲストとして開始されます）"
            : "ローカルセーブを全削除します。\n\n※Play していないため Firebase のログイン状態は残ります。\n完全に新規ゲストにしたい場合は、Play 中にこのメニューを実行してください。";

        if (!EditorUtility.DisplayDialog("セーブデータ初期化", detail, "初期化する", "キャンセル"))
            return;

        if (playing)
        {
            try { AuthManager.Logout(); Debug.Log("[Debug] Firebase からログアウトしました"); }
            catch (System.Exception e) { Debug.LogWarning($"[Debug] ログアウト失敗: {e.Message}"); }
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[Debug] PlayerPrefs を全削除しました。Play を停止して再度 Play してください");
    }
}
