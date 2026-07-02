using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ガチャ画面のランタイムUI（装飾版）
/// </summary>
public class GachaUI : MonoBehaviour
{
    // レアリティカラー
    static readonly Color ColSSR = new Color(1.0f, 0.85f, 0.1f);  // 金
    static readonly Color ColSR  = new Color(0.8f, 0.3f, 1.0f);   // 紫
    static readonly Color ColR   = new Color(0.2f, 0.5f, 1.0f);   // 青
    static readonly Color ColN   = new Color(0.55f, 0.55f, 0.55f); // グレー

    Transform canvasRoot;
    Text orbText;
    Text pityText;
    Text ownedText;
    Text capacityText;
    GameObject resultPanel;
    Button btnSingle;
    Button btnTen;
    GameObject graySingle;  // オーブ不足時のグレーオーバーレイ（単発）
    GameObject grayTen;     // オーブ不足時のグレーオーバーレイ（10連）
    List<RectTransform> particles = new List<RectTransform>();

    GachaPoolData pool;
    CharacterData[] allChars;

    // ===== Phase D: フル本格化アニメ状態管理 =====
    bool   isSkipping;            // スキップボタン押下で true
    GameObject skipButton;        // 演出中表示するスキップボタン
    RectTransform shakeTarget;    // カメラシェイク対象（canvasRoot）
    Vector2 shakeBaseAnchor;

    void Start()
    {
        pool     = Resources.Load<GachaPoolData>("Gacha/GachaPool");
        allChars = Resources.LoadAll<CharacterData>("Characters");
        BuildUI();

        // 段階8 → 段階9: Gacha 段階で到達したら GachaPull 段階へ
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.Gacha)
        {
            TutorialManager.Instance.SetStep(TutorialManager.Step.GachaPull);
            Debug.Log("[Tutorial] GachaPull 段階へ進行（段階9 未実装、ここで停止）");
            // 段階9で実装: ガチャ1回を強制誘導
        }
    }

    void Update()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            var p = particles[i];
            p.anchoredPosition += new Vector2(0f, 30f * Time.deltaTime);
            if (p.anchoredPosition.y > 1000f)
                p.anchoredPosition = new Vector2(Random.Range(-540f, 540f), -1000f);
            float x = p.anchoredPosition.x + Mathf.Sin(Time.time * 0.7f + i * 1.5f) * 12f * Time.deltaTime;
            p.anchoredPosition = new Vector2(x, p.anchoredPosition.y);
        }
    }

    void BuildUI()
    {
        var cGo = new GameObject("GachaCanvas");
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

        canvasRoot = cGo.transform;

        // ===== 背景 =====
        MakeBg(canvasRoot, new Color(0.03f, 0.02f, 0.1f));

        // 背景イラスト（Resources/Gacha/bg）
        var bgSprite = Resources.Load<Sprite>("Gacha/bg");
        if (bgSprite == null)
        {
            // bg が無ければ StageData の illustSpriteFull からランダムに取得
            var stages = Resources.LoadAll<StageData>("Stages");
            foreach (var s in stages)
            {
                if (s.illustSpriteFull != null) { bgSprite = s.illustSpriteFull; break; }
            }
        }
        if (bgSprite != null)
        {
            var bgIllust = new GameObject("BgIllust");
            bgIllust.transform.SetParent(canvasRoot, false);
            var bgImg = bgIllust.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.preserveAspect = true;
            bgImg.color = new Color(1f, 1f, 1f, 0.3f);
            bgImg.raycastTarget = false;
            var bgRt = bgIllust.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        }

        // （中央の紫グロー矩形は「大きい薄い四角が邪魔」とのことで削除）

        // 上部装飾バー
        var topBar = new GameObject("TopBar");
        topBar.transform.SetParent(canvasRoot, false);
        topBar.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.9f, 0.3f);
        var tbRt = topBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 0.96f); tbRt.anchorMax = Vector2.one;
        tbRt.offsetMin = tbRt.offsetMax = Vector2.zero;

        // 下部装飾バー
        var botBar = new GameObject("BotBar");
        botBar.transform.SetParent(canvasRoot, false);
        botBar.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.9f, 0.3f);
        var bbRt = botBar.GetComponent<RectTransform>();
        bbRt.anchorMin = Vector2.zero; bbRt.anchorMax = new Vector2(1f, 0.04f);
        bbRt.offsetMin = bbRt.offsetMax = Vector2.zero;

        CreateParticles(canvasRoot, 15);

        // ===== タイトル =====
        var titleT = MakeText(canvasRoot, "✦ 美少女ガチャ ✦", 58, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f, 0.96f), new Vector2(700f, 80f));
        var titleShadow = titleT.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0.8f, 0.4f, 0f, 0.8f);
        titleShadow.effectDistance = new Vector2(3f, -3f);
        var titleOutline = titleT.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.6f, 0.2f, 0f, 0.6f);
        titleOutline.effectDistance = new Vector2(2f, -2f);

        MakeLine(canvasRoot, new Color(1f, 0.85f, 0.1f, 0.4f), 0.925f, 2f);

        // ===== 左上：オーブ購入ボタン =====
        MakeSmallButton(canvasRoot, "オーブ購入", new Color(0.6f, 0.35f, 0.1f),
            new Vector2(0.15f, 0.89f), new Vector2(260f, 60f),
            () => SceneManager.LoadScene("ShopScene"));

        // 提供割合ボタン（オーブ購入の下）
        MakeSmallButton(canvasRoot, "提供割合", new Color(0.25f, 0.2f, 0.4f),
            new Vector2(0.15f, 0.835f), new Vector2(260f, 60f),
            ShowRateInfo);

        // ===== オーブ表示（大きく） =====
        orbText = MakeText(canvasRoot, $"◆ 所持オーブ: {OrbManager.GetOrbs()}", 40,
            new Color(0.4f, 0.95f, 1f), new Vector2(0.5f, 0.89f), new Vector2(600f, 55f));
        AddShadow(orbText.gameObject);

        // 天井カウンター（大きく）
        pityText = MakeText(canvasRoot, $"天井まで: {OrbManager.PityLimit - OrbManager.GetPityCount()}", 38,
            new Color(1f, 1f, 1f), new Vector2(0.5f, 0.85f), new Vector2(600f, 50f));
        AddShadow(pityText.gameObject);
        pityText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);

        // 所持数表示（大きく、上に）
        ownedText = MakeText(canvasRoot, $"キャラ所持数: {OrbManager.GetOwnedCount()} / {OrbManager.MaxOwnedCharacters}", 38,
            new Color(1f, 1f, 1f), new Vector2(0.5f, 0.81f), new Vector2(600f, 50f));
        AddShadow(ownedText.gameObject);
        ownedText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);

        // 所持上限警告テキスト
        capacityText = MakeText(canvasRoot, "", 26,
            new Color(1f, 0.4f, 0.4f), new Vector2(0.5f, 0.77f), new Vector2(700f, 40f));

        // ===== 結果パネル =====
        var rpGo = new GameObject("ResultPanel");
        rpGo.transform.SetParent(canvasRoot, false);
        var rpImg = rpGo.AddComponent<Image>();
        rpImg.color = new Color(0.5f, 0.2f, 0.8f, 0.5f);
        rpImg.raycastTarget = false; // 装飾フレーム、クリック透過
        var rpRT = rpGo.GetComponent<RectTransform>();
        rpRT.anchorMin = new Vector2(0.03f, 0.30f);
        rpRT.anchorMax = new Vector2(0.97f, 0.78f);
        rpRT.offsetMin = rpRT.offsetMax = Vector2.zero;
        var rpInner = new GameObject("Inner");
        rpInner.transform.SetParent(rpGo.transform, false);
        var rpInnerImg = rpInner.AddComponent<Image>();
        rpInnerImg.color = new Color(0.06f, 0.04f, 0.15f, 0.95f);
        rpInnerImg.raycastTarget = false;
        var riRt = rpInner.GetComponent<RectTransform>();
        riRt.anchorMin = Vector2.zero; riRt.anchorMax = Vector2.one;
        riRt.offsetMin = new Vector2(3f, 3f); riRt.offsetMax = new Vector2(-3f, -3f);
        resultPanel = rpGo;
        resultPanel.SetActive(false);

        // ===== ガチャボタン（オーブ表記） =====
        btnSingle = MakeGachaButton(canvasRoot, $"ガチャ ({OrbManager.CostSingle}オーブ)",
            new Color(0.85f, 0.45f, 0.1f), new Color(1f, 0.65f, 0.2f),
            new Vector2(0.5f, 0.22f), new Vector2(650f, 105f), "♦",
            () => ShowConfirmPopup(1));

        // SR以上確定テキスト（10連ボタンのすぐ上）
        var srText = MakeText(canvasRoot, "SR以上1枚確定♡", 34, new Color(0.9f, 0.5f, 1f),
            new Vector2(0.5f, 0.14f), new Vector2(500f, 40f));
        AddShadow(srText.gameObject);

        btnTen = MakeGachaButton(canvasRoot, $"10連ガチャ  ({OrbManager.CostTen} オーブ)",
            new Color(0.5f, 0.1f, 0.85f), new Color(0.7f, 0.3f, 1f),
            new Vector2(0.5f, 0.10f), new Vector2(650f, 105f), "★",
            () => ShowConfirmPopup(10));

        // オーブ不足時のグレー表示用オーバーレイ（ボタン全面を薄いグレーで覆う）
        graySingle = MakeGrayOverlay(btnSingle);
        grayTen    = MakeGrayOverlay(btnTen);

        MakeLine(canvasRoot, new Color(0.5f, 0.3f, 0.8f, 0.3f), 0.285f, 1f);

        // HOMEボタン（下部中央）
        MakeSmallButton(canvasRoot, "ホーム", new Color(0.2f, 0.2f, 0.3f),
            new Vector2(0.5f, 0.02f), new Vector2(240f, 60f),
            () => SceneManager.LoadScene("HomeScene"));

        RefreshButtons();
    }

    // ---- 光の粒 ----

    void CreateParticles(Transform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var pGo = new GameObject("Particle");
            pGo.transform.SetParent(parent, false);
            var img = pGo.AddComponent<Image>();
            img.raycastTarget = false;
            float r = Random.Range(0.7f, 1f);
            float g = Random.Range(0.5f, 0.85f);
            float bv = Random.Range(0.8f, 1f);
            float a = Random.Range(0.15f, 0.35f);
            img.color = new Color(r, g, bv, a);
            var prt = pGo.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(Random.Range(-540f, 540f), Random.Range(-960f, 960f));
            float size = Random.Range(3f, 10f);
            prt.sizeDelta = new Vector2(size, size);
            particles.Add(prt);
        }
    }

    // ---- 確認ポップアップ ----

    void ShowConfirmPopup(int drawCount)
    {
        int cost = drawCount == 1 ? OrbManager.CostSingle : OrbManager.CostTen;
        string msg = $"{cost}オーブを使用して\n{drawCount}連ガチャを引きますか？";

        var overlay = new GameObject("ConfirmOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ枠
        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(700f, 400f);

        // 内側
        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        // メッセージ
        var msgT = MakeText(dialog.transform, msg, 32, Color.white,
            new Vector2(0.5f, 0.65f), new Vector2(620f, 120f));
        AddShadow(msgT.gameObject);

        // はいボタン
        MakeSmallButton(dialog.transform, "はい", new Color(0.2f, 0.5f, 0.2f),
            new Vector2(0.3f, 0.2f), new Vector2(200f, 70f),
            () =>
            {
                Destroy(overlay);
                if (drawCount == 1) OnSingleDraw();
                else OnTenDraw();
            });

        // いいえボタン
        MakeSmallButton(dialog.transform, "いいえ", new Color(0.5f, 0.2f, 0.2f),
            new Vector2(0.7f, 0.2f), new Vector2(200f, 70f),
            () => Destroy(overlay));
    }

    // ---- 提供割合ダイアログ ----

    void ShowRateInfo()
    {
        var overlay = new GameObject("RateInfoOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // ダイアログ（ScrollRect対応で大きめ）
        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.05f, 0.08f);
        drt.anchorMax = new Vector2(0.95f, 0.92f);
        drt.offsetMin = drt.offsetMax = Vector2.zero;

        // 内側
        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        // タイトル（固定）
        var rateTitle = MakeText(dialog.transform, "✦ 提供割合 ✦", 40, new Color(1f, 0.9f, 0.2f),
            new Vector2(0.5f, 0.95f), new Vector2(600f, 50f));
        rateTitle.gameObject.AddComponent<Shadow>().effectColor = new Color(0.8f, 0.4f, 0f, 0.6f);

        // ScrollRect
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(dialog.transform, false);
        scrollGo.AddComponent<Image>().color = Color.clear;
        var sr = scrollGo.AddComponent<ScrollRect>();
        var scrollRT = scrollGo.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.02f, 0.1f);
        scrollRT.anchorMax = new Vector2(0.98f, 0.90f);
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;

        var vpGo = new GameObject("Viewport");
        vpGo.transform.SetParent(scrollGo.transform, false);
        vpGo.AddComponent<Image>().color = Color.white;
        vpGo.AddComponent<Mask>().showMaskGraphic = false;
        var vpRT = vpGo.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        sr.content = contentRT;
        sr.viewport = vpRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 30f;

        // コンテンツ構築
        float yPos = 0f;
        float lineH = 42f;
        float w = 800f;

        // レアリティ別確率
        yPos -= 10f;
        AddContentText(contentGo.transform, "── レアリティ別確率 ──", 28,
            new Color(0.7f, 0.7f, 0.9f), yPos, w); yPos -= lineH;
        AddContentText(contentGo.transform, "SSR  ★★★★   3%", 28, ColSSR, yPos, w); yPos -= lineH;
        AddContentText(contentGo.transform, "SR   ★★★    12%", 28, ColSR, yPos, w); yPos -= lineH;
        AddContentText(contentGo.transform, "R    ★★     35%", 28, ColR, yPos, w); yPos -= lineH;
        AddContentText(contentGo.transform, "N    ★      50%", 28, ColN, yPos, w); yPos -= lineH;

        // 仕様
        yPos -= 10f;
        AddContentText(contentGo.transform, "── 仕様 ──", 24,
            new Color(0.7f, 0.7f, 0.9f), yPos, w); yPos -= 36f;
        AddContentText(contentGo.transform, "・10連ガチャは SR 以上1体確定", 24, Color.white, yPos, w); yPos -= 36f;
        AddContentText(contentGo.transform, "・天井: 100回で SSR 確定", 24, Color.white, yPos, w); yPos -= 36f;

        // キャラ別排出率
        yPos -= 20f;
        AddContentText(contentGo.transform, "── キャラ別排出率 ──", 28,
            new Color(1f, 0.9f, 0.2f), yPos, w); yPos -= lineH;

        if (allChars != null && allChars.Length > 0)
        {
            // レアリティごとにキャラを分類
            var ssrChars = new List<CharacterData>();
            var srChars = new List<CharacterData>();
            var rChars = new List<CharacterData>();
            var nChars = new List<CharacterData>();

            foreach (var ch in allChars)
            {
                switch (ch.rarity)
                {
                    case Rarity.SSR: ssrChars.Add(ch); break;
                    case Rarity.SR:  srChars.Add(ch); break;
                    case Rarity.R:   rChars.Add(ch); break;
                    default:         nChars.Add(ch); break;
                }
            }

            // SSR
            if (ssrChars.Count > 0)
            {
                float rate = 3f / ssrChars.Count;
                AddContentText(contentGo.transform, $"【SSR】各 {rate:F2}%", 26, ColSSR, yPos, w); yPos -= 38f;
                foreach (var ch in ssrChars)
                {
                    AddContentText(contentGo.transform, $"  {ch.characterName}  ─  {rate:F2}%", 24, ColSSR, yPos, w); yPos -= 34f;
                }
                yPos -= 8f;
            }

            // SR
            if (srChars.Count > 0)
            {
                float rate = 12f / srChars.Count;
                AddContentText(contentGo.transform, $"【SR】各 {rate:F2}%", 26, ColSR, yPos, w); yPos -= 38f;
                foreach (var ch in srChars)
                {
                    AddContentText(contentGo.transform, $"  {ch.characterName}  ─  {rate:F2}%", 24, ColSR, yPos, w); yPos -= 34f;
                }
                yPos -= 8f;
            }

            // R
            if (rChars.Count > 0)
            {
                float rate = 35f / rChars.Count;
                AddContentText(contentGo.transform, $"【R】各 {rate:F2}%", 26, ColR, yPos, w); yPos -= 38f;
                foreach (var ch in rChars)
                {
                    AddContentText(contentGo.transform, $"  {ch.characterName}  ─  {rate:F2}%", 24, ColR, yPos, w); yPos -= 34f;
                }
                yPos -= 8f;
            }

            // N
            if (nChars.Count > 0)
            {
                float rate = 50f / nChars.Count;
                AddContentText(contentGo.transform, $"【N】各 {rate:F2}%", 26, ColN, yPos, w); yPos -= 38f;
                foreach (var ch in nChars)
                {
                    AddContentText(contentGo.transform, $"  {ch.characterName}  ─  {rate:F2}%", 24, ColN, yPos, w); yPos -= 34f;
                }
            }
        }

        contentRT.sizeDelta = new Vector2(0f, Mathf.Abs(yPos) + 20f);

        // 閉じるボタン（固定）
        MakeSmallButton(dialog.transform, "閉じる", new Color(0.3f, 0.2f, 0.45f),
            new Vector2(0.5f, 0.04f), new Vector2(240f, 60f),
            () => Destroy(overlay));
    }

    void AddContentText(Transform parent, string txt, int fontSize, Color col, float yPos, float w)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt; t.fontSize = fontSize; t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(w, fontSize + 12f);
    }

    // ---- ガチャ実行 ----

    void OnSingleDraw()
    {
        if (!OrbManager.SpendOrbs(OrbManager.CostSingle))
        {
            StartCoroutine(ShowInsufficientOrbs());
            return;
        }
        AudioManager.Instance?.PlayGachaSE();
        RefreshOrbDisplay();

        if (pool == null || allChars == null || allChars.Length == 0)
        {
            Debug.LogWarning("GachaPoolData または CharacterData が見つかりません");
            return;
        }

        GachaResult result = GachaEngine.DrawSingle(pool, allChars);
        RefreshOrbDisplay();
        StartCoroutine(ShowSingleResult(result));
    }

    void OnTenDraw()
    {
        if (!OrbManager.SpendOrbs(OrbManager.CostTen))
        {
            StartCoroutine(ShowInsufficientOrbs());
            return;
        }
        AudioManager.Instance?.PlayGachaSE();

        if (pool == null || allChars == null || allChars.Length == 0)
        {
            Debug.LogWarning("GachaPoolData または CharacterData が見つかりません");
            return;
        }

        GachaResult[] results = GachaEngine.DrawTen(pool, allChars);
        RefreshOrbDisplay();
        StartCoroutine(ShowTenResult(results));
    }

    // ---- 結果演出（フル本格化 / Phase D）----

    IEnumerator ShowSingleResult(GachaResult result)
    {
        SetButtonsInteractable(false);
        ClearResultPanel();
        resultPanel.SetActive(true);
        isSkipping = false;
        shakeTarget = canvasRoot as RectTransform;
        if (shakeTarget != null) shakeBaseAnchor = shakeTarget.anchoredPosition;

        // スキップボタン（常時表示）
        CreateSkipButton();

        // Act 0: 背景暗転 + 星空
        var bgDim = CreateBgDim();
        var stars = CreateStarField();
        yield return RunActOrSkip(FadeInBgDim(bgDim, 0.4f));

        // 継続パーティクル開始（バックグラウンドで動かす）
        var partHolder = CreateContinuousParticles();

        // Act 1: 魔法陣 + 光柱 + オーブ収束
        var magicCircle = CreateMagicCircle();
        yield return RunActOrSkip(PlayAct1_Summon(magicCircle));

        // Act 2: レアリティ判定（光線放射）+ SSR虹フラッシュ
        yield return RunActOrSkip(PlayAct2_RarityBeam(result.chara.rarity));

        // 魔法陣を爆発フェードアウト
        StartCoroutine(FadeOutAndDestroy(magicCircle, 0.4f));

        // Act 3: キャラ降臨（シルエット → フルカラー）
        yield return RunActOrSkip(PlayAct3_CharacterReveal(result.chara));

        // Act 4: カード情報 + 星マークアニメ
        var card = BuildResultCard(resultPanel.transform, result,
            new Vector2(0.5f, 0.18f), new Vector2(600f, 220f));
        yield return RunActOrSkip(PlayAct4_CardInfo(card, result));

        // キャラボイス再生
        if (result.chara.voiceSelect != null)
            AudioManager.Instance?.PlayVoice(
                result.chara.voiceSelect,
                result.chara.voiceVolumeMultiplier,
                AudioManager.VoicePriority.High);

        // Act 5: タップ待ち
        DestroySkipButton();
        yield return StartCoroutine(WaitForTapToProceed());

        // クリーンアップ
        if (partHolder != null) Destroy(partHolder);
        if (stars != null) Destroy(stars);
        if (bgDim != null) Destroy(bgDim);
        if (shakeTarget != null) shakeTarget.anchoredPosition = shakeBaseAnchor;

        resultPanel.SetActive(false);
        SetButtonsInteractable(true);
    }

    IEnumerator ShowTenResult(GachaResult[] results)
    {
        SetButtonsInteractable(false);
        ClearResultPanel();
        resultPanel.SetActive(true);
        isSkipping = false;
        shakeTarget = canvasRoot as RectTransform;
        if (shakeTarget != null) shakeBaseAnchor = shakeTarget.anchoredPosition;

        CreateSkipButton();

        // Act 0: 背景暗転
        var bgDim = CreateBgDim();
        var stars = CreateStarField();
        yield return RunActOrSkip(FadeInBgDim(bgDim, 0.4f));

        // 継続パーティクル
        var partHolder = CreateContinuousParticles();

        // 最高レアリティを判定
        Rarity highest = Rarity.N;
        CharacterData topChar = null;
        foreach (var r in results)
        {
            if (r.chara.rarity > highest) highest = r.chara.rarity;
            if (topChar == null || r.chara.rarity > topChar.rarity) topChar = r.chara;
        }

        // Act 1: 召喚（オーブ多め）
        var magicCircle = CreateMagicCircle();
        yield return RunActOrSkip(PlayAct1_Summon(magicCircle, orbCount: 15));

        // Act 2: 最高レアでの判定
        yield return RunActOrSkip(PlayAct2_RarityBeam(highest));

        StartCoroutine(FadeOutAndDestroy(magicCircle, 0.4f));

        // Act 3: 最高レアキャラのみシルエット→フル降臨
        if (topChar != null)
            yield return RunActOrSkip(PlayAct3_CharacterReveal(topChar));

        // Act 3.5: イラストをタップで結果（10連カード）を表示
        // 演出で出たイラストを先に楽しんでもらってからカード一斉表示へ
        GameObject revealedChar = null;
        foreach (Transform child in resultPanel.transform)
        {
            if (child != null && child.name == "CharImage")
            {
                revealedChar = child.gameObject;
                break;
            }
        }
        if (revealedChar != null && !isSkipping)
        {
            yield return StartCoroutine(WaitForIllustTapForCards(revealedChar));
            // タップ後、イラストをフェードアウト（カードが見やすくなるように）
            StartCoroutine(FadeOutAndDestroy(revealedChar, 0.4f));
        }

        // Act 4: 全10カードを2×5グリッドで一斉表示
        float[] xs = { 0.27f, 0.73f };
        float[] ys = { 0.82f, 0.64f, 0.46f, 0.28f, 0.1f };
        var cards = new List<GameObject>();
        for (int i = 0; i < results.Length; i++)
        {
            float ax = xs[i % 2];
            float ay = ys[i / 2];
            var c = BuildResultCard(resultPanel.transform, results[i],
                new Vector2(ax, ay), new Vector2(420f, 120f));
            cards.Add(c);
            var ig = c.GetComponent<Image>();
            ig.color = new Color(ig.color.r, ig.color.g, ig.color.b, 0f);
            c.transform.localScale = Vector3.zero;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            StartCoroutine(FadeInCardWithPop(cards[i], 0.4f));
            yield return new WaitForSeconds(0.08f);
            if (isSkipping) break;
        }
        if (!isSkipping) yield return new WaitForSeconds(0.4f);
        else
        {
            // スキップ時は全カードを即座に表示状態へ
            foreach (var c in cards)
                if (c != null)
                {
                    c.transform.localScale = Vector3.one;
                    var ig = c.GetComponent<Image>();
                    ig.color = new Color(ig.color.r, ig.color.g, ig.color.b, 1f);
                }
        }

        // 最高レアキャラのボイス
        if (topChar != null && topChar.voiceSelect != null)
            AudioManager.Instance?.PlayVoice(
                topChar.voiceSelect,
                topChar.voiceVolumeMultiplier,
                AudioManager.VoicePriority.High);

        DestroySkipButton();
        yield return StartCoroutine(WaitForTapToProceed());

        if (partHolder != null) Destroy(partHolder);
        if (stars != null) Destroy(stars);
        if (bgDim != null) Destroy(bgDim);
        if (shakeTarget != null) shakeTarget.anchoredPosition = shakeBaseAnchor;

        resultPanel.SetActive(false);
        SetButtonsInteractable(true);
    }

    // ============================================================
    // Phase D: フル本格化ガチャ演出メソッド群
    // ============================================================

    /// <summary>スキップ可能なコルーチン実行ヘルパー。isSkipping=true なら早期終了。</summary>
    IEnumerator RunActOrSkip(IEnumerator coroutine)
    {
        while (!isSkipping)
        {
            if (!coroutine.MoveNext()) yield break;
            yield return coroutine.Current;
        }
    }

    /// <summary>スキップボタン生成（右上、常時表示）</summary>
    void CreateSkipButton()
    {
        skipButton = new GameObject("SkipBtn");
        skipButton.transform.SetParent(canvasRoot, false);
        var img = skipButton.AddComponent<Image>();
        img.color = new Color(0.3f, 0.15f, 0.4f, 0.85f);
        var btn = skipButton.AddComponent<Button>();
        btn.onClick.AddListener(() => isSkipping = true);
        var rt = skipButton.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.9f, 0.96f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(180f, 70f);

        var t = new GameObject("SkipTxt").AddComponent<Text>();
        t.transform.SetParent(skipButton.transform, false);
        t.text = "スキップ▶";
        t.fontSize = 30;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 30);
        t.raycastTarget = false;
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var ol = t.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.8f);
        ol.effectDistance = new Vector2(2f, -2f);
    }

    void DestroySkipButton()
    {
        if (skipButton != null) Destroy(skipButton);
        skipButton = null;
    }

    // === Act 0: 背景演出 ===

    GameObject CreateBgDim()
    {
        var go = new GameObject("BgDim");
        go.transform.SetParent(resultPanel.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // 開始時は透明
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    IEnumerator FadeInBgDim(GameObject bgDim, float dur)
    {
        if (bgDim == null) yield break;
        var img = bgDim.GetComponent<Image>();
        float t = 0f;
        while (t < dur && img != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            img.color = new Color(0f, 0f, 0f, k * 0.92f);
            yield return null;
        }
    }

    /// <summary>背景の星空（小さい白い点を散りばめる）</summary>
    GameObject CreateStarField()
    {
        var holder = new GameObject("StarField");
        holder.transform.SetParent(resultPanel.transform, false);
        var hRt = holder.AddComponent<RectTransform>();
        hRt.anchorMin = Vector2.zero; hRt.anchorMax = Vector2.one;
        hRt.offsetMin = hRt.offsetMax = Vector2.zero;

        for (int i = 0; i < 80; i++)
        {
            var star = new GameObject($"Star_{i}");
            star.transform.SetParent(holder.transform, false);
            var img = star.AddComponent<Image>();
            float bright = Random.Range(0.4f, 1f);
            img.color = new Color(1f, 1f, 1f, bright);
            img.raycastTarget = false;
            var rt = star.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Random.value, Random.value);
            rt.anchoredPosition = Vector2.zero;
            float size = Random.Range(2f, 6f);
            rt.sizeDelta = new Vector2(size, size);
        }
        return holder;
    }

    /// <summary>継続的に流れるキラキラパーティクル（演出全体を通して動く）</summary>
    GameObject CreateContinuousParticles()
    {
        var holder = new GameObject("ContinuousParticles");
        holder.transform.SetParent(resultPanel.transform, false);
        var hRt = holder.AddComponent<RectTransform>();
        hRt.anchorMin = Vector2.zero; hRt.anchorMax = Vector2.one;
        hRt.offsetMin = hRt.offsetMax = Vector2.zero;

        StartCoroutine(ContinuousParticlesLoop(holder));
        return holder;
    }

    IEnumerator ContinuousParticlesLoop(GameObject holder)
    {
        // 0.1秒ごとに新しい粒子を1つ生成しながら、既存粒子を上昇＆フェードアウト
        while (holder != null && holder.activeInHierarchy)
        {
            // 粒子生成
            var part = new GameObject("P");
            part.transform.SetParent(holder.transform, false);
            var img = part.AddComponent<Image>();
            float hue = Random.Range(0.6f, 0.95f); // 紫〜ピンク〜黄
            img.color = Color.HSVToRGB(hue, 0.6f, 1f);
            img.raycastTarget = false;
            var rt = part.GetComponent<RectTransform>();
            float startX = Random.Range(0.05f, 0.95f);
            rt.anchorMin = rt.anchorMax = new Vector2(startX, -0.05f);
            rt.anchoredPosition = Vector2.zero;
            float size = Random.Range(4f, 10f);
            rt.sizeDelta = new Vector2(size, size);

            // 上昇アニメ
            StartCoroutine(MoveParticleUp(part));

            yield return new WaitForSeconds(0.08f);
        }
    }

    IEnumerator MoveParticleUp(GameObject part)
    {
        if (part == null) yield break;
        var rt = part.GetComponent<RectTransform>();
        var img = part.GetComponent<Image>();
        float duration = Random.Range(2.5f, 4.5f);
        float t = 0f;
        Vector2 startAnchor = rt.anchorMin;
        float drift = Random.Range(-0.15f, 0.15f);
        while (t < duration && part != null)
        {
            t += Time.deltaTime;
            float k = t / duration;
            rt.anchorMin = rt.anchorMax = new Vector2(
                Mathf.Lerp(startAnchor.x, startAnchor.x + drift, k),
                Mathf.Lerp(startAnchor.y, 1.1f, k)
            );
            if (img != null)
            {
                var c = img.color;
                c.a = Mathf.Sin(k * Mathf.PI); // 中盤で最大、終盤フェード
                img.color = c;
            }
            yield return null;
        }
        if (part != null) Destroy(part);
    }

    // === Act 1: 魔法陣 + 召喚 ===

    /// <summary>
    /// 魔法陣（外円・内円・装飾文字）を生成。
    /// 60個の小ドットを円形配置して環を表現＋装飾アイコン。
    /// </summary>
    GameObject CreateMagicCircle()
    {
        var holder = new GameObject("MagicCircle");
        holder.transform.SetParent(resultPanel.transform, false);
        var hRt = holder.AddComponent<RectTransform>();
        hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.5f);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(600f, 600f);
        hRt.localScale = Vector3.zero;

        // 外環（60ドット）
        BuildDottedRing(holder.transform, 270f, 60, new Color(1f, 0.85f, 0.2f, 0.95f), 6f);
        // 内環（40ドット）
        BuildDottedRing(holder.transform, 180f, 40, new Color(0.9f, 0.5f, 1f, 0.9f), 5f);
        // 内側中央（小さい円）
        BuildDottedRing(holder.transform, 90f, 24, new Color(1f, 1f, 1f, 0.8f), 4f);

        // 装飾シンボル（外周）
        var symbols = new[] { "✦", "✧", "★", "✩", "✶", "❀", "♡", "✦" };
        for (int i = 0; i < 8; i++)
        {
            float angle = (i / 8f) * Mathf.PI * 2f;
            var symGo = new GameObject($"Sym_{i}");
            symGo.transform.SetParent(holder.transform, false);
            var t = symGo.AddComponent<Text>();
            t.text = symbols[i];
            t.fontSize = 48;
            t.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            t.alignment = TextAnchor.MiddleCenter;
            var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
            t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 48);
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = symGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(Mathf.Cos(angle) * 320f, Mathf.Sin(angle) * 320f);
            rt.sizeDelta = new Vector2(60f, 60f);
            var ol = symGo.AddComponent<Outline>();
            ol.effectColor = new Color(0.4f, 0.15f, 0f, 0.9f);
            ol.effectDistance = new Vector2(2f, -2f);
        }

        // 中央コア（光球）
        var core = new GameObject("Core");
        core.transform.SetParent(holder.transform, false);
        var coreImg = core.AddComponent<Image>();
        coreImg.color = new Color(1f, 1f, 0.8f, 0.6f);
        coreImg.raycastTarget = false;
        var coreRt = core.GetComponent<RectTransform>();
        coreRt.anchorMin = coreRt.anchorMax = new Vector2(0.5f, 0.5f);
        coreRt.anchoredPosition = Vector2.zero;
        coreRt.sizeDelta = new Vector2(40f, 40f);

        // 回転コルーチン
        StartCoroutine(RotateMagicCircle(holder));
        return holder;
    }

    void BuildDottedRing(Transform parent, float radius, int dotCount, Color col, float dotSize)
    {
        for (int i = 0; i < dotCount; i++)
        {
            float angle = (i / (float)dotCount) * Mathf.PI * 2f;
            var dot = new GameObject($"Dot_{i}");
            dot.transform.SetParent(parent, false);
            var img = dot.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
            var rt = dot.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            rt.sizeDelta = new Vector2(dotSize, dotSize);
        }
    }

    IEnumerator RotateMagicCircle(GameObject holder)
    {
        // 子オブジェクトを回転（外環は時計回り、内環は反時計回り）
        while (holder != null)
        {
            float dt = Time.deltaTime;
            int idx = 0;
            foreach (Transform child in holder.transform)
            {
                if (child.name.StartsWith("Dot_"))
                {
                    // Holder ごと回転
                }
                idx++;
            }
            // Holder全体を回転（簡易版）
            holder.transform.Rotate(0f, 0f, 30f * dt);
            yield return null;
        }
    }

    /// <summary>
    /// Act 1: 召喚演出本体。
    /// 魔法陣がスケールアップで出現 → 光柱が立ち上がる → オーブが収束 → 爆発
    /// 約3秒。
    /// </summary>
    IEnumerator PlayAct1_Summon(GameObject magicCircle, int orbCount = 10)
    {
        // [1-A] 魔法陣スケールイン（0→1.0、0.8s）
        var mcRt = magicCircle.GetComponent<RectTransform>();
        float t1 = 0f;
        while (t1 < 0.8f && magicCircle != null)
        {
            t1 += Time.deltaTime;
            float k = Mathf.Clamp01(t1 / 0.8f);
            float ease = 1f - Mathf.Pow(1f - k, 3f);
            mcRt.localScale = Vector3.one * ease;
            yield return null;
        }

        // [1-B] 光柱の立ち上がり
        var pillar = new GameObject("LightPillar");
        pillar.transform.SetParent(resultPanel.transform, false);
        var pImg = pillar.AddComponent<Image>();
        pImg.color = new Color(1f, 0.9f, 0.5f, 0.6f);
        pImg.raycastTarget = false;
        var pRt = pillar.GetComponent<RectTransform>();
        pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.anchoredPosition = Vector2.zero;
        pRt.sizeDelta = new Vector2(60f, 0f);

        float t2 = 0f;
        while (t2 < 0.5f)
        {
            t2 += Time.deltaTime;
            float k = Mathf.Clamp01(t2 / 0.5f);
            pRt.sizeDelta = new Vector2(60f + k * 60f, k * 1920f);
            yield return null;
        }

        // [1-C] オーブ多数収束（同時）
        var orbHolder = new GameObject("Orbs");
        orbHolder.transform.SetParent(resultPanel.transform, false);
        var ohRt = orbHolder.AddComponent<RectTransform>();
        ohRt.anchorMin = Vector2.zero; ohRt.anchorMax = Vector2.one;
        ohRt.offsetMin = ohRt.offsetMax = Vector2.zero;

        var orbs = new RectTransform[orbCount];
        var orbStarts = new Vector2[orbCount];
        for (int i = 0; i < orbCount; i++)
        {
            float angle = (i / (float)orbCount) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float dist = Random.Range(700f, 900f);
            orbStarts[i] = new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);

            var orb = new GameObject($"Orb_{i}");
            orb.transform.SetParent(orbHolder.transform, false);
            var img = orb.AddComponent<Image>();
            img.color = new Color(1f, 0.95f, 0.5f, 0.95f);
            img.raycastTarget = false;
            var rt = orb.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = orbStarts[i];
            rt.sizeDelta = new Vector2(Random.Range(30f, 55f), Random.Range(30f, 55f));
            orbs[i] = rt;
        }

        float t3 = 0f;
        while (t3 < 1.0f)
        {
            t3 += Time.deltaTime;
            float k = Mathf.Clamp01(t3 / 1.0f);
            float ease = 1f - Mathf.Pow(1f - k, 4f);
            for (int i = 0; i < orbCount; i++)
                if (orbs[i] != null)
                    orbs[i].anchoredPosition = Vector2.Lerp(orbStarts[i], Vector2.zero, ease);
            yield return null;
        }

        // [1-D] 爆発フラッシュ
        var flash = new GameObject("ExplodeFlash");
        flash.transform.SetParent(resultPanel.transform, false);
        var flImg = flash.AddComponent<Image>();
        flImg.color = new Color(1f, 1f, 1f, 0.95f);
        flImg.raycastTarget = false;
        var flRt = flash.GetComponent<RectTransform>();
        flRt.anchorMin = flRt.anchorMax = new Vector2(0.5f, 0.5f);
        flRt.anchoredPosition = Vector2.zero;
        flRt.sizeDelta = new Vector2(80f, 80f);

        for (int i = 0; i < orbCount; i++)
            if (orbs[i] != null) Destroy(orbs[i].gameObject);

        // カメラシェイク
        StartCoroutine(ShakeCanvas(0.4f, 15f));

        float t4 = 0f;
        while (t4 < 0.5f)
        {
            t4 += Time.deltaTime;
            float k = t4 / 0.5f;
            float sz = Mathf.Lerp(80f, 2400f, k);
            flRt.sizeDelta = new Vector2(sz, sz);
            flImg.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.95f, 0f, k));
            yield return null;
        }

        Destroy(flash);
        Destroy(orbHolder);
        Destroy(pillar);
    }

    // === Act 2: レアリティ判定（光線放射）===

    IEnumerator PlayAct2_RarityBeam(Rarity rarity)
    {
        Color beamCol;
        bool isRainbow = false;
        switch (rarity)
        {
            case Rarity.SSR: beamCol = ColSSR; isRainbow = true; break;
            case Rarity.SR:  beamCol = ColSR;  break;
            case Rarity.R:   beamCol = ColR;   break;
            default:         beamCol = ColN;   break;
        }

        // 12本の光線を生成（中央から放射状）
        var beamHolder = new GameObject("BeamHolder");
        beamHolder.transform.SetParent(resultPanel.transform, false);
        var bhRt = beamHolder.AddComponent<RectTransform>();
        bhRt.anchorMin = bhRt.anchorMax = new Vector2(0.5f, 0.5f);
        bhRt.anchoredPosition = Vector2.zero;
        bhRt.sizeDelta = new Vector2(50f, 50f);
        bhRt.localScale = Vector3.zero;

        for (int i = 0; i < 12; i++)
        {
            float angle = (i / 12f) * 360f;
            var beam = new GameObject($"Beam_{i}");
            beam.transform.SetParent(beamHolder.transform, false);
            var img = beam.AddComponent<Image>();
            img.color = beamCol;
            img.raycastTarget = false;
            var rt = beam.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(50f, 1200f);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 全画面フラッシュ
        var bgFlash = new GameObject("BeamBgFlash");
        bgFlash.transform.SetParent(resultPanel.transform, false);
        var bfImg = bgFlash.AddComponent<Image>();
        bfImg.color = new Color(beamCol.r, beamCol.g, beamCol.b, 0f);
        bfImg.raycastTarget = false;
        var bfRt = bgFlash.GetComponent<RectTransform>();
        bfRt.anchorMin = Vector2.zero; bfRt.anchorMax = Vector2.one;
        bfRt.offsetMin = bfRt.offsetMax = Vector2.zero;

        // SSR時はカメラシェイク強め
        if (rarity == Rarity.SSR)
            StartCoroutine(ShakeCanvas(1.2f, 25f));
        else if (rarity == Rarity.SR)
            StartCoroutine(ShakeCanvas(0.6f, 12f));

        // [2-A] 光線拡大 + 回転 + フラッシュ
        float dur = (rarity >= Rarity.SR) ? 1.5f : 0.7f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);

            // 光線拡大
            beamHolder.transform.localScale = Vector3.one * Mathf.Lerp(0f, 3f, k);
            beamHolder.transform.Rotate(0f, 0f, 60f * Time.deltaTime);

            // 全画面フラッシュ：レインボー時は色循環、それ以外は単色パルス
            float bgAlpha;
            if (k < 0.2f) bgAlpha = Mathf.Lerp(0f, 0.7f, k / 0.2f);
            else if (k < 0.6f) bgAlpha = 0.7f;
            else bgAlpha = Mathf.Lerp(0.7f, 0f, (k - 0.6f) / 0.4f);

            Color cur = beamCol;
            if (isRainbow)
            {
                float hue = (Time.time * 1.5f) % 1f;
                cur = Color.HSVToRGB(hue, 0.85f, 1f);
            }
            bfImg.color = new Color(cur.r, cur.g, cur.b, bgAlpha);

            // 光線本体の色も同期（虹色循環）
            if (isRainbow)
            {
                foreach (Transform beam in beamHolder.transform)
                {
                    var bImg = beam.GetComponent<Image>();
                    if (bImg != null) bImg.color = cur;
                }
            }
            yield return null;
        }

        // [2-B] SSR時のみ「✦ SSR ✦」テキストパンチイン
        if (rarity >= Rarity.SR)
        {
            string label = rarity == Rarity.SSR ? "✦ SSR ✦" : "◆ SR ◆";
            yield return StartCoroutine(PlayRarityText(label, beamCol, isRainbow));
        }

        Destroy(beamHolder);
        Destroy(bgFlash);
    }

    IEnumerator PlayRarityText(string label, Color col, bool isRainbow)
    {
        var txtGo = new GameObject("RarityText");
        txtGo.transform.SetParent(resultPanel.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 160);
        t.fontSize = 160;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var sh = txtGo.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 1f);
        sh.effectDistance = new Vector2(5f, -5f);
        var ol = txtGo.AddComponent<Outline>();
        ol.effectColor = new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 1f);
        ol.effectDistance = new Vector2(4f, -4f);
        var tRt = txtGo.GetComponent<RectTransform>();
        tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = Vector2.zero;
        tRt.sizeDelta = new Vector2(1000f, 240f);
        tRt.localScale = Vector3.zero;

        float dur = 1.2f;
        float et = 0f;
        while (et < dur)
        {
            et += Time.deltaTime;
            float k = et / dur;

            // パンチイン
            float scale;
            if (k < 0.15f) scale = Mathf.Lerp(0f, 1.5f, k / 0.15f);
            else if (k < 0.3f) scale = Mathf.Lerp(1.5f, 1.0f, (k - 0.15f) / 0.15f);
            else if (k > 0.85f) scale = Mathf.Lerp(1.0f, 0f, (k - 0.85f) / 0.15f);
            else scale = 1.0f;
            tRt.localScale = Vector3.one * scale;

            // 揺れ
            if (k > 0.15f && k < 0.5f)
                tRt.anchoredPosition = new Vector2(Random.Range(-15f, 15f), Random.Range(-15f, 15f));
            else
                tRt.anchoredPosition = Vector2.zero;

            // レインボー時は色循環
            if (isRainbow)
                t.color = Color.HSVToRGB((Time.time * 1.5f) % 1f, 0.7f, 1f);

            yield return null;
        }
        Destroy(txtGo);
    }

    // === Act 3: キャラ降臨 ===

    IEnumerator PlayAct3_CharacterReveal(CharacterData chara)
    {
        // フルイラスト取得（無ければスキップ）
        Sprite charSprite = null;
        var allStages = Resources.LoadAll<StageData>("Stages");
        foreach (var s in allStages)
        {
            if (s.characterName == chara.characterName)
            {
                charSprite = s.illustSpriteFull;
                break;
            }
        }
        if (charSprite == null) charSprite = chara.icon; // フォールバック

        // [3-A] シルエット表示（黒）
        var charGo = new GameObject("CharImage");
        charGo.transform.SetParent(resultPanel.transform, false);
        var charImg = charGo.AddComponent<Image>();
        charImg.sprite = charSprite;
        charImg.preserveAspect = true;
        charImg.color = new Color(0.05f, 0.05f, 0.1f, 0f); // 黒シルエット
        charImg.raycastTarget = false;
        var cRt = charGo.GetComponent<RectTransform>();
        cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0.55f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(700f, 1100f);
        cRt.localScale = Vector3.zero;

        // フェードイン（シルエットだけ）
        float t1 = 0f;
        while (t1 < 0.5f)
        {
            t1 += Time.deltaTime;
            float k = Mathf.Clamp01(t1 / 0.5f);
            float ease = 1f - Mathf.Pow(1f - k, 3f);
            cRt.localScale = Vector3.one * Mathf.Lerp(0f, 1f, ease);
            charImg.color = new Color(0.05f, 0.05f, 0.1f, k);
            yield return null;
        }

        // [3-B] 色解像（シルエット → フルカラー）
        float t2 = 0f;
        while (t2 < 0.6f)
        {
            t2 += Time.deltaTime;
            float k = Mathf.Clamp01(t2 / 0.6f);
            charImg.color = Color.Lerp(new Color(0.05f, 0.05f, 0.1f, 1f), Color.white, k);
            yield return null;
        }
        charImg.color = Color.white;

        // [3-C] 解像時の光バースト
        var burst = new GameObject("Burst");
        burst.transform.SetParent(resultPanel.transform, false);
        var burstImg = burst.AddComponent<Image>();
        burstImg.color = new Color(1f, 1f, 0.9f, 0.9f);
        burstImg.raycastTarget = false;
        var bRt = burst.GetComponent<RectTransform>();
        bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 0.55f);
        bRt.anchoredPosition = Vector2.zero;
        bRt.sizeDelta = new Vector2(100f, 100f);

        float t3 = 0f;
        while (t3 < 0.4f)
        {
            t3 += Time.deltaTime;
            float k = t3 / 0.4f;
            float sz = Mathf.Lerp(100f, 1500f, k);
            bRt.sizeDelta = new Vector2(sz, sz);
            burstImg.color = new Color(1f, 1f, 0.9f, Mathf.Lerp(0.9f, 0f, k));
            yield return null;
        }
        Destroy(burst);

        // [3-D] キャラ画像のアイドル微動（少し呼吸）
        StartCoroutine(IdleBreathe(charGo));

        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator IdleBreathe(GameObject target)
    {
        if (target == null) yield break;
        var rt = target.GetComponent<RectTransform>();
        Vector3 baseScale = rt.localScale;
        float t = 0f;
        while (target != null && target.activeInHierarchy)
        {
            t += Time.deltaTime;
            float k = (Mathf.Sin(t * 1.5f) + 1f) * 0.5f;
            float s = Mathf.Lerp(1.0f, 1.02f, k);
            rt.localScale = baseScale * s;
            yield return null;
        }
    }

    /// <summary>
    /// 10連ガチャ Act3.5：イラストをタップで結果カードを表示。
    /// イラストにButton付与＋画面下にヒント文を表示し、タップ待機する。
    /// </summary>
    IEnumerator WaitForIllustTapForCards(GameObject illustGo)
    {
        if (illustGo == null) yield break;

        var charImg = illustGo.GetComponent<Image>();
        if (charImg != null) charImg.raycastTarget = true;

        bool tapped = false;
        var illustBtn = illustGo.AddComponent<Button>();
        illustBtn.transition = Selectable.Transition.None;
        illustBtn.onClick.AddListener(() => tapped = true);

        // ヒント文（画面下、結果パネル外）
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        var hintGo = new GameObject("IllustTapHint");
        hintGo.transform.SetParent(canvasRoot, false);
        var hint = hintGo.AddComponent<Text>();
        hint.text = "イラストをタップで結果を表示";
        hint.fontSize = 42;
        hint.color = new Color(1f, 0.95f, 0.5f, 0f);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 42);
        hint.raycastTarget = false;
        var hSh = hint.gameObject.AddComponent<Shadow>();
        hSh.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.95f);
        hSh.effectDistance = new Vector2(2f, -2f);
        var hOl = hint.gameObject.AddComponent<Outline>();
        hOl.effectColor = new Color(0f, 0f, 0f, 0.95f);
        hOl.effectDistance = new Vector2(2f, -2f);
        var hRt = hint.gameObject.GetComponent<RectTransform>();
        hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.12f);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(900f, 80f);

        // フェードイン
        float fIn = 0f;
        while (fIn < 0.4f && !tapped && !isSkipping)
        {
            fIn += Time.deltaTime;
            hint.color = new Color(1f, 0.95f, 0.5f, Mathf.Clamp01(fIn / 0.4f));
            yield return null;
        }

        // 点滅 + タップ待機
        while (!tapped && !isSkipping)
        {
            float pulse = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            hint.color = new Color(1f, 0.95f, 0.5f, Mathf.Lerp(0.5f, 1f, pulse));
            yield return null;
        }

        // クリーンアップ
        Destroy(hintGo);
        if (illustBtn != null) Destroy(illustBtn);
        if (charImg != null) charImg.raycastTarget = false;
    }

    // === Act 4: カード情報 + 星アニメ ===

    IEnumerator PlayAct4_CardInfo(GameObject card, GachaResult result)
    {
        if (card == null) yield break;

        // カードの初期化
        var imgs = card.GetComponentsInChildren<Image>();
        var texts = card.GetComponentsInChildren<Text>();
        foreach (var img in imgs) if (img != null) { var c = img.color; c.a = 0f; img.color = c; }
        foreach (var tx in texts) if (tx != null) { var c = tx.color; c.a = 0f; tx.color = c; }

        var cardRt = card.GetComponent<RectTransform>();
        Vector2 baseAnchor = cardRt.anchoredPosition;
        cardRt.anchoredPosition = baseAnchor + new Vector2(0f, -200f); // 下からスライドイン

        // [4-A] カードスライドイン + フェードイン
        float t1 = 0f;
        while (t1 < 0.5f)
        {
            t1 += Time.deltaTime;
            float k = Mathf.Clamp01(t1 / 0.5f);
            float ease = 1f - Mathf.Pow(1f - k, 3f);
            cardRt.anchoredPosition = baseAnchor + new Vector2(0f, Mathf.Lerp(-200f, 0f, ease));
            foreach (var img in imgs) if (img != null) { var c = img.color; c.a = k; img.color = c; }
            foreach (var tx in texts) if (tx != null) { var c = tx.color; c.a = k; tx.color = c; }
            yield return null;
        }

        // [4-B] 星マーク表示は廃止（カード内の "SSR ★★★★" 表記で十分なため）
    }

    int StarCountForRarity(Rarity r)
    {
        switch (r)
        {
            case Rarity.SSR: return 4;
            case Rarity.SR:  return 3;
            case Rarity.R:   return 2;
            default:         return 1;
        }
    }

    IEnumerator AnimateStars(int count, Rarity rarity)
    {
        Color starCol;
        switch (rarity)
        {
            case Rarity.SSR: starCol = ColSSR; break;
            case Rarity.SR:  starCol = ColSR;  break;
            case Rarity.R:   starCol = ColR;   break;
            default:         starCol = ColN;   break;
        }

        var holder = new GameObject("StarsHolder");
        holder.transform.SetParent(resultPanel.transform, false);
        var hRt = holder.AddComponent<RectTransform>();
        hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.34f);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(80f * count, 80f);

        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        float startX = -(count - 1) * 80f / 2f;

        for (int i = 0; i < count; i++)
        {
            var starGo = new GameObject($"Star_{i}");
            starGo.transform.SetParent(holder.transform, false);
            var t = starGo.AddComponent<Text>();
            t.text = "★";
            t.fontSize = 64;
            t.color = starCol;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 64);
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var ol = starGo.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
            ol.effectDistance = new Vector2(2f, -2f);
            var rt = starGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(startX + i * 80f, 0f);
            rt.sizeDelta = new Vector2(80f, 80f);
            rt.localScale = Vector3.zero;

            // ポップアニメーション
            StartCoroutine(PopStar(rt));
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator PopStar(RectTransform rt)
    {
        if (rt == null) yield break;
        float dur = 0.4f;
        float t = 0f;
        while (t < dur && rt != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float scale;
            if (k < 0.6f) scale = Mathf.Lerp(0f, 1.4f, k / 0.6f);
            else scale = Mathf.Lerp(1.4f, 1.0f, (k - 0.6f) / 0.4f);
            rt.localScale = Vector3.one * scale;
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    // === カメラシェイク ===

    IEnumerator ShakeCanvas(float duration, float magnitude)
    {
        if (shakeTarget == null) yield break;
        float t = 0f;
        while (t < duration && shakeTarget != null)
        {
            t += Time.deltaTime;
            float damp = 1f - (t / duration);
            shakeTarget.anchoredPosition = shakeBaseAnchor +
                new Vector2(Random.Range(-magnitude, magnitude), Random.Range(-magnitude, magnitude)) * damp;
            yield return null;
        }
        if (shakeTarget != null) shakeTarget.anchoredPosition = shakeBaseAnchor;
    }

    // === 共通ユーティリティ ===

    IEnumerator FadeOutAndDestroy(GameObject target, float dur)
    {
        if (target == null) yield break;
        var imgs = target.GetComponentsInChildren<Image>();
        var texts = target.GetComponentsInChildren<Text>();
        Vector3 baseScale = target.transform.localScale;

        float t = 0f;
        while (t < dur && target != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float alpha = 1f - k;
            foreach (var img in imgs) if (img != null) { var c = img.color; c.a = alpha; img.color = c; }
            foreach (var tx in texts) if (tx != null) { var c = tx.color; c.a = alpha; tx.color = c; }
            target.transform.localScale = baseScale * (1f + k * 0.5f); // 拡大しながら消える
            yield return null;
        }
        if (target != null) Destroy(target);
    }

    /// <summary>（旧Phase B互換ダミー、未使用）</summary>
    IEnumerator PlaySummonEffect()
    {
        var orbHolder = new GameObject("SummonOrbs");
        orbHolder.transform.SetParent(resultPanel.transform, false);
        var ohRt = orbHolder.AddComponent<RectTransform>();
        ohRt.anchorMin = Vector2.zero; ohRt.anchorMax = Vector2.one;
        ohRt.offsetMin = ohRt.offsetMax = Vector2.zero;

        // 8個のオーブを画面外周から中央へ収束
        const int orbCount = 8;
        var orbs = new RectTransform[orbCount];
        var startPositions = new Vector2[orbCount];

        for (int i = 0; i < orbCount; i++)
        {
            float angle = (i / (float)orbCount) * Mathf.PI * 2f;
            float dist = 600f;
            startPositions[i] = new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);

            var orb = new GameObject($"Orb_{i}");
            orb.transform.SetParent(orbHolder.transform, false);
            var img = orb.AddComponent<Image>();
            img.color = new Color(1f, 0.95f, 0.5f, 0.95f); // 黄金色
            img.raycastTarget = false;
            var rt = orb.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = startPositions[i];
            rt.sizeDelta = new Vector2(40f, 40f);
            orbs[i] = rt;
        }

        // 0.7秒かけて中央に収束
        float dur = 0.7f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float ease = 1f - Mathf.Pow(1f - k, 3f); // EaseOutCubic
            for (int i = 0; i < orbCount; i++)
            {
                if (orbs[i] == null) continue;
                orbs[i].anchoredPosition = Vector2.Lerp(startPositions[i], Vector2.zero, ease);
                float scale = 1f + ease * 0.5f;
                orbs[i].localScale = Vector3.one * scale;
            }
            yield return null;
        }

        // 中央で爆発（光が広がる）→ オーブを削除
        var flash = new GameObject("Flash");
        flash.transform.SetParent(orbHolder.transform, false);
        var flashImg = flash.AddComponent<Image>();
        flashImg.color = new Color(1f, 1f, 1f, 0.9f);
        flashImg.raycastTarget = false;
        var flRt = flash.GetComponent<RectTransform>();
        flRt.anchorMin = flRt.anchorMax = new Vector2(0.5f, 0.5f);
        flRt.anchoredPosition = Vector2.zero;
        flRt.sizeDelta = new Vector2(50f, 50f);

        // フラッシュ拡大 + フェードアウト
        for (int i = 0; i < orbCount; i++)
            if (orbs[i] != null) Destroy(orbs[i].gameObject);

        float fdur = 0.3f;
        float ft = 0f;
        while (ft < fdur)
        {
            ft += Time.deltaTime;
            float k = ft / fdur;
            float scale = Mathf.Lerp(50f, 1500f, k);
            flRt.sizeDelta = new Vector2(scale, scale);
            flashImg.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0f, k));
            yield return null;
        }

        Destroy(orbHolder);
    }

    /// <summary>
    /// レアリティ別カットイン演出。
    /// 大きなテキストが画面中央に飛び出し、レアカラーのフラッシュ＋振動。
    /// 約1.2秒間の演出。
    /// </summary>
    IEnumerator PlayRarityCutin(string label, Color rarityCol)
    {
        // フラッシュ全画面
        var flash = new GameObject("CutinFlash");
        flash.transform.SetParent(resultPanel.transform, false);
        var flashImg = flash.AddComponent<Image>();
        flashImg.color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0f);
        flashImg.raycastTarget = false;
        var fRt = flash.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
        fRt.offsetMin = fRt.offsetMax = Vector2.zero;

        // テキスト
        var txtGo = new GameObject("CutinText");
        txtGo.transform.SetParent(resultPanel.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 120);
        t.fontSize = 130;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var sh = txtGo.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
        sh.effectDistance = new Vector2(4f, -4f);
        var ol = txtGo.AddComponent<Outline>();
        ol.effectColor = new Color(rarityCol.r * 0.4f, rarityCol.g * 0.4f, rarityCol.b * 0.4f, 1f);
        ol.effectDistance = new Vector2(3f, -3f);
        var tRt = txtGo.GetComponent<RectTransform>();
        tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = Vector2.zero;
        tRt.sizeDelta = new Vector2(1000f, 200f);
        tRt.localScale = Vector3.zero;

        // フラッシュをパルス、テキストをパンチ
        float dur = 1.2f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float k = elapsed / dur;

            // フラッシュ：0→0.6→0
            float flashAlpha;
            if (k < 0.2f) flashAlpha = Mathf.Lerp(0f, 0.6f, k / 0.2f);
            else if (k < 0.4f) flashAlpha = Mathf.Lerp(0.6f, 0.2f, (k - 0.2f) / 0.2f);
            else flashAlpha = Mathf.Lerp(0.2f, 0f, (k - 0.4f) / 0.6f);
            flashImg.color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, flashAlpha);

            // テキストスケール：パンチ（0→1.3→1.0、0.5sで完成）
            float scale;
            if (k < 0.15f) scale = Mathf.Lerp(0f, 1.3f, k / 0.15f);
            else if (k < 0.3f) scale = Mathf.Lerp(1.3f, 1.0f, (k - 0.15f) / 0.15f);
            else if (k > 0.85f) scale = Mathf.Lerp(1.0f, 0f, (k - 0.85f) / 0.15f); // 終盤に縮小
            else scale = 1.0f;
            tRt.localScale = Vector3.one * scale;

            // 振動（ランダム揺れ）
            if (k > 0.15f && k < 0.4f)
                tRt.anchoredPosition = new Vector2(
                    Random.Range(-12f, 12f), Random.Range(-12f, 12f));
            else
                tRt.anchoredPosition = Vector2.zero;

            yield return null;
        }

        Destroy(flash);
        Destroy(txtGo);
    }

    /// <summary>
    /// カードのフェードイン＋スケールポップ（共通ヘルパー）。
    /// </summary>
    IEnumerator FadeInCardWithPop(GameObject card, float duration)
    {
        if (card == null) yield break;
        var imgs = card.GetComponentsInChildren<Image>();
        var texts = card.GetComponentsInChildren<Text>();

        var rt = card.GetComponent<RectTransform>();
        Vector3 baseScale = Vector3.one;

        float t = 0f;
        while (t < duration && card != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            // スケール：0→1.15→1.0
            float scale;
            if (k < 0.6f) scale = Mathf.Lerp(0f, 1.15f, k / 0.6f);
            else scale = Mathf.Lerp(1.15f, 1.0f, (k - 0.6f) / 0.4f);
            rt.localScale = baseScale * scale;

            // アルファ：0→1
            float alpha = Mathf.Clamp01(k * 1.5f);
            foreach (var img in imgs)
            {
                if (img == null) continue;
                var c = img.color; c.a = alpha; img.color = c;
            }
            foreach (var tx in texts)
            {
                if (tx == null) continue;
                var c = tx.color; c.a = alpha; tx.color = c;
            }

            yield return null;
        }

        if (card != null) rt.localScale = baseScale;
    }

    /// <summary>
    /// 「タップで次へ」表示 → プレイヤーがタップするまで待機。
    /// </summary>
    IEnumerator WaitForTapToProceed()
    {
        // ヒント文表示（結果パネル外、画面最下部に配置してカードと被らないように）
        var hintGo = new GameObject("TapHint");
        hintGo.transform.SetParent(canvasRoot, false); // ★canvasRootに直接
        var hint = hintGo.AddComponent<Text>();
        hint.text = "タップで次へ";
        hint.fontSize = 42;
        hint.color = new Color(1f, 1f, 1f, 0f);
        hint.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        hint.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 42);
        hint.raycastTarget = false;
        var hSh = hintGo.AddComponent<Shadow>();
        hSh.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.9f);
        hSh.effectDistance = new Vector2(2f, -2f);
        var hOl = hintGo.AddComponent<Outline>();
        hOl.effectColor = new Color(0f, 0f, 0f, 0.95f);
        hOl.effectDistance = new Vector2(2f, -2f);
        var hRt = hintGo.GetComponent<RectTransform>();
        // 結果パネル(y=0.30〜0.78)の下端と単発ボタン上端(≈0.247)の間の空きに配置
        // （y=0.12 だと10連ボタンに重なって読みにくいため移動）
        hint.horizontalOverflow = HorizontalWrapMode.Overflow;
        hint.verticalOverflow   = VerticalWrapMode.Overflow; // 高さ不足でも Truncate で消えないように
        hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.275f);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(600f, 70f);

        // フェードイン + 点滅ループ
        bool tapped = false;
        var tapBtnGo = new GameObject("TapCatcher");
        tapBtnGo.transform.SetParent(resultPanel.transform, false);
        tapBtnGo.AddComponent<Image>().color = Color.clear;
        var tbRt = tapBtnGo.GetComponent<RectTransform>();
        tbRt.anchorMin = Vector2.zero; tbRt.anchorMax = Vector2.one;
        tbRt.offsetMin = tbRt.offsetMax = Vector2.zero;
        var tBtn = tapBtnGo.AddComponent<Button>();
        tBtn.transition = Selectable.Transition.None;
        tBtn.onClick.AddListener(() => tapped = true);
        // カードのアイコンタップ（詳細ポップアップ）を阻害しないよう
        // TapCatcher を最背面に配置する（クリック優先度を下げる）
        tapBtnGo.transform.SetAsFirstSibling();

        // 結果パネル外（下部）でもタップで閉じられるよう、追加のタップエリアを canvasRoot に作成
        // ヒントテキストはここに含まれる → タップで次へをタップしても確実に進む
        var outerTapGo = new GameObject("OuterTapCatcher");
        outerTapGo.transform.SetParent(canvasRoot, false);
        var oImg = outerTapGo.AddComponent<Image>();
        oImg.color = Color.clear;
        var oBtn = outerTapGo.AddComponent<Button>();
        oBtn.transition = Selectable.Transition.None;
        oBtn.onClick.AddListener(() => tapped = true);
        var oRt = outerTapGo.GetComponent<RectTransform>();
        // 結果パネル下（y=0〜0.28）の領域、ヒントを完全に含む
        oRt.anchorMin = new Vector2(0f, 0f);
        oRt.anchorMax = new Vector2(1f, 0.28f);
        oRt.offsetMin = oRt.offsetMax = Vector2.zero;
        // hintGo より前のz順に配置することで、ヒント文字自体は普通に表示される
        outerTapGo.transform.SetSiblingIndex(hintGo.transform.GetSiblingIndex());

        // フェードイン
        float fIn = 0f;
        while (fIn < 0.4f && !tapped)
        {
            fIn += Time.deltaTime;
            hint.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fIn / 0.4f));
            yield return null;
        }

        // 点滅
        while (!tapped)
        {
            float pulse = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            hint.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.5f, 1f, pulse));
            yield return null;
        }

        Destroy(hintGo);
        Destroy(tapBtnGo);
        if (outerTapGo != null) Destroy(outerTapGo);
    }

    IEnumerator ShowInsufficientOrbs()
    {
        SetButtonsInteractable(false);
        var warn = MakeText(canvasRoot, "オーブが足りません", 32,
            new Color(1f, 0.3f, 0.3f), new Vector2(0.5f, 0.28f), new Vector2(500f, 50f));
        yield return new WaitForSeconds(1.5f);
        Destroy(warn.gameObject);
        SetButtonsInteractable(true);
    }

    // ---- カード生成 ----

    GameObject BuildResultCard(Transform parent, GachaResult result, Vector2 anchor, Vector2 size)
    {
        Color rarityCol = RarityColor(result.chara.rarity);
        Color bgCol     = new Color(rarityCol.r * 0.2f, rarityCol.g * 0.2f, rarityCol.b * 0.2f);

        var cardGo = new GameObject("Card");
        cardGo.transform.SetParent(parent, false);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0.6f);
        cardImg.raycastTarget = true; // ★カード全体をクリック対象に
        var rt = cardGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        // カード全体をボタン化（アイコン以外のどこをタップしても詳細ポップアップ）
        var cardBtn = cardGo.AddComponent<Button>();
        cardBtn.transition = Selectable.Transition.ColorTint;
        cardBtn.targetGraphic = cardImg;
        CharacterData capturedCharaForCard = result.chara;
        cardBtn.onClick.AddListener(() => OpenCharaDetailPopup(capturedCharaForCard));

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(cardGo.transform, false);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = bgCol;
        innerImg.raycastTarget = false; // 装飾なのでクリック透過
        var iRt = innerGo.GetComponent<RectTransform>();
        iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
        iRt.offsetMin = new Vector2(3f, 3f); iRt.offsetMax = new Vector2(-3f, -3f);

        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        var shineImg = shineGo.AddComponent<Image>();
        shineImg.color = new Color(1f, 1f, 1f, 0.05f); // より控えめに
        shineImg.raycastTarget = false;
        var shRt = shineGo.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0f, 0.75f); shRt.anchorMax = Vector2.one; // 上端だけ
        shRt.offsetMin = shRt.offsetMax = Vector2.zero;

        var barGo = new GameObject("RarityBar");
        barGo.transform.SetParent(cardGo.transform, false);
        var barImg = barGo.AddComponent<Image>();
        barImg.color = rarityCol;
        barImg.raycastTarget = false;
        var brt = barGo.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(0f, 5f);

        float textOffsetX = 0.5f;
        if (result.chara.icon != null)
        {
            float iconSize = Mathf.Min(size.y - 10f, 140f);

            // 覚醒済みなら金枠
            if (OrbManager.IsAwakened(result.chara.characterName))
            {
                var gf = new GameObject("GoldFrame");
                gf.transform.SetParent(cardGo.transform, false);
                var gfImg = gf.AddComponent<Image>();
                gfImg.color = new Color(1f, 0.85f, 0.1f, 0.9f);
                gfImg.raycastTarget = false;
                var gfRt = gf.GetComponent<RectTransform>();
                gfRt.anchorMin = gfRt.anchorMax = new Vector2(0.12f, 0.5f);
                gfRt.anchoredPosition = Vector2.zero;
                gfRt.sizeDelta = new Vector2(iconSize + 10f, iconSize + 10f);
            }

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(cardGo.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = result.chara.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false; // ★クリックは親のカードボタンに委譲
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0.12f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(iconSize, iconSize);
            textOffsetX = 0.58f;
        }

        // テキスト視認性のためのダーク背景パネル
        var nameBgGo = new GameObject("NameBg");
        nameBgGo.transform.SetParent(cardGo.transform, false);
        var nameBgImg = nameBgGo.AddComponent<Image>();
        nameBgImg.color = new Color(0.02f, 0.0f, 0.05f, 0.75f); // ほぼ黒、半透明
        nameBgImg.raycastTarget = false;
        var nbRt = nameBgGo.GetComponent<RectTransform>();
        nbRt.anchorMin = new Vector2(textOffsetX - 0.32f, 0.10f);
        nbRt.anchorMax = new Vector2(textOffsetX + 0.32f, 0.90f);
        nbRt.offsetMin = nbRt.offsetMax = Vector2.zero;

        string nameLabel = result.chara.characterName;
        if (result.isNew) nameLabel += "  ✦NEW!";
        var nameT = MakeText(cardGo.transform, nameLabel, 44, Color.white,
            new Vector2(textOffsetX, 0.65f), new Vector2(size.x * 0.7f, 60f));
        nameT.raycastTarget = false;
        nameT.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameT.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(nameT.gameObject);
        var nameOl = nameT.gameObject.AddComponent<Outline>();
        nameOl.effectColor = new Color(0f, 0f, 0f, 1f);
        nameOl.effectDistance = new Vector2(2f, -2f);

        string stars = RarityStars(result.chara.rarity);
        var rareT = MakeText(cardGo.transform, $"{result.chara.rarity} {stars}", 34, rarityCol,
            new Vector2(textOffsetX, 0.30f), new Vector2(size.x * 0.7f, 48f));
        rareT.raycastTarget = false;
        rareT.horizontalOverflow = HorizontalWrapMode.Overflow;
        rareT.verticalOverflow = VerticalWrapMode.Overflow;
        var rareOl = rareT.gameObject.AddComponent<Outline>();
        rareOl.effectColor = new Color(0f, 0f, 0f, 1f);
        rareOl.effectDistance = new Vector2(2f, -2f);
        var rareSh = rareT.gameObject.AddComponent<Shadow>();
        rareSh.effectColor = new Color(0f, 0f, 0f, 0.9f);
        rareSh.effectDistance = new Vector2(2f, -2f);

        return cardGo;
    }

    string RarityStars(Rarity r)
    {
        switch (r)
        {
            case Rarity.SSR: return "★★★★";
            case Rarity.SR:  return "★★★";
            case Rarity.R:   return "★★";
            default:         return "★";
        }
    }

    // ============================================================
    // キャラ詳細ポップアップ（ガチャカード→アイコンタップで起動）
    // ============================================================

    GameObject charaDetailPanel;

    void OpenCharaDetailPopup(CharacterData cd)
    {
        if (cd == null) return;
        if (charaDetailPanel != null) Destroy(charaDetailPanel);
        BuildCharaDetailPopup(cd);
    }

    void BuildCharaDetailPopup(CharacterData cd)
    {
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        Color rarityCol = RarityColor(cd.rarity);

        // 該当キャラのStageDataからフルイラスト取得
        Sprite fullSprite = null;
        var allStages = Resources.LoadAll<StageData>("Stages");
        foreach (var s in allStages)
        {
            if (s != null && s.characterName == cd.characterName)
            {
                fullSprite = s.illustSpriteFull;
                break;
            }
        }
        if (fullSprite == null) fullSprite = cd.icon; // フォールバック

        // 全画面オーバーレイ（タップで閉じる）
        charaDetailPanel = new GameObject("CharaDetailPanel");
        charaDetailPanel.transform.SetParent(canvasRoot, false);
        var overlayImg = charaDetailPanel.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.88f);
        var rt = charaDetailPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        // 背景タップで閉じる
        var bgBtn = charaDetailPanel.AddComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;
        bgBtn.onClick.AddListener(() => { if (charaDetailPanel != null) Destroy(charaDetailPanel); });

        // [1] フルイラスト（画面上半分・大きく表示）
        var illustGo = new GameObject("Illust");
        illustGo.transform.SetParent(charaDetailPanel.transform, false);
        var illustImg = illustGo.AddComponent<Image>();
        illustImg.sprite = fullSprite;
        illustImg.preserveAspect = true;
        illustImg.color = Color.white;
        illustImg.raycastTarget = false;
        var iRt = illustGo.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0f, 0.45f);
        iRt.anchorMax = new Vector2(1f, 1.0f);
        iRt.offsetMin = new Vector2(40f, 30f);
        iRt.offsetMax = new Vector2(-40f, -120f);

        // [2] レアリティ枠の色ライン（中央仕切り）
        var lineGo = new GameObject("DivLine");
        lineGo.transform.SetParent(charaDetailPanel.transform, false);
        lineGo.AddComponent<Image>().color = rarityCol;
        var lRt = lineGo.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0.45f);
        lRt.anchorMax = new Vector2(1f, 0.45f);
        lRt.pivot = new Vector2(0.5f, 0.5f);
        lRt.anchoredPosition = Vector2.zero;
        lRt.sizeDelta = new Vector2(0f, 4f);

        // [3] キャラ名のみ（星は不要）
        var nameT = new GameObject("NameTxt").AddComponent<Text>();
        nameT.transform.SetParent(charaDetailPanel.transform, false);
        nameT.text = cd.characterName;
        nameT.fontSize = 58;
        nameT.color = Color.white;
        nameT.alignment = TextAnchor.MiddleCenter;
        nameT.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 58);
        nameT.raycastTarget = false;
        nameT.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameT.verticalOverflow = VerticalWrapMode.Overflow;
        var nameShadow = nameT.gameObject.AddComponent<Shadow>();
        nameShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        nameShadow.effectDistance = new Vector2(3f, -3f);
        var nameOl = nameT.gameObject.AddComponent<Outline>();
        nameOl.effectColor = new Color(rarityCol.r * 0.5f, rarityCol.g * 0.5f, rarityCol.b * 0.5f, 1f);
        nameOl.effectDistance = new Vector2(2f, -2f);
        var nRt = nameT.gameObject.GetComponent<RectTransform>();
        nRt.anchorMin = nRt.anchorMax = new Vector2(0.5f, 0.96f);
        nRt.anchoredPosition = Vector2.zero;
        nRt.sizeDelta = new Vector2(1000f, 80f);

        // [4] レアリティラベル
        var rareT = new GameObject("RareTxt").AddComponent<Text>();
        rareT.transform.SetParent(charaDetailPanel.transform, false);
        rareT.text = cd.rarity.ToString();
        rareT.fontSize = 42;
        rareT.color = rarityCol;
        rareT.alignment = TextAnchor.MiddleCenter;
        rareT.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 42);
        rareT.raycastTarget = false;
        var rareOl = rareT.gameObject.AddComponent<Outline>();
        rareOl.effectColor = new Color(0f, 0f, 0f, 0.9f);
        rareOl.effectDistance = new Vector2(2f, -2f);
        var rRt = rareT.gameObject.GetComponent<RectTransform>();
        rRt.anchorMin = rRt.anchorMax = new Vector2(0.5f, 0.42f);
        rRt.anchoredPosition = Vector2.zero;
        rRt.sizeDelta = new Vector2(800f, 55f);

        // [5] パッシブスキル説明
        BuildDetailRow(charaDetailPanel.transform, "◆ パッシブスキル",
            DescribePassiveFull(cd),
            new Vector2(0.5f, 0.30f),
            new Color(0.4f, 0.95f, 1f));

        // [6] 奥義説明
        BuildDetailRow(charaDetailPanel.transform, "✦ 奥義",
            DescribeUltimateFull(cd),
            new Vector2(0.5f, 0.13f),
            new Color(1f, 0.85f, 0.3f));

        // [7] 閉じるボタン（右上 ✕）
        var closeGo = new GameObject("Close");
        closeGo.transform.SetParent(charaDetailPanel.transform, false);
        closeGo.AddComponent<Image>().color = new Color(0.5f, 0.15f, 0.25f, 0.95f);
        var closeBtn = closeGo.AddComponent<Button>();
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.93f, 0.96f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(100f, 90f);
        closeBtn.onClick.AddListener(() => { if (charaDetailPanel != null) Destroy(charaDetailPanel); });
        var closeT = new GameObject("CloseTxt").AddComponent<Text>();
        closeT.transform.SetParent(closeGo.transform, false);
        closeT.text = "✕";
        closeT.fontSize = 48;
        closeT.color = Color.white;
        closeT.alignment = TextAnchor.MiddleCenter;
        closeT.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 48);
        closeT.raycastTarget = false;
        var ctRt = closeT.gameObject.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero; ctRt.anchorMax = Vector2.one;
        ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;
    }

    /// <summary>詳細行（上部にラベル + 下部に本文）を生成</summary>
    void BuildDetailRow(Transform parent, string label, string body, Vector2 anchor, Color labelCol)
    {
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");

        // 背景パネル（縦に広げて、ラベルと本文を見やすく）
        var panelGo = new GameObject($"DetailRow_{label}");
        panelGo.transform.SetParent(parent, false);
        panelGo.AddComponent<Image>().color = new Color(0.08f, 0.05f, 0.18f, 0.92f);
        var pRt = panelGo.GetComponent<RectTransform>();
        pRt.anchorMin = pRt.anchorMax = anchor;
        pRt.anchoredPosition = Vector2.zero;
        pRt.sizeDelta = new Vector2(960f, 160f);

        // 上部ラベル（パッシブ or 奥義 — 大きく目立つ）
        var labelT = new GameObject("Lbl").AddComponent<Text>();
        labelT.transform.SetParent(panelGo.transform, false);
        labelT.text = label;
        labelT.fontSize = 46;
        labelT.color = labelCol;
        labelT.alignment = TextAnchor.MiddleLeft;
        labelT.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 46);
        labelT.raycastTarget = false;
        labelT.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelT.verticalOverflow = VerticalWrapMode.Overflow;
        var lblSh = labelT.gameObject.AddComponent<Shadow>();
        lblSh.effectColor = new Color(0f, 0f, 0f, 0.95f);
        lblSh.effectDistance = new Vector2(2f, -2f);
        var lblOl = labelT.gameObject.AddComponent<Outline>();
        lblOl.effectColor = new Color(0f, 0f, 0f, 1f);
        lblOl.effectDistance = new Vector2(2f, -2f);
        var lblRt = labelT.gameObject.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 1f); lblRt.anchorMax = new Vector2(1f, 1f);
        lblRt.pivot = new Vector2(0f, 1f);
        lblRt.anchoredPosition = new Vector2(30f, -10f);
        lblRt.sizeDelta = new Vector2(900f, 60f);

        // ラベルと本文を区切る装飾ライン
        var divGo = new GameObject("Divider");
        divGo.transform.SetParent(panelGo.transform, false);
        var divImg = divGo.AddComponent<Image>();
        divImg.color = new Color(labelCol.r, labelCol.g, labelCol.b, 0.6f);
        divImg.raycastTarget = false;
        var divRt = divGo.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0f, 1f); divRt.anchorMax = new Vector2(1f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -70f);
        divRt.sizeDelta = new Vector2(-20f, 3f); // 横方向に少し縮める

        // 本文（効果説明）— 大きく
        var bodyT = new GameObject("Body").AddComponent<Text>();
        bodyT.transform.SetParent(panelGo.transform, false);
        bodyT.text = body;
        bodyT.fontSize = 38;
        bodyT.color = Color.white;
        bodyT.alignment = TextAnchor.UpperLeft;
        bodyT.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 38);
        bodyT.raycastTarget = false;
        bodyT.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyT.verticalOverflow = VerticalWrapMode.Overflow;
        var bdySh = bodyT.gameObject.AddComponent<Shadow>();
        bdySh.effectColor = new Color(0f, 0f, 0f, 0.9f);
        bdySh.effectDistance = new Vector2(1.5f, -1.5f);
        var bdyOl = bodyT.gameObject.AddComponent<Outline>();
        bdyOl.effectColor = new Color(0f, 0f, 0f, 0.95f);
        bdyOl.effectDistance = new Vector2(1.5f, -1.5f);
        var bdyRt = bodyT.gameObject.GetComponent<RectTransform>();
        bdyRt.anchorMin = new Vector2(0f, 0f); bdyRt.anchorMax = new Vector2(1f, 1f);
        bdyRt.offsetMin = new Vector2(40f, 10f); bdyRt.offsetMax = new Vector2(-20f, -80f);
    }

    /// <summary>キャラのパッシブ効果を読みやすい日本語に変換</summary>
    string DescribePassiveFull(CharacterData cd)
    {
        var parts = new List<string>();
        string p1 = DescribePassiveOne(cd.passiveType, cd.passiveValue);
        if (!string.IsNullOrEmpty(p1)) parts.Add(p1);
        string p2 = DescribePassiveOne(cd.passiveType2, cd.passiveValue2);
        if (!string.IsNullOrEmpty(p2)) parts.Add(p2);
        return parts.Count > 0 ? string.Join("\n", parts) : "なし";
    }

    string DescribePassiveOne(PassiveEffectType type, float value)
    {
        switch (type)
        {
            case PassiveEffectType.BallDamageUp:
                return $"・ダメージ ×{value:F1}";
            case PassiveEffectType.ExtraDamage:
                return $"・追加ダメージ +{(int)value}";
            case PassiveEffectType.ExtraStock:
                return $"・開始時ストック +{(int)value}";
            case PassiveEffectType.UltGaugeBoost:
                return $"・奥義ゲージ増加量 ×{value:F1}";
            case PassiveEffectType.CriticalRangeUp:
                return $"・クリティカル範囲 +{(int)value}%";
            default:
                return "";
        }
    }

    /// <summary>キャラの奥義を読みやすい日本語に変換</summary>
    string DescribeUltimateFull(CharacterData cd)
    {
        switch (cd.ultimateType)
        {
            case UltimateSkillType.PowerBurst:
                return $"・{cd.ultimateDuration:F0}秒間、ダメージ ×{cd.ultimateValue:F1}";
            case UltimateSkillType.MassDestroy:
                return $"・全ブロックに {(int)cd.ultimateValue} ダメージ";
            case UltimateSkillType.StockRecover:
                return $"・ストック回復 +{(int)cd.ultimateValue}";
            case UltimateSkillType.BarrierShot:
                return "・次の1ミスをキャンセル";
            case UltimateSkillType.Penetrate:
                return $"・{cd.ultimateDuration:F0}秒間、ボールがブロックを貫通";
            case UltimateSkillType.BallSplit:
                return "・ボールを2つに分裂（分裂したボールも再分裂可能）";
            default:
                return "なし";
        }
    }

    void AddCloseOverlay()
    {
        var ov = new GameObject("CloseOverlay");
        ov.transform.SetParent(canvasRoot, false);
        ov.AddComponent<Image>().color = Color.clear;
        var rt = ov.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var btn = ov.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() =>
        {
            resultPanel.SetActive(false);
            Destroy(ov);
        });
    }

    void ClearResultPanel()
    {
        foreach (Transform child in resultPanel.transform)
        {
            if (child.name == "Inner") continue;
            Destroy(child.gameObject);
        }
    }

    // ---- 表示更新 ----

    void RefreshOrbDisplay()
    {
        if (orbText)   orbText.text   = $"◆ 所持オーブ: {OrbManager.GetOrbs()}";
        if (pityText)  pityText.text  = $"天井まで: {OrbManager.PityLimit - OrbManager.GetPityCount()}";
        if (ownedText) ownedText.text = $"キャラ所持数: {OrbManager.GetOwnedCount()} / {OrbManager.MaxOwnedCharacters}";
        RefreshButtons();
    }

    void RefreshButtons()
    {
        bool canSingle = OrbManager.CanAfford(OrbManager.CostSingle);
        bool canTen    = OrbManager.CanAfford(OrbManager.CostTen);
        if (btnSingle) btnSingle.interactable = canSingle;
        if (btnTen)    btnTen.interactable    = canTen;
        UpdateGrayOverlays();

        // 所持上限ロジックは撤廃（余剰キャラはオーブ変換できるため上限不要）
        if (capacityText != null) capacityText.text = "";
    }

    void SetButtonsInteractable(bool value)
    {
        if (btnSingle) btnSingle.interactable = value && OrbManager.CanAfford(OrbManager.CostSingle);
        if (btnTen)    btnTen.interactable    = value && OrbManager.CanAfford(OrbManager.CostTen);
        UpdateGrayOverlays();
    }

    /// <summary>
    /// ボタン全面を覆う薄いグレーの Image を最前面子として生成（初期非表示）。
    /// 引けない状態の視覚表現用。raycast は透過しない設定にして誤タップも防ぐ。
    /// </summary>
    GameObject MakeGrayOverlay(Button btn)
    {
        if (btn == null) return null;
        var go = new GameObject("GrayOverlay");
        go.transform.SetParent(btn.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.55f, 0.55f, 0.58f, 0.72f); // 薄いグレー（半透明）
        img.raycastTarget = true; // オーバーレイ表示中はタップも遮断
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.transform.SetAsLastSibling(); // ラベル・アイコンより前面
        go.SetActive(false);
        return go;
    }

    /// <summary>引けるかどうかに応じてグレーオーバーレイの表示を切替。</summary>
    void UpdateGrayOverlays()
    {
        bool canSingle = OrbManager.CanAfford(OrbManager.CostSingle);
        bool canTen    = OrbManager.CanAfford(OrbManager.CostTen);
        if (graySingle != null) graySingle.SetActive(!canSingle);
        if (grayTen    != null) grayTen.SetActive(!canTen);
    }

    // ---- レアリティカラー ----

    static Color RarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.SSR: return ColSSR;
            case Rarity.SR:  return ColSR;
            case Rarity.R:   return ColR;
            default:         return ColN;
        }
    }

    // ---- ガチャボタン ----

    Button MakeGachaButton(Transform parent, string label, Color baseCol, Color highlightCol,
        Vector2 anchor, Vector2 sizeDelta, string icon, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(highlightCol.r, highlightCol.g, highlightCol.b, 0.6f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        btn.onClick.AddListener(onClick);

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.AddComponent<Image>().color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.93f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        var shRt = shineGo.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0f, 0.5f); shRt.anchorMax = Vector2.one;
        shRt.offsetMin = shRt.offsetMax = Vector2.zero;

        var liGo = new GameObject("LIcon");
        liGo.transform.SetParent(go.transform, false);
        var liT = liGo.AddComponent<Text>();
        liT.text = icon; liT.fontSize = 28;
        liT.color = new Color(1f, 1f, 1f, 0.7f);
        liT.alignment = TextAnchor.MiddleCenter;
        liT.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        var liRt = liGo.GetComponent<RectTransform>();
        liRt.anchorMin = liRt.anchorMax = new Vector2(0f, 0.5f);
        liRt.anchoredPosition = new Vector2(30f, 0f);
        liRt.sizeDelta = new Vector2(36f, 36f);

        var riGo = new GameObject("RIcon");
        riGo.transform.SetParent(go.transform, false);
        var riT = riGo.AddComponent<Text>();
        riT.text = icon; riT.fontSize = 28;
        riT.color = new Color(1f, 1f, 1f, 0.7f);
        riT.alignment = TextAnchor.MiddleCenter;
        riT.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        var riRt = riGo.GetComponent<RectTransform>();
        riRt.anchorMin = riRt.anchorMax = new Vector2(1f, 0.5f);
        riRt.anchoredPosition = new Vector2(-30f, 0f);
        riRt.sizeDelta = new Vector2(36f, 36f);

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 34; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Font.CreateDynamicFontFromOSFont("Arial", 34);
        var tShadow = txtGo.AddComponent<Shadow>();
        tShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        tShadow.effectDistance = new Vector2(2f, -2f);
        var tOutline = txtGo.AddComponent<Outline>();
        tOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
        tOutline.effectDistance = new Vector2(1f, -1f);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var bShadow = go.AddComponent<Shadow>();
        bShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        bShadow.effectDistance = new Vector2(4f, -4f);

        return btn;
    }

    // ---- 小ボタン ----

    Button MakeSmallButton(Transform parent, string label, Color bgCol,
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
        t.text = label; t.fontSize = 28; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 28);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(txtGo);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return btn;
    }

    // ---- ヘルパー ----

    void AddShadow(GameObject go)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.6f);
        s.effectDistance = new Vector2(2f, -2f);
    }

    void MakeLine(Transform parent, Color col, float yAnchor, float height)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, yAnchor);
        rt.anchorMax = new Vector2(0.9f, yAnchor);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);
    }

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
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }
}
