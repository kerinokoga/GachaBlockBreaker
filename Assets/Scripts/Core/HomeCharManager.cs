using UnityEngine;
using UnityEngine.Video;

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
