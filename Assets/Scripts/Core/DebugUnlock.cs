using UnityEngine;

/// <summary>
/// デバッグ用: 全解放処理（エディタメニューとゲーム内デバッグボタンの共通実装）。
/// リリースビルドではゲーム内から呼ばれない（Development Build のみボタン表示）
/// </summary>
public static class DebugUnlock
{
    /// <summary>
    /// 全ステージクリア・全キャラ所持＋覚醒・
    /// エンドレス累計500体（ギャラリー全解放）＋自己ベスト100体（きせかえ全解放）にする
    /// </summary>
    public static void UnlockAll()
    {
        // 全ステージクリア（進行度・ベスト破壊率100%）
        for (int i = 1; i <= ProgressManager.TotalStages; i++)
            ProgressManager.SaveClear(i, 1f);

        // 全キャラ所持＋覚醒
        var allChars = Resources.LoadAll<CharacterData>("Characters");
        foreach (var cd in allChars)
        {
            if (cd == null) continue;
            string n = cd.characterName;
            PlayerPrefs.SetInt($"GachaBlock_Owned_{n}", 1);
            if (PlayerPrefs.GetInt($"GachaBlock_Count_{n}", 0) < 1)
                PlayerPrefs.SetInt($"GachaBlock_Count_{n}", 1);
            PlayerPrefs.SetInt($"GachaBlock_Awakened_{n}", 1);
        }

        // エンドレス累計500体（累計ギャラリー全マイルストーン）
        if (PlayerPrefs.GetInt("GachaBlock_EndlessTotalKills", 0) < 500)
            PlayerPrefs.SetInt("GachaBlock_EndlessTotalKills", 500);
        // 自己ベスト100体（自己ベスト報酬＋きせかえバリアント全解放）
        if (PlayerPrefs.GetInt("GachaBlock_EndlessBest", 0) < 100)
            PlayerPrefs.SetInt("GachaBlock_EndlessBest", 100);

        PlayerPrefs.Save();
        Debug.Log($"[Debug] 全解放完了: ステージ{ProgressManager.TotalStages} / キャラ{allChars.Length}体 / エンドレス累計500体・自己ベスト100体");
    }
}
