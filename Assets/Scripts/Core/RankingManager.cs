using UnityEngine;
using System;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// ランキング（Firebase Firestore 本実装）
/// 構造: rankings/stage_{N}/entries/{uid} → { name, rate, updatedAt }
/// - 1プレイヤー1エントリ（uid をドキュメントIDに使用）
/// - 自己ベスト更新時のみ書き込み
/// - 取得は破壊率降順の上位 N 件
/// </summary>
public static class RankingManager
{
    // ローカル永続化を無効にした共有インスタンス（キャッシュ破損クラッシュ対策）
    static FirebaseFirestore Db => FirestoreProvider.Db;

    static CollectionReference StageEntries(int stage) =>
        Db.Collection("rankings").Document($"stage_{stage}").Collection("entries");

    // ---- スコア送信 ----

    /// <summary>
    /// スコアを送信（非同期・自己ベストのときだけ更新）。
    /// クリア時に呼ばれる。失敗してもゲームは継続。
    /// </summary>
    public static void SubmitScore(int stage, string playerName, float rate)
    {
        // Firebase Auth の実状態から UID を取得
        // （PlayerPrefs キャッシュだと未認証セッションでルール拒否される）
        string uid = null;
        try
        {
            var user = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
            if (user != null) uid = user.UserId;
        }
        catch { }

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[Ranking] 未ログインのため送信スキップ");
            return;
        }
        var docRef = StageEntries(stage).Document(uid);

        // 既存スコアを読み、自己ベスト更新時のみ書き込む
        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompletedSuccessfully)
            {
                Debug.LogWarning($"[Ranking] 既存スコア取得失敗: {task.Exception?.GetBaseException().Message}");
                return;
            }

            var snap = task.Result;
            if (snap.Exists && snap.TryGetValue<double>("rate", out var existing)
                && (float)existing >= rate)
            {
                return; // 既存の方が高い → 更新不要
            }

            var data = new Dictionary<string, object>
            {
                { "name",      playerName },
                { "rate",      rate },
                { "updatedAt", FieldValue.ServerTimestamp }
            };
            docRef.SetAsync(data).ContinueWithOnMainThread(t2 =>
            {
                if (t2.IsCompletedSuccessfully)
                    Debug.Log($"[Ranking] スコア送信完了: stage{stage} {playerName} {rate:P0}");
                else
                    Debug.LogWarning($"[Ranking] スコア送信失敗: {t2.Exception?.GetBaseException().Message}");
            });
        });
    }

    // ---- ランキング取得 ----

    /// <summary>
    /// 指定ステージの上位 count 件を取得（非同期）。
    /// 失敗時は空リストで onDone を呼ぶ。
    /// </summary>
    public static void GetTopRanking(int stage, int count, Action<List<RankEntry>> onDone)
    {
        StageEntries(stage)
            .OrderByDescending("rate")
            .Limit(count)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                var result = new List<RankEntry>();
                if (task.IsCompletedSuccessfully)
                {
                    foreach (var doc in task.Result.Documents)
                    {
                        string name = doc.TryGetValue<string>("name", out var n) ? n : "???";
                        float rate  = doc.TryGetValue<double>("rate", out var r) ? (float)r : 0f;
                        result.Add(new RankEntry { name = name, rate = rate, uid = doc.Id });
                    }
                }
                else
                {
                    Debug.LogWarning($"[Ranking] 取得失敗: {task.Exception?.GetBaseException().Message}");
                }
                onDone?.Invoke(result);
            });
    }

    // ============================================================
    // エンドレスモード用ランキング
    // 構造: rankings/endless/entries/{uid} → { name, score, updatedAt }
    // ============================================================

    static CollectionReference EndlessEntries() =>
        Db.Collection("rankings").Document("endless").Collection("entries");

    /// <summary>
    /// エンドレスのスコア（突破ウェーブ数）を送信。自己ベスト更新時のみ書き込み。
    /// onDone(実際に更新されたか) — 表示側で「自己ベスト更新！」を出すのに使う。
    /// </summary>
    public static void SubmitEndlessScore(string playerName, int score, Action<bool> onDone = null)
    {
        string uid = null;
        try
        {
            var user = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
            if (user != null) uid = user.UserId;
        }
        catch { }

        if (string.IsNullOrEmpty(uid) || score <= 0)
        {
            onDone?.Invoke(false);
            return;
        }
        var docRef = EndlessEntries().Document(uid);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompletedSuccessfully)
            {
                Debug.LogWarning($"[Ranking] エンドレス既存スコア取得失敗: {task.Exception?.GetBaseException().Message}");
                onDone?.Invoke(false);
                return;
            }

            var snap = task.Result;
            if (snap.Exists && snap.TryGetValue<long>("score", out var existing)
                && (int)existing >= score)
            {
                // スコアは更新不要だが、名前が変わっていればランキングの表示名だけ更新
                if (snap.TryGetValue<string>("name", out var oldName) && oldName != playerName)
                    docRef.UpdateAsync("name", playerName);
                onDone?.Invoke(false);
                return;
            }

            var data = new Dictionary<string, object>
            {
                { "name",      playerName },
                { "score",     score },
                { "updatedAt", FieldValue.ServerTimestamp }
            };
            docRef.SetAsync(data).ContinueWithOnMainThread(t2 =>
            {
                bool ok = t2.IsCompletedSuccessfully;
                if (ok) Debug.Log($"[Ranking] エンドレススコア送信完了: {playerName} {score}");
                else Debug.LogWarning($"[Ranking] エンドレススコア送信失敗: {t2.Exception?.GetBaseException().Message}");
                onDone?.Invoke(ok);
            });
        });
    }

    /// <summary>エンドレスの上位 count 件を取得（スコア降順）。失敗時は空リスト。</summary>
    public static void GetEndlessTop(int count, Action<List<EndlessEntry>> onDone)
    {
        EndlessEntries()
            .OrderByDescending("score")
            .Limit(count)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                var result = new List<EndlessEntry>();
                if (task.IsCompletedSuccessfully)
                {
                    foreach (var doc in task.Result.Documents)
                    {
                        string name = doc.TryGetValue<string>("name", out var n) ? n : "???";
                        int score   = doc.TryGetValue<long>("score", out var s) ? (int)s : 0;
                        result.Add(new EndlessEntry { name = name, score = score, uid = doc.Id });
                    }
                }
                else
                {
                    Debug.LogWarning($"[Ranking] エンドレス取得失敗: {task.Exception?.GetBaseException().Message}");
                }
                onDone?.Invoke(result);
            });
    }

    /// <summary>
    /// 自分のスコアの全国順位と全体人数を取得（Count 集計クエリ使用・読み取り課金が軽い）。
    /// onDone(順位, 総人数)。失敗時は (-1, -1)。上位% は 順位/総人数 で計算できる。
    /// </summary>
    public static void GetEndlessMyRank(int myScore, Action<int, int> onDone)
    {
        try
        {
            // 自分より高いスコアの人数 → +1 が自分の順位
            EndlessEntries().WhereGreaterThan("score", myScore).Count
                .GetSnapshotAsync(AggregateSource.Server)
                .ContinueWithOnMainThread(task =>
                {
                    if (!task.IsCompletedSuccessfully)
                    {
                        Debug.LogWarning($"[Ranking] 順位取得失敗: {task.Exception?.GetBaseException().Message}");
                        onDone?.Invoke(-1, -1);
                        return;
                    }
                    int rank = (int)task.Result.Count + 1;

                    // 総人数
                    EndlessEntries().Count
                        .GetSnapshotAsync(AggregateSource.Server)
                        .ContinueWithOnMainThread(t2 =>
                        {
                            if (!t2.IsCompletedSuccessfully)
                            {
                                Debug.LogWarning($"[Ranking] 総数取得失敗: {t2.Exception?.GetBaseException().Message}");
                                onDone?.Invoke(rank, -1);
                                return;
                            }
                            onDone?.Invoke(rank, (int)t2.Result.Count);
                        });
                });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ranking] 順位取得例外: {e.Message}");
            onDone?.Invoke(-1, -1);
        }
    }

    // ---- データ構造 ----

    [System.Serializable]
    public class RankEntry
    {
        public string name;
        public float rate;
        public string uid;  // 自分のエントリ判定用（ドキュメントID = プレイヤーUID）
    }

    [System.Serializable]
    public class EndlessEntry
    {
        public string name;
        public int score;
        public string uid;
    }
}
