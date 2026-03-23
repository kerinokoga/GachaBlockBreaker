using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// メニュー「GachaBlock > Setup Scenes」でシーンを自動生成するエディタツール
/// </summary>
public static class SceneSetup
{
    [MenuItem("GachaBlock/Setup GameScene")]
    public static void SetupGameScene()
    {
        // 新しいシーンを作成
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- カメラ ----
        GameObject cameraGo = new GameObject("Main Camera");
        Camera cam = cameraGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9.6f;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cameraGo.transform.position = new Vector3(0, 0, -10);
        cameraGo.tag = "MainCamera";
        cameraGo.AddComponent<AudioListener>();

        // ---- Walls 親オブジェクト ----
        GameObject walls = new GameObject("Walls");
        walls.transform.position = Vector3.zero;

        // 左壁
        CreateWall("WallLeft", walls.transform,
            new Vector3(-5.5f, 0, 0),
            new Vector2(1f, 22f));

        // 右壁
        CreateWall("WallRight", walls.transform,
            new Vector3(5.5f, 0, 0),
            new Vector2(1f, 22f));

        // 天井
        CreateWall("WallTop", walls.transform,
            new Vector3(0, 10.5f, 0),
            new Vector2(12f, 1f));

        // ---- DeathZone ----
        GameObject deathZone = new GameObject("DeathZone");
        deathZone.transform.position = new Vector3(0, -11f, 0);
        BoxCollider2D dz = deathZone.AddComponent<BoxCollider2D>();
        dz.size = new Vector2(12f, 1f);
        dz.isTrigger = true;  // Is Trigger ON
        deathZone.tag = "DeathZone";

        // ---- BlockParent (ブロック配置の親) ----
        GameObject blockParent = new GameObject("BlockParent");
        blockParent.transform.position = Vector3.zero;

        // ---- シーンを保存 ----
        string scenePath = "Assets/Scenes/GameScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // Build Settings にシーンを追加
        AddSceneToBuildSettings(scenePath);

        Debug.Log("GameScene を作成しました: " + scenePath);
        EditorUtility.DisplayDialog("完了", "GameScene を作成しました！\nAssets/Scenes/GameScene.unity", "OK");
    }

    [MenuItem("GachaBlock/Setup HomeScene")]
    public static void SetupHomeScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraGo = new GameObject("Main Camera");
        Camera cam = cameraGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9.6f;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cameraGo.transform.position = new Vector3(0, 0, -10);
        cameraGo.tag = "MainCamera";
        cameraGo.AddComponent<AudioListener>();

        // Canvas (UI用)
        CreateBasicCanvas("HomeCanvas");

        // EventSystem
        CreateEventSystem();

        string scenePath = "Assets/Scenes/HomeScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AddSceneToBuildSettings(scenePath);

        Debug.Log("HomeScene を作成しました: " + scenePath);
        EditorUtility.DisplayDialog("完了", "HomeScene を作成しました！\nAssets/Scenes/HomeScene.unity", "OK");
    }

    [MenuItem("GachaBlock/Setup ResultScene")]
    public static void SetupResultScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraGo = new GameObject("Main Camera");
        Camera cam = cameraGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9.6f;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cameraGo.transform.position = new Vector3(0, 0, -10);
        cameraGo.tag = "MainCamera";
        cameraGo.AddComponent<AudioListener>();

        CreateBasicCanvas("ResultCanvas");
        CreateEventSystem();

        string scenePath = "Assets/Scenes/ResultScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AddSceneToBuildSettings(scenePath);

        Debug.Log("ResultScene を作成しました: " + scenePath);
        EditorUtility.DisplayDialog("完了", "ResultScene を作成しました！\nAssets/Scenes/ResultScene.unity", "OK");
    }

    [MenuItem("GachaBlock/Setup All Scenes")]
    public static void SetupAllScenes()
    {
        SetupHomeScene();
        SetupGameScene();
        SetupResultScene();
        EditorUtility.DisplayDialog("全シーン作成完了",
            "HomeScene / GameScene / ResultScene をすべて作成しました！", "OK");
    }

    // ---- ヘルパー ----

    private static void CreateWall(string name, Transform parent, Vector3 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    private static GameObject CreateBasicCanvas(string name)
    {
        GameObject canvasGo = new GameObject(name);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // CanvasScaler の設定（1080x1920 縦持ち基準）
        UnityEngine.UI.CanvasScaler scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvasGo;
    }

    private static void CreateEventSystem()
    {
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return; // すでに登録済み
        }

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, newScenes, scenes.Length);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
    }
}
