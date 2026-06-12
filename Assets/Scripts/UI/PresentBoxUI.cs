using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// プレゼントボックス画面
/// </summary>
public class PresentBoxUI : MonoBehaviour
{
    Transform canvasRoot;
    Transform contentRoot;
    RectTransform contentRT;
    Text countText;
    List<RectTransform> particles = new List<RectTransform>();

    // チュートリアル参照
    TutorialOverlay currentTutorialOverlay;

    void Start()
    {
        BuildUI();

        // 段階7 補足: PresentBox 段階なら「すべて受け取る」誘導を起動
        if (TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStep == TutorialManager.Step.PresentBox)
        {
            StartCoroutine(ShowPresentTutorialPhase1AfterDelay(0.6f));
        }
    }

    // ============================================================
    // 段階7 補足: プレゼントボックス画面の操作誘導
    //   Phase 1: 「すべて受け取る」ボタン強調
    //   Phase 2: 「ホーム」ボタン強調（受取後）
    // ============================================================

    System.Collections.IEnumerator ShowPresentTutorialPhase1AfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPresentTutorialPhase1();
    }

    /// <summary>
    /// Phase 1: 「すべて受け取る」ボタンをスポットライト
    /// ボタン位置: anchor (0.5, 0.84), sizeDelta (360, 70)
    ///   x: 0.333〜0.667, y: 0.822〜0.858
    /// </summary>
    void ShowPresentTutorialPhase1()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        Vector2 allMin = new Vector2(0.32f, 0.815f);
        Vector2 allMax = new Vector2(0.68f, 0.865f);

        overlay.ShowSpotlight(allMin, allMax);
        overlay.AddHighlightFrame(allMin, allMax,
            new Color(1f, 0.9f, 0.2f), 10f);

        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.45f),
            new Vector2(0.95f, 0.75f));
        overlay.SetMessage(
            "プレゼントを全部受け取りなさい！\n" +
            "『すべて受け取る』ボタンをタップよ");

        // 矢印をボタンの上に配置
        overlay.AddArrowAt(new Vector2(0.5f, 0.91f), "▼");

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            currentTutorialOverlay = null;
        });

        currentTutorialOverlay = overlay;
    }

    /// <summary>
    /// Phase 2: ホームボタンをスポットライト（受け取り後）
    /// ボタン位置: anchor (0.5, 0.05), sizeDelta (360, 70)
    ///   x: 0.333〜0.667, y: 0.0318〜0.0682
    /// </summary>
    void ShowPresentTutorialPhase2()
    {
        if (canvasRoot == null) return;

        var overlay = TutorialOverlay.Create(canvasRoot);
        overlay.HideCharacter();

        Vector2 homeMin = new Vector2(0.32f, 0.025f);
        Vector2 homeMax = new Vector2(0.68f, 0.075f);

        overlay.ShowSpotlight(homeMin, homeMax);
        overlay.AddHighlightFrame(homeMin, homeMax,
            new Color(1f, 0.9f, 0.2f), 10f);

        overlay.SetBubbleAnchor(
            new Vector2(0.05f, 0.30f),
            new Vector2(0.95f, 0.60f));
        overlay.SetMessage(
            "受け取れたわね♪\n" +
            "ホームに戻りなさい");

        // 矢印をホームボタンの上に配置
        overlay.AddArrowAt(new Vector2(0.5f, 0.13f), "▼");

        overlay.ShowSkipButton(() =>
        {
            TutorialManager.Instance.SkipAll();
            overlay.Close();
            currentTutorialOverlay = null;
        });

        currentTutorialOverlay = overlay;
    }

    void Update()
    {
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
        // 期限切れを掃除
        PresentBoxManager.CleanUp();

        var cGo = new GameObject("PresentCanvas");
        var c = cGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0f;
        cGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        canvasRoot = cGo.transform;

        // 背景
        MakeBg(canvasRoot, new Color(0.03f, 0.02f, 0.1f));

        // 上部バー
        MakeBar(canvasRoot, new Vector2(0f, 0.97f), Vector2.one, new Color(0.6f, 0.2f, 0.8f, 0.5f));
        // 下部バー
        MakeBar(canvasRoot, Vector2.zero, new Vector2(1f, 0.015f), new Color(0.6f, 0.2f, 0.8f, 0.5f));

        // 光の粒
        CreateParticles(canvasRoot, 12);

        // タイトル
        var title = MakeText(canvasRoot, "🎁 プレゼント", 48, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f, 0.93f), new Vector2(600f, 65f));
        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.8f);
        shadow.effectDistance = new Vector2(3f, -3f);
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.2f, 0.4f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        // 区切りライン
        MakeLine(canvasRoot, new Color(1f, 0.85f, 0.3f, 0.6f),
            new Vector2(0.5f, 0.91f), new Vector2(500f, 3f));

        // 件数表示
        countText = MakeText(canvasRoot, "", 28, new Color(0.7f, 0.7f, 0.9f),
            new Vector2(0.5f, 0.88f), new Vector2(500f, 36f));

        // すべて受け取るボタン
        MakeStyledButton(canvasRoot, "すべて受け取る",
            new Color(0.15f, 0.5f, 0.3f), new Color(0.3f, 0.8f, 0.5f, 0.6f),
            new Vector2(0.5f, 0.84f), new Vector2(360f, 70f),
            OnReceiveAll);

        // スクロールビュー
        BuildScrollView(canvasRoot);

        // ホームボタン
        MakeStyledButton(canvasRoot, "ホーム",
            new Color(0.25f, 0.15f, 0.45f), new Color(0.5f, 0.3f, 0.9f, 0.35f),
            new Vector2(0.5f, 0.05f), new Vector2(360f, 70f),
            () =>
            {
                // 段階7 → 段階8: PresentBox 段階でホームに戻ったら Gacha 段階へ
                if (TutorialManager.Instance != null
                    && TutorialManager.Instance.CurrentStep == TutorialManager.Step.PresentBox)
                {
                    TutorialManager.Instance.SetStep(TutorialManager.Step.Gacha);
                    Debug.Log("[Tutorial] Gacha 段階へ進行（段階8 未実装、ここで停止）");
                }
                SceneManager.LoadScene("HomeScene");
            });

        RefreshList();
    }

    void BuildScrollView(Transform parent)
    {
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(parent, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.04f, 0.12f);
        scrollRT.anchorMax = new Vector2(0.96f, 0.80f);
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;
        var sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.scrollSensitivity = 40f;

        var vpGo = new GameObject("Viewport");
        vpGo.transform.SetParent(scrollGo.transform, false);
        vpGo.AddComponent<Image>().color = Color.white;
        vpGo.AddComponent<Mask>().showMaskGraphic = false;
        var vpRT = vpGo.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRoot = contentGo.transform;

        sr.content = contentRT;
        sr.viewport = vpRT;
    }

    void RefreshList()
    {
        // コンテンツクリア
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        var presents = PresentBoxManager.GetPendingPresents();
        countText.text = $"受取可能: {presents.Count}件";

        const float rowH = 190f;
        contentRT.sizeDelta = new Vector2(0f, Mathf.Max(presents.Count, 1) * rowH + 10f);

        if (presents.Count == 0)
        {
            MakeText(contentRoot, "プレゼントはありません", 30, new Color(0.5f, 0.5f, 0.6f),
                new Vector2(0.5f, 0.5f), new Vector2(500f, 50f));
            return;
        }

        for (int i = 0; i < presents.Count; i++)
        {
            BuildPresentRow(presents[i], i, rowH);
        }
    }

    void BuildPresentRow(PresentBoxManager.Present present, int index, float rowH)
    {
        bool isOrb = present.type == PresentBoxManager.PresentType.Orb;
        Color typeColor = isOrb
            ? new Color(0.3f, 0.7f, 1f, 0.5f)
            : new Color(1f, 0.5f, 0.8f, 0.5f);

        // 外枠
        var rowGo = new GameObject($"Present_{index}");
        rowGo.transform.SetParent(contentRoot, false);
        rowGo.AddComponent<Image>().color = typeColor;
        var rowRT = rowGo.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, -index * rowH - 5f);
        rowRT.sizeDelta = new Vector2(-10f, rowH - 6f);

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(rowGo.transform, false);
        innerGo.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.95f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        // 種類アイコン
        string icon = isOrb ? "◆" : "♥";
        MakeText(rowGo.transform, icon, 48,
            isOrb ? new Color(0.4f, 0.9f, 1f) : new Color(1f, 0.5f, 0.8f),
            new Vector2(0.08f, 0.72f), new Vector2(70f, 60f));

        // 内容テキスト
        string contentStr = isOrb
            ? $"オーブ ×{present.orbAmount}"
            : $"キャラ: {present.characterName}";
        var contentTxt = MakeText(rowGo.transform, contentStr, 38, Color.white,
            new Vector2(0.4f, 0.75f), new Vector2(500f, 50f));
        contentTxt.alignment = TextAnchor.MiddleLeft;

        // 備考テキスト
        var msgTxt = MakeText(rowGo.transform, present.message, 32,
            new Color(0.7f, 0.7f, 0.85f),
            new Vector2(0.4f, 0.47f), new Vector2(500f, 42f));
        msgTxt.alignment = TextAnchor.MiddleLeft;

        // 期限テキスト
        string expireStr = "期限: " + present.expireDate;
        if (System.DateTime.TryParse(present.expireDate, out System.DateTime expire))
        {
            var remaining = expire - System.DateTime.Now;
            if (remaining.TotalDays < 1)
                expireStr = $"期限: あと{(int)remaining.TotalHours}時間";
            else if (remaining.TotalDays < 3)
                expireStr = $"期限: あと{(int)remaining.TotalDays}日{remaining.Hours}時間";
            else
                expireStr = $"期限: {expire:M/d} まで";
        }
        Color expireColor = new Color(0.6f, 0.6f, 0.5f);
        if (System.DateTime.TryParse(present.expireDate, out System.DateTime exp2))
        {
            if ((exp2 - System.DateTime.Now).TotalDays < 1)
                expireColor = new Color(1f, 0.4f, 0.3f); // 残り1日未満は赤
        }
        var expTxt = MakeText(rowGo.transform, expireStr, 28, expireColor,
            new Vector2(0.4f, 0.2f), new Vector2(500f, 36f));
        expTxt.alignment = TextAnchor.MiddleLeft;

        // 受け取るボタン
        string pid = present.id;
        MakeRowButton(rowGo.transform, "受取",
            new Color(0.2f, 0.55f, 0.3f), new Color(0.3f, 0.8f, 0.5f, 0.6f),
            new Vector2(0.88f, 0.5f), new Vector2(130f, 70f),
            () => {
                PresentBoxManager.Receive(pid);
                RefreshList();
            });
    }

    void OnReceiveAll()
    {
        int count = PresentBoxManager.ReceiveAll();
        if (count > 0)
        {
            RefreshList();

            // 段階7 補足: チュートリアル中ならフェーズ1閉じてフェーズ2へ
            bool inTutorial = TutorialManager.Instance != null
                           && TutorialManager.Instance.CurrentStep == TutorialManager.Step.PresentBox
                           && currentTutorialOverlay != null;
            if (inTutorial)
            {
                currentTutorialOverlay.Close();
                currentTutorialOverlay = null;
                // Notice の OK 押下後にホーム誘導フェーズ2を表示
                ShowNotice($"{count}件のプレゼントを受け取りました！", () =>
                {
                    ShowPresentTutorialPhase2();
                });
            }
            else
            {
                ShowNotice($"{count}件のプレゼントを受け取りました！");
            }
        }
    }

    void ShowNotice(string message, System.Action onClose = null)
    {
        var overlay = new GameObject("NoticeOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);
        dialog.AddComponent<Image>().color = new Color(0.4f, 0.15f, 0.7f, 0.5f);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero;
        drt.sizeDelta = new Vector2(700f, 300f);

        var dInner = new GameObject("Inner");
        dInner.transform.SetParent(dialog.transform, false);
        dInner.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.15f, 0.97f);
        var diRt = dInner.GetComponent<RectTransform>();
        diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
        diRt.offsetMin = new Vector2(4f, 4f); diRt.offsetMax = new Vector2(-4f, -4f);

        MakeText(dialog.transform, message, 34, new Color(0.4f, 1f, 0.5f),
            new Vector2(0.5f, 0.65f), new Vector2(650f, 80f));

        var okGo = new GameObject("OKBtn");
        okGo.transform.SetParent(dialog.transform, false);
        okGo.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f, 0.9f);
        var okBtn = okGo.AddComponent<Button>();
        var okRt = okGo.GetComponent<RectTransform>();
        okRt.anchorMin = okRt.anchorMax = new Vector2(0.5f, 0.2f);
        okRt.anchoredPosition = Vector2.zero;
        okRt.sizeDelta = new Vector2(240f, 70f);
        var okTxt = MakeText(okGo.transform, "OK", 30, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(220f, 60f));
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        if (cherry != null) okTxt.font = cherry;
        okTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        okTxt.verticalOverflow = VerticalWrapMode.Overflow;
        okBtn.onClick.AddListener(() => { Destroy(overlay); onClose?.Invoke(); });
    }

    // ---- UI ヘルパー ----

    void MakeBg(Transform parent, Color col)
    {
        var go = new GameObject("BG");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void MakeBar(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color col)
    {
        var go = new GameObject("Bar");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void MakeLine(Transform parent, Color col, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        go.GetComponent<Image>().raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
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

    void MakeStyledButton(Transform parent, string label, Color baseCol, Color highlightCol,
        Vector2 anchor, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
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
        innerGo.AddComponent<Image>().color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.92f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f); innerRt.offsetMax = new Vector2(-3f, -3f);

        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 32; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 32);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(txtGo);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        go.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.5f);
    }

    void MakeRowButton(Transform parent, string label, Color baseCol, Color highlightCol,
        Vector2 anchor, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
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
        innerGo.AddComponent<Image>().color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.92f);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2f, 2f); innerRt.offsetMax = new Vector2(-2f, -2f);

        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 26; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : Font.CreateDynamicFontFromOSFont("Arial", 26);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    void AddShadow(GameObject go)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.6f);
        s.effectDistance = new Vector2(2f, -2f);
    }

    void CreateParticles(Transform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var pGo = new GameObject("Particle");
            pGo.transform.SetParent(parent, false);
            var img = pGo.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(Random.Range(0.85f, 1f), Random.Range(0.7f, 0.95f),
                Random.Range(0.8f, 1f), Random.Range(0.15f, 0.4f));
            var prt = pGo.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(Random.Range(-540f, 540f), Random.Range(-960f, 960f));
            float size = Random.Range(4f, 12f);
            prt.sizeDelta = new Vector2(size, size);
            particles.Add(prt);
        }
    }
}
