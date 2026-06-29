using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.LocalNotification;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.Account;
using Ezg.Package.Audio;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.System.Settings
{
    public class SettingsController : FeatureBaseController
    {
        #region Fields

        [Title("Version")] [SerializeField] private Text textVersion;
        [SerializeField] private Button buttonVersion;
        [SerializeField] private Button offCheatButton;

        [Title("Navigation")] [SerializeField] private GameObject gamePlayButtonGrp;
        [SerializeField] private Button btnReplay;
        [SerializeField] private Button buttonQuit;
        [SerializeField] private Button buttonAccount;

        [Title("Uid")] [SerializeField] private Button _copyIdBtn;

        [TabGroup("Setting Toggle")] [Title("Sound")] [SerializeField]
        private Button soundButton;

        [TabGroup("Setting Toggle")] [SerializeField]
        private GameObject soundTick;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Image soundIcon;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Sprite soundIconOn;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Sprite soundIconOff;

        [TabGroup("Setting Toggle")] [Title("Music")] [SerializeField]
        private Button musicButton;

        [TabGroup("Setting Toggle")] [SerializeField]
        private GameObject musicTick;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Image musicIcon;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Sprite musicIconOn;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Sprite musicIconOff;

        [TabGroup("Setting Toggle")] [Title("Vibrate")] [SerializeField]
        private Button vibrateButton;

        [TabGroup("Setting Toggle")] [SerializeField]
        private GameObject vibrateTick;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Image vibrateIcon;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Sprite vibrateIconOn;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Sprite vibrateIconOff;

        [TabGroup("Setting Toggle")] [Title("Notification")] [SerializeField]
        private Button notificationButton;

        [TabGroup("Setting Toggle")] [SerializeField]
        private GameObject notificationTick;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Image notificationIcon;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Sprite notificationIconOn;

        [TabGroup("Setting Toggle")] [SerializeField]
        private Sprite notificationIconOff;

        [SerializeField] [TabGroup("Avatar")] private Image _mainAvt;

        [SerializeField] [TabGroup("Avatar")] private Image _mainFrame;

        [SerializeField] [TabGroup("PlayerName")]
        private Text _playerName;

        private bool _isSoundOn;
        private bool _isMusicOn;
        private bool _isVibrateOn;
        private bool _isNotificationOn;

        private int _countCheat;
        private const int MAX_COUNT_CHEAT = 10;
        private SoundPlayController _sound;

        #endregion

        #region Initialize

        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.StartListening(nameof(EventName.UpdatePlayerName), UpdateName);
            EventManager.StartListening(EventName.SettingLanguageChanged, OnLocalize);
        }

        protected override void Start()
        {
            base.Start();
            //LoadMainAvt();
            btnReplay.onClick.AddListener(OnClickReplay);
            buttonQuit.onClick.AddListener(OnClickQuit);
            buttonAccount.onClick.AddListener(Login);
            _copyIdBtn.onClick.AddListener(OnCopyIdClick);

            InitializeToggles();

            gamePlayButtonGrp.SetActive(SceneManager.GetActiveScene().name == nameof(GameEnums.Scenes.BattleScene));

            buttonVersion.onClick.AddListener(Cheat);
            offCheatButton.onClick.AddListener(OffCheat);
            textVersion.text = $"{GameSystems.Localize("version")} {Application.version}";
            OnLocalize();

            UpdateName();
        }

        private void OnLocalize()
        {
            textVersion.text = $"{GameSystems.Localize("version")} {Application.version}";
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.StopListening(EventName.SettingLanguageChanged, OnLocalize);

            EventManager.StopListening(nameof(EventName.UpdatePlayerName), UpdateName);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Opens the support URL.
        /// </summary>
        public void OnSupport()
        {
            Application.OpenURL("https://forms.gle/zcHiCKjGHPz6iyk26");
        }

        public void OnGmail()
        {
            Application.OpenURL("https://mail.google.com/mail");
        }

        /// <summary>
        ///     Opens the privacy policy URL.
        /// </summary>
        public void OnPolicy()
        {
            Application.OpenURL("https://www.deviloper.games/privacy-policy");
        }

        /// <summary>
        ///     Opens the privacy policy URL.
        /// </summary>
        public void OnTOS()
        {
            Application.OpenURL("https://www.deviloper.games/term-of-service");
        }

        /// <summary>
        ///     Copies account and device information to the clipboard.
        /// </summary>
        public void OnCopyAccountInfor()
        {
            var result = "";
            result += "AccountId: " + PlayerDataManager.Account.AccountId;
            result += "DeviceId: " + SystemInfo.deviceUniqueIdentifier;
            result += "\nVersion: " + Application.version;
            result += "\nDevice: " + SystemInfo.deviceName;
            result += "\nModel: " + SystemInfo.deviceModel;
            result += "\nOS: " + SystemInfo.operatingSystem;
            result.CopyToClipboard();
            GameSystems.ShowSimpleMessage("copy_to_clipboard");
        }

        /// <summary>
        ///     Opens the MAX mediation debugger.
        /// </summary>
        public void OpenMaxDebug()
        {
            MaxSdk.ShowMediationDebugger();
        }

        #endregion

        #region Private Methods

        private void UpdateName()
        {
            _playerName.text = PlayerDataManager.Settings.dataBase.PlayerName;
        }

        /// <summary>
        ///     Loads the main avatar and frame sprites from the player data.
        /// </summary>
        private void LoadMainAvt()
        {
            _mainAvt.sprite = DataManager.UserAvatars[PlayerDataManager.Settings.dataBase.AvatarId];
            _mainFrame.sprite = DataManager.UserFrame[PlayerDataManager.Settings.dataBase.FrameId];
        }

        /// <summary>
        ///     Initiates the login process.
        /// </summary>
        private void Login()
        {
            ProfileManager.Login().Forget();
        }

        /// <summary>
        ///     Initializes the audio and notification toggles based on player settings.
        /// </summary>
        private void InitializeToggles()
        {
            LocalNotificationManager.Init();

            _isSoundOn = PlayerDataManager.Settings.GetSound() > 0;
            _isMusicOn = PlayerDataManager.Settings.GetMusic() > 0;
            _isVibrateOn = PlayerDataManager.Settings.GetVibrate();
            _isNotificationOn = PlayerDataManager.Settings.GetNotification();

            UpdateToggleVisual(soundTick, soundIcon, soundIconOn, soundIconOff, _isSoundOn);
            UpdateToggleVisual(musicTick, musicIcon, musicIconOn, musicIconOff, _isMusicOn);
            UpdateToggleVisual(vibrateTick, vibrateIcon, vibrateIconOn, vibrateIconOff, _isVibrateOn);
            UpdateToggleVisual(notificationTick, notificationIcon, notificationIconOn, notificationIconOff,
                _isNotificationOn);

            _sound = soundButton.GetComponent<SoundPlayController>();
            if (_sound != null) soundButton.onClick.RemoveListener(_sound.PlaySoundCustom);
            soundButton.onClick.AddListener(OnSoundToggled);
            musicButton.onClick.AddListener(OnMusicToggled);
            vibrateButton.onClick.AddListener(OnVibrateToggled);
            notificationButton.onClick.AddListener(OnNotificationToggled);

            SyncNotificationToggleWithSystem();
        }

        /// <summary>
        ///     Updates the visual state of a toggle component.
        /// </summary>
        /// <param name="tick">The tick GameObject to show/hide.</param>
        /// <param name="icon">The icon Image to update.</param>
        /// <param name="spriteOn">The sprite to use when ON.</param>
        /// <param name="spriteOff">The sprite to use when OFF.</param>
        /// <param name="isOn">Whether the toggle is currently ON.</param>
        private void UpdateToggleVisual(GameObject tick, Image icon, Sprite spriteOn, Sprite spriteOff, bool isOn)
        {
            if (tick != null) tick.SetActive(isOn);
            if (icon != null && spriteOn != null && spriteOff != null)
                icon.sprite = isOn ? spriteOn : spriteOff;
        }

        /// <summary>
        ///     Increments the cheat counter and enables cheat mode if the threshold is reached.
        /// </summary>
        private void Cheat()
        {
            _countCheat++;
            if (_countCheat >= MAX_COUNT_CHEAT)
            {
                var isCheatEnabled = GameSystems.TryEnableCheat();
                if (isCheatEnabled) GameSystems.ShowSimpleMessage("cheat_enable");

                offCheatButton.gameObject.SetActive(GameSystems.isCheat);
                //GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
            }
        }

        /// <summary>
        ///     Disables cheat mode and returns to the home scene.
        /// </summary>
        private void OffCheat()
        {
            GameSystems.DisableCheat();
            GameSystems.ShowSimpleMessage("cheat_disable");
            offCheatButton.gameObject.SetActive(GameSystems.isCheat);
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        ///     Handles the sound toggle event.
        /// </summary>
        private void OnSoundToggled()
        {
            _isSoundOn = !_isSoundOn;
            AudioService.Default.SetOnOffSound(_isSoundOn);
            UpdateToggleVisual(soundTick, soundIcon, soundIconOn, soundIconOff, _isSoundOn);
            if (_sound != null && _isSoundOn) _sound.PlaySoundCustom();
        }

        /// <summary>
        ///     Handles the music toggle event.
        /// </summary>
        private void OnMusicToggled()
        {
            _isMusicOn = !_isMusicOn;
            AudioService.Default.SetOnOffMusic(_isMusicOn);
            UpdateToggleVisual(musicTick, musicIcon, musicIconOn, musicIconOff, _isMusicOn);
        }

        /// <summary>
        ///     Handles the vibrate toggle event.
        /// </summary>
        private void OnVibrateToggled()
        {
            _isVibrateOn = !_isVibrateOn;
            PlayerDataManager.Settings.SetVibrate(_isVibrateOn);
            PlayerDataManager.Settings.Save();
            UpdateToggleVisual(vibrateTick, vibrateIcon, vibrateIconOn, vibrateIconOff, _isVibrateOn);
        }

        /// <summary>
        ///     Handles the notification toggle event.
        /// </summary>
        private void OnNotificationToggled()
        {
#if UNITY_EDITOR
            _isNotificationOn = !_isNotificationOn;
            PlayerDataManager.Settings.SetNotification(_isNotificationOn);
            PlayerDataManager.Settings.Save();
            UpdateToggleVisual(notificationTick, notificationIcon, notificationIconOn, notificationIconOff,
                _isNotificationOn);
            return;
#endif

            var currentStatus = LocalNotificationManager.RefreshPermissionStatus();
            if (currentStatus == NotificationPermissionStatus.Unavailable)
            {
                _isNotificationOn = !_isNotificationOn;
                PlayerDataManager.Settings.SetNotification(_isNotificationOn);
                PlayerDataManager.Settings.Save();
                UpdateToggleVisual(notificationTick, notificationIcon, notificationIconOn, notificationIconOff,
                    _isNotificationOn);
                return;
            }

            var systemAllowsNotification = currentStatus == NotificationPermissionStatus.Granted;
            var desiredState = !_isNotificationOn;
            if (desiredState != systemAllowsNotification) LocalNotificationManager.OpenAppNotificationSettings();

            SyncNotificationToggleWithSystem();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;

#if UNITY_EDITOR
            _isNotificationOn = PlayerDataManager.Settings.GetNotification();
            UpdateToggleVisual(notificationTick, notificationIcon, notificationIconOn, notificationIconOff,
                _isNotificationOn);
            return;
#endif

            SyncNotificationToggleWithSystem();
        }

        private void SyncNotificationToggleWithSystem()
        {
#if UNITY_EDITOR
            _isNotificationOn = PlayerDataManager.Settings.GetNotification();
            UpdateToggleVisual(notificationTick, notificationIcon, notificationIconOn, notificationIconOff,
                _isNotificationOn);
            return;
#endif

            var status = LocalNotificationManager.RefreshPermissionStatus();
            if (status == NotificationPermissionStatus.Unavailable)
            {
                _isNotificationOn = PlayerDataManager.Settings.GetNotification();
                UpdateToggleVisual(notificationTick, notificationIcon, notificationIconOn, notificationIconOff,
                    _isNotificationOn);
                return;
            }

            _isNotificationOn = status == NotificationPermissionStatus.Granted;
            if (PlayerDataManager.Settings.GetNotification() != _isNotificationOn)
            {
                PlayerDataManager.Settings.SetNotification(_isNotificationOn);
                PlayerDataManager.Settings.Save();
            }

            UpdateToggleVisual(notificationTick, notificationIcon, notificationIconOn, notificationIconOff,
                _isNotificationOn);
        }

        /// <summary>
        ///     Handles the quit button click event.
        /// </summary>
        private void OnClickQuit()
        {
            CloseMe();
        }

        /// <summary>
        ///     Handles the replay button click event.
        /// </summary>
        private void OnClickReplay()
        {
            if (PlayerResource.IsEnough(EnumBase.MoneyTypes.Energy, 1))
            {
                AudioService.Default.StopMusic();
                PlayerDataManager.PlayerResource.Save();
                CloseMe();
            }
            else
            {
                UIManager.Instance.Show(GameEnums.Features.HeartRefill).Forget();
            }
        }

        private void OnCopyIdClick()
        {
            OnCopyAccountInfor();
            //GameSystems.ShowSimpleMessage("id_copied");
        }

        #endregion
    }
}