using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Ezg.Feature.Social.Account
{
    [Serializable]
    public class PlayerAccountData
    {
        [JsonProperty("acc_name")] public string accountName = GetDefaultAccountName();
        [JsonProperty("acc_email")] public string accountEmail = GetDefaultAccountEmail();
        [JsonProperty("acc_id")] public string accountId = GetDefaultAccountId();
        [JsonProperty("showed_warning_login")] public bool showedWaringLogin;
        public bool IsSaveDataToCloud;

        /// <summary>
        ///     Danh sách các gift code đã nhận
        /// </summary>
        public List<string> GiftCodeClaimed { get; set; } = new();

        private static string GetDefaultAccountId()
        {
#if UNITY_EDITOR
            return SystemInfo.deviceUniqueIdentifier;
#else
            return string.Empty;
#endif
        }

        private static string GetDefaultAccountName()
        {
#if UNITY_EDITOR
            var deviceId = SystemInfo.deviceUniqueIdentifier;
            return deviceId.Length <= 8 ? deviceId : deviceId.Substring(deviceId.Length - 8);
#else
            return string.Empty;
#endif
        }

        private static string GetDefaultAccountEmail()
        {
#if UNITY_EDITOR
            return $"{SystemInfo.deviceUniqueIdentifier}@gmail.com";
#else
            return string.Empty;
#endif
        }
    }
}