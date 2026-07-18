using UnityEngine;
using UnityEngine.Video;
using System.IO;

/// <summary>
/// ホーム画面きせかえ（背景キャラアニメ変更）の管理。
/// - 選択キャラと エンドレス累計撃破数 を PlayerPrefs＋クラウドに保存
/// - 解放条件: N/R=覚醒、SR=覚醒＋ステージ15クリア、SSR=覚醒＋エンドレス累計100体撃破
/// - 動画は Resources/Movies/Home/{キャラ名}.mp4（無いキャラは選択肢に「準備中」表示）
/// - 未選択またはフォールバックはデフォルト（セラ / Movies/home_bg）
/// </summary>
public static class HomeCharManager
{
    public const int SRUnlockStage = 15;      // SR の解放に必要なクリアステージ
    public const int SSRUnlockKills = 100;    // SSR の解放に必要なエンドレス累計撃破数

    const string KeySelected = "GachaBlock_HomeChar";          // 選択キャラ名（"" = デフォルト）
    const string KeyEndlessKills = "GachaBlock_EndlessTotalKills"; // エンドレス累計撃破数

    // ---- 選択キャラ ----

    public static string GetSelected() => PlayerPrefs.GetString(KeySelected, "");

    public static void SetSelected(string charName)
    {
        PlayerPrefs.SetString(KeySelected, charName ?? "");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ホーム背景に使う動画クリップを返す。
    /// 選択キャラの動画 → 無ければデフォルト（home_bg）へフォールバック。
    /// </summary>
    public static VideoClip GetHomeClip()
    {
        string sel = GetSelected();
        if (!string.IsNullOrEmpty(sel))
        {
            var clip = Resources.Load<VideoClip>($"Movies/Home/{sel}");
            if (clip != null) return clip;
        }
        return Resources.Load<VideoClip>("Movies/home_bg");
    }

    /// <summary>キャラ専用のホーム動画が存在するか（未制作キャラは「準備中」表示にする）</summary>
    public static bool HasVideo(string charName)
        => Resources.Load<VideoClip>($"Movies/Home/{charName}") != null;

    // ---- バリアントきせかえ（自己ベスト報酬の特別動画） ----
    // 選択キーには "アカリ_dance" のようにファイル名をそのまま保存する
    // （GetHomeClip は Movies/Home/{選択キー} を読むため追加処理なしで再生される）

    /// <summary>バリアント定義: 表示名・動画ファイル名・必要自己ベスト・ベースキャラ</summary>
    public struct Variant
    {
        public string baseChar;   // 所持＋覚醒チェック用のキャラ名
        public string fileName;   // 選択キー。同梱ならResources/Movies/Home/のファイル名、配信なら配信ファイル名
        public string label;      // きせかえ一覧での表示名
        public int requiredBest;  // 必要な自己ベスト（1ランの最高撃破数）
        public bool streamed;     // true = Firebase Storageから配信（初回選択時にDL＆キャッシュ）
        public string thumb;      // ギャラリー用サムネイル（Storageのgallery/内のjpg）
    }

    public static readonly Variant[] Variants =
    {
        // アプリ同梱（容量が小さいため）
        new Variant { baseChar = "アカリ", fileName = "アカリ_dance",
                      label = "アカリ（ダンス）", requiredBest = 10,
                      thumb = "akari_dance_thumb.jpg" },
        new Variant { baseChar = "アカリ", fileName = "アカリ_swim",
                      label = "アカリ（水着）", requiredBest = 20,
                      thumb = "akari_swim_thumb.jpg" },
        // 配信（アプリ容量200MB制限のためStorageからDL。ファイル名は英字）
        new Variant { baseChar = "アカリ", fileName = "akari_robe",
                      label = "アカリ（白ローブ）", requiredBest = 30, streamed = true,
                      thumb = "akari_robe_thumb.jpg" },
        new Variant { baseChar = "セラ", fileName = "sera_bunny",
                      label = "セラ（バニー）", requiredBest = 40, streamed = true,
                      thumb = "sera_bunny_thumb.jpg" },
        new Variant { baseChar = "アカリ", fileName = "akari_bikini",
                      label = "アカリ（ビキニ）", requiredBest = 50, streamed = true,
                      thumb = "akari_bikini_thumb.jpg" },
        new Variant { baseChar = "ルカ", fileName = "ruka_bunny",
                      label = "ルカ（バニー）", requiredBest = 60, streamed = true,
                      thumb = "ruka_bunny_thumb.jpg" },
        new Variant { baseChar = "ノア", fileName = "noa_dance",
                      label = "ノア（体操着）", requiredBest = 70, streamed = true,
                      thumb = "noa_dance_thumb.jpg" },
        new Variant { baseChar = "ルカ", fileName = "ruka_robe",
                      label = "ルカ（白ローブ）", requiredBest = 80, streamed = true,
                      thumb = "ruka_robe_thumb.jpg" },
        new Variant { baseChar = "リコ", fileName = "riko_bunny",
                      label = "リコ（バニー）", requiredBest = 90, streamed = true,
                      thumb = "riko_bunny_thumb.jpg" },
        new Variant { baseChar = "ナナ", fileName = "nana_bunny",
                      label = "ナナ（バニーダンス）", requiredBest = 100, streamed = true,
                      thumb = "nana_bunny_thumb.jpg" },
    };

    /// <summary>自己ベストのマイルストーンに対応するバリアントを返す</summary>
    public static bool TryGetVariantByBest(int best, out Variant found)
    {
        foreach (var v in Variants)
            if (v.requiredBest == best) { found = v; return true; }
        found = default;
        return false;
    }

    // ---- 配信バリアントのダウンロード・キャッシュ ----

    /// <summary>配信バリアント動画のローカルキャッシュパス</summary>
    public static string VariantCachePath(string fileName) =>
        Path.Combine(Application.persistentDataPath, "kisekae", fileName + ".mp4");

    public static bool IsVariantCached(string fileName) =>
        File.Exists(VariantCachePath(fileName));

    /// <summary>配信バリアント動画のダウンロードURL（ギャラリーと同じStorageのgallery/配下）</summary>
    public static string VariantUrl(string fileName) =>
        EndlessGalleryManager.StorageBase + fileName + ".mp4?alt=media";

    /// <summary>
    /// 選択中が配信バリアントでキャッシュ済みなら、そのファイルURLを返す。
    /// （VideoPlayer は VideoClip ではなく URL 再生を使う）
    /// </summary>
    public static bool TryGetHomeVideoUrl(out string url)
    {
        url = null;
        string sel = GetSelected();
        if (string.IsNullOrEmpty(sel)) return false;
        foreach (var v in Variants)
        {
            if (v.fileName == sel && v.streamed)
            {
                string path = VariantCachePath(sel);
                if (File.Exists(path)) { url = path; return true; }
                return false; // 未キャッシュ（デフォルト動画にフォールバック）
            }
        }
        return false;
    }

    /// <summary>自己ベスト（1ランの最高撃破数）</summary>
    public static int GetEndlessBest() => PlayerPrefs.GetInt("GachaBlock_EndlessBest", 0);

    /// <summary>バリアントが解放済みか（ベースキャラ覚醒＋自己ベスト到達）</summary>
    public static bool IsVariantUnlocked(Variant v)
        => OrbManager.IsAwakened(v.baseChar) && GetEndlessBest() >= v.requiredBest;

    /// <summary>バリアントの解放条件文</summary>
    public static string VariantConditionText(Variant v)
    {
        int best = GetEndlessBest();
        if (best < v.requiredBest)
            return $"覚醒＋エンドレス自己ベスト{v.requiredBest}体で解放（現在{best}体）";
        return $"覚醒＋エンドレス自己ベスト{v.requiredBest}体で解放";
    }

    // ---- 解放条件 ----

    /// <summary>そのキャラをホームに設定できるか</summary>
    public static bool IsUnlocked(CharacterData cd)
    {
        if (cd == null) return false;
        if (!OrbManager.IsAwakened(cd.characterName)) return false;

        switch (cd.rarity)
        {
            case Rarity.SR:
                return ProgressManager.IsCleared(SRUnlockStage);
            case Rarity.SSR:
                return GetEndlessKills() >= SSRUnlockKills;
            default: // N / R は覚醒のみ
                return true;
        }
    }

    /// <summary>解放条件の説明文（選択ポップアップに表示）</summary>
    public static string UnlockConditionText(CharacterData cd)
    {
        if (cd == null) return "";
        switch (cd.rarity)
        {
            case Rarity.SR:
                return $"覚醒＋ステージ{SRUnlockStage}クリアで解放";
            case Rarity.SSR:
                int kills = GetEndlessKills();
                if (kills < SSRUnlockKills)
                    return $"覚醒＋エンドレス累計{SSRUnlockKills}体撃破で解放（あと{SSRUnlockKills - kills}体）";
                return $"覚醒＋エンドレス累計{SSRUnlockKills}体撃破で解放";
            default:
                return "覚醒で解放";
        }
    }

    // ---- エンドレス累計撃破数 ----

    public static int GetEndlessKills() => PlayerPrefs.GetInt(KeyEndlessKills, 0);

    /// <summary>エンドレスのステージ突破時に加算（GameManager から呼ばれる）</summary>
    public static void AddEndlessKills(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(KeyEndlessKills, GetEndlessKills() + amount);
        PlayerPrefs.Save();
    }

    /// <summary>クラウドセーブ連携用</summary>
    public static void SetEndlessKills(int value)
        => PlayerPrefs.SetInt(KeyEndlessKills, Mathf.Max(GetEndlessKills(), value));
}
