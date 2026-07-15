using UnityEngine;

/// <summary>
/// UI共通フォントの提供。
/// 本文・ボタン用は M PLUS Rounded 1c Bold（丸ゴシック・サイトと同一フォント）。
/// 読み込み失敗時は OS フォント（従来の Arial 相当）にフォールバック。
/// </summary>
public static class UIFont
{
    static Font cached;

    public static Font Main
    {
        get
        {
            if (cached == null)
            {
                cached = Resources.Load<Font>("Fonts/MPLUSRounded1c-Bold");
                if (cached == null)
                {
                    Debug.LogWarning("[UIFont] MPLUSRounded1c-Bold が見つかりません。OSフォントで代替します");
                    cached = Font.CreateDynamicFontFromOSFont("Arial", 32);
                }
            }
            return cached;
        }
    }
}
