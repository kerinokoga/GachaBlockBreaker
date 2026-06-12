#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Phase 6: サウンド＆エフェクト セットアップ
/// </summary>
public static class Phase6Setup
{
    // ---- AudioManager セットアップ ----

    [MenuItem("GachaBlock/Phase6/1. Add AudioManager to LoginScene")]
    static void AddAudioManagerToLoginScene()
    {
        AddAudioManagerToScene("Assets/Scenes/LoginScene.unity");
    }

    [MenuItem("GachaBlock/Phase6/2. Add AudioManager to HomeScene")]
    static void AddAudioManagerToHomeScene()
    {
        AddAudioManagerToScene("Assets/Scenes/HomeScene.unity");
    }

    [MenuItem("GachaBlock/Phase6/3. Add AudioManager to GameScene")]
    static void AddAudioManagerToGameScene()
    {
        AddAudioManagerToScene("Assets/Scenes/GameScene.unity");
    }

    [MenuItem("GachaBlock/Phase6/4. Add AudioManager to All Scenes")]
    static void AddAudioManagerToAllScenes()
    {
        string[] scenes = {
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/HomeScene.unity",
            "Assets/Scenes/StageSelectScene.unity",
            "Assets/Scenes/CharaSelectScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/ResultScene.unity",
            "Assets/Scenes/GachaScene.unity",
            "Assets/Scenes/ShopScene.unity",
            "Assets/Scenes/RankingScene.unity",
            "Assets/Scenes/CollectionScene.unity",
            "Assets/Scenes/CharaManageScene.unity",
        };

        int added = 0;
        foreach (var path in scenes)
        {
            if (System.IO.File.Exists(path))
            {
                if (AddAudioManagerToScene(path, quiet: true))
                    added++;
            }
        }
        Debug.Log($"[Phase6] AudioManager を {added} シーンに追加しました。");
        EditorUtility.DisplayDialog("Phase6", $"AudioManager を {added} シーンに追加しました。\n\nInspector で AudioClip を設定してください。", "OK");
    }

    // ---- ヘルパー ----

    static bool AddAudioManagerToScene(string scenePath, bool quiet = false)
    {
        // 現在のシーンを保存
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 既存の AudioManager を探す
        var existing = Object.FindObjectOfType<AudioManager>();
        if (existing != null)
        {
            if (!quiet)
                EditorUtility.DisplayDialog("Phase6", $"AudioManager は既に存在します。\n({scenePath})", "OK");
            return false;
        }

        // AudioManager GameObject 作成
        var go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!quiet)
        {
            EditorUtility.DisplayDialog("Phase6",
                $"AudioManager を追加しました。\n\nInspector で各 AudioClip を設定してください:\n" +
                "・BGM: bgmHome / bgmGame / bgmGacha / bgmResult\n" +
                "・SE: seBlockBreak / seMiss / seClear / seGameOver\n" +
                "      seBallLaunch / seButton / seGacha / seUlt", "OK");
        }
        return true;
    }

    // ---- SE 自動割当 ----

    [MenuItem("GachaBlock/Phase6/5. Assign SE Clips to AudioManager")]
    static void AssignSEClips()
    {
        var am = Object.FindObjectOfType<AudioManager>();
        if (am == null)
        {
            EditorUtility.DisplayDialog("Phase6", "AudioManager が見つかりません。\n先にシーンに追加してください。", "OK");
            return;
        }

        // ファイル名 → AudioManager フィールドの対応表
        var mapping = new System.Collections.Generic.Dictionary<string, System.Action<AudioClip>>
        {
            { "blockbreak",  clip => am.seBlockBreak = clip },
            { "ballstart",   clip => am.seBallLaunch = clip },
            { "miss",        clip => am.seMiss = clip },
            { "gameover",    clip => am.seGameOver = clip },
            { "clear",       clip => am.seClear = clip },
            { "gacha",       clip => am.seGacha = clip },
            { "bottan",      clip => am.seButton = clip },
            { "ougi",        clip => am.seUlt = clip },
            { "ult charge",  clip => am.seUltReady = clip },
        };

        int assigned = 0;
        var sb = new System.Text.StringBuilder();

        // Assets/Audio/ 以下の全 AudioClip を検索
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();

            foreach (var kv in mapping)
            {
                if (fileName == kv.Key.ToLower())
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null)
                    {
                        kv.Value(clip);
                        assigned++;
                        sb.AppendLine($"✓ {kv.Key} → {path}");
                    }
                    break;
                }
            }
        }

        EditorUtility.SetDirty(am);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[Phase6] SE割当完了: {assigned}/{mapping.Count}\n{sb}");
        EditorUtility.DisplayDialog("Phase6",
            $"SE割当完了！ {assigned}/{mapping.Count} 個\n\n{sb}\n\nシーンを保存してください。", "OK");
    }

    // ---- デバッグ ----

    [MenuItem("GachaBlock/Phase6/Debug: Play Button SE")]
    static void DebugPlayButtonSE()
    {
        if (Application.isPlaying)
            AudioManager.Instance?.PlayButtonSE();
        else
            Debug.Log("[Phase6] Play Mode でのみ動作します。");
    }

    [MenuItem("GachaBlock/Phase6/Debug: Play Gacha SE")]
    static void DebugPlayGachaSE()
    {
        if (Application.isPlaying)
            AudioManager.Instance?.PlayGachaSE();
        else
            Debug.Log("[Phase6] Play Mode でのみ動作します。");
    }
}
#endif
