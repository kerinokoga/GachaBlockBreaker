#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Phase3Setup
{
    // ---- 1. ステージアセット生成 ----

    [MenuItem("GachaBlock/Phase3/1. Create Stage Assets")]
    static void CreateStageAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Stages"))
            AssetDatabase.CreateFolder("Assets/Resources", "Stages");

        CreateStage(1, "Luna",
            new Color(0.35f, 0.5f, 0.8f),
            new Color(0.5f, 0.6f, 0.9f),
            new Color(0.7f, 0.85f, 1.0f),
            Stage1Blocks);

        CreateStage(2, "Aria",
            new Color(0.7f, 0.3f, 0.5f),
            new Color(0.85f, 0.45f, 0.65f),
            new Color(1.0f, 0.7f, 0.85f),
            Stage2Blocks);

        CreateStage(3, "Sera",
            new Color(0.6f, 0.4f, 0.2f),
            new Color(0.8f, 0.55f, 0.25f),
            new Color(1.0f, 0.75f, 0.35f),
            Stage3Blocks);

        CreateStage(4, "Luna",
            new Color(0.4f, 0.2f, 0.6f),
            new Color(0.6f, 0.3f, 0.8f),
            new Color(0.85f, 0.5f, 1.0f),
            Stage4Blocks);

        CreateStage(5, "Luna",
            new Color(0.6f, 0.5f, 0.1f),
            new Color(0.8f, 0.7f, 0.15f),
            new Color(1.0f, 0.92f, 0.3f),
            Stage5Blocks);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Phase3 Setup",
            "ステージアセット5体を生成しました！\nAssets/Resources/Stages/ を確認してください。", "OK");
    }

    static void CreateStage(int num, string charName,
        Color c30, Color c60, Color cFull,
        System.Action<StageData> fillBlocks)
    {
        string path = $"Assets/Resources/Stages/Stage{num}.asset";
        var sd = AssetDatabase.LoadAssetAtPath<StageData>(path);
        if (sd == null)
        {
            sd = ScriptableObject.CreateInstance<StageData>();
            AssetDatabase.CreateAsset(sd, path);
        }
        sd.stageNumber    = num;
        sd.characterName  = charName;
        sd.illustColor30  = c30;
        sd.illustColor60  = c60;
        sd.illustColorFull = cFull;
        sd.blocks.Clear();
        fillBlocks(sd);
        EditorUtility.SetDirty(sd);
        Debug.Log($"Stage{num} 作成完了: {sd.blocks.Count} ブロック");
    }

    static void B(StageData sd, int col, int row,
        BlockType type = BlockType.Normal, int hp = 1)
    {
        sd.blocks.Add(new BlockPlacementData
        {
            blockType = type,
            gridPosition = new Vector2Int(col, row),
            hp = hp
        });
    }

    // Stage 1: 5×3 Normal（15ブロック）
    static void Stage1Blocks(StageData sd)
    {
        for (int row = 0; row < 3; row++)
            for (int col = 2; col <= 6; col++)
                B(sd, col, row);
    }

    // Stage 2: 7行×2 Normal + Durable×2（17ブロック）
    static void Stage2Blocks(StageData sd)
    {
        for (int col = 1; col <= 7; col++) B(sd, col, 0);
        B(sd, 2, 1); B(sd, 4, 1); B(sd, 6, 1);
        B(sd, 3, 1, BlockType.Durable, 2);
        B(sd, 5, 1, BlockType.Durable, 2);
        for (int col = 1; col <= 7; col++) B(sd, col, 2);
    }

    // Stage 3: ExplosionBlock 入り（26ブロック）
    static void Stage3Blocks(StageData sd)
    {
        for (int col = 0; col <= 8; col++) B(sd, col, 0);
        B(sd, 1, 1); B(sd, 3, 1); B(sd, 5, 1); B(sd, 7, 1);
        B(sd, 2, 1, BlockType.Explosion);
        B(sd, 6, 1, BlockType.Explosion);
        for (int col = 0; col <= 8; col++) B(sd, col, 2);
        B(sd, 2, 3, BlockType.Durable, 2);
        B(sd, 4, 3, BlockType.Durable, 2);
        B(sd, 6, 3, BlockType.Durable, 2);
    }

    // Stage 4: ChainBlock 入り（23ブロック）
    static void Stage4Blocks(StageData sd)
    {
        for (int col = 0; col <= 8; col += 2) B(sd, col, 0);
        B(sd, 0, 1); B(sd, 2, 1); B(sd, 4, 1); B(sd, 6, 1); B(sd, 8, 1);
        B(sd, 1, 1, BlockType.Chain);
        B(sd, 3, 1, BlockType.Chain);
        B(sd, 5, 1, BlockType.Chain);
        B(sd, 7, 1, BlockType.Chain);
        for (int col = 0; col <= 8; col += 2) B(sd, col, 2);
        B(sd, 1, 3, BlockType.Durable, 3);
        B(sd, 3, 3, BlockType.Durable, 3);
        B(sd, 5, 3, BlockType.Durable, 3);
        B(sd, 7, 3, BlockType.Durable, 3);
    }

    // Stage 5: 全種混合（37ブロック）
    static void Stage5Blocks(StageData sd)
    {
        for (int col = 0; col <= 8; col++) B(sd, col, 0);
        B(sd, 0, 1, BlockType.Durable, 2); B(sd, 4, 1, BlockType.Durable, 2); B(sd, 8, 1, BlockType.Durable, 2);
        B(sd, 2, 1, BlockType.Explosion); B(sd, 6, 1, BlockType.Explosion);
        B(sd, 1, 2, BlockType.Chain); B(sd, 2, 2, BlockType.Chain);
        B(sd, 3, 2); B(sd, 4, 2); B(sd, 5, 2);
        B(sd, 6, 2, BlockType.Chain); B(sd, 7, 2, BlockType.Chain);
        B(sd, 0, 3, BlockType.Durable, 3); B(sd, 4, 3, BlockType.Durable, 3); B(sd, 8, 3, BlockType.Durable, 3);
        B(sd, 2, 3, BlockType.Explosion); B(sd, 6, 3, BlockType.Explosion);
        for (int col = 1; col <= 7; col += 2) B(sd, col, 4);
    }

    // ---- 2. StageSelectScene 生成 ----

    [MenuItem("GachaBlock/Phase3/2. Setup StageSelectScene")]
    static void SetupStageSelectScene()
    {
        CreateScene("Assets/Scenes/StageSelectScene.unity", "StageSelectUIRunner",
            typeof(StageSelectUI));
        EditorUtility.DisplayDialog("Phase3 Setup", "StageSelectScene を作成しました！", "OK");
    }

    // ---- 3. CollectionScene 生成 ----

    [MenuItem("GachaBlock/Phase3/3. Setup CollectionScene")]
    static void SetupCollectionScene()
    {
        CreateScene("Assets/Scenes/CollectionScene.unity", "CollectionUIRunner",
            typeof(CollectionUI));
        EditorUtility.DisplayDialog("Phase3 Setup", "CollectionScene を作成しました！", "OK");
    }

    static void CreateScene(string path, string runnerName, System.Type uiType)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.18f);
        cam.orthographic = true;
        camGo.tag = "MainCamera";
        SceneManager.MoveGameObjectToScene(camGo, scene);

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        SceneManager.MoveGameObjectToScene(esGo, scene);

        var runner = new GameObject(runnerName);
        runner.AddComponent(uiType);
        SceneManager.MoveGameObjectToScene(runner, scene);

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log($"シーン作成: {path}");
    }

    // ---- 4. GameScene に RevealUI を追加 ----

    [MenuItem("GachaBlock/Phase3/4. Add RevealUI to GameScene")]
    static void AddRevealUIToGameScene()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play中は実行できません。", "OK");
            return;
        }
        string scenePath = "Assets/Scenes/GameScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        bool found = false;
        foreach (var go in scene.GetRootGameObjects())
            if (go.GetComponent<RevealUI>() != null) { found = true; break; }

        if (!found)
        {
            var go = new GameObject("RevealUI");
            go.AddComponent<RevealUI>();
            SceneManager.MoveGameObjectToScene(go, scene);
            Debug.Log("RevealUI を GameScene に追加しました");
        }

        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
        EditorUtility.DisplayDialog("Phase3 Setup", "RevealUI を GameScene に追加しました！", "OK");
    }

    // ---- 5. Build Settings 更新 ----

    [MenuItem("GachaBlock/Phase3/5. Fix Build Settings (Phase3)")]
    static void FixBuildSettings()
    {
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/HomeScene.unity",        true),
            new EditorBuildSettingsScene("Assets/Scenes/StageSelectScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/CharaSelectScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity",        true),
            new EditorBuildSettingsScene("Assets/Scenes/ResultScene.unity",      true),
            new EditorBuildSettingsScene("Assets/Scenes/CollectionScene.unity",  true),
        };
        EditorUtility.DisplayDialog("Phase3 Setup",
            "Build Settings 更新完了！\n0:Home / 1:StageSelect / 2:CharaSelect / 3:Game / 4:Result / 5:Collection", "OK");
    }

    // ---- デバッグ: 進行リセット ----

    [MenuItem("GachaBlock/Phase3/Reset Progress (Debug)")]
    static void ResetProgress()
    {
        ProgressManager.ResetAll();
        EditorUtility.DisplayDialog("Phase3 Debug", "進行状況をリセットしました。\n（Stage 1 のみ解放状態）", "OK");
    }
}
#endif
