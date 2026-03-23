using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GachaBlock > Setup Ball & Paddle で Ball・Paddle を GameScene に自動配置する
/// </summary>
public static class GameObjectSetup
{
    [MenuItem("GachaBlock/Setup Ball and Paddle")]
    public static void SetupBallAndPaddle()
    {
        // GameScene を開く
        string scenePath = "Assets/Scenes/GameScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        // ---- PhysicsMaterial2D を作成 ----
        string matPath = "Assets/Sprites/BallPhysics.physicsMaterial2D";
        PhysicsMaterial2D ballMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
        if (ballMat == null)
        {
            ballMat = new PhysicsMaterial2D("BallPhysics");
            ballMat.friction = 0f;
            ballMat.bounciness = 1f;
            AssetDatabase.CreateAsset(ballMat, matPath);
            AssetDatabase.SaveAssets();
        }

        // ---- DeathZone タグを追加 ----
        AddTag("DeathZone");

        // DeathZone オブジェクトにタグを設定
        GameObject deathZone = GameObject.Find("DeathZone");
        if (deathZone != null)
            deathZone.tag = "DeathZone";

        // ---- Ball を作成 ----
        // 既存があれば削除
        GameObject existingBall = GameObject.Find("Ball");
        if (existingBall != null) Object.DestroyImmediate(existingBall);

        GameObject ball = new GameObject("Ball");
        ball.transform.position = new Vector3(0, -5f, 0);

        // SpriteRenderer（白い円）
        SpriteRenderer ballSR = ball.AddComponent<SpriteRenderer>();
        ballSR.sprite = CreateCircleSprite();
        ballSR.color = Color.white;
        ball.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        // Rigidbody2D
        Rigidbody2D ballRb = ball.AddComponent<Rigidbody2D>();
        ballRb.gravityScale = 0f;
        ballRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ballRb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // CircleCollider2D
        CircleCollider2D ballCol = ball.AddComponent<CircleCollider2D>();
        ballCol.sharedMaterial = ballMat;

        // BallController スクリプト
        ball.AddComponent<BallController>();

        // ---- Paddle を作成 ----
        GameObject existingPaddle = GameObject.Find("Paddle");
        if (existingPaddle != null) Object.DestroyImmediate(existingPaddle);

        GameObject paddle = new GameObject("Paddle");
        paddle.transform.position = new Vector3(0, -8f, 0);
        paddle.transform.localScale = new Vector3(3f, 0.4f, 1f);

        // SpriteRenderer（白い四角）
        SpriteRenderer paddleSR = paddle.AddComponent<SpriteRenderer>();
        paddleSR.sprite = CreateSquareSprite();
        paddleSR.color = new Color(0.4f, 0.8f, 1f); // 水色

        // BoxCollider2D
        BoxCollider2D paddleCol = paddle.AddComponent<BoxCollider2D>();
        paddleCol.sharedMaterial = ballMat;

        // PaddleController スクリプト
        paddle.AddComponent<PaddleController>();

        // ---- シーン保存 ----
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Ball と Paddle を GameScene に配置しました。");
        EditorUtility.DisplayDialog("完了",
            "Ball と Paddle を GameScene に配置しました！\n\nStep 6 の確認:\nPlay ボタンを押してマウスを左右に動かし、ボールが発射・反射するか確認してください。\n\n※ ボールの発射は GameManager 作成後に実装します。",
            "OK");
    }

    // ---- ヘルパー ----

    /// <summary>
    /// タグが存在しない場合に追加する
    /// </summary>
    private static void AddTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                return; // すでに存在する
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }

    /// <summary>
    /// 白い円テクスチャからスプライトを生成してアセットに保存する
    /// </summary>
    private static Sprite CreateCircleSprite()
    {
        string path = "Assets/Sprites/Ball_temp.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        tex.Apply();

        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spritePixelsPerUnit = 64;
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>
    /// 白い四角テクスチャからスプライトを生成してアセットに保存する
    /// </summary>
    private static Sprite CreateSquareSprite()
    {
        string path = "Assets/Sprites/Paddle_temp.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        int w = 128, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();

        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spritePixelsPerUnit = 64;
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
