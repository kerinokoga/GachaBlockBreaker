using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// モックランキング（ローカル JSON ファイル）
/// 後から Firebase Realtime Database に差し替え可能
/// </summary>
public static class RankingManager
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "ranking.json");

    // ---- スコア送信 ----

    public static void SubmitScore(int stage, string playerName, float rate)
    {
        var data = LoadOrCreate();
        string key = stage.ToString();

        List<RankEntry> list = FindStage(data, key);
        if (list == null)
        {
            list = new List<RankEntry>();
            data.stageList.Add(new StageRanking { stageKey = key, entries = list });
        }

        // 同名プレイヤーの既存スコアを更新（より高い方を残す）
        int existing = list.FindIndex(e => e.name == playerName);
        if (existing >= 0)
        {
            if (rate > list[existing].rate)
                list[existing].rate = rate;
        }
        else
        {
            list.Add(new RankEntry { name = playerName, rate = rate });
        }

        list.Sort((a, b) => b.rate.CompareTo(a.rate));
        if (list.Count > 100) list.RemoveRange(100, list.Count - 100);

        WriteFile(data);
        Debug.Log($"[Ranking] Stage{stage} スコア送信: {playerName} = {rate:P0}");
    }

    // ---- ランキング取得 ----

    public static List<RankEntry> GetTopRanking(int stage, int count = 20)
    {
        var data = LoadOrCreate();
        var list = FindStage(data, stage.ToString());
        if (list == null) return new List<RankEntry>();
        int n = Mathf.Min(count, list.Count);
        return list.GetRange(0, n);
    }

    // ---- ダミーデータ生成 ----

    public static void GenerateDummyData()
    {
        string[] names = {
            "Alice", "Bob", "Charlie", "Diana", "Eve",
            "Frank", "Grace", "Henry", "Iris", "Jack"
        };

        var data = new RankingData();
        for (int stage = 1; stage <= 5; stage++)
        {
            var list = new List<RankEntry>();
            for (int i = 0; i < names.Length; i++)
            {
                float baseRate = 1.0f - stage * 0.05f;
                float rate = Mathf.Clamp01(baseRate - i * 0.06f + Random.Range(-0.03f, 0.03f));
                list.Add(new RankEntry { name = names[i], rate = rate });
            }
            list.Sort((a, b) => b.rate.CompareTo(a.rate));
            data.stageList.Add(new StageRanking { stageKey = stage.ToString(), entries = list });
        }
        WriteFile(data);
        Debug.Log("[Ranking] ダミーデータ生成完了");
    }

    // ---- 内部ヘルパー ----

    static List<RankEntry> FindStage(RankingData data, string key)
    {
        foreach (var sr in data.stageList)
            if (sr.stageKey == key) return sr.entries;
        return null;
    }

    static RankingData LoadOrCreate()
    {
        if (!File.Exists(FilePath))
            GenerateDummyData();

        string json = File.ReadAllText(FilePath);
        var d = JsonUtility.FromJson<RankingData>(json);
        return d ?? new RankingData();
    }

    static void WriteFile(RankingData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);
    }

    // ---- データ構造（JsonUtility 対応） ----

    [System.Serializable]
    public class RankEntry
    {
        public string name;
        public float rate;
    }

    [System.Serializable]
    class RankingData
    {
        public List<StageRanking> stageList = new List<StageRanking>();
    }

    [System.Serializable]
    class StageRanking
    {
        public string stageKey;
        public List<RankEntry> entries = new List<RankEntry>();
    }
}
