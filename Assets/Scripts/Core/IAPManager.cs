using UnityEngine;

/// <summary>
/// モック課金マネージャー（即時成功）
/// 後から Unity IAP に差し替え可能
/// </summary>
public static class IAPManager
{
    // ---- 商品定義 ----

    public static readonly Product[] Products = {
        new Product("orb_100",  100,  "¥150"),
        new Product("orb_1000", 1000, "¥1,350"),
        new Product("orb_3000", 3000, "¥4,150"),
        new Product("orb_5000", 5000, "¥6,750"),
    };

    public struct Product
    {
        public string id;
        public int orbAmount;
        public string priceLabel;

        public Product(string id, int orbAmount, string priceLabel)
        {
            this.id = id;
            this.orbAmount = orbAmount;
            this.priceLabel = priceLabel;
        }
    }

    // ---- 購入 ----

    /// <summary>
    /// モック購入（即時成功）。戻り値: 成功=true
    /// 本番では Unity IAP の PurchaseProcessingResult に差し替え
    /// </summary>
    public static bool Purchase(string productId)
    {
        Product? product = FindProduct(productId);
        if (product == null)
        {
            Debug.LogWarning($"[IAP] 商品が見つかりません: {productId}");
            return false;
        }

        // オーブ付与
        OrbManager.AddOrbs(product.Value.orbAmount);

        // 購入履歴を PlayerPrefs に記録
        string key = $"GachaBlock_IAP_{productId}";
        int count = PlayerPrefs.GetInt(key, 0);
        PlayerPrefs.SetInt(key, count + 1);
        PlayerPrefs.Save();

        Debug.Log($"[IAP] 購入完了: {productId} (+{product.Value.orbAmount} Orb) 合計購入回数: {count + 1}");
        return true;
    }

    // ---- ヘルパー ----

    static Product? FindProduct(string id)
    {
        foreach (var p in Products)
            if (p.id == id) return p;
        return null;
    }

    public static int GetPurchaseCount(string productId)
    {
        return PlayerPrefs.GetInt($"GachaBlock_IAP_{productId}", 0);
    }
}
