using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Package.ColorSystem;
using Sirenix.Utilities;
using TigerForge;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

public enum PurchaseType
{
    None = -1,
    Free = 0,
    Ads = 1,
    Currency = 2,
    IAP = 3,
    Event = 4
}

public static class RewardsService
{
    #region Extract / Exchange

    public static Resource[] ExtractResource(List<Resource> resources)
    {
        var result = resources.Select(r => r.Clone()).ToList();

        var exchangeAll = DataManager.ResourceExchange.GetAll();
        var toExtract = result
            .Where(x => exchangeAll.Any(y => y.sourceId == x.resId && y.sourceType == x.resType))
            .ToArray();

        if (!toExtract.Any()) return resources.ToArray();

        foreach (var res in toExtract)
        {
            var dataExtract = DataManager.ResourceExchange.GetResourceExchange(res.resId, res.resType);

            var resToAdd = new List<ResourceExchangeDetailModel>();
            if (dataExtract.isGacha)
                for (var i = 0; i < res.resNumber; i++)
                    resToAdd.Add(dataExtract.rewards[dataExtract.rewards.Select(x => x.rate).GetIndexInRate()]);
            else
                resToAdd.AddRange(dataExtract.rewards.CloneJson());

            foreach (var resAdd in resToAdd)
                switch (resAdd.resType)
                {
                    case EnumBase.ResourceTypes.None:
                        break;
                    case EnumBase.ResourceTypes.Money:
                        result.Add(new MoneyResouces
                        {
                            resType = resAdd.resType,
                            resId = resAdd.resId,
                            resNumber = resAdd.resNumber
                        });
                        break;
                    case EnumBase.ResourceTypes.Item:
                        result.Add(new Resource
                        {
                            resType = resAdd.resType,
                            resId = resAdd.resId,
                            resNumber = resAdd.resNumber,
                            customValue = resAdd.customValue
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            result.Remove(res);
        }

        return result.ToArray();
    }

    #endregion

    [Serializable]
    public class CostRequire
    {
        public int costType;
        public int costId;
        public long costNumber;

        public Resource GetResource()
        {
            return new Resource
            {
                resType = costType,
                resId = costId,
                resNumber = costNumber
            };
        }
    }

    #region Receive

    public static Resource[] ReceiveReward(Resource reward, PurchaseType purchasedType = PurchaseType.Free,
        string source = "",
        string sourceDetail = "",
        string sourceId = "",
        string placement = "", bool isShowPopup = true, string productId = null, bool updateResource = true,
        Action onClose = null, bool isSpawnCurrency = false)
    {
        if (reward == null) return null;

        return ReceiveRewards(new[] { reward }, purchasedType, source, sourceDetail, sourceId, placement,
            isShowPopup, productId, updateResource, onClose, isSpawnCurrency);
    }

    public static Resource[] ReceiveReward(List<Resource> rewards, PurchaseType purchasedType = PurchaseType.Free,
        string source = "",
        string sourceDetail = "",
        string sourceId = "",
        string placement = "", bool isShowPopup = true, string productId = null, bool isSpawnCurrency = false,
        Action onClose = null)
    {
        if (rewards is not { Count: > 0 }) return null;

        return ReceiveRewards(rewards.ToArray(), purchasedType, source, sourceDetail, sourceId, placement,
            isShowPopup, productId, onClose: onClose, isSpawnCurrency: isSpawnCurrency);
    }

    public static Resource[] ReceiveRewards(Resource[] rewards, PurchaseType purchasedType = PurchaseType.Free,
        string source = "",
        string sourceDetail = "",
        string sourceId = "",
        string placement = "",
        bool isShowPopup = true,
        string productId = null,
        bool updateResource = true,
        Action onClose = null,
        bool isSpawnCurrency = false)
    {
        if (rewards is not { Length: > 0 }) return null;

        rewards = ExtractResource(rewards.ToList());

        foreach (var reward in rewards)
            ReceiveAReward(reward, updateResource, isSpawnCurrency);

        // Lưu 1 lần sau cả batch thay vì mỗi reward (tránh ghi PlayerPrefs lặp).
        PlayerDataManager.PlayerResource.Save();

        // removed: SpawnCurrencyManager (gameplay removed)
        if (!isSpawnCurrency)
            EventManager.EmitEvent(nameof(EventName.RunIncreaseCurrency));

        ShowToast(rewards, purchasedType, isShowPopup, onClose);
        Tracking(rewards, source, sourceDetail, sourceId, placement, purchasedType, productId);
        return rewards;
    }

    private static void ReceiveAReward(Resource reward, bool updateResource = true, bool isRunAnimation = false)
    {
        switch (reward.resType)
        {
            case EnumBase.ResourceTypes.Money:
                PlayerDataManager.PlayerResource.AddCurrency((EnumBase.MoneyTypes)reward.resId, reward.resNumber,
                    updateResource, isRunAnimation);
                break;

            case EnumBase.ResourceTypes.Item:
                // removed: PlayerDataManager.Gameplay (gameplay removed)
                break;

            case EnumBase.ResourceTypes.Package:
                ShopService.ActivatePackByResId(reward.resId);
                break;
        }
    }

    #endregion

    #region Tracking / Toast

    public static void Tracking(Resource[] resources, string source = "",
        string sourceDetail = "",
        string sourceId = "",
        string placement = "", PurchaseType purchasedType = PurchaseType.Free, string productId = null)
    {
        var resTemp = resources.Select(x => x.Clone()).ToArray().CompileRewardsForTracking().ToArray();
        if (resTemp is not { Length: > 0 }) return;

        string OrNull(string s)
        {
            return s != "" ? s : null;
        }

        long Remaining(Resource x)
        {
            return x.resType == EnumBase.ResourceTypes.Money
                ? PlayerResource.GetCurrencyValue((EnumBase.MoneyTypes)x.resId)
                : PlayerDataManager.PlayerResource.dataBase.Items.Count(y => y.resId == x.resId);
        }

        foreach (var x in resTemp)
            if (purchasedType == PurchaseType.IAP)
                FirebaseEvent.buy_resource.Send(new FirebaseEventConfig
                {
                    event_id = Guid.NewGuid().ToString(),
                    source = source,
                    source_detail = sourceDetail,
                    sourceId = OrNull(sourceId),
                    product_id = productId,
                    item_buy = x.GetName(),
                    value = x.resNumber,
                    remaining_value = Remaining(x),
                    item_id = x.resId
                }).Forget();
            else
                FirebaseEvent.earn_resource.Send(new FirebaseEventConfig
                {
                    source = source,
                    source_detail = OrNull(sourceDetail),
                    sourceId = OrNull(sourceId),
                    placement = purchasedType == PurchaseType.Ads ? OrNull(placement) : null,
                    item_type = x.GetName(),
                    value = x.resNumber,
                    remaining_value = Remaining(x),
                    item_id = x.resId
                }).Forget();
    }

    public static void ShowToast(Resource[] rewards, PurchaseType type, bool isShowPopup = true, Action onClose = null)
    {
        if (!isShowPopup) return;
        GameSystems.ShowRewardPopup(rewards.CompileRewards(), type, onClose);
    }

    #endregion

    #region Compile

    public static List<Resource> CompileRewards(this IEnumerable<Resource> resources)
    {
        var result = new List<Resource>();
        foreach (var item in resources)
        {
            // Equipment không cộng dồn — giữ riêng từng cái và giữ nguyên kiểu con (Rarity...).
            if (item.resType == EnumBase.ResourceTypes.Item &&
                PlayerResource.GetItemTypeById(item.resId) == EnumBase.ItemTypes.Equipment)
            {
                result.Add(item);
                continue;
            }

            var existing = result.FirstOrDefault(x => x.resType == item.resType && x.resId == item.resId);
            if (existing != null)
                existing.resNumber += item.resNumber;
            else
                // Item giữ nguyên kiểu con (không clone); resource khác clone để cộng dồn không sửa vào nguồn.
                result.Add(item.resType == EnumBase.ResourceTypes.Item ? item : item.Clone());
        }

        return result;
    }

    private static List<Resource> CompileRewardsForTracking(this Resource[] resources)
    {
        var result = new List<Resource>();
        foreach (var item in resources)
        {
            if (item.resType == EnumBase.ResourceTypes.Item)
            {
                result.Add(item);
                continue;
            }

            var existing = result.FirstOrDefault(x => x.resType == item.resType && x.resId == item.resId);
            if (existing != null)
                existing.resNumber += item.resNumber;
            else
                result.Add(item);
        }

        return result;
    }

    #endregion

    #region Generate / Bonus / Misc

    public static Resource[] GenerateReward(this Resource[] rewards)
    {
        return rewards.Where(x => IsGeneratable(x.resType)).ToArray();
    }

    public static List<Resource> GenerateReward(this List<Resource> rewards)
    {
        return rewards.Where(x => IsGeneratable(x.resType)).ToList();
    }

    public static Resource GenerateReward(this Resource reward)
    {
        return reward;
    }

    private static bool IsGeneratable(int resType)
    {
        switch (resType)
        {
            case EnumBase.ResourceTypes.None:
            case EnumBase.ResourceTypes.Money:
            case EnumBase.ResourceTypes.Feature:
            case EnumBase.ResourceTypes.Package:
            case EnumBase.ResourceTypes.Item:
                return true;
            default:
                return false;
        }
    }

    public static string ValidResource(this long value, EnumBase.MoneyTypes type, bool useMoneyConvert = false)
    {
        return (useMoneyConvert ? value.MoneyConvert() : value.ToString("n0"))
            .SetColor(PlayerResource.IsEnough(type, value)
                ? DataManager.GeneralAssets.CurrencyEnough
                : DataManager.GeneralAssets.CurrencyNotEnough);
    }

    /// <summary>
    ///     Lấy rewards bonus từ các gói bán được bonus theo stage
    /// </summary>
    public static Resource[] GetFinalRewardsBonus(this PackRewards[] rewards)
    {
        var result = rewards.CloneJson();
        result.ForEach(x =>
            x.resNumber = (long)(x.bonus > 0
                ? x.resNumber + (x.stageBonus == 0
                    ? 0
                    : x.bonus * (PlayerDataManager.Campaign.HighestLevel / x.stageBonus))
                : x.resNumber));

        return result.GenerateReward().ToArray();
    }

    public static Resource GetFinalRewardsBonus(this PackRewards rewards)
    {
        var result = rewards.CloneJson();
        result.resNumber = (long)(result.bonus > 0
            ? result.resNumber + (result.stageBonus == 0
                ? 0
                : result.bonus * (PlayerDataManager.Campaign.HighestLevel / result.stageBonus))
            : result.resNumber);

        return result.GenerateReward();
    }

    #endregion
}