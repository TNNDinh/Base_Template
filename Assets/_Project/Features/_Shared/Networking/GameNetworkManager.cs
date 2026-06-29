using Cysharp.Threading.Tasks;
using Ezg.Core.Networking;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.Account;
using UnityEngine;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.Networking
{
    /// <summary>
    ///     Manages network communication, combining Supabase (Read) and Cloudflare Workers (Write).
    /// </summary>
    public class GameNetworkManager : SupabaseManager<GameNetworkManager>
    {
        #region Public Methods

        // --- Endpoint Access ---

        /// <summary>
        ///     Creates a Cloudflare query for a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The data type expected from the endpoint.</typeparam>
        /// <param name="endPoint">The relative path to the endpoint.</param>
        /// <returns>A new CloudflareQuery instance.</returns>
        public static CloudflareQuery<T> Endpoint<T>(string endPoint)
        {
            return new CloudflareQuery<T>(endPoint);
        }

        // --- Validation ---

        /// <summary>
        ///     Validates if network actions can be performed (online status, login status, etc.).
        /// </summary>
        /// <returns>True if the action is valid, false otherwise.</returns>
        public async UniTask<bool> ValidActionFunction()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable) return false;

            if (!ProfileManager.IsLogon() || string.IsNullOrEmpty(PlayerDataManager.Account.AccountId))
                //|| (ProfileManager.ProfileData != null && ProfileManager.ProfileData.IsBlock))
                return false;

            if (!IsOnline)
            {
                await Init();
                //await TimeManager.GetOnlineTime();
                return IsOnline;
            }

            return IsOnline;
        }

        #endregion
    }
}