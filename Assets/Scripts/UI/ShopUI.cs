using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ショップ画面：オーブ購入（モック IAP）
/// </summary>
public class ShopUI : MonoBehaviour
{
    Text orbText;
    Text messageText;

    void Start() => BuildUI();

    void BuildUI()
    {
        var cGo = new GameObject("ShopCanvas");
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
        MakeBg(root, new Color(0.05f, 0.03f, 0.12f));

        // タイトル
        MakeText(root, "SHOP", 56, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f, 0.93f), new Vector2(400f, 70f));

        // オーブ表示
        orbText = MakeText(root, $"Orb: {OrbManager.GetOrbs()}", 34,
            new Color(0.4f, 0.9f, 1f), new Vector2(0.5f, 0.87f), new Vector2(400f, 45f));

        // メッセージ（購入成功時に表示）
        messageText = MakeText(root, "", 26, new Color(0.4f, 1f, 0.4f),
            new Vector2(0.5f, 0.82f), new Vector2(600f, 36f));

        // 商品カード
        float[] ys = { 0.72f, 0.58f, 0.44f, 0.30f };
        Color[] colors = {
            new Color(0.2f, 0.4f, 0.6f),
            new Color(0.3f, 0.5f, 0.2f),
            new Color(0.5f, 0.35f, 0.15f),
            new Color(0.6f, 0.2f, 0.5f),
        };

        for (int i = 0; i < IAPManager.Products.Length; i++)
        {
            var p = IAPManager.Products[i];
            BuildProductCard(root, p, ys[i], colors[i]);
        }

        // BACK ボタン
        MakeButton(root, "BACK", new Color(0.25f, 0.25f, 0.35f),
            new Vector2(0.5f, 0.05f), new Vector2(360f, 70f),
            () => SceneManager.LoadScene("HomeScene"));
    }

    void BuildProductCard(Transform parent, IAPManager.Product product, float y, Color bgCol)
    {
        string label = $"{product.orbAmount} Orb  —  {product.priceLabel}";

        var go = new GameObject($"Product_{product.id}");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgCol;
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(700f, 100f);

        string pid = product.id;
        int amount = product.orbAmount;
        btn.onClick.AddListener(() => OnPurchase(pid, amount));

        // ラベル
        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 32; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // 購入回数（右下小文字）
        int count = IAPManager.GetPurchaseCount(product.id);
        if (count > 0)
        {
            MakeText(go.transform, $"x{count} purchased", 18,
                new Color(0.8f, 0.8f, 0.8f, 0.6f),
                new Vector2(0.85f, 0.15f), new Vector2(200f, 24f));
        }
    }

    void OnPurchase(string productId, int amount)
    {
        if (IAPManager.Purchase(productId))
        {
            orbText.text = $"Orb: {OrbManager.GetOrbs()}";
            StartCoroutine(ShowMessage($"+{amount} Orb!"));
        }
    }

    IEnumerator ShowMessage(string msg)
    {
        messageText.text = msg;
        yield return new WaitForSeconds(2f);
        if (messageText) messageText.text = "";
    }

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
