using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Phase 4.5 セットアップメニュー
/// </summary>
public static class Phase4_5Setup
{
    // ---- シーン作成 ----

    [MenuItem("GachaBlock/Phase4.5/1. Create CharaManageScene")]
    static void CreateCharaManageScene()
    {
        string path = "Assets/Scenes/CharaManageScene.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        // カメラ（ScreenSpaceOverlay に必要）
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
        cam.orthographic = true;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        EditorSceneManager.MoveGameObjectToScene(camGo, scene);

        // CharaManageUI を持つ GameObject
        var go = new GameObject("CharaManageUI");
        go.AddComponent<CharaManageUI>();
        EditorSceneManager.MoveGameObjectToScene(go, scene);

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh();
        Debug.Log($"[Phase4.5] CharaManageScene 作成完了: {path}");
    }

    [MenuItem("GachaBlock/Phase4.5/2. Fix Build Settings")]
    static void FixBuildSettings()
    {
        string[] scenes =
        {
            "Assets/Scenes/HomeScene.unity",
            "Assets/Scenes/StageSelectScene.unity",
            "Assets/Scenes/CharaSelectScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/ResultScene.unity",
            "Assets/Scenes/CollectionScene.unity",
            "Assets/Scenes/GachaScene.unity",
            "Assets/Scenes/CharaManageScene.unity",
        };

        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);

        EditorBuildSettings.scenes = buildScenes;
        Debug.Log("[Phase4.5] Build Settings 更新完了 (8シーン)");
    }

    // ---- デバッグ ----

    [MenuItem("GachaBlock/Phase4.5/Debug: Add 1000 Orbs")]
    static void DebugAddOrbs()
    {
        OrbManager.AddOrbs(1000);
        Debug.Log($"[Phase4.5] Orbs: {OrbManager.GetOrbs()}");
    }

    [MenuItem("GachaBlock/Phase4.5/Debug: Fill to 49 owned (test capacity)")]
    static void DebugFill49()
    {
        // 既存アセットを所持済みにする
        var all = Resources.LoadAll<CharacterData>("Characters");
        foreach (var c in all)
        {
            if (!OrbManager.IsOwned(c.characterName))
            {
                OrbManager.SetOwned(c.characterName);
                OrbManager.AddCharCount(c.characterName);
            }
        }

        // 不足分はダミー CharacterData アセットを生成
        int current = OrbManager.GetOwnedCount();
        int needed = 49 - current;
        for (int i = 0; i < needed; i++)
        {
            string dummyName = $"Test_{i + 1:D2}";
            string path = $"Assets/Resources/Characters/{dummyName}.asset";
            if (!AssetDatabase.LoadAssetAtPath<CharacterData>(path))
            {
                var cd = ScriptableObject.CreateInstance<CharacterData>();
                cd.characterName = dummyName;
                cd.rarity = (Rarity)(i % 4);
                cd.description = "テスト用ダミーキャラ";
                AssetDatabase.CreateAsset(cd, path);
            }
            OrbManager.SetOwned(dummyName);
            OrbManager.AddCharCount(dummyName);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Phase4.5] 所持: {OrbManager.GetOwnedCount()} 体に設定完了");
    }

    [MenuItem("GachaBlock/Phase4.5/Debug: Remove dummy chars")]
    static void DebugRemoveDummies()
    {
        var all = Resources.LoadAll<CharacterData>("Characters");
        int removed = 0;
        foreach (var c in all)
        {
            if (c.characterName.StartsWith("Test_"))
            {
                OrbManager.RemoveOwned(c.characterName);
                string path = AssetDatabase.GetAssetPath(c);
                AssetDatabase.DeleteAsset(path);
                removed++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Phase4.5] ダミー {removed} 体を削除 → 所持: {OrbManager.GetOwnedCount()} 体");
    }

    [MenuItem("GachaBlock/Phase4.5/Debug: Reset All Progress")]
    static void DebugReset()
    {
        OrbManager.ResetAll();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[Phase4.5] 全データ削除完了");
    }
}
