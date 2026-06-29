using System.Collections.Generic;
using AppsFlyerSDK;
using Cysharp.Threading.Tasks;
using Ezg.Feature.Shared;
using Ezg.Package.AdsManager;
using Ezg.Feature.Shared.GameData;

/// <summary>
///     Implementation phía game của <see cref="IAdsTracker" />: map các sự kiện vòng đời quảng cáo
///     của module Ads sang hệ analytics riêng của dự án (Firebase + AppsFlyer).
///     Đây là code GAME-SPECIFIC nên nằm NGOÀI module AdsManager (không đi theo package).
/// </summary>
public class GameAdsTracker : IAdsTracker
{
    #region Public Methods

    public void OnAdsInitialized()
    {
    }

    public void OnRewardClick(string source)
    {
        FirebaseEvent.ad_click.Send(new FirebaseEventConfig
        {
            sourceId = source
        });
    }

    public void OnRewardShow(string source)
    {
    }

    public void OnRewardLoadFailed(string source)
    {
    }

    public void OnRewardDisplayed(string source)
    {
    }

    public void OnRewardCompleted(string source)
    {
    }

    public void OnAdRevenuePaid(AdRevenueInfo info)
    {
        LogAppsFlyerRevenue(info);

        if (info.Format == AdFormat.Rewarded) SendRewardRevenueEvents(info);
    }

    #endregion

    #region Private Methods

    private void LogAppsFlyerRevenue(AdRevenueInfo info)
    {
        var additionalParams = new Dictionary<string, string>();
        additionalParams.Add(AdRevenueScheme.COUNTRY, info.CountryCode);
        additionalParams.Add(AdRevenueScheme.AD_UNIT, info.AdUnitIdentifier);
        additionalParams.Add(AdRevenueScheme.AD_TYPE, ToAppsFlyerAdType(info.Format));
        additionalParams.Add(AdRevenueScheme.PLACEMENT, info.Placement);

        var logRevenue = new AFAdRevenueData("monetizationNetworkEx", MediationNetwork.ApplovinMax, info.Currency,
            info.Revenue);
        AppsFlyer.logAdRevenue(logRevenue, additionalParams);
    }

    private void SendRewardRevenueEvents(AdRevenueInfo info)
    {
        AppFlyerEvent.af_ad_complete.Send(new AppflyerEventConfig
        {
            player_id = PlayerDataManager.Account.AccountId,
            ads_type = "reward_ads",
            ads_network = info.NetworkName,
            ad_location = info.Source,
            revenue = info.Revenue.ToString("F20"),
            currency = info.Currency
        });

        FirebaseEvent.ad_impression.Send(new FirebaseEventConfig
        {
            ad_platform = info.AdPlatform,
            ad_source = info.NetworkName,
            ad_unit_name = info.AdUnitIdentifier,
            ad_unit_id = info.AdUnitId,
            ad_format = info.AdFormatLabel,
            placement = string.IsNullOrEmpty(info.Placement) ? info.Source : info.Placement,
            value = info.Revenue,
            currency = info.Currency
        }).Forget();
    }

    private static string ToAppsFlyerAdType(AdFormat format)
    {
        switch (format)
        {
            case AdFormat.Rewarded: return "RewardAds";
            case AdFormat.Interstitial: return "Interstitial";
            case AdFormat.Banner: return "Banner";
            default: return format.ToString();
        }
    }

    #endregion
}