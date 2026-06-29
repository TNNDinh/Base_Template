using Ezg.Core.Firebase;
using Ezg.Package.AdsManager;
using TigerForge;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Firebase
{
    /// <summary>
    ///     Lớp glue phía game cho remote config: đọc các key riêng của Merge Two qua các getter generic của
    ///     <see cref="FirebaseRemoteManager" />, map vào field game + <c>AdsManager</c>, rồi emit
    ///     <c>InitRemoteConfigSuccess</c>. Đăng ký <see cref="Apply" /> vào
    ///     <see cref="FirebaseRemoteManager.OnRemoteConfigApplied" /> trước khi gọi
    ///     <see cref="FirebaseRemoteManager.InitRemoteConfig" />. Phần này CỐ TÌNH nằm ngoài package
    ///     Ezg.Core.Firebase vì các key/field ở đây là đặc thù của game.
    /// </summary>
    public static class GameRemoteConfig
    {
        #region Public Methods

        /// <summary>
        ///     Đọc toàn bộ key game từ remote config (đã fetch xong) và áp dụng vào field game + AdsManager,
        ///     sau đó sync cheat state và emit event. Là handler của
        ///     <see cref="FirebaseRemoteManager.OnRemoteConfigApplied" />.
        /// </summary>
        public static void Apply()
        {
#if UNITY_IOS
            appVersionRemote = FirebaseRemoteManager.GetString(APP_VERSION_KEY_IOS, appVersionRemote);
            giftCodeEnable = FirebaseRemoteManager.GetBool(GIFT_CODE_ENABLE_KEY_IOS, giftCodeEnable);
#else
            appVersionRemote = FirebaseRemoteManager.GetString(APP_VERSION_KEY_ANDROID, appVersionRemote);
#endif

            var remoteConfig = AdsManager.Instance.advertisingRemoteConfig;
            if (FirebaseRemoteManager.HasKey(TIME_DELAY_SHOW_INTERSTITIAL_KEY))
                remoteConfig.TimeDelayShowInterstitialAds =
                    FirebaseRemoteManager.GetInt(TIME_DELAY_SHOW_INTERSTITIAL_KEY);
            if (FirebaseRemoteManager.HasKey(INTER_ADS_KEY))
                remoteConfig.IsShowInterstitialAds = FirebaseRemoteManager.GetBool(INTER_ADS_KEY);
            if (FirebaseRemoteManager.HasKey(BANNER_ADS_KEY))
                remoteConfig.IsShowBannerAds = FirebaseRemoteManager.GetBool(BANNER_ADS_KEY);
            if (FirebaseRemoteManager.HasKey(SHOW_INTER_FROM_LEVEL_KEY))
                remoteConfig.ShowInterstitialAdsFromLevel = FirebaseRemoteManager.GetInt(SHOW_INTER_FROM_LEVEL_KEY);
            if (FirebaseRemoteManager.HasKey(SHOW_BANNER_FROM_LEVEL_KEY))
                remoteConfig.ShowBannerAdsFromLevel = FirebaseRemoteManager.GetInt(SHOW_BANNER_FROM_LEVEL_KEY);

            if (FirebaseRemoteManager.HasKey(NUMBER_LEVEL_NO_BACK_HOME_KEY))
                numerLevelNoBackHome = FirebaseRemoteManager.GetInt(NUMBER_LEVEL_NO_BACK_HOME_KEY);
            if (FirebaseRemoteManager.HasKey(BUY_BOOSTER_ADS_KEY))
                ísBuyBoosterAds = FirebaseRemoteManager.GetBool(BUY_BOOSTER_ADS_KEY);
            if (FirebaseRemoteManager.HasKey(REVIVE_ADS_KEY))
                isReviveAds = FirebaseRemoteManager.GetBool(REVIVE_ADS_KEY);
            if (FirebaseRemoteManager.HasKey(LOSE_STREAK_CONFIG_KEY))
                loseStreakConfig = FirebaseRemoteManager.GetString(LOSE_STREAK_CONFIG_KEY);

            enableCheatDefault = FirebaseRemoteManager.GetBool(ENABLE_CHEAT_DEFAULT_KEY);
            enableCheatDefaultDevice = FirebaseRemoteManager.GetString(ENABLE_CHEAT_DEFAULT_DEVICE_KEY, string.Empty);

            GameSystems.SyncCheatStateFromRemoteConfig();
            EventManager.EmitEvent(nameof(EventName.InitRemoteConfigSuccess));
        }

        #endregion

        #region Fields

        // Remote config keys (đặc thù Merge Two)
        public const string APP_VERSION_KEY_ANDROID = "android_free_version";
        public const string APP_VERSION_KEY_IOS = "ios_free_version";
        public const string GIFT_CODE_ENABLE_KEY_ANDROID = "android_free_giftcode_enable";
        public const string GIFT_CODE_ENABLE_KEY_IOS = "ios_free_giftcode_enable";
        public const string ENABLE_CHEAT_DEFAULT_KEY = "enable_cheat_default";
        public const string ENABLE_CHEAT_DEFAULT_DEVICE_KEY = "enable_cheat_default_device";
        public const string LEVEL_TYPE_KEY = "level_type";
        public const string TIME_DELAY_SHOW_INTERSTITIAL_KEY = "time_delay_show_interstitial";
        public const string INTER_ADS_KEY = "android_inter_ads";
        public const string BANNER_ADS_KEY = "android_banner_ads";
        public const string SHOW_INTER_FROM_LEVEL_KEY = "show_inter_from_level";
        public const string SHOW_BANNER_FROM_LEVEL_KEY = "show_banner_from_level";
        public const string NUMBER_LEVEL_NO_BACK_HOME_KEY = "number_level_no_back_home";
        public const string BUY_BOOSTER_ADS_KEY = "android_buy_booster_ads";
        public const string REVIVE_ADS_KEY = "android_revive_ads";
        public const string LOSE_STREAK_CONFIG_KEY = "lose_streak_config";

        // Giá trị remote config đã resolve (đặc thù game)
        public static string appVersionRemote;
        public static bool giftCodeEnable;
        public static bool enableCheatDefault;
        public static string enableCheatDefaultDevice;
        public static string loseStreakConfig;

        public static int levelType = 3;
        public static int numerLevelNoBackHome = 15;
        public static bool ísBuyBoosterAds = true;
        public static bool isReviveAds = true;

        #endregion
    }
}