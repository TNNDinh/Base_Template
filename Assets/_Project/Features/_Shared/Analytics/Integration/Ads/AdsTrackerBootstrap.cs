using Ezg.Feature.Shared;
using Ezg.Package.AdsManager;
using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.GameData;

/// <summary>
///     Nối module Ads (độc lập) với code game lúc startup: inject <see cref="GameAdsTracker" />,
///     cung cấp nguồn lấy level hiện tại (cho gating), và forward banner C# event sang EventManager
///     để giữ nguyên hành vi cũ. Đây là code GAME-SPECIFIC, nằm NGOÀI module.
/// </summary>
public static class AdsTrackerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        var ads = AdsManager.Instance;
        if (ads == null) return;

        ads.Configure(new GameAdsTracker(), () => PlayerDataManager.Campaign.HighestLevel);

        ads.OnBannerLoaded += () => EventManager.EmitEvent(nameof(EventName.LoadedBanner));
        ads.OnBannerFailed += () => EventManager.EmitEvent(nameof(EventName.HideBanner));
    }
}