using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Phase 5 セットアップメニュー（LoginScene / ShopScene / RankingScene + Build Settings）
/// </summary>
public static class Phase5Setup
{
    // ---- シーン作成 ----

    [MenuItem("GachaBlock/Phase5/1. Create LoginScene")]
    static void CreateLoginScene()
    {
        string path = "Assets/Scenes/LoginScene.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.12f);
        cam.orthographic = true;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        EditorSceneManager.MoveGameObjectToScene(camGo, scene);

        var go = new GameObject("LoginUI");
        go.AddComponent<LoginUI>();
        EditorSceneManager.MoveGameObjectToScene(go, scene);

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh();
        Debug.Log($"[Phase5] LoginScene 作成完了: {path}");
    }

    [MenuItem("GachaBlock/Phase5/2. Create ShopScene")]
    static void CreateShopScene()
    {
        string path = "Assets/Scenes/ShopScene.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.03f, 0.12f);
        cam.orthographic = true;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        EditorSceneManager.MoveGameObjectToScene(camGo, scene);

        var go = new GameObject("ShopUI");
        go.AddComponent<ShopUI>();
        EditorSceneManager.MoveGameObjectToScene(go, scene);

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh();
        Debug.Log($"[Phase5] ShopScene 作成完了: {path}");
    }

    [MenuItem("GachaBlock/Phase5/3. Create RankingScene")]
    static void CreateRankingScene()
    {
        string path = "Assets/Scenes/RankingScene.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.03f, 0.12f);
        cam.orthographic = true;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        EditorSceneManager.MoveGameObjectToScene(camGo, scene);

        var go = new GameObject("RankingUI");
        go.AddComponent<RankingUI>();
        EditorSceneManager.MoveGameObjectToScene(go, scene);

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh();
        Debug.Log($"[Phase5] RankingScene 作成完了: {path}");
    }

    [MenuItem("GachaBlock/Phase5/4. Fix Build Settings (11 scenes)")]
    static void FixBuildSettings()
    {
        string[] scenes =
        {
            "Assets/Scenes/LoginScene.unity",       // 0
            "Assets/Scenes/HomeScene.unity",         // 1
            "Assets/Scenes/StageSelectScene.unity",  // 2
            "Assets/Scenes/CharaSelectScene.unity",  // 3
            "Assets/Scenes/GameScene.unity",         // 4
            "Assets/Scenes/ResultScene.unity",       // 5
            "Assets/Scenes/CollectionScene.unity",   // 6
            "Assets/Scenes/GachaScene.unity",        // 7
            "Assets/Scenes/CharaManageScene.unity",  // 8
            "Assets/Scenes/ShopScene.unity",         // 9
            "Assets/Scenes/RankingScene.unity",      // 10
        };

        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);

        EditorBuildSettings.scenes = buildScenes;
        Debug.Log("[Phase5] Build Settings 更新完了 (11シーン, LoginScene=0)");
    }

    // ---- PresentBoxScene 作成 ----

    [MenuItem("GachaBlock/Phase5/5. Create PresentBoxScene")]
    static void CreatePresentBoxScene()
    {
        string path = "Assets/Scenes/PresentBoxScene.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.02f, 0.1f);
        cam.orthographic = true;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        EditorSceneManager.MoveGameObjectToScene(camGo, scene);

        var go = new GameObject("PresentBoxUI");
        go.AddComponent<PresentBoxUI>();
        EditorSceneManager.MoveGameObjectToScene(go, scene);

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh();
        Debug.Log($"[Phase5] PresentBoxScene 作成完了: {path}");
    }

    [MenuItem("GachaBlock/Phase5/6. Fix Build Settings (12 scenes)")]
    static void FixBuildSettings12()
    {
        string[] scenes =
        {
            "Assets/Scenes/LoginScene.unity",         // 0
            "Assets/Scenes/HomeScene.unity",           // 1
            "Assets/Scenes/StageSelectScene.unity",    // 2
            "Assets/Scenes/CharaSelectScene.unity",    // 3
            "Assets/Scenes/GameScene.unity",           // 4
            "Assets/Scenes/ResultScene.unity",         // 5
            "Assets/Scenes/CollectionScene.unity",     // 6
            "Assets/Scenes/GachaScene.unity",          // 7
            "Assets/Scenes/CharaManageScene.unity",    // 8
            "Assets/Scenes/ShopScene.unity",           // 9
            "Assets/Scenes/RankingScene.unity",        // 10
            "Assets/Scenes/PresentBoxScene.unity",     // 11
        };

        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);

        EditorBuildSettings.scenes = buildScenes;
        Debug.Log("[Phase5] Build Settings 更新完了 (12シーン, PresentBoxScene=11)");
    }

    // ---- デバッグ ----

    [MenuItem("GachaBlock/Phase5/Debug: Add Test Presents")]
    static void DebugAddTestPresents()
    {
        PresentBoxManager.AddTestPresents();
    }

    [MenuItem("GachaBlock/Phase5/Debug: Force Cloud Save")]
    static void DebugCloudSave()
    {
        CloudSaveManager.Save();
        Debug.Log("[Phase5] クラウドセーブ完了");
    }

    [MenuItem("GachaBlock/Phase5/Debug: Force Cloud Load")]
    static void DebugCloudLoad()
    {
        if (CloudSaveManager.Load())
            Debug.Log("[Phase5] クラウドロード完了");
        else
            Debug.LogWarning("[Phase5] クラウドロード失敗（データなし or ログインなし）");
    }

    [MenuItem("GachaBlock/Phase5/Debug: Generate Dummy Rankings")]
    static void DebugGenerateRankings()
    {
        RankingManager.GenerateDummyData();
        Debug.Log("[Phase5] ダミーランキングデータ生成完了");
    }
}
