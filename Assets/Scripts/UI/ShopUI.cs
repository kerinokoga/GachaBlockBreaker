using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ショップ画面：オーブ購入（モック IAP）
/// </summary>
public class ShopUI : MonoBehaviour
{
    Text orbText;
    Text messageText;
    List<RectTransform> particles = new List<RectTransform>();

    void Start()
    {
        // Unity IAP を初期化（冪等。初回はストア接続に少し時間がかかる場合あり）
        IAPManager.Initialize();
        BuildUI();
    }

    void Update()
    {
        // 光の粒アニメーション
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            var p = particles[i];
            p.anchoredPosition += new Vector2(0f, 40f * Time.deltaTime);
            if (p.anchoredPosition.y > 1000f)
                p.anchoredPosition = new Vector2(Random.Range(-540f, 540f), -1000f);
            float x = p.anchoredPosition.x + Mathf.Sin(Time.time * 0.8f + i * 1.3f) * 15f * Time.deltaTime;
            p.anchoredPosition = new Vector2(x, p.anchoredPosition.y);
        }
    }

    void BuildUI()
    {
        var cGo = new GameObject("ShopCanvas");
        var c = cGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0.0f;
        cGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var root = cGo.transform;

        // ===== 背景（暗い紫/紺） =====
        MakeBg(root, new Color(0.03f, 0.02f, 0.1f));

        // ===== 上部装飾バー =====
        MakeDecorBar(root, new Vector2(0f, 0.97f), new Vector2(1f, 1f),
            new Color(1f, 0.85f, 0.1f, 0.25f));

        // ===== 下部装飾バー =====
        MakeDecorBar(root, Vector2.zero, new Vector2(1f, 0.02f),
            new Color(1f, 0.85f, 0.1f, 0.25f));

        // ===== 光の粒パーティクル =====
        CreateParticles(root, 12);

        // ===== タイトル（Shadow + Outline付き） =====
        var titleText = MakeText(root, "✦ オーブ購入 ✦", 56, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f, 0.93f), new Vector2(500f, 70f));
        var shadow = titleText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.8f);
        shadow.effectDistance = new Vector2(3f, -3f);
        var outline = titleText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.2f, 0.4f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        // ===== オーブ表示（Shadow付き） =====
        orbText = MakeText(root, $"◆ 所持オーブ: {OrbManager.GetOrbs()}", 40,
            new Color(0.4f, 0.9f, 1f), new Vector2(0.5f, 0.87f), new Vector2(600f, 55f));
        AddShadow(orbText.gameObject);

        // 課金制限表示（未成年のみ）
        int limit = AgeVerificationManager.MonthlyLimit;
        if (limit >= 0)
        {
            int spent = AgeVerificationManager.GetMonthlySpent();
            int remaining = AgeVerificationManager.RemainingLimit;
            var limitText = MakeText(root,
                $"【{AgeVerificationManager.AgeGroupLabel}】今月の残り: ¥{remaining:N0} / ¥{limit:N0}",
                24, new Color(1f, 0.7f, 0.3f),
                new Vector2(0.5f, 0.84f), new Vector2(700f, 32f));
            AddShadow(limitText.gameObject);
        }

        // オーブ表示下の装飾ライン
        float lineY = limit >= 0 ? 0.82f : 0.845f;
        MakeLine(root, new Vector2(0.5f, lineY), 500f, new Color(0.4f, 0.9f, 1f, 0.3f));

        // ===== メッセージ（購入成功/失敗時に表示） =====
        float msgY = limit >= 0 ? 0.79f : 0.82f;
        messageText = MakeText(root, "", 26, new Color(0.4f, 1f, 0.4f),
            new Vector2(0.5f, msgY), new Vector2(700f, 36f));

        // ===== 特別商品（初心者パック・マンスリーパス） =====
        BuildSpecialCards(root, 0.740f);

        // ===== 商品カード =====
        float[] ys = { 0.655f, 0.555f, 0.455f, 0.355f, 0.255f, 0.155f };
        Color[] colors = {
            new Color(0.2f, 0.4f, 0.6f),
            new Color(0.2f, 0.5f, 0.5f),
            new Color(0.3f, 0.5f, 0.2f),
            new Color(0.5f, 0.35f, 0.15f),
            new Color(0.6f, 0.2f, 0.5f),
            new Color(0.7f, 0.55f, 0.1f), // 最上位はゴールド系
        };

        int cardIdx = 0;
        foreach (var p in IAPManager.Products)
        {
            // 特別商品は専用カードで表示済みなのでオーブ一覧からは除外
            if (p.id == IAPManager.StarterPackId || p.id == IAPManager.MonthlyPassId) continue;
            if (cardIdx >= ys.Length) break;
            BuildProductCard(root, p, ys[cardIdx], colors[cardIdx]);
            cardIdx++;
        }

        // ===== HOME ボタン（左寄せ） =====
        MakeStyledButton(root, "ホーム", new Color(0.25f, 0.25f, 0.35f),
            new Color(0.4f, 0.4f, 0.55f),
            new Vector2(0.3f, 0.05f), new Vector2(300f, 70f),
            () => SceneManager.LoadScene("HomeScene"));

        // ===== GACHA ボタン（右寄せ） =====
        MakeStyledButton(root, "ガチャ", new Color(0.5f, 0.1f, 0.5f),
            new Color(0.7f, 0.3f, 0.7f),
            new Vector2(0.7f, 0.05f), new Vector2(300f, 70f),
            () => SceneManager.LoadScene("GachaScene"));
    }

    /// <summary>
    /// 特別商品カード（左: 初心者パック、右: マンスリーパス）。
    /// 初心者パックは購入済みなら非表示。パスは有効中なら残り日数表示＋購入不可。
    /// </summary>
    void BuildSpecialCards(Transform parent, float y)
    {
        // ---- 初心者パック（1回限定・購入済みなら出さない） ----
        if (!IAPManager.StarterPackBought)
        {
            BuildSpecialCard(parent, new Vector2(0.26f, y),
                new Color(0.85f, 0.3f, 0.5f), // ピンク系（目玉商品）
                "初心者パック(1回のみ購入可能)",
                $"1000オーブ  {IAPManager.GetStorePrice(IAPManager.StarterPackId)}",
                "通常¥1,350の52%OFF!",
                true,
                () => OnPurchase(IAPManager.StarterPackId, 1000));
        }

        // ---- マンスリーパス ----
        bool passActive = MonthlyPassManager.IsActive;
        BuildSpecialCard(parent, new Vector2(0.74f, y),
            new Color(0.25f, 0.45f, 0.85f), // ブルー系
            "マンスリーパス",
            passActive
                ? $"有効中  残り{MonthlyPassManager.RemainingDays}日"
                : "購入時400オーブ+30日間毎日80オーブ",
            passActive
                ? "毎日80オーブお届け中♡"
                : $"(合計2800)  {IAPManager.GetStorePrice(IAPManager.MonthlyPassId)}",
            !passActive,
            () => OnPurchase(IAPManager.MonthlyPassId, 400));
    }

    void BuildSpecialCard(Transform parent, Vector2 anchor, Color bgCol,
        string title, string body, string badge, bool interactable, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"Special_{title}");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(bgCol.r * 1.4f, bgCol.g * 1.4f, bgCol.b * 1.4f, 0.6f);
        var btn = go.AddComponent<Button>();
        btn.interactable = interactable;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(500f, 150f);
        if (interactable) btn.onClick.AddListener(onClick);

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(bgCol.r * 0.55f, bgCol.g * 0.55f, bgCol.b * 0.55f, 0.94f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        // タイトル（上段）
        var titleT = MakeText(go.transform, title, 25, new Color(1f, 0.95f, 0.6f),
            new Vector2(0.5f, 0.78f), new Vector2(484f, 32f));
        AddShadow(titleT.gameObject);

        // 内容（中段）
        var bodyT = MakeText(go.transform, body, 21, Color.white,
            new Vector2(0.5f, 0.48f), new Vector2(484f, 28f));
        AddShadow(bodyT.gameObject);

        // 価格・お得アピール（下段）
        var badgeT = MakeText(go.transform, badge, 21, new Color(1f, 0.85f, 0.2f),
            new Vector2(0.5f, 0.18f), new Vector2(484f, 28f));
        AddShadow(badgeT.gameObject);
        var ol = badgeT.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0.4f, 0.15f, 0f, 0.9f);
        ol.effectDistance = new Vector2(1.2f, -1.2f);
    }

    void BuildProductCard(Transform parent, IAPManager.ProductDef product, float y, Color bgCol)
    {
        // ストア接続済みならローカライズ価格、未接続なら定義のフォールバック価格
        string label = $"{product.orbAmount} オーブ  —  {IAPManager.GetStorePrice(product.id)}";

        // 外枠（ハイライトカラー）
        var go = new GameObject($"Product_{product.id}");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(bgCol.r * 1.4f, bgCol.g * 1.4f, bgCol.b * 1.4f, 0.5f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(700f, 100f);

        string pid = product.id;
        int amount = product.orbAmount;
        btn.onClick.AddListener(() => OnPurchase(pid, amount));

        // 内側背景（暗めバージョン）
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(bgCol.r * 0.7f, bgCol.g * 0.7f, bgCol.b * 0.7f, 0.92f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        // 上半分ハイライト（光沢感）
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        // ラベル（Shadow付き）
        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 40; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : UIFont.Main;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(txtGo);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // 下部レアリティバー
        var barGo = new GameObject("RarityBar");
        barGo.transform.SetParent(go.transform, false);
        barGo.AddComponent<Image>().color = new Color(bgCol.r * 1.5f, bgCol.g * 1.5f, bgCol.b * 1.5f, 0.6f);
        var barRt = barGo.GetComponent<RectTransform>();
        barRt.anchorMin = Vector2.zero; barRt.anchorMax = new Vector2(1f, 0.06f);
        barRt.offsetMin = new Vector2(3f, 3f); barRt.offsetMax = new Vector2(-3f, 0f);

        // お得額バッジ（右寄り）: 最小パック（¥150=100オーブ、1.5円/オーブ）換算との差額
        int save = Mathf.RoundToInt(product.orbAmount * 1.5f) - product.priceYen;
        if (save > 0)
        {
            var saveText = MakeText(go.transform, $"¥{save:N0} お得!", 32,
                new Color(1f, 0.9f, 0.3f),
                new Vector2(0.82f, 0.82f), new Vector2(300f, 42f));
            AddShadow(saveText.gameObject);
            var saveOl = saveText.gameObject.AddComponent<Outline>();
            saveOl.effectColor = new Color(0.4f, 0.2f, 0f, 0.9f);
            saveOl.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }

    void OnPurchase(string productId, int amount)
    {
        // 事前に課金制限チェック（エラーメッセージ表示用）
        var product = System.Array.Find(IAPManager.Products, p => p.id == productId);
        if (!AgeVerificationManager.CanPurchase(product.priceYen))
        {
            int remaining = AgeVerificationManager.RemainingLimit;
            messageText.color = new Color(1f, 0.4f, 0.4f);
            StartCoroutine(ShowMessage($"月額制限を超えています（残り ¥{remaining:N0}）"));
            return;
        }

        // 購入開始（結果は非同期で返る。ストアダイアログ中はこの表示のまま）
        messageText.color = new Color(0.8f, 0.8f, 1f);
        messageText.text = "購入処理中...";

        IAPManager.Purchase(productId, (success, message) =>
        {
            if (this == null || messageText == null) return; // シーン離脱済みなら無視

            if (success)
            {
                orbText.text = $"◆ 所持オーブ: {OrbManager.GetOrbs()}";
                messageText.color = new Color(0.4f, 1f, 0.4f);
                string doneMsg = $"+{amount} オーブ!";
                if (productId == IAPManager.StarterPackId) doneMsg = "初心者パック獲得! +1000オーブ";
                else if (productId == IAPManager.MonthlyPassId) doneMsg = "マンスリーパス開始! +400オーブ";
                StartCoroutine(ShowMessage(doneMsg));
                // 画面をリビルドして残額を更新
                StartCoroutine(RebuildAfterDelay());
            }
            else
            {
                messageText.color = new Color(1f, 0.4f, 0.4f);
                StartCoroutine(ShowMessage(message));
            }
        });
    }

    IEnumerator RebuildAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        // シーンリロードで残額表示を更新
        SceneManager.LoadScene("ShopScene");
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

    void MakeDecorBar(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color col)
    {
        var go = new GameObject("DecorBar");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        go.GetComponent<Image>().raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void CreateParticles(Transform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var pGo = new GameObject("Particle");
            pGo.transform.SetParent(parent, false);
            var img = pGo.AddComponent<Image>();
            img.raycastTarget = false;
            float r = Random.Range(0.85f, 1f);
            float g = Random.Range(0.7f, 0.95f);
            float bv = Random.Range(0.8f, 1f);
            float a = Random.Range(0.15f, 0.4f);
            img.color = new Color(r, g, bv, a);

            var prt = pGo.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            float startX = Random.Range(-540f, 540f);
            float startY = Random.Range(-960f, 960f);
            prt.anchoredPosition = new Vector2(startX, startY);
            float size = Random.Range(4f, 12f);
            prt.sizeDelta = new Vector2(size, size);

            particles.Add(prt);
        }
    }

    Text MakeText(Transform parent, string txt, int size, Color col, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    void MakeLine(Transform parent, Vector2 anchor, float width, Color col)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        go.GetComponent<Image>().raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, 2f);
    }

    void MakeStyledButton(Transform parent, string label, Color baseCol, Color highlightCol,
        Vector2 anchor, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        // 外枠（明るい縁取り）
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(highlightCol.r, highlightCol.g, highlightCol.b, 0.6f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        btn.onClick.AddListener(onClick);

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.92f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        // 上半分ハイライト（光沢感）
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        // ラベルテキスト（Shadow付き）
        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 32; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : UIFont.Main;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(txtGo);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // ボタン全体にShadow（浮遊感）
        var btnShadow = go.AddComponent<Shadow>();
        btnShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        btnShadow.effectDistance = new Vector2(4f, -4f);
    }

    void AddShadow(GameObject go)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.6f);
        s.effectDistance = new Vector2(2f, -2f);
    }
}
