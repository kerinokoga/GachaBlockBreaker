using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// モッククラウドセーブ（ローカル JSON ファイル）
/// 後から Firebase Firestore に差し替え可能
/// </summary>
public static class CloudSaveManager
{
    static string SavePath
    {
        get
        {
            string uid = AuthManager.GetUID();
            if (string.IsNullOrEmpty(uid)) uid = "default";
            return Path.Combine(Application.persistentDataPath, $"cloudsave_{uid}.json");
        }
    }

    // ---- セーブ ----

    public static void Save()
    {
        var data = new SaveData();

        // オーブ・ガチャ状態
        data.orbs       = OrbManager.GetOrbs();
        data.pityCount  = OrbManager.GetPityCount();

        // ステージ進行
        data.maxUnlocked = ProgressManager.GetMaxUnlocked();
        for (int i = 1; i <= ProgressManager.TotalStages; i++)
        {
            if (ProgressManager.IsCleared(i))
                data.clearedStages.Add(i);
            float rate = ProgressManager.GetBestRate(i);
            if (rate > 0f)
                data.bestRates[i] = rate;
        }

        // 所持キャラ
        var allChars = Resources.LoadAll<CharacterData>("Characters");
        foreach (var c in allChars)
        {
            if (OrbManager.IsOwned(c.characterName))
            {
                data.ownedChars.Add(new CharSaveEntry
                {
                    name  = c.characterName,
                    count = OrbManager.GetCharCount(c.characterName),
                    level = OrbManager.GetEnhanceLevel(c.characterName)
                });
            }
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[CloudSave] 保存完了: {SavePath}");
    }

    // ---- ロード ----

    public static bool Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning($"[CloudSave] セーブファイルなし: {SavePath}");
            return false;
        }

        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json);

        // オーブ復元
        PlayerPrefs.SetInt("GachaBlock_Orbs", data.orbs);
        PlayerPrefs.SetInt("GachaBlock_PityCount", data.pityCount);

        // ステージ進行復元
        PlayerPrefs.SetInt("GachaBlock_MaxUnlocked", data.maxUnlocked);
        for (int i = 1; i <= ProgressManager.TotalStages; i++)
        {
            PlayerPrefs.SetInt($"GachaBlock_Cleared_{i}",
                data.clearedStages.Contains(i) ? 1 : 0);
            if (data.bestRates.ContainsKey(i))
                PlayerPrefs.SetFloat($"GachaBlock_Rate_{i}", data.bestRates[i]);
        }

        // キャラ復元
        foreach (var entry in data.ownedChars)
        {
            PlayerPrefs.SetInt($"GachaBlock_Owned_{entry.name}", 1);
            PlayerPrefs.SetInt($"GachaBlock_Count_{entry.name}", entry.count);
            PlayerPrefs.SetInt($"GachaBlock_Level_{entry.name}", entry.level);
        }

        PlayerPrefs.Save();
        Debug.Log($"[CloudSave] 読込完了: {SavePath}");
        return true;
    }

    // ---- データ構造 ----

    [System.Serializable]
    class SaveData
    {
        public int orbs;
        public int pityCount;
        public int maxUnlocked = 1;
        public List<int> clearedStages = new List<int>();
        public SerializableDict bestRates = new SerializableDict();
        public List<CharSaveEntry> ownedChars = new List<CharSaveEntry>();
    }

    [System.Serializable]
    class CharSaveEntry
    {
        public string name;
        public int count;
        public int level;
    }

    // JsonUtility は Dictionary 非対応のため簡易ラッパー
    [System.Serializable]
    class SerializableDict
    {
        public List<int> keys   = new List<int>();
        public List<float> vals = new List<float>();

        public bool ContainsKey(int k) => keys.Contains(k);
        public float this[int k]
        {
            get { int i = keys.IndexOf(k); return i >= 0 ? vals[i] : 0f; }
            set
            {
                int i = keys.IndexOf(k);
                if (i >= 0) vals[i] = value;
                else { keys.Add(k); vals.Add(value); }
            }
        }
    }
}
