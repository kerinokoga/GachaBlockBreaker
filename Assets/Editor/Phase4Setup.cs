#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Phase4Setup
{
    // ---- 1. ガチャアセット生成 ----

    [MenuItem("GachaBlock/Phase4/1. Create Gacha Assets")]
    static void CreateGachaAssets()
    {
        // Resources/Characters フォルダ確認
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Characters"))
            AssetDatabase.CreateFolder("Assets/Resources", "Characters");

        // Resources/Gacha フォルダ作成
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Gacha"))
            AssetDatabase.CreateFolder("Assets/Resources", "Gacha");

        // GachaPoolData 作成
        string poolPath = "Assets/Resources/Gacha/GachaPool.asset";
        var pool = AssetDatabase.LoadAssetAtPath<GachaPoolData>(poolPath);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<GachaPoolData>();
            AssetDatabase.CreateAsset(pool, poolPath);
        }
        pool.rateSSR = 0.03f;
        pool.rateSR  = 0.12f;
        pool.rateR   = 0.35f;
        pool.pityThreshold       = 100;
        pool.srGuaranteeInterval = 10;
        EditorUtility.SetDirty(pool);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Phase4 Setup",
            "ガチャアセットを生成しました！\nGachaPool", "OK");
    }

    static void CreateCharacter(string charName, Rarity rarity, Color col,
        PassiveEffectType passive, float passiveVal,
        UltimateSkillType ult, float ultVal, float ultDuration,
        string description)
    {
        string path = $"Assets/Resources/Characters/{charName}.asset";
        var cd = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
        if (cd == null)
        {
            cd = ScriptableObject.CreateInstance<CharacterData>();
            AssetDatabase.CreateAsset(cd, path);
        }
        cd.characterName  = charName;
        cd.rarity         = rarity;
        cd.rarityColor    = col;
        cd.passiveType    = passive;
        cd.passiveValue   = passiveVal;
        cd.ultimateType   = ult;
        cd.ultimateValue  = ultVal;
        cd.ultimateDuration = ultDuration;
        cd.description    = description;
        EditorUtility.SetDirty(cd);
        Debug.Log($"{charName} ({rarity}) を作成しました");
    }

    // ---- 2. GachaScene 生成 ----

    [MenuItem("GachaBlock/Phase4/2. Setup GachaScene")]
    static void SetupGachaScene()
    {
        string path = "Assets/Scenes/GachaScene.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.03f, 0.12f);
        cam.orthographic = true;
        camGo.tag = "MainCamera";
        SceneManager.MoveGameObjectToScene(camGo, scene);

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        SceneManager.MoveGameObjectToScene(esGo, scene);

        var runner = new GameObject("GachaUIRunner");
        runner.AddComponent<GachaUI>();
        SceneManager.MoveGameObjectToScene(runner, scene);

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log($"GachaScene を作成しました: {path}");
        EditorUtility.DisplayDialog("Phase4 Setup", "GachaScene を作成しました！", "OK");
    }

    // ---- 3. Build Settings 更新 ----

    [MenuItem("GachaBlock/Phase4/3. Fix Build Settings (Phase4)")]
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
            new EditorBuildSettingsScene("Assets/Scenes/GachaScene.unity",       true),
        };
        EditorUtility.DisplayDialog("Phase4 Setup",
            "Build Settings 更新完了！\n0:Home / 1:StageSelect / 2:CharaSelect / 3:Game\n4:Result / 5:Collection / 6:Gacha", "OK");
    }

    // ---- デバッグメニュー ----

    [MenuItem("GachaBlock/Phase4/Debug: Add 1000 Orbs")]
    static void DebugAddOrbs()
    {
        OrbManager.AddOrbs(1000);
        EditorUtility.DisplayDialog("Phase4 Debug", $"1000オーブ付与しました。\n現在: {OrbManager.GetOrbs()} Orb", "OK");
    }

    [MenuItem("GachaBlock/Phase4/Debug: Reset Gacha State")]
    static void DebugResetGacha()
    {
        OrbManager.ResetAll();
        EditorUtility.DisplayDialog("Phase4 Debug", "ガチャ状態をリセットしました。\nオーブ / 天井カウント / 所持フラグをクリア", "OK");
    }

    [MenuItem("GachaBlock/Phase4/Debug: Set Pity to 99")]
    static void DebugSetPity99()
    {
        PlayerPrefs.SetInt("GachaBlock_PityCount", 99);
        PlayerPrefs.Save();
        EditorUtility.DisplayDialog("Phase4 Debug", "天井カウントを 99 にセットしました。\n次の1連でSSR確定。", "OK");
    }

    [MenuItem("GachaBlock/Phase4/Debug: Run 100-draw Probability Test")]
    static void DebugProbabilityTest()
    {
        var pool = Resources.Load<GachaPoolData>("Gacha/GachaPool");
        var allChars = Resources.LoadAll<CharacterData>("Characters");

        if (pool == null || allChars.Length == 0)
        {
            EditorUtility.DisplayDialog("エラー", "GachaPool または CharacterData が見つかりません。\nメニュー1を先に実行してください。", "OK");
            return;
        }

        int ssr = 0, sr = 0, r = 0, n = 0;
        int total = 100;

        // 天井をリセットして純粋な確率テスト
        OrbManager.ResetAll();

        for (int i = 0; i < total; i++)
        {
            var result = GachaEngine.DrawSingle(pool, allChars);
            switch (result.chara.rarity)
            {
                case Rarity.SSR: ssr++; break;
                case Rarity.SR:  sr++;  break;
                case Rarity.R:   r++;   break;
                default:         n++;   break;
            }
        }

        // テスト後にリセット
        OrbManager.ResetAll();

        string msg = $"=== 100連テスト結果 ===\n" +
                     $"SSR: {ssr}回 ({ssr}%)\n" +
                     $"SR:  {sr}回 ({sr}%)\n" +
                     $"R:   {r}回 ({r}%)\n" +
                     $"N:   {n}回 ({n}%)\n\n" +
                     $"設定: SSR3% / SR12% / R35% / N50%";
        Debug.Log(msg);
        EditorUtility.DisplayDialog("Phase4 確率テスト", msg, "OK");
    }
}
#endif
