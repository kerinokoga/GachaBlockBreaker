using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI共通の9スライススプライト提供。
/// 白ベース素材なので Image.color の乗算でそのまま既存の配色が活きる。
/// 素材が見つからない場合は何もしない（従来の単色矩形のまま）。
/// </summary>
public static class UISprites
{
    static Sprite button;
    static Sprite panel;
    static bool loaded;

    static void LoadOnce()
    {
        if (loaded) return;
        loaded = true;
        button = Resources.Load<Sprite>("UI/btn_round");
        panel  = Resources.Load<Sprite>("UI/panel_round");
        if (button == null) Debug.LogWarning("[UISprites] UI/btn_round が読み込めません");
    }

    /// <summary>ボタン用の角丸グラデーションを適用（色は既存の img.color が乗算される）</summary>
    public static void Button(Image img)
    {
        LoadOnce();
        if (button == null || img == null) return;
        img.sprite = button;
        img.type = Image.Type.Sliced;
    }

    /// <summary>パネル・ダイアログ用の角丸を適用</summary>
    public static void Panel(Image img)
    {
        LoadOnce();
        if (panel == null || img == null) return;
        img.sprite = panel;
        img.type = Image.Type.Sliced;
    }
}
