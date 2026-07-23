using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ポーズ画面・ホーム設定で共通のUI部品。
/// 「左ラベル＋同じ行の右側にスライダー（丸端トラック・ピンクフィル・白い丸つまみ・%表示なし）」
/// のデザインを1か所で管理する。
/// </summary>
public static class UIWidgets
{
    static Sprite circleSprite;   // 白い正円（つまみ用）
    static Sprite roundBarSprite; // 端が丸いバー（トラック/フィル用・9スライス）

    /// <summary>アンチエイリアス付きの白い正円スプライト（キャッシュ）</summary>
    public static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        float c = (S - 1) / 2f, r = S / 2f - 1.5f;
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
            }
        tex.SetPixels(px);
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        return circleSprite;
    }

    /// <summary>端が丸いバー用スプライト（9スライスで任意の長さに伸ばせる・キャッシュ）</summary>
    public static Sprite RoundBarSprite()
    {
        if (roundBarSprite != null) return roundBarSprite;
        const int S = 32;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        float c = (S - 1) / 2f, r = S / 2f - 1f;
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
            }
        tex.SetPixels(px);
        tex.Apply();
        roundBarSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(15f, 15f, 15f, 15f));
        return roundBarSprite;
    }

    /// <summary>
    /// 音量スライダー行を作る。親の左上(0,1)基準で yTop の高さに、
    /// 左端 labelX からラベル、sliderX から幅 sliderW のスライダーを同じ行に置く。
    /// </summary>
    public static Slider MakeVolumeRow(Transform parent, string label, float yTop,
        float labelX, float sliderX, float sliderW,
        float initialValue, UnityEngine.Events.UnityAction<float> onChanged)
    {
        // ラベル（行の左端）
        var lGo = new GameObject(label + "Label");
        lGo.transform.SetParent(parent, false);
        var lT = lGo.AddComponent<Text>();
        lT.text = label;
        lT.fontSize = 30;
        lT.color = Color.white;
        lT.font = UIFont.Main;
        lT.alignment = TextAnchor.MiddleLeft;
        lT.verticalOverflow = VerticalWrapMode.Overflow;
        lT.horizontalOverflow = HorizontalWrapMode.Overflow;
        lT.raycastTarget = false;
        var lRt = lGo.GetComponent<RectTransform>();
        lRt.anchorMin = lRt.anchorMax = new Vector2(0f, 1f);
        lRt.pivot = new Vector2(0f, 0.5f);
        lRt.anchoredPosition = new Vector2(labelX, yTop);
        lRt.sizeDelta = new Vector2(170f, 44f);

        // スライダー本体（行の右側）
        var sliderGo = new GameObject(label + "Slider");
        sliderGo.transform.SetParent(parent, false);
        var srt = sliderGo.AddComponent<RectTransform>();
        srt.anchorMin = srt.anchorMax = new Vector2(0f, 1f);
        srt.pivot = new Vector2(0f, 0.5f);
        srt.anchoredPosition = new Vector2(sliderX, yTop);
        srt.sizeDelta = new Vector2(sliderW, 44f);
        var slider = sliderGo.AddComponent<Slider>();

        // トラック（濃紫・両端丸）
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = RoundBarSprite();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.227f, 0.165f, 0.322f); // #3a2a52
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(1f, 0.5f);
        bgRt.offsetMin = new Vector2(0f, -7f);
        bgRt.offsetMax = new Vector2(0f, 7f);

        // フィル（ピンク・両端丸）
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.5f);
        faRt.anchorMax = new Vector2(1f, 0.5f);
        faRt.offsetMin = new Vector2(0f, -7f);
        faRt.offsetMax = new Vector2(0f, 7f);
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillArea.transform, false);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.sprite = RoundBarSprite();
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(0.831f, 0.325f, 0.494f); // ピンク #D4537E
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        // つまみ（白い正円）
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGo.transform, false);
        var haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(18f, 0f);
        haRt.offsetMax = new Vector2(-18f, 0f);
        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleArea.transform, false);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.sprite = CircleSprite();
        handleImg.color = Color.white;
        var hRt = handleGo.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(38f, 38f);

        slider.fillRect = fillRt;
        slider.handleRect = hRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }
}
