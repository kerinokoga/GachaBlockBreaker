using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// フェードイン/アウトによるシーン遷移を管理するシングルトン
/// DontDestroyOnLoad でシーン間を維持する
/// 使い方: SceneTransitionManager.LoadScene("HomeScene");
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] private float fadeDuration = 0.4f;

    private Image fadeImage;
    private bool isFading = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeOverlay();
    }

    void CreateFadeOverlay()
    {
        var canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGo.AddComponent<CanvasScaler>();

        var imgGo = new GameObject("FadeImage");
        imgGo.transform.SetParent(canvasGo.transform, false);
        fadeImage = imgGo.AddComponent<Image>();
        fadeImage.color = Color.clear;
        fadeImage.raycastTarget = false;
        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ---- 静的ショートカット ----

    public static void LoadScene(string sceneName)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.FadeAndLoad(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    // ---- フェード遷移 ----

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (isFading) yield break;
        isFading = true;

        // フェードアウト（透明 → 黒）
        yield return StartCoroutine(Fade(Color.clear, Color.black, fadeDuration));

        SceneManager.LoadScene(sceneName);

        // 1フレーム待ってからフェードイン
        yield return null;
        yield return StartCoroutine(Fade(Color.black, Color.clear, fadeDuration));

        isFading = false;
    }

    private IEnumerator Fade(Color from, Color to, float duration)
    {
        fadeImage.raycastTarget = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeImage.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        fadeImage.color = to;
        fadeImage.raycastTarget = (to.a > 0.01f);
    }

    /// <summary>現在シーンのフェードイン（シーン開始時に呼ぶ）</summary>
    public static void FadeIn()
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.Fade(Color.black, Color.clear, Instance.fadeDuration));
    }
}
