using UnityEngine;
using UnityEditor;

/// <summary>
/// デバッグ用: セーブデータ初期化メニュー
/// Tools > デバッグ > セーブデータを完全初期化
/// </summary>
public static class DebugResetMenu
{
    [MenuItem("Tools/デバッグ/全解放（全ステージクリア＋全キャラ覚醒＋エンドレス100体）")]
    public static void UnlockAll()
    {
        if (!EditorUtility.DisplayDialog("全解放デバッグ",
            "全ステージクリア・全キャラ所持＋覚醒・エンドレス累計100体＆自己ベスト100体 の状態にします。\n" +
            "（きせかえ等の解放条件テスト用）実行しますか？",
            "実行する", "キャンセル"))
            return;

        // 全ステージクリア（進行度・ベスト破壊率100%）
        for (int i = 1; i <= ProgressManager.TotalStages; i++)
            ProgressManager.SaveClear(i, 1f);

        // 全キャラ所持＋覚醒
        var allChars = Resources.LoadAll<CharacterData>("Characters");
        foreach (var cd in allChars)
        {
            if (cd == null) continue;
            string n = cd.characterName;
            PlayerPrefs.SetInt($"GachaBlock_Owned_{n}", 1);
            if (PlayerPrefs.GetInt($"GachaBlock_Count_{n}", 0) < 1)
                PlayerPrefs.SetInt($"GachaBlock_Count_{n}", 1);
            PlayerPrefs.SetInt($"GachaBlock_Awakened_{n}", 1);
        }

        // エンドレス累計100体撃破＋自己ベスト100体（きせかえバリアントの解放条件）
        if (PlayerPrefs.GetInt("GachaBlock_EndlessTotalKills", 0) < 100)
            PlayerPrefs.SetInt("GachaBlock_EndlessTotalKills", 100);
        if (PlayerPrefs.GetInt("GachaBlock_EndlessBest", 0) < 100)
            PlayerPrefs.SetInt("GachaBlock_EndlessBest", 100);

        PlayerPrefs.Save();
        Debug.Log($"[Debug] 全解放完了: ステージ{ProgressManager.TotalStages} / キャラ{allChars.Length}体 / エンドレス100体");
        EditorUtility.DisplayDialog("全解放デバッグ",
            "完了しました。Play中の場合はシーンを読み込み直すと反映されます。", "OK");
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
