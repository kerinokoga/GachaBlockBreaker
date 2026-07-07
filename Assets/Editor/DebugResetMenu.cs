using UnityEngine;
using UnityEditor;

/// <summary>
/// デバッグ用: セーブデータ初期化メニュー
/// Tools > デバッグ > セーブデータを完全初期化
/// </summary>
public static class DebugResetMenu
{
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
