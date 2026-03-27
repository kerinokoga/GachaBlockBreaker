using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ログイン画面：ゲスト / ユーザー名＋パスワード登録・ログイン
/// </summary>
public class LoginUI : MonoBehaviour
{
    InputField nameInput;
    InputField passInput;
    Text statusText;

    void Start()
    {
        // スターターキャラを常に保証（Reset後も自動復元）
        OrbManager.EnsureStarterCharacters();

        // 既にログイン済みなら HomeScene へ
        if (AuthManager.IsLoggedIn)
        {
            SceneManager.LoadScene("HomeScene");
            return;
        }
        BuildUI();
    }

    void BuildUI()
    {
        var cGo = new GameObject("LoginCanvas");
        var c = cGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0.5f;
        cGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var root = cGo.transform;

        // 背景
        MakeBg(root, new Color(0.03f, 0.03f, 0.12f));

        // タイトル
        MakeText(root, "GACHA BLOCK\nBREAKER", 56, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.75f), new Vector2(800f, 160f));

        // ユーザー名入力
        MakeText(root, "Username", 24, new Color(0.6f, 0.6f, 0.7f),
            new Vector2(0.5f, 0.60f), new Vector2(500f, 30f));
        nameInput = MakeInputField(root, new Vector2(0.5f, 0.56f), new Vector2(550f, 70f));

        // パスワード入力
        MakeText(root, "Password", 24, new Color(0.6f, 0.6f, 0.7f),
            new Vector2(0.5f, 0.49f), new Vector2(500f, 30f));
        passInput = MakeInputField(root, new Vector2(0.5f, 0.45f), new Vector2(550f, 70f));
        passInput.contentType = InputField.ContentType.Password;

        // LOGIN ボタン
        MakeButton(root, "LOGIN", new Color(0.2f, 0.5f, 0.8f),
            new Vector2(0.5f, 0.35f), new Vector2(500f, 85f), OnLogin);

        // REGISTER ボタン
        MakeButton(root, "REGISTER", new Color(0.3f, 0.6f, 0.3f),
            new Vector2(0.5f, 0.25f), new Vector2(500f, 85f), OnRegister);

        // GUEST ボタン
        MakeButton(root, "GUEST LOGIN", new Color(0.4f, 0.35f, 0.5f),
            new Vector2(0.5f, 0.14f), new Vector2(500f, 85f), OnGuest);

        // ステータスメッセージ
        statusText = MakeText(root, "", 26, new Color(1f, 0.4f, 0.4f),
            new Vector2(0.5f, 0.06f), new Vector2(700f, 40f));
    }

    // ---- ボタンハンドラ ----

    void OnLogin()
    {
        string error;
        if (AuthManager.Login(nameInput.text, passInput.text, out error))
        {
            SceneManager.LoadScene("HomeScene");
        }
        else
        {
            statusText.text = error;
        }
    }

    void OnRegister()
    {
        string error;
        if (AuthManager.Register(nameInput.text, passInput.text, out error))
        {
            statusText.color = new Color(0.4f, 1f, 0.4f);
            statusText.text = "登録完了！ HomeScene に移動します...";
            Invoke("GoHome", 0.5f);
        }
        else
        {
            statusText.color = new Color(1f, 0.4f, 0.4f);
            statusText.text = error;
        }
    }

    void OnGuest()
    {
        AuthManager.LoginAsGuest();
        SceneManager.LoadScene("HomeScene");
    }

    void GoHome() => SceneManager.LoadScene("HomeScene");

    // ---- ファクトリーメソッド ----

    void MakeBg(Transform parent, Color col)
    {
        var go = new GameObject("BG");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    Text MakeText(Transform parent, string txt, int size, Color col, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    InputField MakeInputField(Transform parent, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject("InputField");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;

        // テキスト子オブジェクト
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.fontSize = 28; t.color = Color.white;
        t.alignment = TextAnchor.MiddleLeft;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        t.supportRichText = false;
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(15f, 5f); trt.offsetMax = new Vector2(-15f, -5f);

        // Placeholder
        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(go.transform, false);
        var ph = phGo.AddComponent<Text>();
        ph.fontSize = 28; ph.color = new Color(0.4f, 0.4f, 0.5f);
        ph.alignment = TextAnchor.MiddleLeft;
        ph.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        ph.fontStyle = FontStyle.Italic;
        ph.text = "...";
        var phrt = phGo.GetComponent<RectTransform>();
        phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one;
        phrt.offsetMin = new Vector2(15f, 5f); phrt.offsetMax = new Vector2(-15f, -5f);

        var input = go.AddComponent<InputField>();
        input.textComponent = t;
        input.placeholder = ph;

        return input;
    }

    Button MakeButton(Transform parent, string label, Color bgCol,
        Vector2 anchor, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgCol;
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        btn.onClick.AddListener(onClick);

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 32; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return btn;
    }
}
