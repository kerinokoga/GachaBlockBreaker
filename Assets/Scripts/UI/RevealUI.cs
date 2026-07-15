using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// GameScene 右半分に表示する美少女解放オーバーレイ
/// State 0:非表示 / 1:シルエット(30%) / 2:カラー(60%) / 3:フル(100%)
/// illustSprite30/60/Full が設定されている場合はスプライト表示、未設定時はカラーで代用
/// </summary>
public class RevealUI : MonoBehaviour
{
    private Image colorPanel;    // カラー背景（スプライト未設定時に使用）
    private Image spriteImage;   // イラストスプライト
    private Image darkOverlay;   // 暗いオーバーレイ（徐々に薄くなる）
    private Image flashImage;    // クリア時の白フラッシュ
    private Text  charNameText;

    private Color c30, c60, cFull;
    private Sprite sprite0, sprite30, sprite60, spriteFull;
    private bool useSprites = false;
    private int currentState = 0;

    void Start() => BuildUI();

    void BuildUI()
    {
        var cGo = new GameObject("RevealCanvas");
        var canvas = cGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = -1;  // ゲームスプライト(0)より背後に描画
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0.0f;

        Transform root = cGo.transform;

        // カラーパネル（スプライト未設定時の背景）
        var panelGo = new GameObject("ColorPanel");
        panelGo.transform.SetParent(root, false);
        colorPanel = panelGo.AddComponent<Image>();
        colorPanel.color = new Color(0.1f, 0.05f, 0.15f, 0f);
        SetRectHalf(panelGo.GetComponent<RectTransform>());

        // スプライトイメージ（colorPanel の前面）
        var spriteGo = new GameObject("IllustSprite");
        spriteGo.transform.SetParent(root, false);
        spriteImage = spriteGo.AddComponent<Image>();
        spriteImage.color = Color.white;
        spriteImage.preserveAspect = true;
        SetRectHalf(spriteGo.GetComponent<RectTransform>());
        spriteGo.SetActive(false); // 初期は非表示

        // 黒オーバーレイ（初期ほぼ全黒）
        var overlayGo = new GameObject("DarkOverlay");
        overlayGo.transform.SetParent(root, false);
        darkOverlay = overlayGo.AddComponent<Image>();
        darkOverlay.color = new Color(0f, 0f, 0f, 0.95f);
        SetRectHalf(overlayGo.GetComponent<RectTransform>());

        // キャラ名テキスト
        var txtGo = new GameObject("CharName");
        txtGo.transform.SetParent(root, false);
        charNameText = txtGo.AddComponent<Text>();
        charNameText.text = "";
        charNameText.fontSize = 36;
        charNameText.color = new Color(1f, 1f, 1f, 0f);
        charNameText.alignment = TextAnchor.UpperCenter;
        charNameText.font = UIFont.Main; charNameText.verticalOverflow = VerticalWrapMode.Overflow;
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.75f, 0.88f);
        trt.anchorMax = new Vector2(1.0f, 0.88f);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(0f, 50f);

        // 白フラッシュ（全画面、初期は透明）
        var flashGo = new GameObject("Flash");
        flashGo.transform.SetParent(root, false);
        flashImage = flashGo.AddComponent<Image>();
        flashImage.color = Color.clear;
        flashImage.raycastTarget = false;
        var frt = flashGo.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
    }

    // 全画面レイアウト（ブロックの背面に表示、上部を空けてボスアイコンとの被りを防ぐ）
    void SetRectHalf(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(1f, 0.75f); // 上部25%を空ける
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// ステージデータをセット（GameManager から呼ばれる）
    /// </summary>
    public void SetStageData(Color color30, Color color60, Color colorFull,
                             string charName,
                             Sprite spr0 = null, Sprite spr30 = null,
                             Sprite spr60 = null, Sprite sprFull = null)
    {
        c30 = color30; c60 = color60; cFull = colorFull;
        sprite0 = spr0; sprite30 = spr30; sprite60 = spr60; spriteFull = sprFull;
        useSprites = (spr0 != null || spr30 != null || spr60 != null || sprFull != null);

        if (charNameText != null) charNameText.text = charName;

        if (useSprites)
        {
            spriteImage.gameObject.SetActive(true);
            spriteImage.color = Color.white;
            // 0% イラストがあれば初期表示
            if (sprite0 != null) spriteImage.sprite = sprite0;
            colorPanel.gameObject.SetActive(false);
        }
        else
        {
            spriteImage.gameObject.SetActive(false);
            colorPanel.gameObject.SetActive(true);
        }

        // 裏ステージ突入などで再セットされた場合に備えて状態を初期化
        currentState = 0;
        if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, 0.95f);
        if (charNameText != null) charNameText.color = new Color(1f, 1f, 1f, 0f);
    }

    public void AdvanceToState(int state)
    {
        // state=0 指定は明示的なリセット要求として扱う
        if (state == 0)
        {
            StopAllCoroutines();
            currentState = 0;
            if (darkOverlay != null) darkOverlay.color = new Color(0f, 0f, 0f, 0.95f);
            if (charNameText != null) charNameText.color = new Color(1f, 1f, 1f, 0f);
            if (useSprites && spriteImage != null && sprite0 != null)
                spriteImage.sprite = sprite0;
            return;
        }
        if (state <= currentState) return;
        currentState = state;
        StartCoroutine(FadeToState(state));
        if (state == 3) StartCoroutine(FlashEffect());
    }

    IEnumerator FadeToState(int state)
    {
        Color targetPanelColor;
        float targetOverlayAlpha;

        switch (state)
        {
            case 1: targetPanelColor = c30;  targetOverlayAlpha = 0.60f; break;
            case 2: targetPanelColor = c60;  targetOverlayAlpha = 0.15f; break;
            case 3: targetPanelColor = cFull; targetOverlayAlpha = 0f;   break;
            default: yield break;
        }

        // スプライト切り替え
        if (useSprites)
        {
            Sprite targetSprite = null;
            switch (state)
            {
                case 1: targetSprite = sprite30; break;
                case 2: targetSprite = sprite60; break;
                case 3: targetSprite = spriteFull; break;
            }
            if (targetSprite != null)
                spriteImage.sprite = targetSprite;
        }

        float elapsed  = 0f;
        float duration = 0.4f;
        Color startPanel   = colorPanel.color;
        Color startOverlay = darkOverlay.color;
        Color startTxt     = charNameText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // カラーパネルは sprite なし時のみ変化
            if (!useSprites)
                colorPanel.color = Color.Lerp(startPanel, targetPanelColor, t);

            darkOverlay.color = Color.Lerp(startOverlay,
                new Color(0f, 0f, 0f, targetOverlayAlpha), t);
            charNameText.color = Color.Lerp(startTxt,
                new Color(1f, 1f, 1f, state >= 1 ? 1f : 0f), t);
            yield return null;
        }

        if (!useSprites)
            colorPanel.color = targetPanelColor;

        darkOverlay.color = new Color(0f, 0f, 0f, targetOverlayAlpha);
        if (state >= 1) charNameText.color = Color.white;
    }

    IEnumerator FlashEffect()
    {
        yield return new WaitForSeconds(0.1f);
        flashImage.color = new Color(1f, 1f, 1f, 0.75f);
        yield return new WaitForSeconds(0.08f);

        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.75f, 0f, t / 0.35f));
            yield return null;
        }
        flashImage.color = Color.clear;
    }
}
