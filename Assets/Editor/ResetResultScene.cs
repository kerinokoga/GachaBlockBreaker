using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ResetResultScene
{
    [MenuItem("GachaBlock/Reset All UI Scenes")]
    public static void ResetAll()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play モードを停止してください。", "OK");
            return;
        }

        ResetScene("Assets/Scenes/HomeScene.unity", "HomeUIRunner", typeof(HomeUI));
        ResetScene("Assets/Scenes/ResultScene.unity", "ResultUIRunner", typeof(ResultUI));

        EditorUtility.DisplayDialog("完了",
            "HomeScene / ResultScene をリセットしました。\nHomeScene を開いて Play してください。", "OK");
    }

    static void ResetScene(string scenePath, string runnerName, System.Type uiType)
    {
        var scene = EditorSceneManager.OpenScene(scenePath);

        foreach (GameObject go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        // カメラ
        GameObject cam = new GameObject("Main Camera");
        Camera c = cam.AddComponent<Camera>();
        c.orthographic = true;
        c.orthographicSize = 9.6f;
        c.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
        c.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.tag = "MainCamera";
        cam.AddComponent<AudioListener>();

        // UI ランナー
        GameObject runner = new GameObject(runnerName);
        runner.AddComponent(uiType);

        EditorSceneManager.SaveScene(scene);
    }
}
