using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;
using Unity.Services.Core;

/// <summary>
/// 課金マネージャー（Unity IAP 本実装）
/// - 商品はすべて消費型（オーブ）
/// - Editor ではフェイクストアで動作（購入ダイアログが出て常に成功させられる）
/// - 実機（Google Play）では Play Console に同じ productId の商品登録が必要
/// - 受け取り検証（Receipt Validation）は実機のみ実行。
///   Play Console の公開鍵を難読化した GooglePlayTangle を使用する。
/// </summary>
public static class IAPManager
{
    // ---- 商品定義 ----
    // priceLabel はストア未接続時のフォールバック表示。
    // 接続後はストアのローカライズ価格（GetStorePrice）を優先。

    // 単価が上位ほど安くなるはしご: 1.50 → 1.40 → 1.35 → 1.30 → 1.28 → 1.25 円/オーブ
    public static readonly ProductDef[] Products = {
        new ProductDef("orb_100",  100,  "¥150",    150),
        new ProductDef("orb_500",  500,  "¥700",    700),
        new ProductDef("orb_1000", 1000, "¥1,350",  1350),
        new ProductDef("orb_3000", 3000, "¥3,900",  3900),
        new ProductDef("orb_5000", 5000, "¥6,400",  6400),
        new ProductDef("orb_8000", 8000, "¥10,000", 10000),
        // 特別商品（ShopUI では専用カードで表示され、オーブカードの一覧からは除外される）
        new ProductDef(StarterPackId, 1000, "¥650", 650),  // 初心者パック（1人1回限定）
        new ProductDef(MonthlyPassId, 400,  "¥800", 800),  // マンスリーパス（即時400＋毎日80×30日）
    };

    // ---- 特別商品 ----

    public const string StarterPackId = "starter_pack";
    public const string MonthlyPassId = "monthly_pass";
    const string KeyStarterPackBought = "GachaBlock_StarterPackBought";

    /// <summary>初心者パック購入済みか（1人1回限定の判定。クラウドセーブにも保存される）</summary>
    public static bool StarterPackBought => PlayerPrefs.GetInt(KeyStarterPackBought, 0) == 1;

    /// <summary>商品定義（UnityEngine.Purchasing.Product と名前衝突しないよう ProductDef）</summary>
    public struct ProductDef
    {
        public string id;
        public int orbAmount;
        public string priceLabel;
        public int priceYen;

        public ProductDef(string id, int orbAmount, string priceLabel, int priceYen)
        {
            this.id = id;
            this.orbAmount = orbAmount;
            this.priceLabel = priceLabel;
            this.priceYen = priceYen;
        }
    }

    // ---- 内部状態 ----

    static IStoreController controller;
    static bool initializing;
    static Action<bool, string> pendingCallback; // 進行中の購入コールバック（同時1件）

    /// <summary>ストア初期化済みで購入可能か</summary>
    public static bool IsReady => controller != null;

    // ---- 初期化 ----

    /// <summary>
    /// Unity Gaming Services + Unity IAP を初期化（冪等・非同期）。
    /// ShopUI 表示時に呼ばれる。
    /// </summary>
    public static void Initialize()
    {
        if (IsReady || initializing) return;
        initializing = true;
        InitializeAsync();
    }

    static async void InitializeAsync()
    {
        // Unity IAP は Unity Gaming Services の初期化が前提
        //（プロジェクトを Unity Cloud にリンクしておくこと）
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[IAP] UGS 初期化に失敗: {e.Message}（Editor のフェイクストアは続行できる場合があります）");
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        foreach (var p in Products)
            builder.AddProduct(p.id, ProductType.Consumable);

        UnityPurchasing.Initialize(new StoreListener(), builder);
    }

    // ---- 購入 ----

    /// <summary>
    /// 購入を開始する（非同期）。結果は onComplete(成功, メッセージ) で返る。
    /// 課金制限（年齢確認）チェックもここで行う。
    /// </summary>
    public static void Purchase(string productId, Action<bool, string> onComplete)
    {
        var def = FindProduct(productId);
        if (def == null)
        {
            Debug.LogWarning($"[IAP] 商品が見つかりません: {productId}");
            onComplete?.Invoke(false, "商品が見つかりません");
            return;
        }

        // 課金制限チェック
        if (!AgeVerificationManager.CanPurchase(def.Value.priceYen))
        {
            Debug.Log($"[IAP] 課金制限により購入できません: {productId}");
            onComplete?.Invoke(false, "月額の課金上限を超えています");
            return;
        }

        // 初心者パックは1人1回限定（ShopUI 側でも非表示にしているが二重ガード）
        if (productId == StarterPackId && StarterPackBought)
        {
            onComplete?.Invoke(false, "初心者パックは購入済みです");
            return;
        }

        if (!IsReady)
        {
            Initialize(); // 未初期化なら開始しておく
            onComplete?.Invoke(false, "ストアに接続中です。しばらくしてからお試しください");
            return;
        }

        if (pendingCallback != null)
        {
            onComplete?.Invoke(false, "別の購入処理が進行中です");
            return;
        }

        pendingCallback = onComplete;
        controller.InitiatePurchase(productId);
    }

    /// <summary>購入成立時の付与処理（ProcessPurchase から呼ばれる）</summary>
    static void GrantProduct(string productId)
    {
        var def = FindProduct(productId);
        if (def == null) return;

        // オーブ付与
        OrbManager.AddOrbs(def.Value.orbAmount);

        // 特別商品の追加効果
        if (productId == StarterPackId)
            PlayerPrefs.SetInt(KeyStarterPackBought, 1); // 以後は購入不可（クラウドにも保存）
        else if (productId == MonthlyPassId)
            MonthlyPassManager.Activate(MonthlyPassManager.PassDays);

        // 課金額を記録（年齢別の月額上限管理用）
        AgeVerificationManager.AddSpent(def.Value.priceYen);

        // 購入履歴
        string key = $"GachaBlock_IAP_{productId}";
        int count = PlayerPrefs.GetInt(key, 0);
        PlayerPrefs.SetInt(key, count + 1);
        PlayerPrefs.Save();

        Debug.Log($"[IAP] 購入完了: {productId} (+{def.Value.orbAmount} Orb) 合計購入回数: {count + 1}");
    }

    // ---- ストアリスナー ----

    class StoreListener : IDetailedStoreListener
    {
        public void OnInitialized(IStoreController c, IExtensionProvider extensions)
        {
            controller = c;
            initializing = false;
            Debug.Log("[IAP] ストア初期化完了");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
            => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            initializing = false;
            Debug.LogWarning($"[IAP] ストア初期化失敗: {error} {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string id = args.purchasedProduct.definition.id;

            // レシート検証（実機のみ。改ざん・別アプリのレシート流用を弾く）
            if (!ValidateReceipt(args.purchasedProduct))
            {
                Debug.LogWarning($"[IAP] レシート検証に失敗したため付与しません: {id}");
                var failCb = pendingCallback;
                pendingCallback = null;
                failCb?.Invoke(false, "購入の確認ができませんでした");
                // Complete を返して取引を閉じる（Pending のままだと毎回再通知されるため）
                return PurchaseProcessingResult.Complete;
            }

            GrantProduct(id);

            // アプリ起動時に届く保留購入（pendingCallback == null）でも付与だけは行われる
            var cb = pendingCallback;
            pendingCallback = null;
            cb?.Invoke(true, "購入が完了しました");

            return PurchaseProcessingResult.Complete;
        }

        /// <summary>
        /// レシートの署名を検証する。
        /// Editor / フェイクストアは検証対象外（常に true）。
        /// 検証機構を用意できない環境でも購入をブロックしないよう、例外時のみ true 側に倒す。
        /// </summary>
        static bool ValidateReceipt(Product product)
        {
#if UNITY_EDITOR
            return true;
#else
            if (product == null || string.IsNullOrEmpty(product.receipt)) return false;

            try
            {
                var validator = new CrossPlatformValidator(
                    GooglePlayTangle.Data(), null, Application.identifier);
                var results = validator.Validate(product.receipt);

                // 検証済みレシートに購入した商品IDが含まれているか確認
                foreach (var r in results)
                {
                    if (r.productID == product.definition.id)
                    {
                        Debug.Log($"[IAP] レシート検証OK: {r.productID} ({r.purchaseDate})");
                        return true;
                    }
                }
                Debug.LogWarning($"[IAP] レシート内に該当商品なし: {product.definition.id}");
                return false;
            }
            catch (IAPSecurityException e)
            {
                // 署名不正・改ざんの疑い → 付与しない
                Debug.LogWarning($"[IAP] レシート検証NG（不正の疑い）: {e.Message}");
                return false;
            }
            catch (NotImplementedException)
            {
                // 未対応ストア（Amazon 等）では検証不可 → 購入自体は通す
                Debug.Log("[IAP] このストアはレシート検証に未対応のためスキップ");
                return true;
            }
            catch (Exception e)
            {
                // 想定外の例外で正規購入を弾かないよう通す（サーバー検証は将来課題）
                Debug.LogWarning($"[IAP] レシート検証で例外: {e.Message}");
                return true;
            }
#endif
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            HandleFailure(product, reason == PurchaseFailureReason.UserCancelled
                ? "購入をキャンセルしました"
                : $"購入に失敗しました（{reason}）");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription description)
        {
            HandleFailure(product, description.reason == PurchaseFailureReason.UserCancelled
                ? "購入をキャンセルしました"
                : $"購入に失敗しました（{description.reason}）");
        }

        static void HandleFailure(Product product, string userMessage)
        {
            Debug.LogWarning($"[IAP] 購入失敗: {product?.definition?.id} → {userMessage}");
            var cb = pendingCallback;
            pendingCallback = null;
            cb?.Invoke(false, userMessage);
        }
    }

    // ---- ヘルパー ----

    static ProductDef? FindProduct(string id)
    {
        foreach (var p in Products)
            if (p.id == id) return p;
        return null;
    }

    /// <summary>
    /// ストアのローカライズ価格を返す（未接続時は定義のフォールバック表示）。
    /// </summary>
    public static string GetStorePrice(string productId)
    {
        // Editor のフェイクストアは全商品 "$0.01" を返すため、Editor では定義価格を表示
        if (!Application.isEditor && controller != null)
        {
            var sp = controller.products.WithID(productId);
            if (sp != null && !string.IsNullOrEmpty(sp.metadata.localizedPriceString))
                return sp.metadata.localizedPriceString;
        }
        var def = FindProduct(productId);
        return def?.priceLabel ?? "";
    }

    public static int GetPurchaseCount(string productId)
    {
        return PlayerPrefs.GetInt($"GachaBlock_IAP_{productId}", 0);
    }
}
