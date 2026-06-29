public static class AdsPlacement
{
    public const string DailyReward = "daily_reward";
    public const string BuyCurrency = "buy_currency";
    public const string LuckySpin = "lucky_spin";
    public const string VideoBonuses = "video_bonuses";
    public const string DailyShopRefresh = "daily_shop_refresh";

    // Shop rewarded ads — placement = ShopPrefix + packName
    public const string ShopPrefix = "shop_";

    public static string Shop(string packName)
    {
        return ShopPrefix + packName;
    }
}