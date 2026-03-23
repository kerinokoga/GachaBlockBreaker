using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GachaBlock > Setup Blocks で NormalBlock Prefab 作成 + GameScene に StageManager を配置する
/// </summary>
public static class BlockSetup
{
    [MenuItem("GachaBlock/Setup Blocks and StageManager")]
    public static void SetupBlocks()
    {
        // ---- NormalBlock Prefab を作成 ----
        string normalPrefabPath = "Assets/Prefabs/Blocks/NormalBlock.prefab";
        GameObject normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(normalPrefabPath);
        if (normalPrefab == null)
        {
            normalPrefab = CreateBlockPrefab(
                name: "NormalBlock",
                color: new Color(0.3f, 0.7f, 1f),   // 青
                size: new Vector2(0.9f, 0.4f),
                scriptType: typeof(NormalBlock),
                savePath: normalPrefabPath
            );
        }

        // ---- DurableBlock Prefab を作成 ----
        string durablePrefabPath = "Assets/Prefabs/Blocks/DurableBlock.prefab";
        GameObject durablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(durablePrefabPath);
        if (durablePrefab == null)
        {
            durablePrefab = CreateBlockPrefab(
                name: "DurableBlock",
                color: new Color(1f, 0.3f, 0.3f),   // 赤
                size: new Vector2(0.9f, 0.4f),
                scriptType: typeof(DurableBlock),
                savePath: durablePrefabPath
            );
        }

        // ---- Ball に "Ball" タグを設定 ----
        AddTag("Ball");

        // ---- GameScene を開いて StageManager を配置 ----
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");

        // Ball に Ball タグを設定
        GameObject ball = GameObject.Find("Ball");
        if (ball != null) ball.tag = "Ball";

        // StageManager が既にあれば削除
        GameObject existingSM = GameObject.Find("StageManager");
        if (existingSM != null) Object.DestroyImmediate(existingSM);

        // StageManager を作成
        GameObject smGo = new GameObject("StageManager");
        StageManager sm = smGo.AddComponent<StageManager>();

        // BlockParent を取得 or 作成
        GameObject blockParent = GameObject.Find("BlockParent");
        if (blockParent == null)
        {
            blockParent = new GameObject("BlockParent");
            blockParent.transform.position = Vector3.zero;
        }

        // Inspector の blockParent フィールドに BlockParent をセット
        SerializedObject soSM = new SerializedObject(sm);
        soSM.FindProperty("blockParent").objectReferenceValue = blockParent.transform;

        // blockPrefabs 配列に NormalBlock と DurableBlock をセット
        SerializedProperty prefabsProp = soSM.FindProperty("blockPrefabs");
        prefabsProp.arraySize = 4;
        prefabsProp.GetArrayElementAtIndex(0).objectReferenceValue = normalPrefab;
        prefabsProp.GetArrayElementAtIndex(1).objectReferenceValue = durablePrefab;
        prefabsProp.GetArrayElementAtIndex(2).objectReferenceValue = null; // Explosion は後で
        prefabsProp.GetArrayElementAtIndex(3).objectReferenceValue = null; // Chain は後で
        soSM.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("NormalBlock / DurableBlock Prefab と StageManager を配置しました。");
        EditorUtility.DisplayDialog("完了",
            "NormalBlock / DurableBlock Prefab を作成し\nStageManager を GameScene に配置しました！\n\n次: GachaBlock > Run Test Stage でブロックを配置してテストできます。",
            "OK");
    }

    [MenuItem("GachaBlock/Run Test Stage")]
    public static void RunTestStage()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("注意", "Play モードで実行してください。\nPlay ボタンを押してから再度実行してください。", "OK");
            return;
        }

        StageManager sm = Object.FindObjectOfType<StageManager>();
        if (sm == null)
        {
            Debug.LogError("StageManager が見つかりません。");
            return;
        }

        sm.BuildTestStage();
        Debug.Log("テストステージを生成しました。");
    }

    // ---- ヘルパー ----

    private static GameObject CreateBlockPrefab(string name, Color color, Vector2 size,
        System.Type scriptType, string savePath)
    {
        // テクスチャを作成
        string texPath = $"Assets/Sprites/{name}_temp.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            int w = 128, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
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

        // Prefab 用 GameObject を作成
        GameObject go = new GameObject(name);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();

        // PhysicsMaterial2D（壁と同じ BallPhysics）を適用
        PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Sprites/BallPhysics.physicsMaterial2D");
        if (mat != null) col.sharedMaterial = mat;

        go.AddComponent(scriptType);

        // Prefab として保存
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, savePath);
        Object.DestroyImmediate(go);

        return prefab;
    }

    private static void AddTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) return;

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }
}
