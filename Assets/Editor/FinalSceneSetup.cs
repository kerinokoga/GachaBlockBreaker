using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// AudioManager のセットアップと Build Settings の修正
/// </summary>
public static class FinalSceneSetup
{
    [MenuItem("GachaBlock/Setup AudioManager")]
    public static void SetupAudioManager()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play モードを停止してから実行してください。", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");

        GameObject existing = GameObject.Find("AudioManager");
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject amGo = new GameObject("AudioManager");
        AudioManager am = amGo.AddComponent<AudioManager>();

        AudioSource bgmSource = amGo.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        AudioSource seSource = amGo.AddComponent<AudioSource>();
        seSource.loop = false;
        seSource.playOnAwake = false;

        SerializedObject soAM = new SerializedObject(am);
        soAM.FindProperty("bgmSource").objectReferenceValue = bgmSource;
        soAM.FindProperty("seSource").objectReferenceValue = seSource;
        soAM.ApplyModifiedProperties();

        // BGM/SE スライダーと AudioManager を連携
        Slider bgmSl = GameObject.Find("BGMSlider")?.GetComponent<Slider>();
        Slider seSl  = GameObject.Find("SESlider")?.GetComponent<Slider>();
        if (bgmSl != null)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(bgmSl.onValueChanged, am.SetBGMVolume);
        if (seSl != null)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(seSl.onValueChanged, am.SetSEVolume);

        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("完了", "AudioManager を GameScene に配置しました。", "OK");
    }

    [MenuItem("GachaBlock/Fix Build Settings")]
    public static void FixBuildSettings()
    {
        string[] paths = {
            "Assets/Scenes/HomeScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/ResultScene.unity"
        };

        var scenes = new EditorBuildSettingsScene[paths.Length];
        for (int i = 0; i < paths.Length; i++)
            scenes[i] = new EditorBuildSettingsScene(paths[i], true);

        EditorBuildSettings.scenes = scenes;
        EditorUtility.DisplayDialog("完了",
            "Build Settings を更新しました:\n0: HomeScene\n1: GameScene\n2: ResultScene", "OK");
    }
}
