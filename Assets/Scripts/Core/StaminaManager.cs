using UnityEngine;
using System;

/// <summary>
/// スタミナ管理（PlayerPrefs ベース）
/// Max20 / 30分で1回復 / ステージ挑戦で1消費 / 100オーブで全回復
/// </summary>
public static class StaminaManager
{
    public const int MaxStamina = 20;
    public const int RecoveryMinutes = 30;
    public const int OrbCostFullRecover = 100;

    /// <summary>ステージ番号に応じたスタミナ消費量</summary>
    public static int GetCost(int stageNumber)
    {
        if (stageNumber <= 3)  return 1;
        if (stageNumber <= 5)  return 2;
        if (stageNumber <= 8)  return 3;
        if (stageNumber <= 11) return 4;
        if (stageNumber <= 15) return 5;
        return 6; // 16〜20
    }

    private static string KeyStamina => "GachaBlock_Stamina";
    private static string KeyLastTime => "GachaBlock_StaminaLastTime";
    private static bool initialized = false;

    /// <summary>初回起動時にスタミナを最大値にセット</summary>
    static void EnsureInit()
    {
        if (initialized) return;
        initialized = true;
        if (!PlayerPrefs.HasKey(KeyStamina))
        {
            PlayerPrefs.SetInt(KeyStamina, MaxStamina);
            PlayerPrefs.SetString(KeyLastTime, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
        }
    }

    /// <summary>時間経過による自然回復を反映した現在スタミナ</summary>
    public static int GetStamina()
    {
        EnsureInit();
        int stored = PlayerPrefs.GetInt(KeyStamina, MaxStamina);
        if (stored >= MaxStamina) return MaxStamina;

        string lastStr = PlayerPrefs.GetString(KeyLastTime, "");
        if (string.IsNullOrEmpty(lastStr)) return stored;

        DateTime lastTime;
        if (!DateTime.TryParse(lastStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out lastTime))
            return stored;

        double minutesElapsed = (DateTime.UtcNow - lastTime).TotalMinutes;
        int recovered = Mathf.FloorToInt((float)(minutesElapsed / RecoveryMinutes));
        if (recovered <= 0) return stored;

        int newStamina = Mathf.Min(stored + recovered, MaxStamina);
        // 保存（次回計算の起点を更新）
        PlayerPrefs.SetInt(KeyStamina, newStamina);
        // 端数の時間を残すため、回復した分だけ時刻を進める
        DateTime newBase = lastTime.AddMinutes(recovered * RecoveryMinutes);
        if (newStamina >= MaxStamina) newBase = DateTime.UtcNow;
        PlayerPrefs.SetString(KeyLastTime, newBase.ToString("o"));
        PlayerPrefs.Save();
        return newStamina;
    }

    /// <summary>次の1回復までの残り秒数</summary>
    public static int GetSecondsUntilNext()
    {
        EnsureInit();
        if (GetStamina() >= MaxStamina) return 0;

        string lastStr = PlayerPrefs.GetString(KeyLastTime, "");
        if (string.IsNullOrEmpty(lastStr)) return 0;

        DateTime lastTime;
        if (!DateTime.TryParse(lastStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out lastTime))
            return 0;

        double elapsed = (DateTime.UtcNow - lastTime).TotalSeconds;
        int totalSec = RecoveryMinutes * 60;
        int remaining = totalSec - Mathf.FloorToInt((float)(elapsed % totalSec));
        return remaining;
    }

    /// <summary>スタミナを消費（ステージ番号に応じた量）</summary>
    public static bool TryConsume(int stageNumber)
    {
        int cost = GetCost(stageNumber);
        int cur = GetStamina();
        if (cur < cost) return false;

        PlayerPrefs.SetInt(KeyStamina, cur - cost);
        if (cur >= MaxStamina)
            PlayerPrefs.SetString(KeyLastTime, DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>100オーブで全回復</summary>
    public static bool TryFullRecoverWithOrbs()
    {
        if (!OrbManager.CanAfford(OrbCostFullRecover)) return false;
        OrbManager.SpendOrbs(OrbCostFullRecover);
        PlayerPrefs.SetInt(KeyStamina, MaxStamina);
        PlayerPrefs.SetString(KeyLastTime, DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
        return true;
    }

    public static bool HasStamina(int stageNumber) => GetStamina() >= GetCost(stageNumber);
}
