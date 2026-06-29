// Thêm event và field bên dưới.

public enum AppFlyerEvent
{
    af_first_open,
    af_ad_complete,
    af_purchase,
    af_complete_tut,
    af_pass_stage_1,
    af_pass_stage_2,
    af_pass_stage_3,
    af_pass_stage_4,
    af_pass_stage_5,
    af_ad_impression
}

public class AppflyerEventConfig
{
    public string ad_location;
    public string ads_network;
    public string ads_type;
    public string af_ad_revenue;
    public string af_content_id;
    public string af_currency;

    // IAP Purchased
    public string af_revenue;

    public string currency;

    // Ads Impression Complete
    public string player_id;
    public string revenue;
}