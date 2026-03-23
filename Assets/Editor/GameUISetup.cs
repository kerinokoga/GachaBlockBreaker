using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GachaBlock > Setup GameUI
/// GameScene に GameUI ランナーを配置する（UI はランタイムで自動構築）
/// </summary>
public static class GameUISetup
{
    [MenuItem("GachaBlock/Setup GameUI")]
    public static void SetupGameUI()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play モードを停止してから実行してください。", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");

        // 既存の GameCanvas と GameUIRunner を削除
        foreach (var name in new[] { "GameCanvas", "GameUIRunner" })
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        // GameUI ランナーを配置（UI は GameUI.Start() でランタイム構築）
        var runner = new GameObject("GameUIRunner");
        runner.AddComponent<GameUI>();

        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("完了",
            "GameUIRunner を GameScene に配置しました。\nPlay すると UI が自動構築されます。", "OK");
    }
}
