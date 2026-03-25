using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// GameScene 右半分に表示する美少女解放オーバーレイ
/// State 0:非表示 / 1:シルエット(30%) / 2:カラー(60%) / 3:フル(100%)
/// </summary>
public class RevealUI : MonoBehaviour
{
    private Image colorPanel;    // 解放カラー
    private Image darkOverlay;   // 暗いオーバーレイ（徐々に薄くなる）
    private Image flashImage;    // クリア時の白フラッシュ
    private Text  charNameText;

    private Color c30, c60, cFull;
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
        cs.matchWidthOrHeight = 0.5f;
        // GraphicRaycaster は不要（RevealUI は入力を受け取らない）

        Transform root = cGo.transform;

        // カラーパネル（右半分・縦80%）
        var panelGo = new GameObject("ColorPanel");
        panelGo.transform.SetParent(root, false);
        colorPanel = panelGo.AddComponent<Image>();
        colorPanel.color = new Color(0.1f, 0.05f, 0.15f);
        var rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.1f);
        rt.anchorMax = new Vector2(1.0f, 0.9f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // 黒オーバーレイ（初期ほぼ全黒）
        var overlayGo = new GameObject("DarkOverlay");
        overlayGo.transform.SetParent(root, false);
        darkOverlay = overlayGo.AddComponent<Image>();
        darkOverlay.color = new Color(0f, 0f, 0f, 0.95f);
        var ort = overlayGo.GetComponent<RectTransform>();
        ort.anchorMin = new Vector2(0.5f, 0.1f);
        ort.anchorMax = new Vector2(1.0f, 0.9f);
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // キャラ名テキスト
        var txtGo = new GameObject("CharName");
        txtGo.transform.SetParent(root, false);
        charNameText = txtGo.AddComponent<Text>();
        charNameText.text = "";
        charNameText.fontSize = 36;
        charNameText.color = new Color(1f, 1f, 1f, 0f);
        charNameText.alignment = TextAnchor.UpperCenter;
        charNameText.font = Font.CreateDynamicFontFromOSFont("Arial", 36);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0.88f);
        trt.anchorMax = new Vector2(1.0f, 0.88f);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(0f, 50f);

        // 白フラッシュ（全画面、初期は透明）
        var flashGo = new GameObject("Flash");
        flashGo.transform.SetParent(root, false);
        flashImage = flashGo.AddComponent<Image>();
        flashImage.color = Color.clear;
        var frt = flashGo.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
    }

    public void SetStageData(Color color30, Color color60, Color colorFull, string charName)
    {
        c30 = color30; c60 = color60; cFull = colorFull;
        if (charNameText != null) charNameText.text = charName;
    }

    public void AdvanceToState(int state)
    {
        if (state <= currentState) return;
        currentState = state;
        StartCoroutine(FadeToState(state));
        if (state == 3) StartCoroutine(FlashEffect());
    }

    IEnumerator FadeToState(int state)
    {
        Color targetColor;
        float targetOverlayAlpha;

        switch (state)
        {
            case 1: targetColor = c30;  targetOverlayAlpha = 0.6f;  break;
            case 2: targetColor = c60;  targetOverlayAlpha = 0.15f; break;
            case 3: targetColor = cFull; targetOverlayAlpha = 0f;   break;
            default: yield break;
        }

        float elapsed = 0f;
        float duration = 0.4f;
        Color startColor   = colorPanel.color;
        Color startOverlay = darkOverlay.color;
        Color startTxt     = charNameText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            colorPanel.color  = Color.Lerp(startColor, targetColor, t);
            darkOverlay.color = Color.Lerp(startOverlay, new Color(0f, 0f, 0f, targetOverlayAlpha), t);
            charNameText.color = Color.Lerp(startTxt, new Color(1f, 1f, 1f, state >= 1 ? 1f : 0f), t);
            yield return null;
        }
        colorPanel.color   = targetColor;
        darkOverlay.color  = new Color(0f, 0f, 0f, targetOverlayAlpha);
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
