using Firebase.Firestore;

/// <summary>
/// Firestore インスタンスの共有プロバイダ。
/// ローカル永続化（LevelDB）を無効化して提供する。
///
/// 理由: 端末内キャッシュが破損すると起動時にネイティブクラッシュ
/// （SIGSEGV）して以後起動不能になる事例が発生したため。
/// 本ゲームは進行データの正本が PlayerPrefs であり、Firestore は
/// クラウドバックアップ・ランキング送信専用のため、オフライン
/// キャッシュを失っても実害はない（オフライン時は各呼び出し側の
/// ガードでスキップされる）。
///
/// 注意: PersistenceEnabled は Firestore の最初の操作より前に
/// 設定する必要があるため、必ずこのプロバイダ経由で取得すること。
/// </summary>
public static class FirestoreProvider
{
    static FirebaseFirestore db;

    public static FirebaseFirestore Db
    {
        get
        {
            if (db == null)
            {
                db = FirebaseFirestore.DefaultInstance;
                var settings = db.Settings;
                settings.PersistenceEnabled = false;
            }
            return db;
        }
    }
}
