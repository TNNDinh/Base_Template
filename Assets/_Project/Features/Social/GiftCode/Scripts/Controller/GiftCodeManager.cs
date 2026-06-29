using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.Networking;
using Ezg.Feature.Shared;
using Postgrest;
using Sirenix.Utilities;
using UnityEngine;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.Social.GiftCode
{
    public static class GiftCodeManager
    {
        /// <summary>
        ///     Tạo gift code mới (admin)
        /// </summary>
        public const int MIN_CODE_LENGTH = 4;

        public const int MAX_CODE_LENGTH = 15;

        /// <summary>
        ///     Nhận gift code
        /// </summary>
        public static async UniTask ClaimGiftCode(string giftCode)
        {
            if (!ValidUseGiftCode(giftCode))
                return;

            var supabase = GameNetworkManager.Instance.Supabase();
            if (supabase == null)
            {
                GameSystems.ShowSimpleMessage("gift_code_wrong");
                return;
            }

            GiftCodeModel result = null;
            try
            {
                var response = await supabase
                    .From<GiftCodeModel>()
                    .Filter("id", Constants.Operator.Equals, giftCode)
                    .Get()
                    .AsUniTask()
                    .Timeout(TimeSpan.FromSeconds(10));
                result = response?.Models?.FirstOrDefault();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GiftCode] Fetch error: {e.Message}");
            }

            if (result == null)
            {
                GameSystems.ShowSimpleMessage("gift_code_wrong");
                return;
            }

            if (!ValidClaimGiftCode(result))
                return;

            if (result.HaveExpired)
            {
                bool isExpired;
                if (TimeManager.IsOnline is null or false)
                {
                    var expiredFlag = false;
                    await TimeManager.GetOnlineTime(() =>
                    {
                        expiredFlag = result.ExpiredTime < TimeManager.GetOnlineNow();
                    });
                    isExpired = expiredFlag;
                }
                else
                {
                    isExpired = result.ExpiredTime < TimeManager.GetOnlineNow();
                }

                if (isExpired)
                {
                    GameSystems.ShowSimpleMessage("gift_code_expired");
                    return;
                }
            }

            var rewards = GetGiftCodeRewards(result);
            if (rewards == null)
                return;

            try
            {
                await supabase.Rpc("rpc_claim_gift_code", new Dictionary<string, object> { { "p_code", giftCode } });
            }
            catch (Exception e)
            {
                Debug.LogError($"[GiftCode] RPC error: {e.Message}");
                GameSystems.ShowSimpleMessage("gift_code_wrong");
                return;
            }

            rewards.ForEach(x => { Debug.Log(x.resType + ": " + x.resId); });

            PlayerDataManager.Account.dataBase.GiftCodeClaimed.Add(giftCode);

            var combinedRewards = new List<Resource>();
            foreach (var reward in rewards)
                if (reward.resType == EnumBase.ResourceTypes.Package)
                    combinedRewards.AddRange(ShopService.ActivatePackByResId(reward.resId));
                else
                    combinedRewards.Add(reward);
            if (combinedRewards.Any())
                RewardsService.ReceiveReward(combinedRewards, source: SourceTracking.Gameplay,
                    sourceDetail: SourceDetailTracking.Giftcode, sourceId: giftCode);

            PlayerDataManager.Account.Save();
        }

        /// <summary>
        ///     Check các điều kiện sử dụng
        /// </summary>
        private static bool ValidUseGiftCode(string giftCode)
        {
            if (IsClaimed(giftCode))
            {
                GameSystems.ShowSimpleMessage("gift_code_used");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Check các điều kiện để nhận
        /// </summary>
        private static bool ValidClaimGiftCode(GiftCodeModel data)
        {
            if (!data.IsActive || (data.IsLimit && data.ClaimedTimes >= data.LimitTimes))
            {
                GameSystems.ShowSimpleMessage("gift_code_wrong");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Đã nhận gift code hay chưa
        /// </summary>
        private static bool IsClaimed(string giftCode)
        {
            return PlayerDataManager.Account.dataBase.GiftCodeClaimed.Contains(giftCode);
        }

        /// <summary>
        ///     Đọc reward từ giftCode
        /// </summary>
        public static List<Resource> GetGiftCodeRewards(GiftCodeModel data)
        {
            if (data.ResId is not { Length: > 0 })
                return null;

            var resTypes = data.ResType.Split(';');
            var resIds = data.ResId.Split(';');
            var resNumber = data.ResNumber.Split(';');
            var customValue = data.ResCustomValue?.Split(';');

            if (resTypes.Length == resIds.Length && resTypes.Length == resNumber.Length && resTypes.Length != 0)
            {
                var res = new List<Resource>();
                var index = 0;
                resTypes.ForEach(x =>
                {
                    if (!string.IsNullOrEmpty(resTypes[index]))
                        res.Add(new Resource
                        {
                            resType = Convert.ToInt32(resTypes[index]),
                            resId = Convert.ToInt32(resIds[index]),
                            resNumber = Convert.ToInt32(resNumber[index]),
                            customValue = string.IsNullOrEmpty(data.ResCustomValue)
                                ? null
                                : string.IsNullOrEmpty(customValue[index])
                                    ? null
                                    : customValue[index].Contains(",")
                                        ? customValue[index].Split(',').Select(Convert.ToSingle).ToArray()
                                        : new[] { Convert.ToSingle(customValue[index]) }
                        });
                    index++;
                });

                return res.GenerateReward();
            }

            return null;
        }

        public static async UniTask CreateGiftCode(List<Resource> reward, int limitTimes,
            string resType,
            string resId,
            string resNumber,
            string resCustom,
            bool haveExpired,
            long expiredTime,
            string manualGiftCode = null, string versionLimit = null, int length = 6)
        {
            var codeLength = Mathf.Clamp(length, MIN_CODE_LENGTH, MAX_CODE_LENGTH);
            var giftCode = manualGiftCode == null ? codeLength.RandomString() : manualGiftCode.ToUpper();

            var giftCodeData = new GiftCodeModel
            {
                Id = giftCode,
                IsActive = true,
                IsLimit = limitTimes > 0,
                LimitTimes = limitTimes,
                ClaimedTimes = 0,
                CreatedBy = PlayerDataManager.Account.AccountName,
                HaveExpired = haveExpired,
                ExpiredTime = expiredTime,
                ResId = resId,
                ResType = resType,
                ResNumber = resNumber,
                ResCustomValue = resCustom,
                VersionLimit = versionLimit
            };

            GameSystems.ShowWaitingScreen(true);
            try
            {
                await GameNetworkManager.Instance.Supabase()
                    .From<GiftCodeModel>()
                    .Insert(giftCodeData);
                GameSystems.ShowSimpleMessage("Create gift code successfully: " + giftCode);
                GUIUtility.systemCopyBuffer = giftCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GiftCode] Create error: {e.Message}");
                GameSystems.ShowSimpleMessage("gift_code_create_failed");
            }

            GameSystems.ShowWaitingScreen(false);
        }
    }
}