using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// チュートリアル用の汎用オーバーレイUI。
/// 立ち絵 + 装飾吹き出し + (任意) 選択肢/スキップ/次へボタン。
/// 各シーンで TutorialOverlay.Create(canvasRoot) して使う。
/// </summary>
public class TutorialOverlay : MonoBehaviour
{
    Image bgDim;
    Image charImg;
    Image charShadowImg;
    Text  bubbleText;
    GameObject choicePanel;
    GameObject continuePanel;
    GameObject skipBtnGo;

    /// <summary>キャンバス直下にオーバーレイを生成</summary>
    public static TutorialOverlay Create(Transform canvasRoot)
    {
        var go = new GameObject("TutorialOverlay");
        go.transform.SetParent(canvasRoot, false);
        var overlay = go.AddComponent<TutorialOverlay>();
        overlay.Build();
        return overlay;
    }

    void Build()
    {
        // ルート（全画面）
        var rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // 暗いオーバーレイ（クリックブロックする）
        bgDim = gameObject.AddComponent<Image>();
        bgDim.color = new Color(0.02f, 0f, 0.08f, 0.88f);
        bgDim.raycastTarget = true;

        // ===== 立ち絵 =====
        // 立ち絵の影（足元の楕円ベース）— 未使用だが念のため確保
        var charShadowGo = new GameObject("CharShadow");
        charShadowGo.transform.SetParent(transform, false);
        charShadowImg = charShadowGo.AddComponent<Image>();
        charShadowImg.color = new Color(0f, 0f, 0f, 0f);
        charShadowImg.raycastTarget = false;
        var csRt = charShadowGo.GetComponent<RectTransform>();
        csRt.anchorMin = new Vector2(0.35f, 0.30f);
        csRt.anchorMax = new Vector2(0.65f, 0.33f);
        csRt.offsetMin = csRt.offsetMax = Vector2.zero;

        // 立ち絵本体（画面中央・大きく）
        var charGo = new GameObject("GuideChar");
        charGo.transform.SetParent(transform, false);
        charImg = charGo.AddComponent<Image>();
        charImg.color = new Color(1f, 1f, 1f, 0f);
        charImg.preserveAspect = true;
        charImg.raycastTarget = false;
        var crt = charGo.GetComponent<RectTransform>();
        // 横：画面の左右ほぼいっぱい (10%~90%)
        // 縦：画面の30%~99% （下に吹き出しスペース確保しつつ最大化）
        crt.anchorMin = new Vector2(0.10f, 0.30f);
        crt.anchorMax = new Vector2(0.90f, 0.99f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;

        // ===== 装飾吹き出し（3層構造: 影 + 外枠 + 内枠） =====
        // 影レイヤー
        var bubbleShadow = new GameObject("BubbleShadow");
        bubbleShadow.transform.SetParent(transform, false);
        var bsImg = bubbleShadow.AddComponent<Image>();
        bsImg.color = new Color(0f, 0f, 0f, 0.6f);
        bsImg.raycastTarget = false;
        var bsRt = bubbleShadow.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(0.06f, 0.07f);
        bsRt.anchorMax = new Vector2(0.96f, 0.31f);
        bsRt.offsetMin = new Vector2(6f, -8f);
        bsRt.offsetMax = new Vector2(6f, -8f);

        // 外枠（紫グラデーション風単色）
        var bubbleFrame = new GameObject("BubbleFrame");
        bubbleFrame.transform.SetParent(transform, false);
        var bfImg = bubbleFrame.AddComponent<Image>();
        bfImg.color = new Color(0.55f, 0.25f, 0.65f, 1f);
        bfImg.raycastTarget = false;
        var bfRt = bubbleFrame.GetComponent<RectTransform>();
        bfRt.anchorMin = new Vector2(0.06f, 0.07f);
        bfRt.anchorMax = new Vector2(0.96f, 0.31f);
        bfRt.offsetMin = bfRt.offsetMax = Vector2.zero;

        // 中枠（ピンク細枠）
        var bubbleMid = new GameObject("BubbleMid");
        bubbleMid.transform.SetParent(bubbleFrame.transform, false);
        var bmImg = bubbleMid.AddComponent<Image>();
        bmImg.color = new Color(1f, 0.7f, 0.85f, 1f);
        bmImg.raycastTarget = false;
        var bmRt = bubbleMid.GetComponent<RectTransform>();
        bmRt.anchorMin = Vector2.zero; bmRt.anchorMax = Vector2.one;
        bmRt.offsetMin = new Vector2(8f, 8f); bmRt.offsetMax = new Vector2(-8f, -8f);

        // 内側（白/クリーム背景）
        var bubbleInner = new GameObject("BubbleInner");
        bubbleInner.transform.SetParent(bubbleMid.transform, false);
        var biImg = bubbleInner.AddComponent<Image>();
        biImg.color = new Color(0.99f, 0.97f, 1f, 1f);
        biImg.raycastTarget = false;
        var birt = bubbleInner.GetComponent<RectTransform>();
        birt.anchorMin = Vector2.zero; birt.anchorMax = Vector2.one;
        birt.offsetMin = new Vector2(6f, 6f); birt.offsetMax = new Vector2(-6f, -6f);

        // 角の装飾ハート（4隅）
        AddCornerDecoration(bubbleInner.transform, new Vector2(0.02f, 0.85f), "♡");
        AddCornerDecoration(bubbleInner.transform, new Vector2(0.98f, 0.85f), "♡");
        AddCornerDecoration(bubbleInner.transform, new Vector2(0.02f, 0.15f), "✦");
        AddCornerDecoration(bubbleInner.transform, new Vector2(0.98f, 0.15f), "✦");

        // 吹き出しテキスト（漢字・かな統一フォント使用）
        var txtGo = new GameObject("BubbleText");
        txtGo.transform.SetParent(bubbleInner.transform, false);
        bubbleText = txtGo.AddComponent<Text>();
        bubbleText.text = "";
        bubbleText.fontSize = 40;
        bubbleText.color = new Color(0.15f, 0.05f, 0.3f, 1f);
        bubbleText.alignment = TextAnchor.MiddleCenter;
        // 漢字・かなを同じフォントで揃えるため共通フォントを使用
        bubbleText.font = UIFont.Main; bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
        bubbleText.fontStyle = FontStyle.Bold;
        bubbleText.raycastTarget = false;
        bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
        bubbleText.lineSpacing = 1.15f;
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(48f, 28f); txtRt.offsetMax = new Vector2(-48f, -28f);

        // ===== 選択肢パネル（初期非表示） =====
        choicePanel = new GameObject("ChoicePanel");
        choicePanel.transform.SetParent(transform, false);
        choicePanel.AddComponent<RectTransform>();
        var cpRt = choicePanel.GetComponent<RectTransform>();
        cpRt.anchorMin = new Vector2(0.05f, 0.005f);
        cpRt.anchorMax = new Vector2(0.95f, 0.065f);
        cpRt.offsetMin = cpRt.offsetMax = Vector2.zero;
        choicePanel.SetActive(false);

        // ===== 「次へ」パネル =====
        continuePanel = new GameObject("ContinuePanel");
        continuePanel.transform.SetParent(transform, false);
        continuePanel.AddComponent<RectTransform>();
        var cnRt = continuePanel.GetComponent<RectTransform>();
        cnRt.anchorMin = new Vector2(0.30f, 0.005f);
        cnRt.anchorMax = new Vector2(0.70f, 0.065f);
        cnRt.offsetMin = cnRt.offsetMax = Vector2.zero;
        continuePanel.SetActive(false);

        // ===== スキップボタン（右上、初期非表示） =====
        skipBtnGo = new GameObject("SkipBtn");
        skipBtnGo.transform.SetParent(transform, false);
        skipBtnGo.AddComponent<Image>().color = new Color(0.3f, 0.15f, 0.4f, 0.85f);
        var srt = skipBtnGo.GetComponent<RectTransform>();
        srt.anchorMin = srt.anchorMax = new Vector2(0.91f, 0.965f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(180f, 60f);
        var skipTxtGo = new GameObject("Txt");
        skipTxtGo.transform.SetParent(skipBtnGo.transform, false);
        var skipTxt = skipTxtGo.AddComponent<Text>();
        skipTxt.text = "スキップ▶";
        skipTxt.fontSize = 26;
        skipTxt.color = Color.white;
        skipTxt.alignment = TextAnchor.MiddleCenter;
        skipTxt.font = UIFont.Main; skipTxt.verticalOverflow = VerticalWrapMode.Overflow;
        skipTxt.fontStyle = FontStyle.Bold;
        skipTxt.raycastTarget = false;
        var stOl = skipTxtGo.AddComponent<Outline>();
        stOl.effectColor = new Color(0f, 0f, 0f, 0.8f);
        stOl.effectDistance = new Vector2(2f, -2f);
        var stRt = skipTxtGo.GetComponent<RectTransform>();
        stRt.anchorMin = Vector2.zero; stRt.anchorMax = Vector2.one;
        stRt.offsetMin = stRt.offsetMax = Vector2.zero;
        skipBtnGo.SetActive(false);
    }

    /// <summary>
    /// キャラ立ち絵を設定。
    /// "Rei"等のキャラ名(英)を指定すると、まず Resources/Tutorial/{name} を試し、
    /// なければStageDataのillustSpriteFullにフォールバック。
    /// </summary>
    public void SetCharacterByName(string englishKey, string japaneseName)
    {
        Sprite sprite = null;

        // 1. Resources/Tutorial/{englishKey} を試す
        if (!string.IsNullOrEmpty(englishKey))
        {
            sprite = Resources.Load<Sprite>($"Tutorial/{englishKey}");
        }

        // 2. StageData.illustSpriteFull を試す
        if (sprite == null && !string.IsNullOrEmpty(japaneseName))
        {
            var stages = Resources.LoadAll<StageData>("Stages");
            foreach (var s in stages)
            {
                if (s != null && s.characterName == japaneseName && s.illustSpriteFull != null)
                {
                    sprite = s.illustSpriteFull;
                    break;
                }
            }
        }

        // 3. CharacterData.icon を最終フォールバック
        if (sprite == null && !string.IsNullOrEmpty(japaneseName))
        {
            var charas = Resources.LoadAll<CharacterData>("Characters");
            foreach (var c in charas)
            {
                if (c != null && c.characterName == japaneseName)
                {
                    sprite = c.icon;
                    break;
                }
            }
        }

        SetCharacter(sprite);
    }

    /// <summary>キャラ立ち絵をスプライト直接指定で設定</summary>
    public void SetCharacter(Sprite sprite)
    {
        if (charImg == null) return;
        charImg.sprite = sprite;
        StartCoroutine(FadeIn(charImg, 0.4f));
    }

    /// <summary>吹き出しのテキストを設定</summary>
    public void SetMessage(string text)
    {
        if (bubbleText != null) bubbleText.text = text;
    }

    /// <summary>吹き出しのテキスト配置を変更（左寄せ・中央など）</summary>
    public void SetMessageAlignment(TextAnchor anchor)
    {
        if (bubbleText != null) bubbleText.alignment = anchor;
    }

    /// <summary>吹き出しのフォントサイズを変更（既定は40）</summary>
    public void SetMessageFontSize(int size)
    {
        if (bubbleText != null) bubbleText.fontSize = size;
    }

    /// <summary>
    /// ターゲット UI 要素の実位置からスポットライト＋枠を表示（アスペクト比非依存）。
    /// ScreenSpaceOverlay では GetWorldCorners がスクリーンpxを返すため、画面サイズで割って
    /// 正規化アンカーに変換する。端末の縦横比に関係なく枠がぴったり合う。
    /// 戻り値は中心アンカー（矢印配置などに利用）。
    /// </summary>
    public Vector2 HighlightTarget(RectTransform target, float padPx, Color frameColor)
    {
        if (target == null) return new Vector2(0.5f, 0.5f);
        var corners = new Vector3[4];
        target.GetWorldCorners(corners); // 0=左下, 2=右上（スクリーンpx）
        float w = Screen.width, h = Screen.height;
        if (w <= 0f || h <= 0f) return new Vector2(0.5f, 0.5f);
        float padX = padPx / w, padY = padPx / h;
        Vector2 min = new Vector2(
            Mathf.Clamp01(corners[0].x / w - padX),
            Mathf.Clamp01(corners[0].y / h - padY));
        Vector2 max = new Vector2(
            Mathf.Clamp01(corners[2].x / w + padX),
            Mathf.Clamp01(corners[2].y / h + padY));
        ShowSpotlight(min, max);
        AddHighlightFrame(min, max, frameColor, 10f);
        return new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
    }

    /// <summary>「はい / いいえ」選択肢を表示</summary>
    public void ShowYesNo(string yesLabel, string noLabel, Action<bool> onChoice)
    {
        if (choicePanel == null) return;
        for (int i = choicePanel.transform.childCount - 1; i >= 0; i--)
            Destroy(choicePanel.transform.GetChild(i).gameObject);
        choicePanel.SetActive(true);

        MakeChoiceButton(choicePanel.transform, yesLabel,
            new Color(0.2f, 0.6f, 0.3f), new Vector2(0.30f, 0.5f),
            () => onChoice?.Invoke(true));
        MakeChoiceButton(choicePanel.transform, noLabel,
            new Color(0.5f, 0.2f, 0.2f), new Vector2(0.70f, 0.5f),
            () => onChoice?.Invoke(false));
    }

    /// <summary>「次へ」だけ表示</summary>
    public void ShowContinue(string label, Action onClick)
    {
        if (continuePanel == null) return;
        for (int i = continuePanel.transform.childCount - 1; i >= 0; i--)
            Destroy(continuePanel.transform.GetChild(i).gameObject);
        continuePanel.SetActive(true);

        MakeChoiceButton(continuePanel.transform, label,
            new Color(0.2f, 0.5f, 1f), new Vector2(0.5f, 0.5f), onClick);
    }

    /// <summary>スキップボタンの表示と動作を設定</summary>
    public void ShowSkipButton(Action onSkip)
    {
        if (skipBtnGo == null) return;
        skipBtnGo.SetActive(true);
        var btn = skipBtnGo.GetComponent<Button>();
        if (btn == null) btn = skipBtnGo.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onSkip?.Invoke());
    }

    /// <summary>選択肢/次へを非表示</summary>
    public void HideButtons()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        if (continuePanel != null) continuePanel.SetActive(false);
    }

    /// <summary>オーバーレイ全体を破棄</summary>
    public void Close()
    {
        if (this == null || gameObject == null) return;
        Destroy(gameObject);
    }

    // ============================================================
    // 拡張API：ガイド系オーバーレイ用ヘルパー
    // ============================================================

    /// <summary>暗いオーバーレイを無効化（背後のUIをクリック可能に）</summary>
    public void HideDim()
    {
        if (bgDim != null)
        {
            bgDim.color = new Color(0f, 0f, 0f, 0f);
            bgDim.raycastTarget = false;
        }
    }

    /// <summary>
    /// 指定範囲だけ「穴」を空けて、その他の領域は暗くしてクリック禁止にする。
    /// 4つの暗矩形（上/下/左/右）を最背面に配置して穴を作る。
    /// anchorMin / anchorMax は穴の範囲（canvas 0-1）。
    /// </summary>
    public void ShowSpotlight(Vector2 anchorMin, Vector2 anchorMax)
    {
        // メインdim は無効化（4枚で代替するため）
        if (bgDim != null)
        {
            bgDim.color = new Color(0f, 0f, 0f, 0f);
            bgDim.raycastTarget = false;
        }

        // 4方向のdim矩形を生成
        MakeDimRect("DimTop",    new Vector2(0f,          anchorMax.y), new Vector2(1f, 1f));
        MakeDimRect("DimBottom", new Vector2(0f,          0f),          new Vector2(1f, anchorMin.y));
        MakeDimRect("DimLeft",   new Vector2(0f,          anchorMin.y), new Vector2(anchorMin.x, anchorMax.y));
        MakeDimRect("DimRight",  new Vector2(anchorMax.x, anchorMin.y), new Vector2(1f, anchorMax.y));
    }

    void MakeDimRect(string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        // 範囲が0以下なら矩形を作らない
        if (anchorMin.x >= anchorMax.x - 0.001f || anchorMin.y >= anchorMax.y - 0.001f)
            return;
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.78f);
        img.raycastTarget = true; // 穴の外はクリック遮断
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        // 最背面に配置（吹き出し・矢印・スキップボタンは前面に保つ）
        go.transform.SetAsFirstSibling();
    }

    /// <summary>キャラ立ち絵を非表示にする（小さなヒント用）</summary>
    public void HideCharacter()
    {
        if (charImg != null) charImg.gameObject.SetActive(false);
        if (charShadowImg != null) charShadowImg.gameObject.SetActive(false);
    }

    /// <summary>吹き出しのanchor位置を上書き（既存装飾はそのまま付随）</summary>
    public void SetBubbleAnchor(Vector2 anchorMin, Vector2 anchorMax)
    {
        // BubbleShadow と BubbleFrame の両方を移動
        SetChildAnchor("BubbleShadow", anchorMin, anchorMax, new Vector2(6f, -8f), new Vector2(6f, -8f));
        SetChildAnchor("BubbleFrame", anchorMin, anchorMax, Vector2.zero, Vector2.zero);
    }

    void SetChildAnchor(string childName, Vector2 anchorMin, Vector2 anchorMax,
                        Vector2 offsetMin, Vector2 offsetMax)
    {
        var child = transform.Find(childName);
        if (child == null) return;
        var rt = child.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    /// <summary>
    /// 指定位置に脈動する矢印を表示（▼▲◀▶ いずれか）
    /// </summary>
    public void AddArrowAt(Vector2 anchor, string symbol = "▼", Color? color = null)
    {
        var go = new GameObject("GuideArrow");
        go.transform.SetParent(transform, false);
        var t = go.AddComponent<Text>();
        t.text = symbol;
        t.fontSize = 140;
        t.color = color ?? new Color(1f, 0.3f, 0.45f, 1f);
        t.alignment = TextAnchor.MiddleCenter;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.95f);
        sh.effectDistance = new Vector2(3f, -3f);
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0.5f, 0f, 0.1f, 0.95f);
        ol.effectDistance = new Vector2(3f, -3f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(160f, 160f);

        StartCoroutine(PulseArrow(rt));
    }

    System.Collections.IEnumerator PulseArrow(RectTransform rt)
    {
        Vector2 basePos = rt.anchoredPosition;
        while (rt != null)
        {
            float t = Time.unscaledTime * 4f;
            float offset = Mathf.Sin(t) * 18f;
            rt.anchoredPosition = basePos + new Vector2(0f, offset);
            yield return null;
        }
    }

    /// <summary>
    /// 指定範囲を囲む脈動フレームを追加（スポットライト強調用）。
    /// 4辺をImage矩形で構成し、アルファとスケールを脈動させて目立たせる。
    /// </summary>
    public void AddHighlightFrame(Vector2 anchorMin, Vector2 anchorMax,
                                   Color color, float thicknessPx = 10f)
    {
        var frame = new GameObject("HighlightFrame");
        frame.transform.SetParent(transform, false);
        var frt = frame.AddComponent<RectTransform>();
        frt.anchorMin = anchorMin;
        frt.anchorMax = anchorMax;
        frt.offsetMin = frt.offsetMax = Vector2.zero;

        // 4辺のImageを生成
        var imgs = new System.Collections.Generic.List<Image>();
        imgs.Add(MakeFrameEdge(frame.transform, "EdgeTop",    color, thicknessPx, 0));
        imgs.Add(MakeFrameEdge(frame.transform, "EdgeBottom", color, thicknessPx, 1));
        imgs.Add(MakeFrameEdge(frame.transform, "EdgeLeft",   color, thicknessPx, 2));
        imgs.Add(MakeFrameEdge(frame.transform, "EdgeRight",  color, thicknessPx, 3));

        StartCoroutine(PulseHighlightFrame(frame.GetComponent<RectTransform>(), imgs, color));
    }

    Image MakeFrameEdge(Transform parent, string name, Color color, float thickness, int side)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();

        // 4辺: 0=top, 1=bottom, 2=left, 3=right
        switch (side)
        {
            case 0: // Top
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, thickness);
                break;
            case 1: // Bottom
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, thickness);
                break;
            case 2: // Left
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(thickness, 0f);
                break;
            case 3: // Right
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(thickness, 0f);
                break;
        }
        return img;
    }

    System.Collections.IEnumerator PulseHighlightFrame(RectTransform rt,
        System.Collections.Generic.List<Image> imgs, Color baseColor)
    {
        Vector3 baseScale = Vector3.one;
        while (rt != null)
        {
            float t = Time.unscaledTime * 3f;
            float k = (Mathf.Sin(t) + 1f) * 0.5f; // 0〜1
            // 軽くスケール変化（5%増減）+ 明度変化
            float scale = 1f + (k - 0.5f) * 0.08f;
            rt.localScale = baseScale * scale;
            float alpha = Mathf.Lerp(0.55f, 1f, k);
            for (int i = 0; i < imgs.Count; i++)
            {
                if (imgs[i] != null)
                    imgs[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }
            yield return null;
        }
    }

    void AddCornerDecoration(Transform parent, Vector2 anchor, string symbol)
    {
        var go = new GameObject($"Deco_{symbol}");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = symbol;
        t.fontSize = 24;
        t.color = new Color(0.85f, 0.4f, 0.6f, 0.9f);
        t.alignment = TextAnchor.MiddleCenter;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(40f, 40f);
    }

    void MakeChoiceButton(Transform parent, string label, Color baseCol,
        Vector2 anchor, Action onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(Mathf.Clamp01(baseCol.r * 1.5f),
                              Mathf.Clamp01(baseCol.g * 1.5f),
                              Mathf.Clamp01(baseCol.b * 1.5f), 0.85f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(320f, 100f);

        var inner = new GameObject("Inner");
        inner.transform.SetParent(go.transform, false);
        inner.AddComponent<Image>().color = baseCol;
        var iRt = inner.GetComponent<RectTransform>();
        iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
        iRt.offsetMin = new Vector2(4f, 4f); iRt.offsetMax = new Vector2(-4f, -4f);

        var t = new GameObject("Txt").AddComponent<Text>();
        t.transform.SetParent(go.transform, false);
        t.text = label;
        t.fontSize = 40;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        var sh = t.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sh.effectDistance = new Vector2(2f, -2f);
        var ol = t.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.85f);
        ol.effectDistance = new Vector2(2f, -2f);
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    System.Collections.IEnumerator FadeIn(Image img, float dur)
    {
        float t = 0f;
        while (t < dur && img != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            img.color = new Color(1f, 1f, 1f, k);
            yield return null;
        }
    }
}
