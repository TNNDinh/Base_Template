using System;
using System.Collections.Generic;
using System.Text;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Firebase;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Package.Factory;
using Ezg.Package.Singleton;
using Newtonsoft.Json;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.Social.Account
{
    public class PlayerDataSyncManager : Singleton<PlayerDataSyncManager>
    {
        // removed: PushDataByLevel / LevelDataManager (gameplay removed)

        #region Fields

        /// <summary>
        ///     Delay time for pushing data in seconds.
        /// </summary>
        private readonly int _cooldownTime = 300;

        /// <summary>
        ///     The last time data was synchronized.
        /// </summary>
        private float _lastSyncTime;

        /// <summary>
        ///     The sync data interface.
        /// </summary>
        private static ISyncData _syncData;

        /// <summary>
        ///     Data received from the server.
        /// </summary>
        private static byte[] _serverData;

        /// <summary>
        ///     Flag indicating if data exists on the server.
        /// </summary>
        public static bool IsHaveDataInServer;

        /// <summary>
        ///     Blocks background sync while the player has not resolved a local/server conflict yet.
        /// </summary>
        private static bool _isWaitingForSyncChoice;

        /// <summary>
        ///     Stores the account id that still has an unresolved local/server sync choice.
        /// </summary>
        private const string PendingSyncChoiceAccountPrefsKey = "PlayerDataSyncManager.PendingSyncChoiceAccountId";

        private readonly int levelPush = 4;

        #endregion

        #region Initialize

        /// <summary>
        ///     Initializes the sync manager.
        /// </summary>
        public static void Init()
        {
            IsHaveDataInServer = false;
            _isWaitingForSyncChoice = false;
            // Drop server payload carried over from a previous session so iOS
            // soft-restart starts from a clean state. _lastSyncTime is an
            // instance field and is reset automatically when ResetIOS destroys
            // the singleton GameObject (Fix #4) — the auto-created replacement
            // starts with the default 0.
            _serverData = null;
            _syncData = new FirebaseStorageManager();
            //_syncData = new SupabaseGetDataUser();

            TimeManager.RegEventNextDay(NextDay);
        }

        private void OnEnable()
        {
            EventManager.StartListening(EventName.ForceSyncData, OnForceSyncData);
            // removed: OnUpLevel -> PushDataByLevel (gameplay removed)
        }

        private void OnDisable()
        {
            EventManager.StopListening(EventName.ForceSyncData, OnForceSyncData);
            // removed: OnUpLevel -> PushDataByLevel (gameplay removed)
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Pushes player data to the server.
        /// </summary>
        /// <param name="ignoreCooldown">Whether to ignore the sync cooldown.</param>
        /// <param name="failedAction">Action to execute on failure.</param>
        /// <param name="successAction">Action to execute on success.</param>
        /// <param name="showMessage">Whether to show UI messages.</param>
        /// <param name="runInBackground">Whether to run the process in the background.</param>
        /// <param name="accountIdCustom">Custom account ID to use.</param>
        /// <param name="isLogout">Whether the push is part of a logout process.</param>
        /// <returns>UniTask.</returns>
        public async UniTask PushPlayerData(bool ignoreCooldown = false, UnityAction failedAction = null,
            UnityAction successAction = null, bool showMessage = true, bool runInBackground = false,
            string accountIdCustom = null, bool isLogout = false, bool isCloseWaitingScreenAfterPush = true)
        {
            Debug.Log(
                $"[LoginFlow][PlayerDataSyncManager.PushPlayerData] Start. ignoreCooldown={ignoreCooldown}, runInBackground={runInBackground}, isLogout={isLogout}, accountId={accountIdCustom ?? PlayerDataManager.Account.AccountId}");
#if UNITY_EDITOR
            await _syncData.PushPlayerData(CreateAllData(), accountIdCustom ?? PlayerDataManager.Account.AccountId,
                failedAction, successAction);
            GameSystems.ShowSimpleMessage("login_sync_data_done");
            if (isCloseWaitingScreenAfterPush) GameSystems.ShowWaitingScreen(false);

            return;
#endif

            if (string.IsNullOrEmpty(accountIdCustom ?? PlayerDataManager.Account.AccountId))
            {
                Debug.LogWarning("[LoginFlow][PlayerDataSyncManager.PushPlayerData] AccountId is null or empty.");
                GameSystems.ShowWaitingScreen(false);
                if (showMessage)
                    GameSystems.ShowSimpleMessage("login_null_account");
                return;
            }

            if (!ValidPushData(ignoreCooldown))
            {
                Debug.LogWarning("[LoginFlow][PlayerDataSyncManager.PushPlayerData] Push blocked by cooldown.");
                GameSystems.ShowWaitingScreen(false);
                return;
            }

            if (!runInBackground)
                GameSystems.ShowWaitingScreen(true);
            failedAction += () =>
            {
                Debug.LogWarning("[LoginFlow][PlayerDataSyncManager.PushPlayerData] Push failed callback invoked.");
                if (showMessage)
                    GameSystems.ShowSimpleMessage("login_sync_data_fail");
                GameSystems.ShowWaitingScreen(false);
            };

            UnityAction bonusAction = () =>
            {
                Debug.Log("[LoginFlow][PlayerDataSyncManager.PushPlayerData] Push success callback invoked.");
                if (showMessage)
                    GameSystems.ShowSimpleMessage("login_sync_data_done");
                _lastSyncTime = Time.time;
                if (isLogout)
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    Instance.DelayRealTimeMethod(3f, () =>
                    {
                        GameSystems.ShowWaitingScreen(false);
                        successAction?.Invoke();
                    });
                }
                else
                {
                    PlayerDataManager.Account.SaveDataInCloud();
                    successAction?.Invoke();
                    GameSystems.ShowWaitingScreen(false);
                }
            };

            PlayerDataManager.Account.SaveDataInCloud();
            PlayerDataManager.Settings.dataBase.LastTimeSyncData = TimeManager.GetNow();
            PlayerDataManager.Settings.dataBase.LastDevice = isLogout ? "" : SystemInfo.deviceUniqueIdentifier;
            PlayerDataManager.Settings.Save();
            Debug.Log("[LoginFlow][PlayerDataSyncManager.PushPlayerData] Sending data to storage provider.");
            await _syncData.PushPlayerData(CreateAllData(), accountIdCustom ?? PlayerDataManager.Account.AccountId,
                failedAction, bonusAction);
        }

        /// <summary>
        ///     Pushes player data for admin purposes.
        /// </summary>
        /// <param name="accountIdCustom">Custom account ID.</param>
        /// <returns>UniTask.</returns>
        public async UniTask PushPlayerDataAdmin(string accountIdCustom = null)
        {
            await _syncData.PushPlayerData(CreateAllData(), accountIdCustom ?? PlayerDataManager.Account.AccountId,
                null, null);
            GameSystems.ShowSimpleMessage("login_sync_data_done");
        }

        /// <summary>
        ///     Gets player data from the server.
        /// </summary>
        /// <param name="forceGet">Whether to force get data.</param>
        public async void GetPlayerData(bool forceGet = false, UnityAction onSyncResolved = null)
        {
            Debug.Log(
                $"[LoginFlow][PlayerDataSyncManager.GetPlayerData] Start. forceGet={forceGet}, accountId={PlayerDataManager.Account.AccountId}");
            if (string.IsNullOrEmpty(PlayerDataManager.Account.AccountId))
            {
                Debug.LogWarning("[LoginFlow][PlayerDataSyncManager.GetPlayerData] AccountId is null or empty.");
                GameSystems.ShowWaitingScreen(false);
                GameSystems.ShowSimpleMessage("login_null_account");
                // removed: TutorialContainer.CheckTutorial(Location.Home) (gameplay removed)
                return;
            }

            GameSystems.ShowWaitingScreen(true);
            GameSystems.ShowSimpleMessage("login_syncing_data");

            try
            {
                Debug.Log(
                    "[LoginFlow][PlayerDataSyncManager.GetPlayerData] Requesting player data from storage provider.");
                _serverData = await _syncData.GetPlayerData(PlayerDataManager.Account.AccountId)
                    .Timeout(TimeSpan.FromSeconds(6));

                Debug.Log(
                    $"[LoginFlow][PlayerDataSyncManager.GetPlayerData] Data received. IsNull={_serverData == null}");
                IsHaveDataInServer = _serverData != null;
                GameSystems.ShowWaitingScreen(false);

                if (_serverData == null)
                {
                    Debug.Log(
                        "[LoginFlow][PlayerDataSyncManager.GetPlayerData] No server data. Save local data and push to cloud.");
                    PushLocalDataToCloudInBackground();
                    ContinueAfterSyncResolved(onSyncResolved);
                }
                else
                {
                    Debug.Log(
                        "[LoginFlow][PlayerDataSyncManager.GetPlayerData] Server data exists. Show conflict popup and wait for player choice.");
                    ShowSaveFoundPopup();
                }
            }
            catch (TimeoutException)
            {
                Debug.LogWarning(
                    "[LoginFlow][PlayerDataSyncManager.GetPlayerData] Timeout while waiting for player data.");
                GameSystems.ShowWaitingScreen(false);
                GameSystems.ShowSimpleMessage("login_timeout");
                OnGetPlayerDataTimeout(onSyncResolved);
            }
            catch (Exception ex)
            {
                GameSystems.ShowWaitingScreen(false);
                Debug.LogError("Lỗi lấy dữ liệu server: " + ex);
                GameSystems.ShowSimpleMessage("login_error");
                ContinueAfterSyncResolved(onSyncResolved);
            }
        }

        /// <summary>
        ///     Gets a specific data type from the server data.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve.</typeparam>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data of type T.</returns>
        public static T GetDataInServer<T>(string key)
        {
            var allData =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(TryDecodeServerData(_serverData));
            if (allData != null)
                if (allData.ContainsKey(key))
                    return JsonConvert.DeserializeObject<T>(allData[key]);

            return default;
        }

        /// <summary>
        ///     Gets player data for admin purposes based on account ID.
        /// </summary>
        /// <param name="accountId">The account ID.</param>
        public async void GetPlayerDataAdmin(string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                GameSystems.ShowWaitingScreen(false);
                GameSystems.ShowSimpleMessage("login_null_account");
                return;
            }

            GameSystems.ShowWaitingScreen(true);
            GameSystems.ShowSimpleMessage("login_syncing_data");
            _serverData = await _syncData.GetPlayerData(accountId);
            if (_serverData == null)
            {
                GameSystems.ShowSimpleMessage("Not have cloud data");
                GameSystems.ShowWaitingScreen(false);
            }
            else
            {
                DataPlayer.ClearAllData();
                var allData =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(TryDecodeServerData(_serverData));
                foreach (var data in allData) DataPlayer.GetModule(data.Key).SynchronizeData(data.Value);

                DataPlayer.SaveAllData();
                GameSystems.ShowWaitingScreen(false);
                DataPlayer.LoadAllData();
                GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
            }
        }

        /// <summary>
        ///     Checks server data for an already authenticated player when entering Home.
        /// </summary>
        /// <returns>UniTask.</returns>
        public async UniTask CheckServerDataForLoggedInUser(UnityAction onSyncResolved = null)
        {
            Debug.Log(
                $"[LoginFlow][PlayerDataSyncManager.CheckServerDataForLoggedInUser] Start. isLogon={ProfileManager.IsLogon()}, accountId={PlayerDataManager.Account.AccountId}");
            if (!ProfileManager.IsLogon())
            {
                GameSystems.ShowWaitingScreen(false);
                ContinueAfterSyncResolved(onSyncResolved);
                return;
            }

            if (string.IsNullOrEmpty(PlayerDataManager.Account.AccountId))
            {
                GameSystems.ShowWaitingScreen(false);
                ContinueAfterSyncResolved(onSyncResolved);
                return;
            }

            try
            {
                var serverDataResult = await _syncData.GetPlayerData(PlayerDataManager.Account.AccountId)
                    .Timeout(TimeSpan.FromSeconds(3));

                _serverData = serverDataResult;
                IsHaveDataInServer = serverDataResult != null;

                if (serverDataResult == null)
                {
                    Debug.Log(
                        "[LoginFlow][PlayerDataSyncManager.CheckServerDataForLoggedInUser] No server data. Push local data to cloud.");
                    PushLocalDataToCloudInBackground();
                    GameSystems.ShowWaitingScreen(false);
                    ContinueAfterSyncResolved(onSyncResolved);
                }
                else
                {
                    if (!TryGetServerSettingData(serverDataResult, out var serverSettingData))
                    {
                        Debug.LogWarning(
                            "[LoginFlow][PlayerDataSyncManager.CheckServerDataForLoggedInUser] Cannot read server device id. Show conflict popup to avoid unintended overwrite.");
                        GameSystems.ShowWaitingScreen(false);
                        ShowSaveFoundPopup();
                        return;
                    }

                    var localDeviceId = SystemInfo.deviceUniqueIdentifier;
                    var hasPendingSyncChoice = HasPendingSyncChoiceForCurrentAccount();
                    var hasLinkedThisInstallToCloud = PlayerDataManager.Account.IsSaveDataToCloud();
                    if (hasPendingSyncChoice)
                    {
                        Debug.Log(
                            "[LoginFlow][PlayerDataSyncManager.CheckServerDataForLoggedInUser] Pending sync choice found from previous session. Show popup again.");
                        GameSystems.ShowWaitingScreen(false);
                        ShowSaveFoundPopup();
                        return;
                    }

                    if (!string.IsNullOrEmpty(serverSettingData.LastDevice) &&
                        serverSettingData.LastDevice == localDeviceId &&
                        hasLinkedThisInstallToCloud)
                    {
                        Debug.Log(
                            $"[LoginFlow][PlayerDataSyncManager.CheckServerDataForLoggedInUser] Same device detected and this install was already linked to cloud. Push local data. serverDevice={serverSettingData.LastDevice}, localDevice={localDeviceId}");
                        PushLocalDataToCloudInBackground();
                        GameSystems.ShowWaitingScreen(false);
                        ContinueAfterSyncResolved(onSyncResolved);
                    }
                    else
                    {
                        Debug.Log(
                            $"[LoginFlow][PlayerDataSyncManager.CheckServerDataForLoggedInUser] Auto-push is not safe. Show conflict popup. serverDevice={serverSettingData.LastDevice ?? "<null>"}, localDevice={localDeviceId}, hasLinkedThisInstallToCloud={hasLinkedThisInstallToCloud}");
                        GameSystems.ShowWaitingScreen(false);
                        ShowSaveFoundPopup();
                    }
                }
            }
            catch (TimeoutException)
            {
                Debug.LogWarning(
                    "[LoginFlow][PlayerDataSyncManager.CheckServerDataForLoggedInUser] Timeout while checking server data.");
                GameSystems.ShowWaitingScreen(false);
                ContinueAfterSyncResolved(onSyncResolved);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[LoginFlow][PlayerDataSyncManager.CheckServerDataForLoggedInUser] Failed to check server data: " +
                    ex);
                GameSystems.ShowWaitingScreen(false);
                ContinueAfterSyncResolved(onSyncResolved);
            }
        }

        /// <summary>
        ///     Legacy alias for the existing logged-in validation flow.
        /// </summary>
        public UniTask ValidDataOnOpenGame()
        {
            return CheckServerDataForLoggedInUser();
        }

        /// <summary>
        ///     Gets the date and time when the data was created.
        /// </summary>
        /// <returns>DateTime.</returns>
        public static DateTime GetDateTimeCreateData()
        {
            return _syncData.GetTimeCreateData();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Handles logic for the next day.
        /// </summary>
        private static void NextDay()
        {
            if (_isWaitingForSyncChoice || HasPendingSyncChoiceForCurrentAccount())
            {
                Debug.Log(
                    "[LoginFlow][PlayerDataSyncManager.NextDay] Skip auto sync because sync choice popup is pending.");
                return;
            }

            if (!ProfileManager.IsLogon())
            {
                Debug.Log("[LoginFlow][PlayerDataSyncManager.NextDay] Skip auto sync because player is not logged in.");
                return;
            }

            if (string.IsNullOrEmpty(PlayerDataManager.Account.AccountId)) return;

            if (!PlayerDataManager.Account.IsSaveDataToCloud()) return;
            if (!TimeManager.IsNextDay(PlayerDataManager.Settings.dataBase.LastTimeSyncData)) return;
            PushData();
        }

        private static void PushData()
        {
            Instance.PushPlayerData(true, runInBackground: true, showMessage: false).Forget();
        }

        /// <summary>
        ///     Validates if data can be pushed.
        /// </summary>
        /// <param name="ignoreCooldown">Whether to ignore the cooldown.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private bool ValidPushData(bool ignoreCooldown = false)
        {
            if (Time.time - _cooldownTime * 60 < _lastSyncTime && _lastSyncTime != 0 && !ignoreCooldown) return false;

            return true;
        }

        /// <summary>
        ///     Handles data retrieval timeout.
        /// </summary>
        private void OnGetPlayerDataTimeout(UnityAction onSyncResolved = null)
        {
            Debug.LogWarning("Lấy dữ liệu server quá 3s → Timeout!");
            ContinueAfterSyncResolved(onSyncResolved);
        }

        /// <summary>
        ///     Creates a JSON string representing all player data.
        /// </summary>
        /// <returns>JSON string.</returns>
        private static string CreateAllData()
        {
            FlushRuntimeDataBeforeCloudPush();
            var data = new Dictionary<string, string>();
            var resgisteredModules = DataPlayer.GetRegisteredModules();
            foreach (var _data in resgisteredModules) data.Add(_data.Key, _data.Value.GetDataJson());

            return JsonConvert.SerializeObject(data);
        }

        private static void FlushRuntimeDataBeforeCloudPush()
        {
            // removed: GridService / GridType (gameplay removed)
        }

        private static void ClearRuntimeDataAfterCloudPull()
        {
            // removed: GridService (gameplay removed)
        }

        /// <summary>
        ///     Decodes server byte array to JSON string.
        ///     Tries UTF-8 first (new format); falls back to BinaryFormatter for legacy data already on global::Firebase.
        /// </summary>
        private static string TryDecodeServerData(byte[] bytes)
        {
            if (bytes == null) return null;

            try
            {
                var json = Encoding.UTF8.GetString(bytes);
                JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                return json;
            }
            catch
            {
            }

            try
            {
                return bytes.ByteArrayToObject<string>();
            }
            catch (Exception ex)
            {
                Debug.LogError("[PlayerDataSyncManager] Cannot decode server data: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        ///     Saves current local data and pushes it silently to cloud.
        /// </summary>
        private void PushLocalDataToCloudInBackground()
        {
            _isWaitingForSyncChoice = false;
            ClearPendingSyncChoiceForCurrentAccount();
            DataPlayer.SaveAllData();
            PushPlayerData(true, runInBackground: true, showMessage: false).Forget();
        }

        /// <summary>
        ///     Continues the Home flow after sync completes without opening the conflict popup.
        /// </summary>
        private static void ContinueAfterSyncResolved(UnityAction onSyncResolved = null)
        {
            if (onSyncResolved != null)
            {
                onSyncResolved.Invoke();
                return;
            }

            // removed: TutorialContainer.CheckTutorial(Location.Home) (gameplay removed)
        }

        /// <summary>
        ///     Registers popup listeners and waits for the player to pick local or server data.
        /// </summary>
        private void ShowSaveFoundPopup()
        {
            if (ProfileManager.IsEnsureHomeAuthInProgress)
            {
                Debug.Log(
                    "[LoginFlow][PlayerDataSyncManager.ShowSaveFoundPopup] Skip - EnsureHomeAuthentication still in progress.");
                return;
            }

            _isWaitingForSyncChoice = true;
            MarkPendingSyncChoiceForCurrentAccount();

            //EventManager.StopListening(EventName.GetServerData, GetServerData);
            //EventManager.StopListening(EventName.GetLocalData, GetLocalData);
            //EventManager.StartListening(EventName.GetServerData, GetServerData);
            //EventManager.StartListening(EventName.GetLocalData, GetLocalData);
            UIManager.Instance.Show(GameEnums.Features.SaveFound).Forget();
        }

        /// <summary>
        ///     Returns true when the current account still has an unresolved conflict from a previous popup.
        /// </summary>
        private static bool HasPendingSyncChoiceForCurrentAccount()
        {
            return HasPendingSyncChoice(PlayerDataManager.Account.AccountId);
        }

        /// <summary>
        ///     Returns true when the specified account still has an unresolved conflict from a previous popup.
        /// </summary>
        private static bool HasPendingSyncChoice(string accountId)
        {
            if (string.IsNullOrEmpty(accountId)) return false;

            return PlayerPrefs.GetString(PendingSyncChoiceAccountPrefsKey, string.Empty) == accountId;
        }

        /// <summary>
        ///     Marks the current account as waiting for the player to choose between local and server data.
        /// </summary>
        private static void MarkPendingSyncChoiceForCurrentAccount()
        {
            var accountId = PlayerDataManager.Account.AccountId;
            if (string.IsNullOrEmpty(accountId)) return;

            PlayerPrefs.SetString(PendingSyncChoiceAccountPrefsKey, accountId);
            PlayerPrefs.Save();
        }

        /// <summary>
        ///     Clears the unresolved sync choice marker for the current account.
        /// </summary>
        private static void ClearPendingSyncChoiceForCurrentAccount()
        {
            var accountId = PlayerDataManager.Account.AccountId;
            if (string.IsNullOrEmpty(accountId)) return;

            if (!HasPendingSyncChoice(accountId)) return;

            PlayerPrefs.DeleteKey(PendingSyncChoiceAccountPrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        ///     Reads PlayerSetting from server payload to determine which device last wrote the cloud save.
        /// </summary>
        private static bool TryGetServerSettingData(byte[] serverData, out PlayerSettingData settingData)
        {
            settingData = null;

            if (serverData == null) return false;

            try
            {
                var allData =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(TryDecodeServerData(serverData));

                if (allData == null || !allData.TryGetValue("PlayerSetting", out var settingJson) ||
                    string.IsNullOrEmpty(settingJson))
                    return false;

                settingData = JsonConvert.DeserializeObject<PlayerSettingData>(settingJson,
                    DataPlayerBase.JsonConvertSettings);
                return settingData != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[LoginFlow][PlayerDataSyncManager.TryGetServerSettingData] Failed to parse PlayerSetting from server data: " +
                    ex);
                return false;
            }
        }

        /// <summary>
        ///     Synchronizes data from a JSON string.
        /// </summary>
        /// <param name="dataJson">The JSON data string.</param>
        /// <param name="isValidOnOpenGame">Whether this is valid on open game.</param>
        /// <param name="isLogin">Whether this is part of login.</param>
        private static void SyncAllData(string dataJson, bool isValidOnOpenGame = false, bool isLogin = true)
        {
            var allData = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataJson);

            UnityAction syncAction = () =>
            {
                foreach (var data in allData) DataPlayer.GetModule(data.Key).SynchronizeData(data.Value);

                // SynchronizeData already writes each module to PlayerPrefs. Calling
                // SaveAllData here would re-serialize every *registered* module from
                // memory, including any whose key was absent from allData — that
                // would overwrite synced server data with stale local state. Just
                // flush PlayerPrefs to disk instead.
                PlayerPrefs.Save();
                ClearRuntimeDataAfterCloudPull();
            };

            var settingData =
                JsonConvert.DeserializeObject<PlayerSettingData>(allData["PlayerSetting"],
                    DataPlayerBase.JsonConvertSettings);

            void PushSyncedServerDataAndRestart(bool isCloseWaitingScreenAfterPush = true)
            {
#if !UNITY_EDITOR
                PlayerDataSyncManager.Instance.PushPlayerData(true, () => GameSystems.ShowWaitingScreen(false),
                    () =>
                    {
                        GameSystems.ShowWaitingScreen(true);
                        Instance.DelayRealTimeMethod(3f, GameSystems.RestartApplication);
                    }, isCloseWaitingScreenAfterPush: isCloseWaitingScreenAfterPush).Forget();
#else
                GameSystems.ShowSimpleMessage("Load data to client success");
                GameSystems.ShowWaitingScreen(false);
#endif
            }

            if (isValidOnOpenGame)
                if (string.IsNullOrEmpty(settingData.LastDevice) && settingData.LastTimeSyncData != 0)
                {
                    GameSystems.ShowWaitingScreen(true);
                    GameSystems.ShowSimpleMessage("backup_taking_data");
                    syncAction += () => PushSyncedServerDataAndRestart();
                    syncAction?.Invoke();
                    return;
                }

            if (!string.IsNullOrEmpty(settingData.LastDevice) &&
                settingData.LastDevice != SystemInfo.deviceUniqueIdentifier)
            {
                GameSystems.ShowMessage(
                    string.Format(GameSystems.Localize("backup_data_warning"),
                        PlayerDataManager.Settings.dataBase.LastTimeSyncData == 0
                            ? "..."
                            : new DateTimeOffset(GetDateTimeCreateData())
                                .ToString("yyyy\\/MM\\/dd hh\\:mm\\:ss")), "backup_data_warning_title",
                    () =>
                    {
                        if (isLogin)
                            ProfileManager.Logout(false);
                        else
                            Application.Quit();
                    }, () =>
                    {
                        GameSystems.ShowMessage(
                            isValidOnOpenGame ? "conflict_restore_confirm" : "login_restore_confirm", "", () =>
                            {
                                GameSystems.ShowWaitingScreen(true);
                                syncAction?.Invoke();
                                PushSyncedServerDataAndRestart(false);
                            },
                            () =>
                            {
                                //if (isValidOnOpenGame)
                                {
                                    Instance.PushPlayerData(true, showMessage: false, runInBackground: true).Forget();
                                    GameSystems.ShowWaitingScreen(false);
                                }
                                //else
                                //{
                                //    ProfileManager.Logout(false);
                                //}
                            });
                    });
            }
            else
            {
                if (!isValidOnOpenGame)
                {
                    syncAction += () => PushSyncedServerDataAndRestart();
                    syncAction?.Invoke();
                }
                else
                {
                    Instance.PushPlayerData(true, showMessage: false, runInBackground: true).Forget();
                    GameSystems.ShowWaitingScreen(false);
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        ///     Handler for force synchronization event.
        /// </summary>
        private void OnForceSyncData()
        {
            if (_isWaitingForSyncChoice || HasPendingSyncChoiceForCurrentAccount())
            {
                Debug.Log(
                    "[LoginFlow][PlayerDataSyncManager.OnForceSyncData] Skip force sync because sync choice popup is pending.");
                return;
            }

            PushPlayerData(true, runInBackground: true, showMessage: false).Forget();
        }

        /// <summary>
        ///     Handler for getting server data.
        /// </summary>
        public void GetServerData()
        {
            Debug.Log("[LoginFlow][PlayerDataSyncManager.GetServerData] Start.");
            _isWaitingForSyncChoice = false;
            ClearPendingSyncChoiceForCurrentAccount();
            //EventManager.StopListening(EventName.GetServerData, GetServerData);
            //EventManager.StopListening(EventName.GetLocalData, GetLocalData);
            if (_serverData == null)
            {
                Debug.LogWarning("[LoginFlow][PlayerDataSyncManager.GetServerData] _serverData is null.");
                return;
            }

            Debug.Log("[LoginFlow][PlayerDataSyncManager.GetServerData] Syncing server data to local.");
            SyncAllData(TryDecodeServerData(_serverData));
        }

        /// <summary>
        ///     Handler for getting local data.
        /// </summary>
        public void GetLocalData()
        {
            Debug.Log("[LoginFlow][PlayerDataSyncManager.GetLocalData] Start.");
            _isWaitingForSyncChoice = false;
            ClearPendingSyncChoiceForCurrentAccount();
            //EventManager.StopListening(EventName.GetServerData, GetServerData);
            //EventManager.StopListening(EventName.GetLocalData, GetLocalData);
            Debug.Log(
                "[LoginFlow][PlayerDataSyncManager.GetLocalData] Save cloud data from local and push in background.");
            PlayerDataManager.Account.SaveDataInCloud();
            PushPlayerData(true, DataPlayer.SaveAllData, DataPlayer.SaveAllData).Forget();
        }

        #endregion
    }
}