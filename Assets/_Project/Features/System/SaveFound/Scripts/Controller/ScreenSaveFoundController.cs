using System;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Utils;
using Ezg.Feature.Meta.HomeScene;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.Account;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.System.SaveFound
{
    /// <summary>
    ///     Controller for the Screen Save Found UI, allowing users to choose between local and server data.
    /// </summary>
    public class ScreenSaveFoundController : FeatureBaseController
    {
        #region Public Methods

        /// <summary>
        ///     Closes the screen and triggers follow-up actions.
        /// </summary>
        /// <param name="completeAction">Action to execute after closing.</param>
        public override void CloseMe(Action completeAction = null)
        {
            base.CloseMe(HomeSceneManager.CheckTut);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        ///     Closes the screen when the close event is received.
        /// </summary>
        private void CloseScreen()
        {
            CloseMe();
        }

        #endregion

        #region Fields

        [TabGroup("Data Server")] [SerializeField]
        private Text textLevelServer;

        [TabGroup("Data Server")] [SerializeField]
        private Text textExpServer;

        [TabGroup("Data Server")] [SerializeField]
        private Text textDateServer;

        [TabGroup("Data Server")] [SerializeField]
        private Text textTimeServer;

        [TabGroup("Data Server")] [SerializeField]
        private IconView coinServer;

        [TabGroup("Data Server")] [SerializeField]
        private IconView gemServer;

        [TabGroup("Data Server")] [SerializeField]
        private Button buttonConfirmDataServer;

        [TabGroup("Data Local")] [SerializeField]
        private Text textLevelLocal;

        [TabGroup("Data Local")] [SerializeField]
        private Text textExpLocal;

        [TabGroup("Data Local")] [SerializeField]
        private Text textDateLocal;

        [TabGroup("Data Local")] [SerializeField]
        private Text textTimeLocal;

        [TabGroup("Data Local")] [SerializeField]
        private IconView coinLocal;

        [TabGroup("Data Local")] [SerializeField]
        private IconView gemLocal;

        [TabGroup("Data Local")] [SerializeField]
        private Button buttonConfirmDataLocal;

        #endregion

        #region Initialize

        /// <summary>
        ///     Called when the script instance is being loaded.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.StartListening(EventName.ScreenSaveFoundCLose, CloseScreen);
        }

        /// <summary>
        ///     Called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            //EventManager.EmitEventData(EventName.BehindUI,this.ThisCanvas.sortingOrder);
        }

        /// <summary>
        ///     Loads necessary data and updates UI.
        /// </summary>
        protected override void LoadData()
        {
            base.LoadData();
            UpdateUI();
        }

        /// <summary>
        ///     Called when the behavior becomes disabled or inactive.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.StopListening(EventName.ScreenSaveFoundCLose, CloseScreen);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Updates the UI with data from both server and local sources.
        /// </summary>
        private void UpdateUI()
        {
            // removed: OrderData, PlayerDataManager.OrderDataManager (gameplay removed)
            textLevelServer.text = string.Empty;
            textLevelLocal.text = string.Empty;

            var resourceDataServer = PlayerDataSyncManager.GetDataInServer<PlayerResourceData>("PlayerResource");
            var resourceDataLocal = PlayerDataManager.PlayerResource.dataBase;

            coinServer.SetData(new Resource
            {
                resType = EnumBase.ResourceTypes.Money,
                resId = 1,
                resNumber = resourceDataServer.Monies[EnumBase.MoneyTypes.Gold]
            });
            gemServer.SetData(new Resource
            {
                resType = EnumBase.ResourceTypes.Money,
                resId = 2,
                resNumber = resourceDataServer.Monies[EnumBase.MoneyTypes.Diamonds]
            });
            coinLocal.SetData(new Resource
            {
                resType = EnumBase.ResourceTypes.Money,
                resId = 1,
                resNumber = resourceDataLocal.Monies[EnumBase.MoneyTypes.Gold]
            });
            gemLocal.SetData(new Resource
            {
                resType = EnumBase.ResourceTypes.Money,
                resId = 2,
                resNumber = resourceDataLocal.Monies[EnumBase.MoneyTypes.Diamonds]
            });

            var dateTimeServer = PlayerDataSyncManager.GetDateTimeCreateData();
            var unix = TimeManager.GetOnlineNow();
            var dateTimeLocal = DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;

            textDateServer.text = dateTimeServer.ToString("dd/MM/yyyy");
            textTimeServer.text = dateTimeServer.ToString("HH:mm:ss");
            textDateLocal.text = dateTimeLocal.ToString("dd/MM/yyyy");
            textTimeLocal.text = dateTimeLocal.ToString("HH:mm:ss");

            RegisButton();
        }

        /// <summary>
        ///     Registers button click listeners.
        /// </summary>
        private void RegisButton()
        {
            buttonConfirmDataServer.onClick.RemoveAllListeners();
            buttonConfirmDataServer.onClick.AddListener(ConfirmDataServer);
            buttonConfirmDataLocal.onClick.RemoveAllListeners();
            buttonConfirmDataLocal.onClick.AddListener(ConfirmDataLocal);
        }

        /// <summary>
        ///     Handles server data confirmation.
        /// </summary>
        private void ConfirmDataServer()
        {
            UIManager.Instance.Show(GameEnums.Features.ConfirmAccountServer).Forget();
        }

        /// <summary>
        ///     Handles local data confirmation.
        /// </summary>
        private void ConfirmDataLocal()
        {
            UIManager.Instance.Show(GameEnums.Features.ConfirmAccount).Forget();
        }

        #endregion
    }
}