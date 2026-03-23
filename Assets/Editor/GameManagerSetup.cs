using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GachaBlock > Setup GameManager で GameManager を GameScene に自動配置する
/// </summary>
public static class GameManagerSetup
{
    [MenuItem("GachaBlock/Setup GameManager")]
    public static void SetupGameManager()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play モードを停止してから実行してください。", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");

        // 既存の GameManager を削除
        GameObject existing = GameObject.Find("GameManager");
        if (existing != null) Object.DestroyImmediate(existing);

        // GameManager オブジェクトを作成
        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();

        // シーン内の参照を取得してセット
        BallController ball = Object.FindObjectOfType<BallController>();
        PaddleController paddle = Object.FindObjectOfType<PaddleController>();
        StageManager stageManager = Object.FindObjectOfType<StageManager>();

        SerializedObject soGM = new SerializedObject(gm);
        if (ball != null)
            soGM.FindProperty("ball").objectReferenceValue = ball;
        if (paddle != null)
            soGM.FindProperty("paddle").objectReferenceValue = paddle;
        if (stageManager != null)
            soGM.FindProperty("stageManager").objectReferenceValue = stageManager;
        soGM.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene);

        string missing = "";
        if (ball == null) missing += "\n- Ball (BallController) が見つかりません";
        if (paddle == null) missing += "\n- Paddle (PaddleController) が見つかりません";
        if (stageManager == null) missing += "\n- StageManager が見つかりません";

        if (missing != "")
            EditorUtility.DisplayDialog("警告", "以下のコンポーネントが見つからず未設定です:" + missing, "OK");
        else
            EditorUtility.DisplayDialog("完了", "GameManager を GameScene に配置しました！\n\nPlay ボタンを押してテストしてください:\n- 画面タップでボール発射\n- ボールを落とすとストックが減る\n- ストック0でResultSceneへ遷移", "OK");
    }
}
