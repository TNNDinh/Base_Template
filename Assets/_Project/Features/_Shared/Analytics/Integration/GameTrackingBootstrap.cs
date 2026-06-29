using AppsFlyerSDK;
using Ezg.Core.Utils;
using Ezg.Feature.Firebase;
using Ezg.Feature.Shared;
using Ezg.Tracking;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

/// <summary>
///     Wires this project's player data into the generic <see cref="TrackingService" /> engine. Builds a
///     <see cref="UserPropertyConfig" /> snapshot from <c>PlayerDataManager</c> and registers it as the engine's
///     user-property provider. This is the single place that couples tracking to gameplay data — the engine
///     itself stays game-agnostic.
/// </summary>
public static class GameTrackingBootstrap
{
    /// <summary>
    ///     Registers the user-property provider. Call once Firebase is initialized (i.e. when
    ///     <c>TrackingService.IsInitFirebase</c> becomes true), before any event is sent.
    /// </summary>
    public static void Register()
    {
        TrackingService.UserPropertyProvider = BuildUserPropertyConfig;
    }

    /// <summary>
    ///     Builds a fresh <see cref="UserPropertyConfig" /> snapshot from the current player data.
    /// </summary>
    /// <returns>The populated user property config.</returns>
    private static object BuildUserPropertyConfig()
    {
        return new UserPropertyConfig
        {
            player_id = PlayerDataManager.Account.AccountId,
            appsflyer_id = AppsFlyer.getAppsFlyerId(),
            created_timestamp = PlayerDataManager.Settings.dataBase.CreatedTime,
            online_time = PlayerDataManager.Settings.dataBase.OnlineTime,
            current_level = 0, // removed: PlayerDataManager.OrderDataManager (gameplay removed)
            iap_count = PlayerDataManager.PlayerShop.dataBase.IAPCount,
            iaa_count = PlayerDataManager.PlayerShop.dataBase.IAACount,
            type_player = GameRemoteConfig.levelType,
            remaining_energy = PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.Energy),
            remaining_gem = PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.Diamonds),
            remaining_gold = PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.Gold),
            inventory_slots = 0, // removed: PlayerDataManager.Inventory (gameplay removed)
            current_task_id = null, // removed: PlayerBuildUpGoalDataManager (gameplay removed)
            order_mania_silver = 0, // removed: OrderManager (gameplay removed)
            order_mania_gold = 0, // removed: PlayerDataManager.OrderDataManager (gameplay removed)
            active_day = PlayerDataManager.LoginActivity.ActiveDayCount,
            current_event_id = null // removed: EventMergeService (gameplay removed)
        };
    }
}