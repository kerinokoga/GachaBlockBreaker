using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 奥義発動アニメーションの設定と動画クリップの解決。
/// 動画の配置場所: Assets/Resources/Movies/Ults/
///   - キャラ専用: {キャラ名}.mp4（例: アカリ.mp4）
///   - 全キャラ共通: common.mp4（専用が無いキャラのフォールバック）
/// どちらも無ければアニメなし（従来どおり即時発動）。
/// </summary>
public static class UltAnimationManager
{
    const string KeyEnabled = "GachaBlock_UltAnimEnabled";

    /// <summary>奥義アニメを再生するか（設定・ポーズ画面から切替。既定はON）</summary>
    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(KeyEnabled, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(KeyEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>キャラ専用 → 共通 の順で奥義アニメを探す（無ければ null）</summary>
    public static VideoClip GetClipFor(CharacterData cd)
    {
        if (cd == null) return null;
        var clip = Resources.Load<VideoClip>($"Movies/Ults/{cd.characterName}");
        if (clip == null) clip = Resources.Load<VideoClip>("Movies/Ults/common");
        return clip;
    }
}
