using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// コレクション画面：クリア済みステージの美少女解放状態を一覧表示（ScrollRect 対応）
/// タップでイラストギャラリー（0%/30%/60%/100% 切り替え）
/// </summary>
public class CollectionUI : MonoBehaviour
{
    Transform canvasRoot;
    GameObject galleryPanel;
    Image galleryImage;
    Text galleryLabel;
    Text galleryLockText;
    // モザイク用（シェーダー不要、CPU でピクセル化テクスチャ生成）
    Sprite[] gallerySprites; // 現在表示中キャラの5枚（覚醒含む）
    bool[] galleryLocked;    // 5枚目が未覚醒ロック状態か
    string[] gallerySpriteLabels = { "0%", "30%", "60%", "100%", "✦覚醒" };
    int galleryIndex = 0;

    List<RectTransform> particles = new List<RectTransform>();

    // カタカナ名 → ローマ字名マッピング（Resources.Load用）
    static readonly Dictionary<string, string> KanaToRomaji = new Dictionary<string, string>
    {
        {"ルナ","Luna"}, {"アリア","Aria"}, {"セラ","Sera"}, {"リコ","Riko"}, {"ミカ","Mika"},
        {"ハナ","Hana"}, {"ユキ","Yuki"}, {"ナナ","Nana"}, {"ソラ","Sora"}, {"レイ","Rei"},
        {"リン","Rin"},  {"アイ","Ai"},   {"カナ","Kana"}, {"メイ","Mei"},  {"ノア","Noa"},
        {"ルカ","Ruka"}, {"サキ","Saki"}, {"ユナ","Yuna"}, {"トモ","Tomo"}, {"アカリ","Akari"},
    };

    void Start() => BuildUI();

    void Update()
    {
        // パーティクルアニメーション
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            var p = particles[i];
            float speed = 8f + (i % 5) * 4f;
            float sway = Mathf.Sin(Time.time * (0.3f + i * 0.1f)) * 0.3f;
            p.anchoredPosition += new Vector2(sway, speed * Time.deltaTime);
            var c = p.GetComponent<Image>().color;
            c.a = Mathf.PingPong(Time.time * 0.4f + i * 0.5f, 0.35f) + 0.05f;
            p.GetComponent<Image>().color = c;
            if (p.anchoredPosition.y > 1920f)
                p.anchoredPosition = new Vector2(Random.Range(0f, 1080f), -20f);
        }
    }

    void BuildUI()
    {
        var cGo = new GameObject("CollectionCanvas");
        var c = cGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0.0f;
        cGo.AddComponent<GraphicRaycaster>();
        canvasRoot = cGo.transform;

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 背景（ダークパープル/ネイビー）
        MakeBg(canvasRoot, new Color(0.03f, 0.02f, 0.1f));

        // 上部デコバー
        MakeDecoBar(canvasRoot, new Vector2(0f, 0.97f), new Vector2(1f, 1f),
            new Color(0.6f, 0.4f, 1f, 0.25f));
        // 下部デコバー
        MakeDecoBar(canvasRoot, new Vector2(0f, 0f), new Vector2(1f, 0.025f),
            new Color(0.6f, 0.4f, 1f, 0.25f));

        // パーティクル（12個）
        SpawnParticles(canvasRoot, 12);

        // タイトル（Shadow + Outline 付き）
        var titleCherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        var titleTxt = MakeText(canvasRoot, "\u2726 \u30b3\u30ec\u30af\u30b7\u30e7\u30f3 \u2726", 52,
            new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f, 0.94f), new Vector2(900f, 80f));
        if (titleCherry != null) titleTxt.font = titleCherry;
        titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(titleTxt.gameObject, new Color(0.2f, 0.1f, 0f, 0.7f), new Vector2(3f, -3f));
        var outline = titleTxt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.5f, 0f, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);

        // タイトル下デコライン
        MakeLine(canvasRoot, new Vector2(0.5f, 0.915f), new Vector2(700f, 3f),
            new Color(1f, 0.85f, 0.4f, 0.5f));

        // ほーむ ボタン＋エンドレスギャラリーボタン（下部に2つ並べる）
        MakeStyledButton(canvasRoot, "ホーム", new Color(0.25f, 0.15f, 0.45f),
            new Color(0.5f, 0.3f, 0.9f, 0.35f),
            new Vector2(0.28f, 0.06f), new Vector2(360f, 80f),
            () => SceneManager.LoadScene("HomeScene"));

        MakeStyledButton(canvasRoot, "エンドレスギャラリー", new Color(0.45f, 0.2f, 0.4f),
            new Color(0.9f, 0.4f, 0.7f, 0.4f),
            new Vector2(0.72f, 0.06f), new Vector2(420f, 80f),
            ShowEndlessGallery);

        // ScrollRect
        BuildScrollList(canvasRoot);

        // ギャラリーパネル（初期非表示）
        BuildGalleryPanel();
    }

    // ============================================================
    // エンドレスギャラリー（撃破報酬イラストの閲覧）
    // ============================================================

    /// <summary>エンドレスギャラリーの一覧ポップアップを開く</summary>
    // ギャラリーセルのサムネイル・ラベル参照（初ダウンロード完了時にセルへ即反映するため）
    readonly Dictionary<string, RawImage> galleryThumbs = new Dictionary<string, RawImage>();
    readonly Dictionary<string, Text> galleryLabels = new Dictionary<string, Text>();

    // デコード済みサムネイルのメモリキャッシュ（ギャラリーを開き直しても再デコードしない）
    readonly Dictionary<string, Texture2D> galleryTexCache = new Dictionary<string, Texture2D>();

    /// <summary>
    /// セル用サムネイルを読み込む。フレーム分散（delayFrames待ち）→
    /// サムネイル→（無ければ）フル画像の順で試し、成功したらメモリキャッシュする
    /// </summary>
    IEnumerator LoadCellThumb(int delayFrames, string thumbFile, string fullFile,
        System.Action<Texture2D> onDone)
    {
        for (int i = 0; i < delayFrames; i++) yield return null;

        Texture2D result = null;
        yield return EndlessGalleryManager.LoadImage(thumbFile, t => result = t);
        if (result == null && fullFile != null)
            yield return EndlessGalleryManager.LoadImage(fullFile, t => result = t);

        if (result != null) galleryTexCache[thumbFile] = result;
        onDone?.Invoke(result);
    }

    void ShowEndlessGallery()
    {
        galleryThumbs.Clear();
        galleryLabels.Clear();
        var overlay = new GameObject("EndlessGalleryOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        overlay.AddComponent<Image>().color = new Color(0.03f, 0.02f, 0.1f, 0.98f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var title = MakeText(overlay.transform, "エンドレスギャラリー", 46,
            new Color(1f, 0.85f, 0.1f), new Vector2(0.5f, 0.945f), new Vector2(900f, 64f));
        AddShadow(title.gameObject, new Color(0f, 0f, 0f, 0.7f), new Vector2(2f, -2f));

        var info = MakeText(overlay.transform,
            $"累計撃破: {EndlessGalleryManager.TotalKills}体　自己ベスト: {EndlessGalleryManager.BestScore}体",
            28, new Color(0.75f, 0.8f, 0.95f), new Vector2(0.5f, 0.9f), new Vector2(900f, 40f));

        // スクロールリスト
        var scrollGo = new GameObject("GalleryScroll");
        scrollGo.transform.SetParent(overlay.transform, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.03f, 0.12f);
        scrollRT.anchorMax = new Vector2(0.97f, 0.87f);
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
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        sr.content = contentRT;
        sr.viewport = vpRT;

        // ---- セル配置（4列グリッド・9:16縦長セル） ----
        const float cell = 226f, gap = 22f;
        const int cols = 4;
        float cellH = cell * CellAspect;
        float y = 20f;

        y = AddGalleryHeader(contentGo.transform, "◆ 累計撃破報酬", y);
        int ti = 0;
        foreach (int n in EndlessGalleryManager.TotalMilestones)
        {
            int col = ti % cols;
            if (col == 0 && ti > 0) y += cellH + gap;
            BuildGalleryCell(contentGo.transform,
                EndlessGalleryManager.TotalFile(n), $"{n}体",
                EndlessGalleryManager.IsTotalUnlocked(n), false,
                $"累計{n}体で解放", col, y, cell, gap);
            ti++;
        }
        y += cellH + gap + 30f;

        y = AddGalleryHeader(contentGo.transform, "◆ 自己ベスト報酬（1ランの最高記録）", y);
        int bi = 0;
        foreach (int m in EndlessGalleryManager.BestMilestones)
        {
            int col = bi % cols;
            if (col == 0 && bi > 0) y += cellH + gap;
            bool isKisekae = EndlessGalleryManager.IsKisekaeMilestone(m);
            BuildGalleryCell(contentGo.transform,
                isKisekae ? null : EndlessGalleryManager.BestFile(m),
                $"{m}体", EndlessGalleryManager.IsBestUnlocked(m), isKisekae,
                $"自己ベスト{m}体で解放", col, y, cell, gap, isKisekae ? m : 0);
            bi++;
        }
        y += cellH + gap + 20f;

        contentRT.sizeDelta = new Vector2(0f, y);

        // とじる
        MakeStyledButton(overlay.transform, "とじる", new Color(0.25f, 0.25f, 0.35f),
            new Color(0.45f, 0.45f, 0.6f, 0.6f),
            new Vector2(0.5f, 0.055f), new Vector2(300f, 76f),
            () => Destroy(overlay));
    }

    // セルの縦横比（9:16縦長・イラストと同じ向き）。高さ = 幅 × CellAspect
    const float CellAspect = 16f / 9f;

    /// <summary>
    /// セルのサムネイルにテクスチャを設定する。
    /// 9:16セルに合わせてクロップし、縦横比の歪みを防ぐ。
    /// 縦長イラストはほぼ全体表示、横長のきせかえサムネは中央を切り抜く
    /// </summary>
    static void SetCellThumb(RawImage raw, Texture2D tex)
    {
        raw.texture = tex;
        raw.color = Color.white;
        float texAspect = (float)tex.width / tex.height;   // 幅/高さ
        float cellAspect = 1f / CellAspect;
        if (texAspect >= cellAspect)
        {
            // セルより横長→左右を中央基準でカット
            float w = cellAspect / texAspect;
            raw.uvRect = new Rect((1f - w) / 2f, 0f, w, 1f);
        }
        else
        {
            // セルより縦長→上下をカット（上端＝顔側を残す）
            float h = texAspect / cellAspect;
            raw.uvRect = new Rect(0f, 1f - h, 1f, h);
        }
    }

    /// <summary>セル内に全面のサムネイル枠（RawImage）を作る。テクスチャが入るまで透明</summary>
    static RawImage MakeCellThumbSlot(Transform cellParent)
    {
        var go = new GameObject("Thumb");
        go.transform.SetParent(cellParent, false);
        var raw = go.AddComponent<RawImage>();
        raw.color = Color.clear;
        raw.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(6f, 6f); rt.offsetMax = new Vector2(-6f, -6f);
        return raw;
    }

    /// <summary>ギャラリーのセクション見出しを配置し、次のY位置を返す</summary>
    float AddGalleryHeader(Transform parent, string text, float y)
    {
        var t = new GameObject("Header").AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.text = text; t.fontSize = 30; t.color = new Color(1f, 0.8f, 0.3f);
        t.alignment = TextAnchor.MiddleLeft;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -y);
        rt.sizeDelta = new Vector2(-20f, 44f);
        return y + 56f;
    }

    /// <summary>
    /// ギャラリーの1セル。解放済みはタップで拡大表示（キャッシュ済みならサムネも出す）。
    /// isKisekae のセルは画像ではなく「きせかえ報酬」の案内。
    /// </summary>
    void BuildGalleryCell(Transform parent, string file, string label,
        bool unlocked, bool isKisekae, string lockText, int col, float y, float cell, float gap,
        int kisekaeBest = 0)
    {
        var go = new GameObject($"Cell_{label}");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = unlocked
            ? (isKisekae ? new Color(0.45f, 0.25f, 0.4f, 0.95f) : new Color(0.2f, 0.16f, 0.38f, 0.95f))
            : new Color(0.09f, 0.08f, 0.14f, 0.9f);
        UISprites.Button(img);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        float x = gap + col * (cell + gap);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(cell, cell * CellAspect);

        // ラベル（体数）。解放済みの画像セルは読み込み完了までの表示。
        // ※サムネイル読込より先に生成する（キャッシュ済みだと読込コールバックが
        //   同期実行されるため、後から生成するとラベルを消す処理が空振りする）
        string thumbFile = file != null ? EndlessGalleryManager.ThumbFile(file) : null;
        bool loading = unlocked && !isKisekae && file != null
            && !galleryTexCache.ContainsKey(thumbFile);
        var t = new GameObject("Label").AddComponent<Text>();
        t.transform.SetParent(go.transform, false);
        t.text = unlocked
            ? (isKisekae ? $"{label}\nきせかえ\nタップで再生"
               : loading ? $"{label}\n読み込み中..." : label)
            : $"{label}\n未解放";
        t.fontSize = (loading || (unlocked && isKisekae)) ? 22 : (unlocked ? 26 : 24);
        if (unlocked && !isKisekae && file != null) galleryLabels[file] = t;
        t.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.6f);
        t.alignment = TextAnchor.MiddleCenter;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        var lrt = t.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var sh = t.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sh.effectDistance = new Vector2(2f, -2f);

        // きせかえセルのサムネイル（小さいjpgなので自動ダウンロードして表示）
        if (unlocked && isKisekae
            && HomeCharManager.TryGetVariantByBest(kisekaeBest, out var thumbVar)
            && !string.IsNullOrEmpty(thumbVar.thumb))
        {
            var kRaw = MakeCellThumbSlot(go.transform);
            void ApplyKisekaeThumb(Texture2D tex)
            {
                if (kRaw == null || tex == null) return;
                SetCellThumb(kRaw, tex);
                // サムネイルが出たら文字は再生マークだけに
                if (t != null) { t.text = "きせかえ▶"; t.fontSize = 24; }
            }
            if (galleryTexCache.TryGetValue(thumbVar.thumb, out var kCached) && kCached != null)
                ApplyKisekaeThumb(kCached);
            else
                StartCoroutine(LoadCellThumb(galleryThumbs.Count / 4, thumbVar.thumb, null,
                    ApplyKisekaeThumb));
        }

        // サムネイル（解放済みの画像セルは自動で読み込む。
        // メモリキャッシュ→軽量サムネ→フル画像の順。読み込みはフレーム分散）
        if (unlocked && !isKisekae && file != null)
        {
            var raw = MakeCellThumbSlot(go.transform);
            galleryThumbs[file] = raw;

            if (galleryTexCache.TryGetValue(thumbFile, out var cached) && cached != null)
            {
                SetCellThumb(raw, cached);
                t.gameObject.SetActive(false);
            }
            else
            {
                string capturedLabel2 = label;
                StartCoroutine(LoadCellThumb(galleryThumbs.Count / 4, thumbFile, file, tex =>
                {
                    if (raw == null) return;
                    if (tex == null)
                    {
                        // 取得失敗（オフライン等）。タップで拡大表示から再試行できる
                        if (t != null) t.text = $"{capturedLabel2}\nタップで表示";
                        return;
                    }
                    SetCellThumb(raw, tex);
                    // サムネイル表示中は体数ラベルを消す
                    if (t != null) t.gameObject.SetActive(false);
                }));
            }
        }

        // ラベルをサムネイルより手前に（きせかえ▶等を画像の上に表示）
        t.transform.SetAsLastSibling();

        // タップ動作
        var btn = go.AddComponent<Button>();
        if (unlocked && !isKisekae && file != null)
        {
            string capturedFile = file;
            string capturedLabel = label;
            btn.onClick.AddListener(() => ShowGalleryImage(capturedFile, capturedLabel));
        }
        else if (unlocked && isKisekae)
        {
            int capturedBest = kisekaeBest;
            btn.onClick.AddListener(() => ShowKisekaeVideo(capturedBest));
        }
        else
        {
            string capturedLock = lockText;
            btn.onClick.AddListener(() => ShowGalleryNotice(capturedLock));
        }
    }

    /// <summary>フルスクリーンのイラスト表示（必要ならダウンロード）</summary>
    void ShowGalleryImage(string file, string label)
    {
        var viewer = new GameObject("GalleryViewer");
        viewer.transform.SetParent(canvasRoot, false);
        viewer.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.97f);
        var vrt = viewer.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
        vrt.offsetMin = vrt.offsetMax = Vector2.zero;
        // 画面タップで閉じる
        var closeBtn = viewer.AddComponent<Button>();
        closeBtn.transition = Selectable.Transition.None;
        closeBtn.onClick.AddListener(() => Destroy(viewer));

        var status = MakeText(viewer.transform, "読み込み中...", 32,
            new Color(0.8f, 0.8f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(600f, 50f));

        var imgGo = new GameObject("Img");
        imgGo.transform.SetParent(viewer.transform, false);
        var raw = imgGo.AddComponent<RawImage>();
        raw.color = Color.clear;
        raw.raycastTarget = false;
        var irt = imgGo.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.02f, 0.06f);
        irt.anchorMax = new Vector2(0.98f, 0.94f);
        irt.offsetMin = irt.offsetMax = Vector2.zero;
        var fitter = imgGo.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        var cap = MakeText(viewer.transform, $"エンドレスギャラリー {label}", 28,
            new Color(0.9f, 0.9f, 1f), new Vector2(0.5f, 0.97f), new Vector2(800f, 40f));
        var hint = MakeText(viewer.transform, "タップで閉じる", 24,
            new Color(0.6f, 0.6f, 0.7f), new Vector2(0.5f, 0.03f), new Vector2(500f, 36f));

        StartCoroutine(EndlessGalleryManager.LoadImage(file, tex =>
        {
            if (viewer == null) return;
            if (tex == null)
            {
                if (status != null)
                    status.text = "取得できませんでした\n通信環境を確認してもう一度お試しください";
                return;
            }
            if (status != null) status.gameObject.SetActive(false);
            if (raw != null)
            {
                raw.texture = tex;
                raw.color = Color.white;
                fitter.aspectRatio = (float)tex.width / tex.height;
            }
            // 初ダウンロード時、一覧のセルにもサムネイルを即反映（体数ラベルは消す）
            if (galleryThumbs.TryGetValue(file, out var cellThumb) && cellThumb != null)
                SetCellThumb(cellThumb, tex);
            if (galleryLabels.TryGetValue(file, out var cellLabel) && cellLabel != null)
                cellLabel.gameObject.SetActive(false);
        }));
    }

    /// <summary>
    /// きせかえ動画のフルスクリーン再生（解放済みセル用）。
    /// 配信タイプで未DLならその場でダウンロードしてから再生する
    /// </summary>
    void ShowKisekaeVideo(int best)
    {
        // マイルストーンに対応するバリアントを検索
        if (!HomeCharManager.TryGetVariantByBest(best, out var v))
        {
            ShowGalleryNotice("このきせかえは準備中です");
            return;
        }

        var viewer = new GameObject("KisekaeViewer");
        viewer.transform.SetParent(canvasRoot, false);
        viewer.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.97f);
        var vrt = viewer.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
        vrt.offsetMin = vrt.offsetMax = Vector2.zero;

        var status = MakeText(viewer.transform, "読み込み中...", 32,
            new Color(0.8f, 0.8f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(600f, 90f));

        var imgGo = new GameObject("Img");
        imgGo.transform.SetParent(viewer.transform, false);
        var raw = imgGo.AddComponent<RawImage>();
        raw.color = Color.clear;
        raw.raycastTarget = false;
        var irt = imgGo.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.02f, 0.06f);
        irt.anchorMax = new Vector2(0.98f, 0.94f);
        irt.offsetMin = irt.offsetMax = Vector2.zero;
        var fitter = imgGo.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        MakeText(viewer.transform, $"きせかえ {v.label}", 28,
            new Color(0.9f, 0.9f, 1f), new Vector2(0.5f, 0.97f), new Vector2(800f, 40f));
        MakeText(viewer.transform, "タップで閉じる", 24,
            new Color(0.6f, 0.6f, 0.7f), new Vector2(0.5f, 0.03f), new Vector2(500f, 36f));

        var vp = viewer.AddComponent<UnityEngine.Video.VideoPlayer>();
        vp.playOnAwake = false;
        vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
        vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;
        vp.isLooping = true;

        RenderTexture rtex = null;
        vp.prepareCompleted += p =>
        {
            if (viewer == null || raw == null) return;
            rtex = new RenderTexture((int)p.width, (int)p.height, 0);
            p.targetTexture = rtex;
            raw.texture = rtex;
            raw.color = Color.white;
            fitter.aspectRatio = (float)p.width / p.height;
            if (status != null) status.gameObject.SetActive(false);
            p.Play();
        };

        // 画面タップで閉じる（RenderTextureも解放）
        var closeBtn = viewer.AddComponent<Button>();
        closeBtn.transition = Selectable.Transition.None;
        closeBtn.onClick.AddListener(() =>
        {
            vp.Stop();
            if (rtex != null) { rtex.Release(); Destroy(rtex); }
            Destroy(viewer);
        });

        // ホームのきせかえに設定ボタン（右下）
        var setGo = new GameObject("SetHomeBtn");
        setGo.transform.SetParent(viewer.transform, false);
        var setImg = setGo.AddComponent<Image>();
        setImg.color = new Color(0.75f, 0.3f, 0.6f, 0.95f);
        UISprites.Button(setImg);
        var setBtn = setGo.AddComponent<Button>();
        var setRt = setGo.GetComponent<RectTransform>();
        setRt.anchorMin = setRt.anchorMax = new Vector2(0.76f, 0.095f);
        setRt.anchoredPosition = Vector2.zero;
        setRt.sizeDelta = new Vector2(400f, 76f);
        var setLabel = MakeText(setGo.transform, "ホームのきせかえに設定", 26,
            Color.white, new Vector2(0.5f, 0.5f), new Vector2(390f, 70f));
        AddShadow(setLabel.gameObject, new Color(0f, 0f, 0f, 0.6f), new Vector2(2f, -2f));
        if (HomeCharManager.GetSelected() == v.fileName)
        {
            setLabel.text = "設定中";
            setBtn.interactable = false;
        }
        setBtn.onClick.AddListener(() =>
        {
            if (!HomeCharManager.IsVariantUnlocked(v))
            {
                ShowGalleryNotice($"{v.baseChar}を覚醒すると設定できます");
                return;
            }
            if (v.streamed && !HomeCharManager.IsVariantCached(v.fileName))
            {
                ShowGalleryNotice("読み込み完了までお待ちください");
                return;
            }
            HomeCharManager.SetSelected(v.fileName);
            setLabel.text = "設定中";
            setBtn.interactable = false;
            ShowGalleryNotice("ホームのきせかえに設定しました♪");
        });

        void StartPlay()
        {
            if (v.streamed)
            {
                vp.source = UnityEngine.Video.VideoSource.Url;
                vp.url = HomeCharManager.VariantCachePath(v.fileName);
            }
            else
            {
                var clip = Resources.Load<UnityEngine.Video.VideoClip>($"Movies/Home/{v.fileName}");
                if (clip == null)
                {
                    if (status != null) status.text = "動画が見つかりません";
                    return;
                }
                vp.clip = clip;
            }
            vp.Prepare();
        }

        if (v.streamed && !HomeCharManager.IsVariantCached(v.fileName))
        {
            status.text = $"{v.label} を読み込み中...";
            StartCoroutine(EndlessGalleryManager.DownloadFile(
                HomeCharManager.VariantUrl(v.fileName),
                HomeCharManager.VariantCachePath(v.fileName),
                ok =>
                {
                    if (viewer == null) return;
                    if (!ok)
                    {
                        if (status != null)
                            status.text = "取得できませんでした\n通信環境を確認してもう一度お試しください";
                        return;
                    }
                    StartPlay();
                }));
        }
        else StartPlay();
    }

    /// <summary>ギャラリー内の小さな通知（ロック条件など）</summary>
    void ShowGalleryNotice(string message)
    {
        var notice = new GameObject("GalleryNotice");
        notice.transform.SetParent(canvasRoot, false);
        var bg = notice.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.05f, 0.18f, 0.97f);
        UISprites.Button(bg);
        var nrt = notice.GetComponent<RectTransform>();
        nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0.5f);
        nrt.anchoredPosition = Vector2.zero;
        nrt.sizeDelta = new Vector2(760f, 220f);

        var t = MakeText(notice.transform, message, 30, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(700f, 160f));

        var btn = notice.AddComponent<Button>();
        btn.onClick.AddListener(() => Destroy(notice));
        Destroy(notice, 2.5f); // 放置でも自動で消える
    }

    void BuildScrollList(Transform root)
    {
        // ScrollRect（外枠）
        var srGo = new GameObject("ScrollView");
        srGo.transform.SetParent(root, false);
        var srRT = srGo.AddComponent<RectTransform>();
        srRT.anchorMin = new Vector2(0.02f, 0.10f);
        srRT.anchorMax = new Vector2(0.98f, 0.88f);
        srRT.offsetMin = srRT.offsetMax = Vector2.zero;
        var sr = srGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical   = true;
        sr.scrollSensitivity = 30f;

        // Viewport（ScrollRect の子）
        var viewGo = new GameObject("Viewport");
        viewGo.transform.SetParent(srGo.transform, false);
        viewGo.AddComponent<Image>().color = Color.white;
        viewGo.AddComponent<Mask>().showMaskGraphic = false;
        var viewRT = viewGo.GetComponent<RectTransform>();
        viewRT.anchorMin = Vector2.zero;
        viewRT.anchorMax = Vector2.one;
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        // Content（Viewport の子）
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewGo.transform, false);
        var contentRT = contentGo.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        sr.content  = contentRT;
        sr.viewport = viewRT;

        var allStages = Resources.LoadAll<StageData>("Stages");

        float cellH  = 310f;
        float cellW  = 480f;
        float padY   = 16f;
        int   cols   = 2;
        int   total  = ProgressManager.TotalStages;
        int   rows   = Mathf.CeilToInt((float)total / cols);

        float contentHeight = rows * (cellH + padY) + padY + cellH * 0.5f;
        contentRT.sizeDelta = new Vector2(0f, contentHeight);

        for (int i = 0; i < total; i++)
        {
            int stageNum = i + 1;
            int col = i % cols;
            int row = i / cols;

            float xAnchor = col == 0 ? 0.27f : 0.73f;
            float yPos    = -(padY + row * (cellH + padY) + cellH * 0.5f);

            StageData sd = null;
            foreach (var s in allStages)
                if (s.stageNumber == stageNum) { sd = s; break; }

            bool  cleared = ProgressManager.IsCleared(stageNum);
            float rate    = ProgressManager.GetBestRate(stageNum);

            BuildCell(contentRT, stageNum, sd, cleared, rate, xAnchor, yPos, cellW, cellH);
        }
    }

    void BuildCell(Transform parent, int stageNum, StageData sd,
        bool cleared, float rate, float anchorX, float yPos, float w, float h)
    {
        // 外枠フレーム
        Color frameCol = cleared && sd != null
            ? new Color(sd.illustColorFull.r, sd.illustColorFull.g, sd.illustColorFull.b, 0.5f)
            : new Color(0.08f, 0.06f, 0.15f);

        var frameGo = new GameObject($"Stage{stageNum}Frame");
        frameGo.transform.SetParent(parent, false);
        frameGo.AddComponent<Image>().color = frameCol;
        var frt = frameGo.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(anchorX, 1f);
        frt.pivot = new Vector2(0.5f, 1f);
        frt.anchoredPosition = new Vector2(0f, yPos);
        frt.sizeDelta = new Vector2(w + 6f, h + 6f);

        // 内側背景
        Color bgCol = cleared
            ? new Color(0.12f, 0.10f, 0.22f)
            : new Color(0.06f, 0.05f, 0.10f);

        var cellGo = new GameObject($"Stage{stageNum}Cell");
        cellGo.transform.SetParent(frameGo.transform, false);
        cellGo.AddComponent<Image>().color = bgCol;
        var rt = cellGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(3f, 3f);
        rt.offsetMax = new Vector2(-3f, -3f);

        // 上部シャイン
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(cellGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
        var shRT = shineGo.GetComponent<RectTransform>();
        shRT.anchorMin = new Vector2(0f, 0.7f);
        shRT.anchorMax = new Vector2(1f, 1f);
        shRT.offsetMin = shRT.offsetMax = Vector2.zero;

        Transform ct = cellGo.transform;

        // ステージ番号（CherryBombOne フォント）
        var cellCherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        Color titleCol = cleared ? Color.white : new Color(0.4f, 0.4f, 0.4f);
        var stageT = MakeCellText(ct, $"ステージ{stageNum}", 36, titleCol,
            new Vector2(0.5f, 0.88f), new Vector2(w - 20f, 50f));
        if (cellCherry != null) stageT.font = cellCherry;
        stageT.horizontalOverflow = HorizontalWrapMode.Overflow;
        stageT.verticalOverflow = VerticalWrapMode.Overflow;
        var stageOutline = stageT.gameObject.AddComponent<Outline>();
        stageOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        stageOutline.effectDistance = new Vector2(2f, -2f);

        if (cleared && sd != null)
        {
            // サムネイル（大きめ表示）
            var swatchGo = new GameObject("Swatch");
            swatchGo.transform.SetParent(ct, false);
            var swatchImg = swatchGo.AddComponent<Image>();
            if (sd.illustSpriteFull != null)
            {
                swatchImg.sprite = sd.illustSpriteFull;
                swatchImg.preserveAspect = true;
                swatchImg.color = Color.white;
            }
            else
            {
                swatchImg.color = sd.illustColorFull;
            }
            var sRT = swatchGo.GetComponent<RectTransform>();
            sRT.anchorMin = sRT.anchorMax = new Vector2(0.25f, 0.42f);
            sRT.sizeDelta = new Vector2(220f, 220f);

            // キャラ名（CherryBombOne フォントで大きく）
            string charName = !string.IsNullOrEmpty(sd.characterName) ? sd.characterName : "???";
            var charNameT = MakeCellText(ct, charName, 56, new Color(1f, 0.95f, 1f),
                new Vector2(0.70f, 0.55f), new Vector2(280f, 75f));
            if (cellCherry != null) charNameT.font = cellCherry;
            charNameT.horizontalOverflow = HorizontalWrapMode.Overflow;
            charNameT.verticalOverflow = VerticalWrapMode.Overflow;
            var charOutline = charNameT.gameObject.AddComponent<Outline>();
            charOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            charOutline.effectDistance = new Vector2(2f, -2f);

            // 覚醒済みマーク + ボイス再生ボタン
            bool isAwakened = !string.IsNullOrEmpty(sd.characterName) && OrbManager.IsAwakened(sd.characterName);
            if (isAwakened)
            {
                // 覚醒済表記（小さめにして上に）
                var awakenT = MakeCellText(ct, "✦ 覚醒済", 28, new Color(1f, 0.85f, 0.1f),
                    new Vector2(0.70f, 0.32f), new Vector2(280f, 40f));
                if (cellCherry != null) awakenT.font = cellCherry;
                awakenT.horizontalOverflow = HorizontalWrapMode.Overflow;
                awakenT.verticalOverflow = VerticalWrapMode.Overflow;
                var awakenOutline = awakenT.gameObject.AddComponent<Outline>();
                awakenOutline.effectColor = new Color(0.4f, 0.2f, 0f, 0.8f);
                awakenOutline.effectDistance = new Vector2(2f, -2f);

                // ボイス再生ボタン（覚醒解放）
                StageData capturedSd = sd;
                MakeVoiceButton(ct, "♪ ボイス ♪",
                    new Color(0.7f, 0.15f, 0.4f), new Color(0.95f, 0.4f, 0.65f),
                    new Vector2(0.70f, 0.10f), new Vector2(260f, 60f), cellCherry,
                    () => OpenVoicePopup(capturedSd));
            }
            else
            {
                // 未覚醒: ボイス解放案内（読みやすく大きめ）
                string lockMsg = $"{sd.characterName}覚醒で\nボイス解放";
                var lockT = MakeCellText(ct, lockMsg, 30, new Color(0.85f, 0.85f, 0.95f),
                    new Vector2(0.70f, 0.16f), new Vector2(310f, 120f));
                if (cellCherry != null) lockT.font = cellCherry;
                lockT.horizontalOverflow = HorizontalWrapMode.Overflow;
                lockT.verticalOverflow = VerticalWrapMode.Overflow;
                var lockShadow = lockT.gameObject.AddComponent<Shadow>();
                lockShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
                lockShadow.effectDistance = new Vector2(2f, -2f);
                var lockOutline = lockT.gameObject.AddComponent<Outline>();
                lockOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
                lockOutline.effectDistance = new Vector2(2f, -2f);
            }

            // 上端カラーライン
            var lineGo = new GameObject("Line");
            lineGo.transform.SetParent(ct, false);
            lineGo.AddComponent<Image>().color = sd.illustColorFull;
            var lRT = lineGo.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0f, 1f);
            lRT.anchorMax = new Vector2(1f, 1f);
            lRT.pivot = new Vector2(0.5f, 1f);
            lRT.anchoredPosition = Vector2.zero;
            lRT.sizeDelta = new Vector2(0f, 5f);

            // セル全体をタップでギャラリー表示
            bool hasAnyIllust = (sd.illustSprite0 != null || sd.illustSprite30 != null ||
                                 sd.illustSprite60 != null || sd.illustSpriteFull != null);
            if (hasAnyIllust)
            {
                StageData captured = sd;
                var btn = frameGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                btn.onClick.AddListener(() => OpenGallery(captured));
            }
        }
        else
        {
            MakeCellText(ct, "???", 36, new Color(0.3f, 0.3f, 0.3f),
                new Vector2(0.5f, 0.47f), new Vector2(w - 20f, 50f));
            MakeCellText(ct, "Not cleared", 20, new Color(0.3f, 0.3f, 0.3f),
                new Vector2(0.5f, 0.2f), new Vector2(w - 20f, 28f));
        }
    }

    // ---- ギャラリーパネル ----

    void BuildGalleryPanel()
    {
        galleryPanel = new GameObject("GalleryPanel");
        galleryPanel.transform.SetParent(canvasRoot, false);

        // 黒背景
        var bgImg = galleryPanel.AddComponent<Image>();
        bgImg.color = Color.black;
        var rt = galleryPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // イラスト表示
        var illustGo = new GameObject("GalleryIllust");
        illustGo.transform.SetParent(galleryPanel.transform, false);
        galleryImage = illustGo.AddComponent<Image>();
        galleryImage.preserveAspect = true;
        galleryImage.color = Color.white;
        galleryImage.raycastTarget = false;
        var irt = illustGo.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0f, 0.08f);
        irt.anchorMax = new Vector2(1f, 0.95f);
        irt.offsetMin = irt.offsetMax = Vector2.zero;

        // モザイク処理はCPU側で行う（シェーダー不要）

        // ロックテキスト（未覚醒5枚目用、初期非表示）
        var lockGo = new GameObject("LockText");
        lockGo.transform.SetParent(galleryPanel.transform, false);
        galleryLockText = lockGo.AddComponent<Text>();
        galleryLockText.text = "キャラ覚醒後アンロック";
        galleryLockText.fontSize = 56;
        galleryLockText.color = new Color(1f, 0.85f, 0.1f, 1f);
        galleryLockText.alignment = TextAnchor.MiddleCenter;
        galleryLockText.font = UIFont.Main; galleryLockText.verticalOverflow = VerticalWrapMode.Overflow;
        galleryLockText.fontStyle = FontStyle.Bold;
        galleryLockText.raycastTarget = false;
        var lockShadow = lockGo.AddComponent<Shadow>();
        lockShadow.effectColor = new Color(0f, 0f, 0f, 1f);
        lockShadow.effectDistance = new Vector2(3f, -3f);
        var lockOutline = lockGo.AddComponent<Outline>();
        lockOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        lockOutline.effectDistance = new Vector2(2f, -2f);
        var lockRt = lockGo.GetComponent<RectTransform>();
        lockRt.anchorMin = lockRt.anchorMax = new Vector2(0.5f, 0.5f);
        lockRt.anchoredPosition = Vector2.zero;
        lockRt.sizeDelta = new Vector2(800f, 80f);
        lockGo.SetActive(false);

        // ラベル（キャラ名 + 破壊率）
        var labelGo = new GameObject("GalleryLabel");
        labelGo.transform.SetParent(galleryPanel.transform, false);
        galleryLabel = labelGo.AddComponent<Text>();
        galleryLabel.fontSize = 32;
        galleryLabel.color = Color.white;
        galleryLabel.alignment = TextAnchor.MiddleCenter;
        galleryLabel.font = UIFont.Main; galleryLabel.verticalOverflow = VerticalWrapMode.Overflow;
        galleryLabel.raycastTarget = false;
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.91f);
        lrt.anchoredPosition = Vector2.zero;
        lrt.sizeDelta = new Vector2(800f, 50f);

        // 左ボタン（前の画像）- パープルティント付き
        var leftGo = MakeGalleryNavButton(galleryPanel.transform, "\uff1c", new Vector2(0.08f, 0.5f));
        leftGo.GetComponent<Button>().onClick.AddListener(() => GalleryNav(-1));

        // 右ボタン（次の画像）- パープルティント付き
        var rightGo = MakeGalleryNavButton(galleryPanel.transform, "\uff1e", new Vector2(0.92f, 0.5f));
        rightGo.GetComponent<Button>().onClick.AddListener(() => GalleryNav(1));

        // 閉じるボタン
        var closeGo = new GameObject("CloseBtn");
        closeGo.transform.SetParent(galleryPanel.transform, false);
        closeGo.AddComponent<Image>().color = new Color(0.25f, 0.15f, 0.35f, 0.85f);
        var closeBtn = closeGo.AddComponent<Button>();
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.03f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(240f, 60f);
        closeBtn.onClick.AddListener(() => galleryPanel.SetActive(false));

        var closeTxt = new GameObject("Txt");
        closeTxt.transform.SetParent(closeGo.transform, false);
        var ct = closeTxt.AddComponent<Text>();
        ct.text = "\u9589\u3058\u308b"; ct.fontSize = 28; ct.color = Color.white;
        ct.alignment = TextAnchor.MiddleCenter;
        ct.font = UIFont.Main; ct.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(closeTxt, new Color(0f, 0f, 0f, 0.5f), new Vector2(1f, -1f));
        var ctrt = closeTxt.GetComponent<RectTransform>();
        ctrt.anchorMin = Vector2.zero; ctrt.anchorMax = Vector2.one;
        ctrt.offsetMin = ctrt.offsetMax = Vector2.zero;

        galleryPanel.SetActive(false);
    }

    GameObject MakeGalleryNavButton(Transform parent, string label, Vector2 anchor)
    {
        var go = new GameObject(label + "NavBtn");
        go.transform.SetParent(parent, false);
        // パープルティント付きナビボタン
        var navImg = go.AddComponent<Image>(); navImg.color = new Color(0.15f, 0.1f, 0.3f, 0.75f); UISprites.Button(navImg);
        go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(80f, 120f);

        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 40; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return go;
    }

    void OpenGallery(StageData sd)
    {
        // 覚醒済みチェック
        bool awakened = !string.IsNullOrEmpty(sd.characterName) && OrbManager.IsAwakened(sd.characterName);

        // カタカナ名→ローマ字名に変換してリソース読み込み
        string romajiName = sd.characterName;
        if (KanaToRomaji.ContainsKey(sd.characterName))
            romajiName = KanaToRomaji[sd.characterName];
        Sprite awakenSprite = Resources.Load<Sprite>($"Characters/Awaken/{romajiName}");
        if (awakenSprite == null) awakenSprite = sd.illustSpriteFull; // フォールバック（未覚醒でもロック表示用に使う）

        // 裏ステージクリア済みなら裏イラストもギャラリーに追加
        bool trueCleared = sd.hasTrueStage && ProgressManager.IsTrueStageClear(sd.stageNumber);

        if (trueCleared)
        {
            gallerySprites = new Sprite[]
            {
                sd.illustSprite0,
                sd.illustSprite30,
                sd.illustSprite60,
                sd.illustSpriteFull,
                sd.trueIllustSprite0,
                sd.trueIllustSprite30,
                sd.trueIllustSprite60,
                sd.trueIllustSpriteFull,
                awakenSprite
            };
            galleryLocked = new bool[] { false, false, false, false, false, false, false, false, !awakened };
            gallerySpriteLabels = new[] { "0%", "30%", "60%", "100%", "裏0%", "裏30%", "裏60%", "裏100%", "✦覚醒" };
        }
        else
        {
            gallerySprites = new Sprite[]
            {
                sd.illustSprite0,
                sd.illustSprite30,
                sd.illustSprite60,
                sd.illustSpriteFull,
                awakenSprite
            };
            galleryLocked = new bool[] { false, false, false, false, !awakened };
            gallerySpriteLabels = new[] { "0%", "30%", "60%", "100%", "✦覚醒" };
        }

        // 最初に表示可能な画像を探す
        galleryIndex = 0;
        for (int i = 0; i < gallerySprites.Length; i++)
        {
            if (gallerySprites[i] != null) { galleryIndex = i; break; }
        }

        string charName = !string.IsNullOrEmpty(sd.characterName) ? sd.characterName : "???";
        UpdateGalleryDisplay(charName);
        galleryPanel.SetActive(true);
    }

    void GalleryNav(int dir)
    {
        int total = gallerySprites.Length;
        // 次の有効なスプライトを探す（ロック画像も表示可能なのでスキップしない）
        for (int i = 1; i <= total; i++)
        {
            int next = (galleryIndex + dir * i + total) % total;
            if (gallerySprites[next] != null)
            {
                galleryIndex = next;
                UpdateGalleryDisplay(galleryLabel.text.Split(' ')[0]); // キャラ名を維持
                return;
            }
        }
    }

    void UpdateGalleryDisplay(string charName)
    {
        bool isLocked = galleryLocked != null && galleryIndex < galleryLocked.Length && galleryLocked[galleryIndex];

        if (gallerySprites[galleryIndex] != null)
        {
            galleryImage.gameObject.SetActive(true);
            galleryImage.color = Color.white;
            galleryImage.material = null;

            if (isLocked)
            {
                // CPU側でモザイク処理したスプライトを生成
                galleryImage.sprite = CreateMosaicSprite(gallerySprites[galleryIndex], 48);
            }
            else
            {
                galleryImage.sprite = gallerySprites[galleryIndex];
            }
        }
        else
        {
            galleryImage.gameObject.SetActive(false);
        }

        // ロックテキスト表示/非表示
        if (galleryLockText != null)
            galleryLockText.gameObject.SetActive(isLocked);

        galleryLabel.text = $"{charName} {gallerySpriteLabels[galleryIndex]}";
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

    void MakeDecoBar(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color col)
    {
        var go = new GameObject("DecoBar");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void SpawnParticles(Transform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Particle{i}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            float hue = Random.Range(0.6f, 0.85f);
            img.color = Color.HSVToRGB(hue, 0.3f, 1f) * new Color(1, 1, 1, 0.15f);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = new Vector2(Random.Range(0f, 1080f), Random.Range(0f, 1920f));
            float sz = Random.Range(4f, 10f);
            rt.sizeDelta = new Vector2(sz, sz);
            particles.Add(rt);
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
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    Text MakeCellText(Transform parent, string txt, int size, Color col, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = UIFont.Main; t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    void MakeLine(Transform parent, Vector2 anchor, Vector2 sizeDelta, Color col)
    {
        var go = new GameObject("DecoLine");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
    }

    void MakeStyledButton(Transform parent, string label, Color bgCol, Color frameCol,
        Vector2 anchor, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        // 外枠フレーム
        var frameGo = new GameObject(label + "BtnFrame");
        frameGo.transform.SetParent(parent, false);
        var frameImg = frameGo.AddComponent<Image>(); frameImg.color = frameCol; UISprites.Button(frameImg);
        var frt = frameGo.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = anchor;
        frt.anchoredPosition = Vector2.zero;
        frt.sizeDelta = sizeDelta + new Vector2(6f, 6f);

        // 内側背景
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(frameGo.transform, false);
        var btnImg = go.AddComponent<Image>(); btnImg.color = bgCol; UISprites.Button(btnImg);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(3f, 3f);
        rt.offsetMax = new Vector2(-3f, -3f);
        btn.onClick.AddListener(onClick);

        // 上部シャイン
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(go.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
        shineGo.GetComponent<Image>().raycastTarget = false;
        var shRT = shineGo.GetComponent<RectTransform>();
        shRT.anchorMin = new Vector2(0f, 0.5f);
        shRT.anchorMax = new Vector2(1f, 1f);
        shRT.offsetMin = shRT.offsetMax = Vector2.zero;

        // ラベルテキスト
        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 36; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");
        t.font = cherry != null ? cherry : UIFont.Main;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        AddShadow(txtGo, new Color(0f, 0f, 0f, 0.5f), new Vector2(2f, -2f));
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    void AddShadow(GameObject go, Color col, Vector2 offset)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = col;
        s.effectDistance = offset;
    }

    /// <summary>
    /// スプライトをCPU側でモザイク化した新しいスプライトを生成する
    /// pixelCount: モザイクの粗さ（小さいほど粗い）
    /// </summary>
    Sprite CreateMosaicSprite(Sprite src, int pixelCount)
    {
        if (src == null || src.texture == null) return src;

        var srcTex = src.texture;

        // 読み取り可能なコピーを作成
        var rt = RenderTexture.GetTemporary(srcTex.width, srcTex.height);
        Graphics.Blit(srcTex, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var readable = new Texture2D(srcTex.width, srcTex.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, srcTex.width, srcTex.height), 0, 0);
        readable.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        int w = readable.width;
        int h = readable.height;
        int blockW = Mathf.Max(1, w / pixelCount);
        int blockH = Mathf.Max(1, h / pixelCount);

        var pixels = readable.GetPixels();
        var result = new Color[pixels.Length];

        // ブロックごとに平均色を計算
        for (int by = 0; by < h; by += blockH)
        {
            for (int bx = 0; bx < w; bx += blockW)
            {
                float r = 0, g = 0, b = 0, a = 0;
                int count = 0;
                for (int dy = 0; dy < blockH && by + dy < h; dy++)
                {
                    for (int dx = 0; dx < blockW && bx + dx < w; dx++)
                    {
                        var c = pixels[(by + dy) * w + (bx + dx)];
                        r += c.r; g += c.g; b += c.b; a += c.a;
                        count++;
                    }
                }
                Color avg = new Color(r / count, g / count, b / count, a / count);
                for (int dy = 0; dy < blockH && by + dy < h; dy++)
                    for (int dx = 0; dx < blockW && bx + dx < w; dx++)
                        result[(by + dy) * w + (bx + dx)] = avg;
            }
        }

        var mosaicTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        mosaicTex.SetPixels(result);
        mosaicTex.Apply();
        mosaicTex.filterMode = FilterMode.Point; // ドット感を出す

        return Sprite.Create(mosaicTex, new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f), src.pixelsPerUnit);
    }

    // ============================================================
    // ボイス再生機能（覚醒済キャラのみ解放）
    // ============================================================

    GameObject voicePopupPanel;
    // ボイスポップアップ用のイラスト切替状態
    Sprite[] voicePopupSprites;
    string[] voicePopupLabels;
    bool[]   voicePopupLocked;
    int      voicePopupIndex;
    Image    voicePopupIllustImg;
    Text     voicePopupIllustLabel;

    /// <summary>セル内のボイス再生ボタン（外枠+内側+シャイン+影）</summary>
    void MakeVoiceButton(Transform parent, string label,
        Color baseCol, Color highlightCol, Vector2 anchor, Vector2 sizeDelta,
        Font cherry, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("VoiceBtn");
        go.transform.SetParent(parent, false);
        var outerImg = go.AddComponent<Image>();
        outerImg.color = new Color(highlightCol.r, highlightCol.g, highlightCol.b, 0.7f); UISprites.Button(outerImg);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        btn.onClick.AddListener(onClick);

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        var innerImg = innerGo.AddComponent<Image>(); innerImg.color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.95f); UISprites.Button(innerImg);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2.5f, 2.5f); innerRt.offsetMax = new Vector2(-2.5f, -2.5f);

        // 上半分シャイン
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(innerGo.transform, false);
        shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
        var shineRt = shineGo.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f); shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        // ラベル
        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.fontSize = 30; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = cherry != null ? cherry : UIFont.Main;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var sh = txtGo.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
        sh.effectDistance = new Vector2(2f, -2f);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
    }

    /// <summary>ボイスポップアップ用のイラストリスト（0%/30%/60%/100%＋裏＋覚醒）を構築</summary>
    void BuildVoicePopupIllustList(StageData sd)
    {
        // カタカナ名→ローマ字に変換して覚醒イラスト読み込み
        string romajiName = sd.characterName;
        if (KanaToRomaji.ContainsKey(sd.characterName))
            romajiName = KanaToRomaji[sd.characterName];
        Sprite awakenSprite = Resources.Load<Sprite>($"Characters/Awaken/{romajiName}");

        bool awakened = !string.IsNullOrEmpty(sd.characterName) && OrbManager.IsAwakened(sd.characterName);
        bool trueCleared = sd.hasTrueStage && ProgressManager.IsTrueStageClear(sd.stageNumber);

        if (trueCleared)
        {
            voicePopupSprites = new Sprite[]
            {
                sd.illustSprite0,
                sd.illustSprite30,
                sd.illustSprite60,
                sd.illustSpriteFull,
                sd.trueIllustSprite0,
                sd.trueIllustSprite30,
                sd.trueIllustSprite60,
                sd.trueIllustSpriteFull,
                awakenSprite,
            };
            voicePopupLabels = new[]
            {
                "0%", "30%", "60%", "100%",
                "裏 0%", "裏 30%", "裏 60%", "裏 100%",
                "✦ 覚醒",
            };
            voicePopupLocked = new[]
            {
                false, false, false, false,
                false, false, false, false,
                !awakened,
            };
        }
        else
        {
            voicePopupSprites = new Sprite[]
            {
                sd.illustSprite0,
                sd.illustSprite30,
                sd.illustSprite60,
                sd.illustSpriteFull,
                awakenSprite,
            };
            voicePopupLabels = new[] { "0%", "30%", "60%", "100%", "✦ 覚醒" };
            voicePopupLocked = new[] { false, false, false, false, !awakened };
        }

        // 初期表示は最初の有効スプライト（通常 100% を優先）
        voicePopupIndex = 0;
        // 100% を初期表示にしたいので index 3（fullsprite）から探す
        for (int j = 3; j >= 0; j--)
        {
            if (voicePopupSprites[j] != null)
            {
                voicePopupIndex = j;
                break;
            }
        }
    }

    /// <summary>イラスト切替（左右ボタン）</summary>
    void VoicePopupNav(int dir)
    {
        if (voicePopupSprites == null || voicePopupSprites.Length == 0) return;
        int total = voicePopupSprites.Length;
        for (int i = 1; i <= total; i++)
        {
            int next = (voicePopupIndex + dir * i + total) % total;
            if (voicePopupSprites[next] != null)
            {
                voicePopupIndex = next;
                UpdateVoicePopupIllust();
                return;
            }
        }
    }

    /// <summary>イラスト切替の初回表示（キャラ名引数は廃止、ラベルは種別のみ表示）</summary>
    void UpdateVoicePopupIllust(string _ = null)
    {
        if (voicePopupSprites == null || voicePopupIllustImg == null) return;
        if (voicePopupIndex < 0 || voicePopupIndex >= voicePopupSprites.Length) return;

        var sprite = voicePopupSprites[voicePopupIndex];
        bool isLocked = voicePopupLocked != null
                        && voicePopupIndex < voicePopupLocked.Length
                        && voicePopupLocked[voicePopupIndex];

        if (sprite != null)
        {
            // ロック状態（覚醒未達）はモザイク
            voicePopupIllustImg.sprite = isLocked ? CreateMosaicSprite(sprite, 48) : sprite;
            voicePopupIllustImg.color = Color.white;
        }
        else
        {
            voicePopupIllustImg.sprite = null;
            voicePopupIllustImg.color = new Color(0.2f, 0.2f, 0.25f);
        }

        if (voicePopupIllustLabel != null)
        {
            // キャラ名は付けず、種別ラベルのみを表示（例：「0%」「裏 0%」「✦ 覚醒」）
            string lockSuffix = isLocked ? " 🔒" : "";
            voicePopupIllustLabel.text = $"{voicePopupLabels[voicePopupIndex]}{lockSuffix}";
        }
    }

    /// <summary>キャラのボイス再生ポップアップを開く（イラスト＋ボイス選択）</summary>
    void OpenVoicePopup(StageData sd)
    {
        if (sd == null) return;

        // 該当キャラのCharacterDataを取得
        var allChars = Resources.LoadAll<CharacterData>("Characters");
        CharacterData cd = null;
        foreach (var c in allChars)
        {
            if (c != null && c.characterName == sd.characterName) { cd = c; break; }
        }
        if (cd == null) return;

        // 既存パネル破棄
        if (voicePopupPanel != null) Destroy(voicePopupPanel);

        BuildVoicePopup(cd, sd);
    }

    void BuildVoicePopup(CharacterData cd, StageData sd)
    {
        var cherry = Resources.Load<Font>("Fonts/CherryBombOne-Regular");

        // ===== 0. 表示用イラストリストを構築 =====
        BuildVoicePopupIllustList(sd);

        voicePopupPanel = new GameObject("VoicePopup");
        voicePopupPanel.transform.SetParent(canvasRoot, false);

        // 全画面オーバーレイ（黒）
        var overlayImg = voicePopupPanel.AddComponent<Image>();
        overlayImg.color = Color.black;
        var rt = voicePopupPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // ===== 1. イラスト表示エリア =====
        var illustGo = new GameObject("Illust");
        illustGo.transform.SetParent(voicePopupPanel.transform, false);
        voicePopupIllustImg = illustGo.AddComponent<Image>();
        voicePopupIllustImg.preserveAspect = true;
        voicePopupIllustImg.color = Color.white;
        voicePopupIllustImg.raycastTarget = false;
        var irt = illustGo.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0f, 0.42f);
        irt.anchorMax = new Vector2(1f, 1.0f);
        irt.offsetMin = new Vector2(120f, 20f);   // 左右にナビボタン用余白
        irt.offsetMax = new Vector2(-120f, -200f); // 上部にタイトル+ラベル分のスペース確保

        // ===== 2. タイトル（最上部） =====
        var titleT = MakeCellText(voicePopupPanel.transform,
            $"♪ {cd.characterName} のボイス ♪", 56,
            new Color(1f, 0.85f, 0.3f),
            new Vector2(0.5f, 0.96f), new Vector2(900f, 80f));
        if (cherry != null) titleT.font = cherry;
        titleT.raycastTarget = false;
        var titleShadow = titleT.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0.6f, 0.1f, 0.3f, 0.8f);
        titleShadow.effectDistance = new Vector2(3f, -3f);
        var titleOutline = titleT.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        titleOutline.effectDistance = new Vector2(2f, -2f);

        // ===== 2b. イラストラベル（"0%" / "裏 0%" / "✦ 覚醒" などをイラストの上に表示） =====
        var labelGo = new GameObject("IllustLabel");
        labelGo.transform.SetParent(voicePopupPanel.transform, false);
        voicePopupIllustLabel = labelGo.AddComponent<Text>();
        voicePopupIllustLabel.fontSize = 48;
        voicePopupIllustLabel.color = new Color(1f, 0.95f, 0.5f, 1f);
        voicePopupIllustLabel.alignment = TextAnchor.MiddleCenter;
        voicePopupIllustLabel.font = cherry != null ? cherry : UIFont.Main;
        voicePopupIllustLabel.raycastTarget = false;
        voicePopupIllustLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        voicePopupIllustLabel.verticalOverflow = VerticalWrapMode.Overflow;
        var labelShadow = labelGo.AddComponent<Shadow>();
        labelShadow.effectColor = new Color(0.4f, 0.1f, 0.2f, 0.9f);
        labelShadow.effectDistance = new Vector2(2f, -2f);
        var labelOutline = labelGo.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        labelOutline.effectDistance = new Vector2(2f, -2f);
        var lrt = labelGo.GetComponent<RectTransform>();
        // イラスト上部（タイトル直下）
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.91f);
        lrt.anchoredPosition = Vector2.zero;
        lrt.sizeDelta = new Vector2(800f, 60f);

        // ===== 2c. イラスト切替矢印（左右） =====
        // 左矢印
        var leftBtnGo = new GameObject("PrevBtn");
        leftBtnGo.transform.SetParent(voicePopupPanel.transform, false);
        leftBtnGo.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.4f, 0.8f);
        var leftBtn = leftBtnGo.AddComponent<Button>();
        leftBtn.onClick.AddListener(() => VoicePopupNav(-1));
        var lbrt = leftBtnGo.GetComponent<RectTransform>();
        lbrt.anchorMin = lbrt.anchorMax = new Vector2(0.06f, 0.71f);
        lbrt.anchoredPosition = Vector2.zero;
        lbrt.sizeDelta = new Vector2(100f, 100f);
        var leftTxt = MakeCellText(leftBtnGo.transform, "◁", 60, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(100f, 100f));
        if (cherry != null) leftTxt.font = cherry;
        leftTxt.raycastTarget = false;

        // 右矢印
        var rightBtnGo = new GameObject("NextBtn");
        rightBtnGo.transform.SetParent(voicePopupPanel.transform, false);
        rightBtnGo.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.4f, 0.8f);
        var rightBtn = rightBtnGo.AddComponent<Button>();
        rightBtn.onClick.AddListener(() => VoicePopupNav(1));
        var rbrt = rightBtnGo.GetComponent<RectTransform>();
        rbrt.anchorMin = rbrt.anchorMax = new Vector2(0.94f, 0.71f);
        rbrt.anchoredPosition = Vector2.zero;
        rbrt.sizeDelta = new Vector2(100f, 100f);
        var rightTxt = MakeCellText(rightBtnGo.transform, "▷", 60, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(100f, 100f));
        if (cherry != null) rightTxt.font = cherry;
        rightTxt.raycastTarget = false;

        // 初回表示
        UpdateVoicePopupIllust(cd.characterName);

        // ===== 3. ボイスリストパネル（画面下部 40%） =====
        var listPanel = new GameObject("VoiceListPanel");
        listPanel.transform.SetParent(voicePopupPanel.transform, false);
        listPanel.AddComponent<Image>().color = new Color(0.05f, 0.03f, 0.12f, 0.92f);
        var lpRt = listPanel.GetComponent<RectTransform>();
        lpRt.anchorMin = new Vector2(0f, 0f);
        lpRt.anchorMax = new Vector2(1f, 0.40f);
        lpRt.offsetMin = lpRt.offsetMax = Vector2.zero;

        // タップ吸収（背景に伝わらないように Button 付与・空 onClick）
        var listPanelBtn = listPanel.AddComponent<Button>();
        listPanelBtn.transition = Selectable.Transition.None;

        // 上端カラーライン（キャラカラーで装飾）
        var lineGo = new GameObject("TopLine");
        lineGo.transform.SetParent(listPanel.transform, false);
        lineGo.AddComponent<Image>().color = sd.illustColorFull;
        var lRt = lineGo.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 1f);
        lRt.anchorMax = new Vector2(1f, 1f);
        lRt.pivot = new Vector2(0.5f, 1f);
        lRt.anchoredPosition = Vector2.zero;
        lRt.sizeDelta = new Vector2(0f, 6f);

        // ===== 3b. 「ボイス再生」見出し =====
        var headingT = MakeCellText(listPanel.transform, "♪ ボイス再生 ♪", 40,
            new Color(1f, 0.95f, 0.5f),
            new Vector2(0.5f, 0.93f), new Vector2(800f, 60f));
        if (cherry != null) headingT.font = cherry;
        headingT.raycastTarget = false;
        var headingShadow = headingT.gameObject.AddComponent<Shadow>();
        headingShadow.effectColor = new Color(0.4f, 0.1f, 0.2f, 0.9f);
        headingShadow.effectDistance = new Vector2(2f, -2f);
        var headingOutline = headingT.gameObject.AddComponent<Outline>();
        headingOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        headingOutline.effectDistance = new Vector2(2f, -2f);

        // ===== 4. ボイスボタンを 2列×5行 のグリッドで配置 =====
        var voiceList = new (string label, AudioClip clip)[]
        {
            ("タイトル",       cd.voiceTitle),
            ("選択時",         cd.voiceSelect),
            ("奥義発動",       cd.voiceUlt),
            ("ステージ開始",   cd.voiceStageStart),
            ("HP30%減",        cd.voiceDestroy30),
            ("HP60%減",        cd.voiceDestroy60),
            ("被弾",           cd.voiceBossDamaged),
            ("攻撃",           cd.voiceBossAttack),
            ("敗北時",         cd.voiceVictory),
            ("勝利時",         cd.voiceDefeat),
        };

        // 2列5行：左列は anchorX=0.27、右列は anchorX=0.73
        // 見出しを上部に配置するため topY を下げる
        float[] colX = { 0.27f, 0.73f };
        float topY = 0.73f;
        float stepY = 0.145f;

        for (int i = 0; i < voiceList.Length; i++)
        {
            int col = i % 2;
            int row = i / 2;
            float x = colX[col];
            float y = topY - row * stepY;

            string label = voiceList[i].label;
            AudioClip clip = voiceList[i].clip;
            bool hasClip = clip != null;

            var btnGo = new GameObject($"Voice_{i}");
            btnGo.transform.SetParent(listPanel.transform, false);
            var btnOuterImg = btnGo.AddComponent<Image>();
            btnOuterImg.color = hasClip
                ? new Color(0.95f, 0.4f, 0.65f, 0.7f)   // 解放：明るい縁
                : new Color(0.3f, 0.3f, 0.35f, 0.6f);   // 未収録：グレー縁
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(x, y);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(450f, 100f);

            // 内側
            var inner = new GameObject("Inner");
            inner.transform.SetParent(btnGo.transform, false);
            inner.AddComponent<Image>().color = hasClip
                ? new Color(0.7f, 0.15f, 0.4f, 0.95f)
                : new Color(0.18f, 0.18f, 0.22f, 0.85f);
            var iRt = inner.GetComponent<RectTransform>();
            iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
            iRt.offsetMin = new Vector2(3f, 3f); iRt.offsetMax = new Vector2(-3f, -3f);

            // シャイン
            var shineGo = new GameObject("Shine");
            shineGo.transform.SetParent(inner.transform, false);
            shineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var shRt = shineGo.GetComponent<RectTransform>();
            shRt.anchorMin = new Vector2(0f, 0.5f); shRt.anchorMax = Vector2.one;
            shRt.offsetMin = shRt.offsetMax = Vector2.zero;

            // ▶アイコン（大きく）
            var iconT = MakeCellText(btnGo.transform,
                hasClip ? "▶" : "🔒", 44,
                hasClip ? Color.white : new Color(0.55f, 0.55f, 0.6f),
                new Vector2(0.13f, 0.5f), new Vector2(70f, 70f));
            if (cherry != null) iconT.font = cherry;
            iconT.raycastTarget = false;

            // ラベル（大きく見やすく）
            var btnT = MakeCellText(btnGo.transform,
                hasClip ? label : $"{label}\n(未収録)",
                hasClip ? 38 : 30,
                hasClip ? Color.white : new Color(0.55f, 0.55f, 0.6f),
                new Vector2(0.58f, 0.5f), new Vector2(340f, 90f));
            if (cherry != null) btnT.font = cherry;
            btnT.raycastTarget = false;
            btnT.horizontalOverflow = HorizontalWrapMode.Wrap;
            btnT.verticalOverflow = VerticalWrapMode.Overflow;
            if (hasClip)
            {
                var btnTOutline = btnT.gameObject.AddComponent<Outline>();
                btnTOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
                btnTOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            // 全体ボタン化
            if (hasClip)
            {
                var playBtn = btnGo.AddComponent<Button>();
                CharacterData capturedCd = cd;
                AudioClip capturedClip = clip;
                playBtn.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayVoice(capturedClip,
                        capturedCd.voiceVolumeMultiplier,
                        AudioManager.VoicePriority.High);
                });
            }
        }

        // ===== 5. 閉じるボタン（右上） =====
        var closeGo = new GameObject("Close");
        closeGo.transform.SetParent(voicePopupPanel.transform, false);
        closeGo.AddComponent<Image>().color = new Color(0.5f, 0.15f, 0.25f, 0.95f);
        var closeBtn = closeGo.AddComponent<Button>();
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.92f, 0.94f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(120f, 100f);
        closeBtn.onClick.AddListener(() => { if (voicePopupPanel != null) Destroy(voicePopupPanel); });

        var closeT = MakeCellText(closeGo.transform, "✕", 56, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(120f, 100f));
        if (cherry != null) closeT.font = cherry;
        closeT.raycastTarget = false;
        var closeOutline = closeT.gameObject.AddComponent<Outline>();
        closeOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        closeOutline.effectDistance = new Vector2(2f, -2f);
    }
}
