using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ezg.Core.Security;
using Ezg.Core.Utils;
using Ezg.Feature.Networking;
using Ezg.Feature.Shared;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.System.Admin
{
    public static class AdminManager
    {
        public enum AdminResponseTypes
        {
            NotAdmin,
            NotHaveUser,
            Failed,
            Success
        }

        public static bool AdminManagerEvent;

        public static bool IsAd;

        public static List<string> DevicesTest = new();

        public static AdminModel AdData;

        public static async UniTask InitAdmin()
        {
#if !UNITY_EDITOR
            if (!ProfileManager.IsLogon() || string.IsNullOrEmpty(PlayerDataManager.Account.AccountId))
                return;
#endif

            if (Application.internetReachability == NetworkReachability.NotReachable) return;

            if (GameNetworkManager.Instance.Supabase() == null) return;

            var result = await GameNetworkManager.Instance.Supabase()
                .From<AdminModel>().Where(x => x.Id == PlayerDataManager.Account.AccountId).Get();
            IsAd = result.Models.Any();
            AdData = result.Model;
            EventManager.EmitEventData(nameof(AdminManagerEvent), IsAd);
        }

        public static async UniTask InitTestDevice()
        {
            if (DevicesTest is { Count: > 0 }) return;

            if (Application.internetReachability == NetworkReachability.NotReachable) return;

            if (GameNetworkManager.Instance.Supabase() == null) return;

            var result = await GameNetworkManager.Instance.Supabase()
                .From<TestDeviceModel>().Get();
            if (result.Models.Any()) DevicesTest = result.Models.Select(x => x.Device).ToList();
        }

        public static async UniTask Login(string password, UnityAction callBack)
        {
            GameSystems.ShowWaitingScreen(true);
            var result = await GameNetworkManager.Instance.Supabase().Rpc("admin_login", new Dictionary<string, object>
            {
                { "admin", PlayerDataManager.Account.AccountId },
                { "password", SecuritySystems.CreateMD5(password) }
            });

            if (result.ResponseMessage is not { IsSuccessStatusCode: true })
            {
                GameSystems.ShowSimpleMessage("Failed");
                GameSystems.ShowWaitingScreen(false);
                return;
            }

            switch ((AdminResponseTypes)Convert.ToInt32(result.Content))
            {
                case AdminResponseTypes.NotAdmin:
                    GameSystems.ShowSimpleMessage("Wrong password");
                    break;
                case AdminResponseTypes.NotHaveUser:
                    break;
                case AdminResponseTypes.Failed:
                    break;
                case AdminResponseTypes.Success:
                    callBack?.Invoke();
                    break;
            }

            GameSystems.ShowWaitingScreen(false);
        }

        public static async UniTask CheckUserId(string userId, UnityAction successAction)
        {
            GameSystems.ShowWaitingScreen(true);

            if (Application.internetReachability == NetworkReachability.NotReachable ||
                GameNetworkManager.Instance.Supabase() == null)
            {
                GameSystems.ShowSimpleMessage("Failed");
                GameSystems.ShowWaitingScreen(false);
                return;
            }

            var result = await GameNetworkManager.Instance.Supabase()
                .Rpc("check_user_exists", new Dictionary<string, object>
                {
                    { "user_id", userId }
                });

            if (result.ResponseMessage is not { IsSuccessStatusCode: true })
            {
                GameSystems.ShowSimpleMessage("Failed");
                GameSystems.ShowWaitingScreen(false);
                return;
            }

            if (Convert.ToInt32(result.Content) == (int)AdminResponseTypes.Success)
                successAction?.Invoke();
            else
                GameSystems.ShowSimpleMessage("User not found");

            GameSystems.ShowWaitingScreen(false);
        }

        public static async UniTask SendMail(
            bool isGlobal,
            string admin,
            string password,
            string targetUserId,
            string title,
            string content,
            string resType,
            string resId,
            string resNumber,
            string resCustom,
            int lifeTime,
            UnityAction callBack)
        {
            GameSystems.ShowWaitingScreen(true);

            var now = TimeManager.GetNow();

            var result = isGlobal
                ? await GameNetworkManager.Instance.Supabase().Rpc("send_email_global", new Dictionary<string, object>
                {
                    { "admin", admin },
                    { "password", SecuritySystems.CreateMD5(password) },
                    { "title", title },
                    { "content", content },
                    { "created_time", now },
                    { "expired_time", now + 86400 * lifeTime },
                    { "res_type", resType },
                    { "res_id", resId },
                    { "res_number", resNumber },
                    { "res_custom", resCustom }
                })
                : await GameNetworkManager.Instance.Supabase().Rpc("send_email", new Dictionary<string, object>
                {
                    { "admin", admin },
                    { "password", SecuritySystems.CreateMD5(password) },
                    { "target_user_id", targetUserId },
                    { "title", title },
                    { "content", content },
                    { "created_time", now },
                    { "res_type", resType },
                    { "res_id", resId },
                    { "res_number", resNumber },
                    { "res_custom", resCustom }
                });

            if (result.ResponseMessage is not { IsSuccessStatusCode: true })
            {
                GameSystems.ShowSimpleMessage("Failed");
                GameSystems.ShowWaitingScreen(false);
                return;
            }

            switch ((AdminResponseTypes)Convert.ToInt32(result.Content))
            {
                case AdminResponseTypes.NotAdmin:
                    GameSystems.ShowSimpleMessage("You are not admin");
                    break;
                case AdminResponseTypes.NotHaveUser:
                    GameSystems.ShowSimpleMessage("Cannot found user");
                    break;
                case AdminResponseTypes.Failed:
                    break;
                case AdminResponseTypes.Success:
                    GameSystems.ShowSimpleMessage("Send success");
                    callBack?.Invoke();
                    break;
            }

            GameSystems.ShowWaitingScreen(false);
        }

        public static async UniTask ChangePassword(string oldPass, string newPass, UnityAction callBack)
        {
            GameSystems.ShowWaitingScreen(true);
            var result = await GameNetworkManager.Instance.Supabase().Rpc("admin_change_pass",
                new Dictionary<string, object>
                {
                    { "admin", PlayerDataManager.Account.AccountId },
                    { "old_pass", SecuritySystems.CreateMD5(oldPass) },
                    { "new_pass", SecuritySystems.CreateMD5(newPass) }
                });

            if (result.ResponseMessage is not { IsSuccessStatusCode: true })
            {
                GameSystems.ShowSimpleMessage("Failed");
                GameSystems.ShowWaitingScreen(false);
                return;
            }

            switch ((AdminResponseTypes)Convert.ToInt32(result.Content))
            {
                case AdminResponseTypes.NotAdmin:
                    GameSystems.ShowSimpleMessage("Wrong old password");
                    break;
                case AdminResponseTypes.NotHaveUser:
                    break;
                case AdminResponseTypes.Failed:
                    break;
                case AdminResponseTypes.Success:
                    GameSystems.ShowSimpleMessage("Change password success");
                    callBack?.Invoke();
                    break;
            }

            GameSystems.ShowWaitingScreen(false);
        }

        public static bool ValidateDate(int year, int month, int day)
        {
            if (year < 2000 || year > 9999) return false;

            if (month < 1 || month > 12) return false;

            try
            {
                var date = new DateTime(year, month, day);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}