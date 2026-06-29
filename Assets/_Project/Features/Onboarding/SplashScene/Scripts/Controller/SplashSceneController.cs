using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;
#if UNITY_ANDROID || PLATFORM_ANDROID || UNITY_IOS
using Ezg.Feature.LocalNotification;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Ezg.Core.Adapter;
using Ezg.Core.Firebase;
using Ezg.Core.Utils;
using Ezg.Feature.Firebase;
using Ezg.Feature.IAP;
using Ezg.Feature.Networking;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.Account;
using Ezg.Feature.System.Settings;
using Ezg.Package.AdsManager;
using Ezg.Package.Audio;
using Ezg.Package.Factory;
using Ezg.Package.Localize.Localization;
using Ezg.Tracking;
using Firebase;
using Firebase.Crashlytics;
using Firebase.Extensions;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UIManager = BlackFace.Libraries.Modules.UIModule.UIManager;

namespace Ezg.Feature.Onboarding.SplashScene
{
    public class SplashSceneController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private Button _continueButton;

        [SerializeField] [TabGroup("Cấu hình")]
        private Slider _loadingSlider;

        [SerializeField] [TabGroup("Cấu hình")]
        private Text _loadingDetail;

        private int _progressValue;

        private void Awake()
        {
            _continueButton.gameObject.SetActive(false);
            _loadingSlider.minValue = 0;
            _loadingSlider.maxValue = 21;
        }

        private async void Start()
        {
            if (!GameSystems.IsInternetConnection())
            {
                UIManager.Instance.Show(GameEnums.Features.RequireInternet).Forget();
                return;
            }

            _continueButton.onClick.AddListener(OnContinue);
#if UNITY_EDITOR
            await InitForEditor();
#else
            await InitForDevice();
#endif
        }

        private async UniTask WaitForRemoteConfigSuccessAsync()
        {
            if (FirebaseRemoteManager.isRemoteConfigInitialized) return;

            var completionSource = new UniTaskCompletionSource();
            UnityAction onRemoteConfigSuccess = null;
            onRemoteConfigSuccess = () =>
            {
                EventManager.StopListening(nameof(EventName.InitRemoteConfigSuccess), onRemoteConfigSuccess);
                completionSource.TrySetResult();
            };

            EventManager.StartListening(nameof(EventName.InitRemoteConfigSuccess), onRemoteConfigSuccess);

            if (FirebaseRemoteManager.isRemoteConfigInitialized)
            {
                EventManager.StopListening(nameof(EventName.InitRemoteConfigSuccess), onRemoteConfigSuccess);
                return;
            }

            try
            {
                var completedTaskIndex = await UniTask.WhenAny(
                    completionSource.Task,
                    UniTask.Delay(TimeSpan.FromSeconds(15)));

                if (completedTaskIndex != 0)
                    Debug.LogWarning("WaitForRemoteConfigSuccessAsync timed out after 15 seconds.");
            }
            finally
            {
                EventManager.StopListening(nameof(EventName.InitRemoteConfigSuccess), onRemoteConfigSuccess);
            }
        }

#if UNITY_EDITOR
        private async UniTask InitForEditor()
        {
#if USE_PAD || USE_BUNDLE
            await DownloadAssetBundle();
#endif
            GameInitialize.ClearStatic();
            AdsManager.Instance.Init();
            DataManager.LoadAllData();
            IapBootstrap.Configure();
            InAppManager.Instance.Init();
            DataPlayer.Init();
            await TimeManager.GetOnlineTime();
            TimeManager.BonusTimeNow = PlayerDataManager.Settings.dataBase.BonusTimeCheat;
            GameSystems.InitCancelToken(GameEnums.Scenes.BattleScene);

            // removed: DailyRewardService, InventoryService, DataItemCacheManager (gameplay removed)
            //GameNetworkManager.Instance.Init().Forget();
            //PlayerDataSyncManager.Init();

            UIManager.Instance.InitUI();
            await UIManager.Instance.Show(GameEnums.Features.OverviewCanvas, UIManager.UIGroupName.Toast_Container);
            //TrackingService.IsInitFirebase = true;
            //GameTrackingBootstrap.Register();
            //TrackingService.SetUAProperties(null);

            AudioService.Default.Initialize();
            DataPlayer.LoadAllData();
            PlayerDataManager.Settings.EnsureCreatedTime();
            Application.targetFrameRate = PlayerDataManager.Settings.GetPerformance() ? 30 : 120;
            GameSystems.SetupNewAccount();
            Localization.Current.localCultureInfo =
                Locale.GetCultureInfoByLanguage(PlayerDataManager.Settings.GetLanguage());

            // removed: VideoBonusesDataService (gameplay removed)
            PlayerResource.AutoRestoreEnergy().Forget();

            //UIAdapter.Show(nameof(GameEnums.Features.Tutorial), null, UIManager.UIGroupName.Tutorial_Container);

            TimeManager.Init();
            // removed: BuyCurrencyService (gameplay removed)
            PlayerDataManager.LoginActivity.RecordDailyLogin();

            //Utils.OpenScreenGamePlay(false);

            // await UpdateProgress("Preloading UI...");
            // await UIManager.PreloadAllFeatures((name, current, total) =>
            // {
            //     _loadingDetail.text = $"Loading UI: {name}";
            // });

            // removed: InfinityPackManager (gameplay removed)

#if UNITY_ANDROID || PLATFORM_ANDROID
            LocalNotificationManager.Init();
#endif
            GameSystems.DisableCheat(false);

            UIManager.EnableTouch(true);
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
        }
#endif

        private async UniTask InitForDevice()
        {
            await DownloadAssetBundle();
            await UpdateProgress("Initialize...");
            await UniTask.Delay(500);
            AdsManager.Instance.Init();
            await UpdateProgress("Data sync...");
            await UpdateProgress("Data clean");
            DataManager.Clear();
            await UpdateProgress("Data init");
            DataManager.LoadAllData();

            await UpdateProgress("Online data");
            var shouldWaitForRemoteConfig = false;
            await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                EventManager.EmitEvent(nameof(EventName.InitFirebaseSuccessfully));
                var dependencyStatus = task.Result;
                Debug.Log("EventManager.EmitEvent(OnInitFirebaseFinished)---------------------------------------------"
                          + (dependencyStatus == DependencyStatus.Available));
                if (dependencyStatus == DependencyStatus.Available)
                {
                    var app = FirebaseApp.DefaultInstance;
                    Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                    TrackingService.IsInitFirebase = true;
                    GameTrackingBootstrap.Register();
                    TrackingService.SetUAProperties(null);
                    FirebaseFirestoreManager.Init();
                    shouldWaitForRemoteConfig = true;
                    // Đăng ký trước khi fetch: FirebaseRemoteManager (assembly Ezg.Core.Firebase) không biết
                    // các key game-specific, nên GameRemoteConfig.Apply (Assembly-CSharp) sẽ đọc key, map vào
                    // AdsManager/field game, sync cheat state rồi emit InitRemoteConfigSuccess.
                    FirebaseRemoteManager.OnRemoteConfigApplied = GameRemoteConfig.Apply;
                    FirebaseRemoteManager.InitRemoteConfig();
                }
                else
                {
                    Debug.LogError(string.Format(
                        "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                }
            });

            IapBootstrap.Configure();
            GameInitialize.InitAppsFlyer();

            await UpdateProgress("Player data init");
            DataPlayer.Init();
            await UpdateProgress("Time online");
            await TimeManager.GetOnlineTime();
            await UpdateProgress("Time bonus");
            TimeManager.BonusTimeNow = PlayerDataManager.Settings.dataBase.BonusTimeCheat;
            await UpdateProgress("Token system");
            GameSystems.InitCancelToken(GameEnums.Scenes.BattleScene);


            UIManager.Instance.InitUI();
            await UpdateProgress("Sound system");
            AudioService.Default.Initialize();
            await UpdateProgress("Load data");
            DataPlayer.LoadAllData();
            PlayerDataManager.Settings.EnsureCreatedTime();
            await UpdateProgress("Frame rate system");
            Application.targetFrameRate = PlayerDataManager.Settings.GetPerformance() ? 30 : 120;
            await UpdateProgress("Account system");
            GameSystems.SetupNewAccount();
            Localization.Current.localCultureInfo =
                Locale.GetCultureInfoByLanguage(PlayerDataManager.Settings.GetLanguage());
            PlayerResource.AutoRestoreEnergy().Forget();
            await UpdateProgress("Energy system");
            await UpdateProgress("Purchase system");
            InAppManager.Instance.Init();
            await UpdateProgress("Overview layout");
            await UIManager.Instance.Show(GameEnums.Features.OverviewCanvas, UIManager.UIGroupName.Toast_Container);
            await UpdateProgress("Feature system");
            UnlockFeatureService.Init();
            // removed: VideoBonusesDataService (gameplay removed)

            // removed: TutorialContainer / TutorialFactory init (gameplay removed)

            await UpdateProgress("Time", true);
            TimeManager.Init();
            // removed: BuyCurrencyService (gameplay removed)
            PlayerDataManager.LoginActivity.RecordDailyLogin();
            //await UpdateProgress("Daily reward", true);
            //DailyRewardService.Init();
            // removed: InventoryService, DataItemCacheManager (gameplay removed)
            GameNetworkManager.Instance.Init().Forget();
            PlayerDataSyncManager.Init();
            // removed: InfinityPackManager, EventMergeService (gameplay removed)


            // await UpdateProgress("Preloading UI...");
            // await UIManager.PreloadAllFeatures((name, current, total) =>
            // {
            //     _loadingDetail.text = $"Loading UI: {name}";
            // });

            // await UpdateProgress("Init Gameplay");
            // Utils.OpenScreenGamePlay(false);

            SettingManager.CountOnlineTime().Forget();
#if UNITY_ANDROID || PLATFORM_ANDROID || UNITY_IOS
            LocalNotificationManager.Init();
#endif
            await UpdateProgress("Ads");

            CheckInstallerValidation();

            if (shouldWaitForRemoteConfig)
            {
                await UpdateProgress("Remote config");
                await WaitForRemoteConfigSuccessAsync();
            }

            UIManager.EnableTouch(true);
            AudioService.Default.Initialize();
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
        }

        private void CheckInstallerValidation()
        {
#if PLATFORM_ANDROID
            if (Application.installerName != "com.android.vending")
            {
                TrackingService.IsTracking = false;
                GameSystems.SetupMessage().Forget();
            }
#else
            if (Application.installMode != ApplicationInstallMode.Store)
            {
                TrackingService.IsTracking = false;
                GameSystems.SetupMessage().Forget();
            }
#endif
        }

        private async UniTask UpdateProgress(string content = "", bool isEnd = false)
        {
            _loadingSlider.value++;
            _loadingDetail.text = content;
            if (isEnd)
            {
                _loadingDetail.gameObject.SetActive(false);
                _loadingSlider.value = _loadingSlider.maxValue;
                UIManager.EnableTouch(true);
            }

            await UniTask.Yield();
        }

        private void UpdateProgressBundle()
        {
            _loadingSlider.value++;
            _loadingDetail.text = GameSystems.Localize("downloading") + ": " +
                                  $"{(int)_loadingSlider.value}/{(int)_loadingSlider.maxValue}";
        }

        private void OnContinue()
        {
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
        }

        private async UniTask DownloadAssetBundle()
        {
            var bundleNames = new List<string>();
            CollectBundleNames(typeof(AssetBundleName), bundleNames);

            _loadingSlider.maxValue = bundleNames.Count;
            _loadingSlider.value = 0;
            _loadingDetail.text = $"0/{bundleNames.Count}";

            var downloadTasks = new List<UniTask>();

            foreach (var bundleName in bundleNames)
                downloadTasks.Add(LoadAssetBundleAsync(bundleName));

            await UniTask.WhenAll(downloadTasks);

            Debug.Log("[Splash] All Asset Bundles downloaded!");
        }

        private static void CollectBundleNames(Type type, List<string> result)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                result.Add((string)field.GetValue(null));
            foreach (var nested in type.GetNestedTypes(BindingFlags.Public))
                CollectBundleNames(nested, result);
        }

        private async UniTask LoadAssetBundleAsync(string assetBundleName)
        {
            var success = await AssetBundleManager.Instance.LoadBundleAsync(assetBundleName);
            if (!success)
                Debug.LogError($"[Splash] Failed to load bundle: {assetBundleName}");
            UpdateProgressBundle();
        }
    }
}