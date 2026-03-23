using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GachaBlock > Setup Special Blocks で ExplosionBlock / ChainBlock の Prefab を作成し
/// StageManager の blockPrefabs にセットする
/// </summary>
public static class SpecialBlockSetup
{
    [MenuItem("GachaBlock/Setup Special Blocks")]
    public static void SetupSpecialBlocks()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "Play モードを停止してから実行してください。", "OK");
            return;
        }

        PhysicsMaterial2D ballMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(
            "Assets/Sprites/BallPhysics.physicsMaterial2D");

        // ---- ExplosionBlock Prefab ----
        string expPath = "Assets/Prefabs/Blocks/ExplosionBlock.prefab";
        GameObject expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(expPath);
        if (expPrefab == null)
        {
            expPrefab = CreateSpecialBlockPrefab(
                "ExplosionBlock",
                new Color(1f, 0.5f, 0.1f), // オレンジ
                typeof(ExplosionBlock),
                expPath,
                ballMat
            );
        }

        // ---- ChainBlock Prefab ----
        string chainPath = "Assets/Prefabs/Blocks/ChainBlock.prefab";
        GameObject chainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(chainPath);
        if (chainPrefab == null)
        {
            chainPrefab = CreateSpecialBlockPrefab(
                "ChainBlock",
                new Color(0.3f, 1f, 0.4f), // 緑
                typeof(ChainBlock),
                chainPath,
                ballMat
            );
        }

        // ---- ExplosionBlock の Layer を "Block" に設定 ----
        // Block レイヤーを追加
        AddLayer("Block");

        // ---- GameScene の StageManager に Prefab をセット ----
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");

        StageManager sm = Object.FindObjectOfType<StageManager>();
        if (sm == null)
        {
            EditorUtility.DisplayDialog("エラー", "StageManager が見つかりません。", "OK");
            return;
        }

        SerializedObject soSM = new SerializedObject(sm);
        SerializedProperty prefabsProp = soSM.FindProperty("blockPrefabs");

        // 配列サイズを確認・拡張
        if (prefabsProp.arraySize < 4)
            prefabsProp.arraySize = 4;

        prefabsProp.GetArrayElementAtIndex(2).objectReferenceValue = expPrefab;
        prefabsProp.GetArrayElementAtIndex(3).objectReferenceValue = chainPrefab;
        soSM.ApplyModifiedProperties();

        // 既存ブロック Prefab に "Block" レイヤーを設定
        SetPrefabLayer("Assets/Prefabs/Blocks/NormalBlock.prefab");
        SetPrefabLayer("Assets/Prefabs/Blocks/DurableBlock.prefab");
        SetPrefabLayer(expPath);
        SetPrefabLayer(chainPath);

        // ExplosionBlock の blockLayer マスクを設定
        SetExplosionBlockLayer(expPrefab);

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("完了",
            "ExplosionBlock / ChainBlock Prefab を作成しました！\n\nPlay して GachaBlock > Run Test Stage を実行すると\n各種ブロックが並んだステージでテストできます。",
            "OK");
    }

    [MenuItem("GachaBlock/Run Mixed Test Stage")]
    public static void RunMixedTestStage()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("注意", "Play モードで実行してください。", "OK");
            return;
        }

        StageManager sm = Object.FindObjectOfType<StageManager>();
        if (sm == null) { Debug.LogError("StageManager が見つかりません。"); return; }

        sm.BuildMixedTestStage();
    }

    // ---- ヘルパー ----

    static GameObject CreateSpecialBlockPrefab(string name, Color color,
        System.Type scriptType, string savePath, PhysicsMaterial2D mat)
    {
        string texPath = $"Assets/Sprites/{name}_temp.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            Texture2D tex = new Texture2D(128, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 128; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
            TextureImporter ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 64;
            AssetDatabase.ImportAsset(texPath);
        }
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);

        GameObject go = new GameObject(name);
        go.transform.localScale = new Vector3(0.9f, 0.4f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        if (mat != null) col.sharedMaterial = mat;

        go.AddComponent(scriptType);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, savePath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void SetExplosionBlockLayer(GameObject prefab)
    {
        if (prefab == null) return;
        int blockLayer = LayerMask.NameToLayer("Block");
        if (blockLayer < 0) return;

        ExplosionBlock eb = prefab.GetComponent<ExplosionBlock>();
        if (eb == null) return;

        SerializedObject so = new SerializedObject(eb);
        so.FindProperty("blockLayer").intValue = 1 << blockLayer;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    static void SetPrefabLayer(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;
        int blockLayer = LayerMask.NameToLayer("Block");
        if (blockLayer < 0) return;

        prefab.layer = blockLayer;
        PrefabUtility.SavePrefabAsset(prefab);
    }

    static void AddLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (layer.stringValue == layerName) return;
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }
}
