#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Phase 2 セットアップ用エディタースクリプト
/// GachaBlock/Phase2/ メニューから実行する
/// </summary>
public static class Phase2Setup
{
    // ---- スターターキャラアセット生成 ----

    [MenuItem("GachaBlock/Phase2/Create Starter Characters")]
    static void CreateStarterCharacters()
    {
        string folder = "Assets/Resources/Characters";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Characters");

        CreateCharacter(folder, "Luna",
            "ルナ", Rarity.SR, new Color(0.75f, 0.3f, 1f),
            PassiveEffectType.BallDamageUp, 1.2f,
            UltimateSkillType.PowerBurst, 1.5f, 3f, 10,
            "SR キャラ。ボール速度が 1.2 倍になる。奥義で 3 秒間さらに加速！");

        CreateCharacter(folder, "Aria",
            "アリア", Rarity.R, new Color(0.3f, 0.5f, 1f),
            PassiveEffectType.ExtraDamage, 1f,
            UltimateSkillType.MassDestroy, 1f, 0f, 10,
            "R キャラ。ブロックへのダメージが 2 になる。奥義で全ブロックを攻撃！");

        CreateCharacter(folder, "Sera",
            "セラ", Rarity.N, new Color(0.6f, 0.6f, 0.6f),
            PassiveEffectType.None, 0f,
            UltimateSkillType.StockRecover, 1f, 0f, 10,
            "N キャラ。パッシブはなし。奥義でストックを 1 回復できる。");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Phase2 Setup", "スターターキャラ 3 体を生成しました！\nAssets/Resources/Characters/ を確認してください。", "OK");
    }

    static void CreateCharacter(string folder, string fileName,
        string charName, Rarity rarity, Color rarityColor,
        PassiveEffectType passive, float passiveVal,
        UltimateSkillType ult, float ultVal, float ultDur, int gaugeCost,
        string desc)
    {
        string path = $"{folder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
        if (existing != null)
        {
            Debug.Log($"既存アセットを更新: {path}");
            existing.characterName    = charName;
            existing.rarity           = rarity;
            existing.rarityColor      = rarityColor;
            existing.passiveType      = passive;
            existing.passiveValue     = passiveVal;
            existing.ultimateType     = ult;
            existing.ultimateValue    = ultVal;
            existing.ultimateDuration = ultDur;
            existing.ultimateGaugeCost = gaugeCost;
            existing.description      = desc;
            EditorUtility.SetDirty(existing);
            return;
        }

        var cd = ScriptableObject.CreateInstance<CharacterData>();
        cd.characterName    = charName;
        cd.rarity           = rarity;
        cd.rarityColor      = rarityColor;
        cd.passiveType      = passive;
        cd.passiveValue     = passiveVal;
        cd.ultimateType     = ult;
        cd.ultimateValue    = ultVal;
        cd.ultimateDuration = ultDur;
        cd.ultimateGaugeCost = gaugeCost;
        cd.description      = desc;

        AssetDatabase.CreateAsset(cd, path);
        Debug.Log($"キャラアセット作成: {path}");
    }

    // ---- CharaSelectScene 生成 ----

    [MenuItem("GachaBlock/Phase2/Setup CharaSelectScene")]
    static void SetupCharaSelectScene()
    {
        string scenePath = "Assets/Scenes/CharaSelectScene.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        // Main Camera
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.18f);
        cam.orthographic = true;
        camGo.tag = "MainCamera";
        SceneManager.MoveGameObjectToScene(camGo, scene);

        // EventSystem
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        SceneManager.MoveGameObjectToScene(esGo, scene);

        // CharaSelectUI Runner
        var runnerGo = new GameObject("CharaSelectUIRunner");
        runnerGo.AddComponent<CharaSelectUI>();
        SceneManager.MoveGameObjectToScene(runnerGo, scene);

        EditorSceneManager.SaveScene(scene, scenePath);
        EditorSceneManager.CloseScene(scene, true);

        Debug.Log($"CharaSelectScene を作成しました: {scenePath}");
        EditorUtility.DisplayDialog("Phase2 Setup", "CharaSelectScene を作成しました！\n次に「Fix Build Settings (Phase2)」を実行してください。", "OK");
    }

    // ---- Build Settings 更新 ----

    [MenuItem("GachaBlock/Phase2/Fix Build Settings (Phase2)")]
    static void FixBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/HomeScene.unity",        true),
            new EditorBuildSettingsScene("Assets/Scenes/CharaSelectScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity",        true),
            new EditorBuildSettingsScene("Assets/Scenes/ResultScene.unity",      true),
        };
        EditorBuildSettings.scenes = scenes;
        EditorUtility.DisplayDialog("Phase2 Setup",
            "Build Settings を更新しました！\n0:Home / 1:CharaSelect / 2:Game / 3:Result", "OK");
    }

    // ---- GameScene に CharacterManager を追加 ----

    [MenuItem("GachaBlock/Phase2/Add CharacterManager to GameScene")]
    static void AddCharacterManagerToGameScene()
    {
        string scenePath = "Assets/Scenes/GameScene.unity";

        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play中は実行できません。", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        // 既存の CharacterManager を確認
        bool found = false;
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.GetComponent<CharacterManager>() != null) { found = true; break; }
            foreach (var cm in go.GetComponentsInChildren<CharacterManager>(true))
                if (cm != null) { found = true; break; }
        }

        if (!found)
        {
            var cmGo = new GameObject("CharacterManager");
            cmGo.AddComponent<CharacterManager>();
            SceneManager.MoveGameObjectToScene(cmGo, scene);
            Debug.Log("CharacterManager を GameScene に追加しました");
        }
        else
        {
            Debug.Log("CharacterManager は既に存在します");
        }

        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);

        EditorUtility.DisplayDialog("Phase2 Setup", "CharacterManager を GameScene に追加しました！", "OK");
    }
}
#endif
