using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Firebase;
using Ezg.Feature.Shared;
using Ezg.Package.Factory;
using Ezg.Tracking;
using UnityEngine;
using UnityEngine.Events;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Social.Account
{
    public static class ProfileManager
    {
        #region Fields

        private static bool _updateDataInSession;
        private static UnityAction _pendingHomeAuthenticationContinueAction;

        /// <summary>
        ///     True while EnsureHomeAuthentication is waiting for Firebase sign-in to complete.
        /// </summary>
        public static bool IsEnsureHomeAuthInProgress { get; private set; }

        #endregion

        #region Public Methods

        #region Authentication

        /// <summary>
        ///     Checks if the user is currently logged in via global::Firebase.
        /// </summary>
        /// <returns>True if logged in and user data exists.</returns>
        public static bool IsLogon()
        {
            return TrackingService.IsInitFirebase && FirebaseLoginManager.GetUserData() != null;
        }

        /// <summary>
        ///     Initiates the login process using Firebase for Google (Android) or Apple (iOS).
        /// </summary>
        /// <param name="callback">Action to execute after successful login.</param>
        /// <param name="forceLogin">If true, forces a new login even if already logged on.</param>
        public static async UniTask Login(UnityAction callback = null, UnityAction onFail = null,
            bool forceLogin = false)
        {
#if UNITY_EDITOR
            Debug.Log("[LoginFlow][ProfileManager.Login] Skip login in UNITY_EDITOR.");
            return;
#endif
            Debug.Log(
                $"[LoginFlow][ProfileManager.Login] Start. forceLogin={forceLogin}, isLogon={IsLogon()}, platform={Application.platform}");

            if (IsLogon() && !forceLogin)
            {
                Debug.Log("[LoginFlow][ProfileManager.Login] Already logged in and forceLogin is false. Abort login.");
                onFail?.Invoke();
                return;
            }

            UnityAction callbackResult = MappingData;
            callbackResult += callback;
            UnityAction onFailResult = () =>
            {
                Debug.Log("[LoginFlow][ProfileManager.Login] Login failed. Hide waiting screen and invoke onFail.");
                GameSystems.ShowWaitingScreen(false);
                onFail?.Invoke();
            };
            Debug.Log("[LoginFlow][ProfileManager.Login] Show waiting screen and call provider sign-in.");
            GameSystems.ShowWaitingScreen(true);

#if UNITY_IOS
            Debug.Log("[LoginFlow][ProfileManager.Login] Calling SignInWithGameCenter.");
            await FirebaseLoginManager.SignInWithGameCenter(callbackResult, onFailResult);
#else
            Debug.Log("[LoginFlow][ProfileManager.Login] Calling SignInWithGoogle.");
            await FirebaseLoginManager.SignInWithGoogle(callbackResult, onFailResult);
#endif
            Debug.Log("[LoginFlow][ProfileManager.Login] Sign-in task completed.");
        }

        /// <summary>
        ///     Ensures the Home scene uses the correct authentication flow.
        ///     Existing sessions skip sign-in and validate cloud data directly.
        /// </summary>
        /// <param name="onContinueWithoutFreshLogin">
        ///     Action executed when Home continues without a fresh sign-in.
        /// </param>
        public static void EnsureHomeAuthentication(UnityAction onContinueWithoutFreshLogin = null)
        {
#if UNITY_EDITOR
            onContinueWithoutFreshLogin?.Invoke();
            return;
#endif
            IsEnsureHomeAuthInProgress = true;
            _pendingHomeAuthenticationContinueAction = onContinueWithoutFreshLogin;
            Debug.Log($"[LoginFlow][ProfileManager.EnsureHomeAuthentication] Start. isLogon={IsLogon()}");
            GameSystems.ShowWaitingScreen(true);
            if (IsLogon())
            {
                Debug.Log(
                    "[LoginFlow][ProfileManager.EnsureHomeAuthentication] Existing session detected. Map account and validate cloud data.");
                IsEnsureHomeAuthInProgress = false;
                MapFirebaseUserToLocalAccount();
                PlayerDataSyncManager.Instance
                    .CheckServerDataForLoggedInUser(TakePendingHomeAuthenticationContinueAction()).Forget();
                return;
            }

            Debug.Log("[LoginFlow][ProfileManager.EnsureHomeAuthentication] No active session. Start sign-in flow.");
            Login(null, () =>
            {
                IsEnsureHomeAuthInProgress = false;
                GameSystems.ShowWaitingScreen(false);
                TakePendingHomeAuthenticationContinueAction()?.Invoke();
            }).Forget();
        }

        /// <summary>
        ///     Logs the user out and optionally pushes local data to the server before restarting.
        /// </summary>
        /// <param name="pushData">Whether to push player data before logging out.</param>
        public static void Logout(bool pushData = true)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                GameSystems.ShowSimpleMessage("internet_required");
                return;
            }

            GameSystems.ShowWaitingScreen(true);

#if UNITY_ANDROID
            FirebaseLoginManager.OnGoogleLogout(() => PerformLogout(pushData));
#elif UNITY_IOS
            FirebaseLoginManager.OnGameCenterLogout(() => PerformLogout(pushData));
#endif
        }

        /// <summary>
        ///     Executes the logout sequence after Firebase sign-out completes.
        ///     Shared across all platforms.
        /// </summary>
        private static void PerformLogout(bool pushData)
        {
            if (pushData)
            {
                GameSystems.ShowWaitingScreen(true);
                PlayerDataSyncManager.Instance.PushPlayerData(true,
                    () => GameSystems.ShowWaitingScreen(false),
                    () =>
                    {
                        ClearLocalData();
                        GameSystems.RestartApplication();
                    },
                    isLogout: true).Forget();
            }
            else
            {
                PlayerDataSyncManager.Instance.DelayRealTimeMethod(3f, () =>
                {
                    ClearLocalData();
                    GameSystems.RestartApplication();
                });
            }
        }

        /// <summary>
        ///     Clears all in-memory and cached player data before restart or logout.
        /// </summary>
        private static void ClearLocalData()
        {
            DeleteAllLocalPlayerData();
            DataPlayer.ClearAllData();
            DataPlayer.ClearData();
            // removed: TutorialContainer.ClearData() (gameplay removed)
        }

        /// <summary>
        ///     Validates if the player has entered a name.
        /// </summary>
        public static void ValidInputName()
        {
            // if (string.IsNullOrEmpty(PlayerDataManager.Account.AccountName))
            // {
            //     UIManager.Instance.Show(GameEnums.Features.InputName, isAsync: true).Forget();
            // }
        }

        #endregion

        #region Data Management

        /// <summary>
        ///     Maps Firebase user info to local account data and fetches player data.
        /// </summary>
        public static void MappingData()
        {
            IsEnsureHomeAuthInProgress = false;
            MapFirebaseUserToLocalAccount();
            Debug.Log("[LoginFlow][ProfileManager.MappingData] Request player data sync.");
            PlayerDataSyncManager.Instance.GetPlayerData(onSyncResolved: TakePendingHomeAuthenticationContinueAction());
        }

        /// <summary>
        ///     Fetches profile data from the server and updates local state.
        /// </summary>
        public static async UniTask GetAndUpdateProfileData()
        {
            // if (!await GameNetworkManager.Instance.ValidActionFunction())
            // {
            //     return;
            // }
            //
            // if (ProfileData == null)
            // {
            //     var result = await GameNetworkManager.Instance.Supabase().From<ProfileModel>().Where(x => x.AccountId == PlayerDataManager.Account.AccountId).Get();
            //     ProfileData = result.Model;
            //     if (ProfileData != null)
            //     {
            //         await UpdateProfileData();
            //     }
            //     else
            //     {
            //         ProfileData = new ProfileModel()
            //         {
            //             AccountId = PlayerDataManager.Account.AccountId,
            //             Name = FirebaseLoginManager.GetUserData().DisplayName,
            //             Email = PlayerDataManager.Account.Email,
            //             CreatedTime = TimeManager.GetNow(),
            //             IsBlock = PlayerDataManager.Account.dataBase.IsLocked,
            //         };
            //         await UpdateProfileData();
            //     }
            // }
            // else
            // {
            //     await UpdateProfileData();
            // }
        }

        /// <summary>
        ///     Creates a new profile or updates an existing one on the server.
        /// </summary>
        /// <returns>Always returns true (work in progress).</returns>
        public static async UniTask<bool> CreateOrUpdateData()
        {
            // if (!await GameNetworkManager.Instance.ValidActionFunction())
            //     return false;
            //
            // var onlineData = await GameNetworkManager.Instance.Supabase().From<ProfileModel>().Where(x => x.AccountId == PlayerDataManager.Account.AccountId).Get();
            //
            // ProfileData = onlineData.Model;
            // if (ProfileData != null)
            // {
            //     ProfileData.Name = FirebaseLoginManager.GetUserData().DisplayName;
            // }
            // else
            // {
            //     ProfileData = new ProfileModel()
            //     {
            //         AccountId = PlayerDataManager.Account.AccountId,
            //         Name = FirebaseLoginManager.GetUserData().DisplayName,
            //         Email = PlayerDataManager.Account.Email,
            //         CreatedTime = TimeManager.GetNow(),
            //     };
            // }
            //
            // await UpdateProfileData();
            return true;
        }

        /// <summary>
        ///     Resets all local player data by clearing static fields in PlayerDataManager.
        /// </summary>
        public static void DeleteAllLocalPlayerData()
        {
            PlayerDataManager.ClearCachedModules();
        }

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        ///     Logic for updating profile data before syncing with Supabase (internal use).
        /// </summary>
        private static async UniTask UpdateProfileData()
        {
            // if (!await GameNetworkManager.Instance.ValidActionFunction())
            //     return;
            //
            // if (ProfileData.IsBlock != PlayerDataManager.Account.dataBase.IsLocked)
            // {
            //     PlayerDataManager.Account.dataBase.IsLocked = ProfileData.IsBlock;
            //     PlayerDataManager.Account.Save();
            //     PlayerDataSyncManager.Instance.PushPlayerData(true, runInBackground: true, showMessage: false).Forget();
            // }
            //
            // if (PlayerDataManager.Account.dataBase.IsLocked || ProfileData == null || ProfileData.IsBlock)
            // {
            //     return;
            // }
            //
            // ProfileData.TotalHeroLevel = PlayerDataManager.PlayerResource.GetAllHeroOwned().Sum(x => HeroesManager.GetHeroLevel(x));
            // ProfileData.EquipmentLevel = PlayerDataManager.PlayerResource.dataBase.EquipmentLevel.Sum(x => x.Value);
            // ProfileData.TalentLevel = TalentManager.GetFeatureLevel();
            // ProfileData.HeroUnlocked = PlayerDataManager.PlayerResource.GetAllHeroOwned().Count;
            // ProfileData.TotalRuns = (int)PlayerDataManager.Statis.dataBase.BattlePlayTimes;
            // ProfileData.TotalWins = (int)PlayerDataManager.Statis.dataBase.BattleWinTimes;
            // ProfileData.Gold = PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.Coin);
            // ProfileData.Gem = PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.Gem);
            // ProfileData.Email = PlayerDataManager.Account.Email;
            // ProfileData.UpdatedTime = TimeManager.GetNow();

            //await GameNetworkManager.Instance.Supabase().From<ProfileModel>().Upsert(ProfileData);
        }

        /// <summary>
        ///     Maps the authenticated Firebase user into the local account model.
        /// </summary>
        private static void MapFirebaseUserToLocalAccount()
        {
            Debug.Log("[LoginFlow][ProfileManager.MapFirebaseUserToLocalAccount] Start.");
            var userInfo = FirebaseLoginManager.GetUserData();
            if (userInfo == null)
            {
                Debug.LogWarning("[LoginFlow][ProfileManager.MapFirebaseUserToLocalAccount] Firebase user is null.");
                return;
            }

            Debug.Log(
                $"[LoginFlow][ProfileManager.MapFirebaseUserToLocalAccount] Firebase user found. userId={userInfo.UserId}, displayName={userInfo.DisplayName}, email={userInfo.Email}");
            PlayerDataManager.Account.SetAccountId(userInfo.UserId);
            if (string.IsNullOrEmpty(PlayerDataManager.Account.AccountName))
                PlayerDataManager.Account.SetAccountName(userInfo.DisplayName);
            PlayerDataManager.Account.AccountEmail = userInfo.Email;
        }

        private static UnityAction TakePendingHomeAuthenticationContinueAction()
        {
            var action = _pendingHomeAuthenticationContinueAction;
            _pendingHomeAuthenticationContinueAction = null;
            return action;
        }

        #endregion
    }
}