using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;

/// <summary>
/// エンドレスギャラリー管理。
/// - 累計撃破 1〜100体: 1体ごとにイラスト解放（total_001.jpg 〜 total_100.jpg）
/// - 自己ベスト マイルストーン: 特別イラスト解放（best_001.jpg 等）。
///   ただし10体・20体はアカリのきせかえ動画が報酬（画像なし）
/// - 画像は Firebase Storage の公開URLから取得し、端末にキャッシュする
///   （アプリ本体の容量を消費しない。イラスト追加はStorageへのアップロードのみで可能）
/// </summary>
public static class EndlessGalleryManager
{
    // Firebase Storage（gallery/ フォルダ・公開読み取り）の配信URLベース
    public const string StorageBase =
        "https://firebasestorage.googleapis.com/v0/b/gachablockbreaker.firebasestorage.app/o/gallery%2F";

    /// <summary>
    /// 累計撃破のマイルストーン（1, 5, 10, 以降は10刻みで500まで＝計52箇所）。
    /// 到達ごとにイラストを1枚解放する
    /// </summary>
    public static readonly int[] TotalMilestones = BuildTotalMilestones();

    static int[] BuildTotalMilestones()
    {
        var list = new System.Collections.Generic.List<int> { 1, 5 };
        for (int n = 10; n <= 500; n += 10) list.Add(n);
        return list.ToArray();
    }

    /// <summary>自己ベストのマイルストーン一覧</summary>
    public static readonly int[] BestMilestones =
        { 1, 3, 5, 7, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 };

    /// <summary>
    /// 報酬がきせかえ動画のマイルストーン（画像は無い）。
    /// 10の倍数（10/20/.../100 の計10箇所）はすべてきせかえアニメ報酬。
    /// 対応する動画は HomeCharManager.Variants に順次登録する（未登録の間は準備中扱い）
    /// </summary>
    public static bool IsKisekaeMilestone(int n) => n % 10 == 0;

    // ---- 解放判定 ----

    public static int TotalKills => HomeCharManager.GetEndlessKills();
    public static int BestScore  => PlayerPrefs.GetInt("GachaBlock_EndlessBest", 0);

    public static bool IsTotalUnlocked(int n) => TotalKills >= n;
    public static bool IsBestUnlocked(int n)  => BestScore >= n;

    // ---- ファイル名・URL・キャッシュ ----

    public static string TotalFile(int n) => $"total_{n:D3}.jpg";
    public static string BestFile(int n)  => $"best_{n:D3}.jpg";

    /// <summary>一覧セル用の軽量サムネイル（幅256px・約20KB）のファイル名</summary>
    public static string ThumbFile(string file) => file.Replace(".jpg", "_thumb.jpg");

    public static string CachePath(string file) =>
        Path.Combine(Application.persistentDataPath, "gallery", file);

    public static bool IsCached(string file) => File.Exists(CachePath(file));

    public static string Url(string file) => StorageBase + file + "?alt=media";

    /// <summary>
    /// 画像を取得する。キャッシュ済みなら即時、無ければダウンロードして保存。
    /// 失敗時（オフライン等）は null を返す。コルーチンとして実行すること。
    /// </summary>
    public static IEnumerator LoadImage(string file, Action<Texture2D> onDone)
    {
        string path = CachePath(file);

        if (File.Exists(path))
        {
            byte[] cached = null;
            try { cached = File.ReadAllBytes(path); }
            catch (Exception e) { Debug.LogWarning($"[Gallery] キャッシュ読込失敗: {e.Message}"); }

            if (cached != null)
            {
                var cachedTex = new Texture2D(2, 2);
                if (cachedTex.LoadImage(cached))
                {
                    onDone?.Invoke(cachedTex);
                    yield break;
                }
                // 壊れたキャッシュは削除して再ダウンロードへ
                try { File.Delete(path); } catch { }
            }
        }

        using (var req = UnityWebRequest.Get(Url(file)))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Gallery] ダウンロード失敗 {file}: {req.error}");
                onDone?.Invoke(null);
                yield break;
            }

            byte[] data = req.downloadHandler.data;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, data);
            }
            catch (Exception e) { Debug.LogWarning($"[Gallery] キャッシュ保存失敗: {e.Message}"); }

            var tex = new Texture2D(2, 2);
            onDone?.Invoke(tex.LoadImage(data) ? tex : null);
        }
    }

    /// <summary>
    /// 汎用ファイルダウンロード（きせかえ動画等）。保存先に書き込み、成否を返す。
    /// </summary>
    public static IEnumerator DownloadFile(string url, string savePath, Action<bool> onDone)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 60;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Gallery] ファイルDL失敗 {url}: {req.error}");
                onDone?.Invoke(false);
                yield break;
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.WriteAllBytes(savePath, req.downloadHandler.data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Gallery] ファイル保存失敗: {e.Message}");
                onDone?.Invoke(false);
                yield break;
            }
            onDone?.Invoke(true);
        }
    }

    // ---- 新規解放の検知（リザルト通知用） ----

    const string KeySeenTotal = "GachaBlock_GallerySeenTotal";
    const string KeySeenBest  = "GachaBlock_GallerySeenBest";

    /// <summary>
    /// 前回確認時から新しく解放された報酬（イラスト＋きせかえ）の件数を返し、確認済みとして記録する。
    /// エンドレスのリザルト表示時に呼ぶ。
    /// newImageFiles / newKisekaeBests を渡すと、新規解放の内訳も受け取れる
    /// </summary>
    public static int ConsumeNewUnlocks(out int newKisekae,
        System.Collections.Generic.List<string> newImageFiles = null,
        System.Collections.Generic.List<int> newKisekaeBests = null)
    {
        int seenTotal = PlayerPrefs.GetInt(KeySeenTotal, 0);
        int seenBest  = PlayerPrefs.GetInt(KeySeenBest, 0);

        int count = 0;
        foreach (int m in TotalMilestones)
            if (TotalKills >= m && seenTotal < m)
            {
                count++;
                newImageFiles?.Add(TotalFile(m));
            }

        newKisekae = 0;
        foreach (int m in BestMilestones)
        {
            if (BestScore >= m && seenBest < m)
            {
                if (IsKisekaeMilestone(m))
                {
                    newKisekae++;
                    newKisekaeBests?.Add(m);
                }
                else
                {
                    count++;
                    newImageFiles?.Add(BestFile(m));
                }
            }
        }

        PlayerPrefs.SetInt(KeySeenTotal, TotalKills);
        PlayerPrefs.SetInt(KeySeenBest, BestScore);
        PlayerPrefs.Save();
        return count;
    }

    // ---- 未確認バッジ（ギャラリーを開くまで表示） ----

    const string KeyViewTotal = "GachaBlock_GalleryViewTotal";
    const string KeyViewBest  = "GachaBlock_GalleryViewBest";

    /// <summary>ギャラリーでまだ確認していない解放済み報酬の数（バッジ表示用）</summary>
    public static int UnseenRewardCount()
    {
        int vt = PlayerPrefs.GetInt(KeyViewTotal, 0);
        int vb = PlayerPrefs.GetInt(KeyViewBest, 0);
        int count = 0;
        foreach (int m in TotalMilestones)
            if (TotalKills >= m && vt < m) count++;
        foreach (int m in BestMilestones)
            if (BestScore >= m && vb < m) count++;
        return count;
    }

    /// <summary>エンドレスギャラリーを開いたときに呼ぶ（バッジを消す）</summary>
    public static void MarkRewardsSeen()
    {
        PlayerPrefs.SetInt(KeyViewTotal, TotalKills);
        PlayerPrefs.SetInt(KeyViewBest, BestScore);
        PlayerPrefs.Save();
    }
}
