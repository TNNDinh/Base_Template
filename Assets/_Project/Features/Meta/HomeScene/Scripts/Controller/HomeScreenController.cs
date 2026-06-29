using System;
using System.Collections;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Firebase;
using Ezg.Feature.Firebase;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.Account;
using Ezg.Package.Audio;
using Ezg.Tracking;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.Meta.HomeScene
{
    public class HomeScreenController : FeatureBaseController
    {
        private void OnApplicationQuit()
        {
            GameSystems.OnQuitGame();
        }

        public void OpenMaxDebug()
        {
            MaxSdk.ShowMediationDebugger();
        }

        public void GotoPreBattle()
        {
            UIManager.Instance.Show(GameEnums.Features.PreBattle, isAsync: true).Forget();
        }

        public void OpenScreenGame()
        {
            EventManager.EmitEvent(EventName.ClickButtonPlay);
            // removed: Utils.OpenScreenGamePlay (gameplay removed)
        }

        private void OpenView()
        {
            this.DelayRealTimeMethod(0.5f, () =>
            {
                HandleOpenView(true);
                //this.HideIcon(true);
                EventManager.EmitEventData(EventName.SetViewTheme, true);
                buttonExitView.onClick.RemoveAllListeners();
                buttonExitView.onClick.AddListener(() =>
                {
                    Action actionDone = () =>
                    {
                        EventManager.EmitEventData(EventName.SetViewTheme, false);
                        // removed: BuildUpGoalManager.SelectTheme / PlayerBuildUpGoalDataManager (gameplay removed)
                        HandleOpenView(false);
                    };
                    // removed: Utils.ShowMaskChangeScene / PlayerBuildUpGoalDataManager (gameplay removed)
                    actionDone();
                });
            });
        }

        private void HandleOpenView(bool isView)
        {
            buttonExitView.gameObject.SetActive(isView);
        }

        private void HideIcon()
        {
            var isHide = EventManager.GetBool(EventName.SetViewTheme);
            for (var i = 0; i < elements.Length; i++)
                if (!isHide)
                    elements[i].MoveIn();
                else
                    elements[i].MoveOut();
        }

        /// <summary>
        ///     Loads the main avatar and frame sprites from the player data.
        /// </summary>
        private void LoadMainAvt()
        {
            _mainAvt.sprite = DataManager.UserAvatars[PlayerDataManager.Settings.dataBase.AvatarId];
            _mainFrame.sprite = DataManager.UserFrame[PlayerDataManager.Settings.dataBase.FrameId];
        }

        #region Fields

        [SerializeField] [TabGroup("Stage")] private Button buttonMap;

        [SerializeField] [TabGroup("Stage")] private Text energyText;

        [SerializeField] [TabGroup("Header")] private Button buttonSettings;

        //[SerializeField][TabGroup("Header")] private MoneyBarSlider energyBar;

        [SerializeField] [TabGroup("Header")] private Button _adminButton;

        [SerializeField] [TabGroup("Header")] public UI_TabExtensions MainTab;

        [SerializeField] [TabGroup("Stage")] private Button buttonExitView;
        [SerializeField] [TabGroup("Stage")] private Transform midLayout;
        [SerializeField] [TabGroup("Stage")] private Transform bottomLayout;

        [FormerlySerializedAs("_dinhAnimController")] [SerializeField] [TabGroup("BuildUpGoal")]
        public UIElementAnimator uiElementAnimator;

        [SerializeField] [TabGroup("BuildUpGoal")]
        private MoneyBarSlider _starBar;

        [SerializeField] [TabGroup("StarChest")]
        public Transform posSpawnStarChest;

        [SerializeField] [TabGroup("View")] private UIElementAnimator[] elements;

        [SerializeField] [TabGroup("Avatar")] private Image _mainAvt;

        [SerializeField] [TabGroup("Avatar")] private Image _mainFrame;

        #endregion

        #region Initialize

        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.StartListening(EventName.ViewTheme, OpenView);
            EventManager.StartListening(EventName.SetViewTheme, HideIcon);
            EventManager.StartListening(EventName.AvatarSelectAvatarEvent, LoadMainAvt);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.StopListening(EventName.ViewTheme, OpenView);
            EventManager.StopListening(EventName.SetViewTheme, HideIcon);
            EventManager.StopListening(EventName.AvatarSelectAvatarEvent, LoadMainAvt);
        }

        protected override void Awake()
        {
            HomeSceneManager.Controller = this;
            // removed: StarChestManager.moneyBarSlider (gameplay removed)
            base.Awake();

            //GotoBattle();

            //else
            //{
            //    ProfileManager.ValidInputName();
            //}
            //TutorialContainer.CheckTutorial(Location.Home, true);

            EventManager.EmitEvent(EventName.CloseScreenGameplay);
        }

        protected override async void Start()
        {
            if (!GameSystems.IsInternetConnection())
                UIManager.Instance.Show(GameEnums.Features.RequireInternet).Forget();

            GameSystems.CheckInternet();


            base.Start();
            //PlayMusic();
            RefreshNetwork();

            AudioService.Default.UpdateVolumes();
            LoadMainAvt();
            CheckUpdate();
            // removed: GameplayService.Init (gameplay removed)
            //ItemMergePool.Instance.PreInit(DataManager.GeneralAssets.itemMerge.gameObject, 63);
            //BattleRoyaleManager.Init();

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


            if (!UnlockFeatureService.ShowUnlockFeature())
                TriggerShowRatingPopup();

            if (PlayerDataManager.Settings.IsNewAccount())
            {
#if !PLATFORM_ANDROID
                UIManager.Instance.Show(GameEnums.Features.VideoIntro,UIManager.UIGroupName.Toast_Container).Forget();
#endif
                GameSystems.isNewOpen = true;
                //AudioService.Default.StopMusic();
                //GameSystems.ChangeScene(GameEnums.Scenes.BattleScene);

                PlayerDataManager.Settings.RemoveNewAccount();
                PlayerDataManager.PlayerResource.Save();
                PlayerDataManager.Settings.Save();
            }

            HomeSceneManager.InitScene();

            // if (!PlayerDataManager.Account.IsSaveDataToCloud())
            // {
            //     ProfileManager.MappingData();
            // }

            // if (!TutorialContainer.isRunning)
            // {
            //     ProfileManager.MappingData();
            // }
            this.DelayMethod(1f, () => { ProfileManager.GetAndUpdateProfileData().Forget(); });
        }

        //private static bool _isShowRatingInDay;

        public static bool TriggerShowRatingPopup()
        {
            // if (PlayerDataManager.Campaign.HighestLevel >= GameRemoteConfig.numerLevelNoBackHome
            //     && !_isShowRatingInDay
            //     && !PlayerDataManager.Settings.dataBase.IsRating
            //     && Time.realtimeSinceStartup >= 300)
            if (RatingService.CanRating())
            {
                RatingService.ShowRating();
                //_isShowRatingInDay = true;
                return true;
            }

            return false;
        }

        private void RefreshNetwork()
        {
            //if (!GameNetworkManager.Instance.IsOnline)
            //    GameNetworkManager.Instance.Init().Forget();
        }

        /// <summary>
        ///     Load các event từ server
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadGameEvents()
        {
            //if (GameEventManager.AllEvents.Any())
            //{
            //    yield break;
            //}

            var delay = new WaitForSeconds(1);
            if (FirebaseFirestoreManager.GetSource() == null)
                while (FirebaseFirestoreManager.GetSource() == null)
                    yield return delay;
            else
                yield return delay;
            //GameEventManager.InitData();
        }

        // private void PlayMusic()
        // {
        //     if (DataManager.SoundConfig.MainMenuMusics != null && DataManager.SoundConfig.MainMenuMusics.Length > 0)
        //     {
        //         AudioService.Default.PlayMusic(
        //             DataManager.SoundConfig.MainMenuMusics[PlayerDataManager.PlayerBuildUpGoalDataManager.dataBase.themeCurrentId],
        //             true);
        //     }
        // }

        public override bool CloseWithBackKey()
        {
            if (!UIManager.IsShowLoading)
            {
                GameSystems.ShowMessage("quit_game_confirm", "", Application.Quit, () => { }, focusYes: false);
                return true;
            }

            return false;
        }

        private void CheckUpdate()
        {
            if (string.IsNullOrEmpty(GameRemoteConfig.appVersionRemote))
                EventManager.StartListening(nameof(EventName.InitRemoteConfigSuccess), ShowUpdateNewVersion);
            else
                ShowUpdateNewVersion();
        }

        /// <summary>
        ///     Show popup yêu cầu update nếu có version mới
        /// </summary>
        private void ShowUpdateNewVersion()
        {
#if UNITY_EDITOR
            return;
#endif

#if PLATFORM_ANDROID || UNITY_ANDROID
            var currentVersion = Application.version;
            var serverVersion = GameRemoteConfig.appVersionRemote;
            var current = new Version(currentVersion);
            var update = new Version(serverVersion);
            if (current < update)
                GameSystems.ShowMessage("update_version_notice",
                    "update_version_title", () =>
                    {
#if UNITY_IOS
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
#else
                        var STORE_APP_ID = "";
                        Application.OpenURL("https://apps.apple.com/app/id" + STORE_APP_ID);
                        Application.OpenURL(
                            "https://play.google.com/store/apps/details?id=com.fansipan.survivor.roguelike.casual.game");
#endif
                        Application.Quit();
                    }, focusYes: true, showCancelButton: false);
#endif
        }

        #endregion

#if UNITY_EDITOR
        //private async void Update()
        //{
        //    if (Input.GetKeyUp(KeyCode.A))
        //    {
        //        //GameSystems.ShowMessage("Required internet", "Information", showCancelButton: false);
        //        //GameSystems.ShowSimpleMessage("tank");
        //        UIManager.Instance.Show(GameEnums.Features.Rating).Forget();
        //    }
        //}

#endif
    }
}