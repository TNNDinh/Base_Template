using System.Collections.Generic;
using System.Linq;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.RedDot;
using Ezg.Feature.Shared;
using Ezg.Package.Factory;
using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;

public class PlayerShop : DataPlayerBaseGeneric<PlayerShopData>
{
    public EnumBase.MoneyTypes GetCurrentScrollReshuffle => dataBase.currentScrollReshuffle;

    public void SetCurrentScrollReshuffle(EnumBase.MoneyTypes scroll)
    {
        dataBase.currentScrollReshuffle = scroll;
        Save();
    }

    protected override void AfterLoad()
    {
        base.AfterLoad();
        ValidLimitedPack();
    }

    /// <summary>
    ///     valid data các pack, xoá các pack đã hết hạn
    ///     gọi khi load game và trở về home
    /// </summary>
    public void ValidLimitedPack()
    {
        foreach (var pack in dataBase.packLimitPurchaseDict.ToList())
            if (TimeManager.GetNow() >= pack.Value)
                dataBase.packLimitPurchaseDict.Remove(pack.Key);

        Save();
    }

    public void BuyPack(string productId, bool isLimitPack = false, long limitTime = -1)
    {
        if (isLimitPack)
        {
            if (dataBase.packLimitPurchaseDict.ContainsKey(productId))
                dataBase.packLimitPurchaseDict[productId] = limitTime;
            else
                dataBase.packLimitPurchaseDict.Add(productId, limitTime);

            Save();
            return;
        }

        if (!dataBase.packPurchaseDict.TryAdd(productId, 1)) dataBase.packPurchaseDict[productId]++;

        Save();
    }

    public bool IsBuyPack(string productId, bool isLimitPack = false)
    {
        if (isLimitPack)
        {
            if (dataBase.packLimitPurchaseDict.TryGetValue(productId, out var limitTime))
                return TimeManager.GetNow() >= limitTime;

            return false;
        }

        if (dataBase.packPurchaseDict.TryGetValue(productId, out var value)) return value >= 1;

        return false;
    }

    public int GetNumberBuyPack(string productId)
    {
        return dataBase.packPurchaseDict.GetValueOrDefault(productId, 0);
    }

    public void ResetNumberPurchaseProductId(string productId)
    {
        if (dataBase.packPurchaseDict.ContainsKey(productId))
        {
            dataBase.packPurchaseDict[productId] = 0;
            Save();
        }
    }

    public void RemoveProductId(string productId)
    {
        dataBase.packPurchaseDict.Remove(productId);
        Save();
    }

    public void ClearAllPacks()
    {
        dataBase.packPurchaseDict.Clear();
    }


    public void NextDay()
    {
        dataBase.packDailyDealsResource.Clear();
        dataBase.packDailyDealsHistory.Clear();
        dataBase.packDailyDealsRefreshCount.Clear();
        dataBase.packDailyDeals2Resource.Clear();
        dataBase.packDailyDeals2History.Clear();
        dataBase.packDailyDeals2RefreshCount.Clear();
        dataBase.pack2SpawnToDay.Clear();
        Save();
    }

    public void Init()
    {
        if (dataBase.packDailyDeals2Resource != null && dataBase.packDailyDeals2Resource.Count > 0) return;
        if (dataBase.packDailyDeals2Resource == null)
            dataBase.packDailyDeals2Resource = new Dictionary<string, Resource>();
        var listProductId = DataManager.DailyDealsPack2.dataGroups.Select(x => x.ProductId).ToList();
        foreach (var productId in listProductId)
            if (!dataBase.packDailyDeals2Resource.ContainsKey(productId))
                InitDailyDealsPack2(productId);
    }

    #region Daily Deals Pack 2

    public List<Resource> GetResourceDailyDealsPack2(List<string> productIds)
    {
        var resources = new List<Resource>();
        foreach (var productId in productIds)
            if (dataBase.packDailyDeals2Resource.TryGetValue(productId, out var resource))
            {
                resources.Add(resource);
            }
            else
            {
                var result = InitDailyDealsPack2(productId);
                resources.Add(result);
            }

        return resources;
    }

    private Resource InitDailyDealsPack2(string productId)
    {
        var result = RandomResourceInDailyDealsPack2(productId);
        if (result == null) return null;
        dataBase.packDailyDeals2Resource[productId] = result;
        Save();
        TimeManager.RegEventNextDay(NextDay);
        return result;
    }

    private Resource RandomResourceInDailyDealsPack2(string productId)
    {
        var config = DataManager.DailyDealsPack2.dataGroups.FirstOrDefault(x => x.ProductId == productId);
        var listTemp = dataBase.packDailyDeals2Resource.ToList();
        var poolReward = DataManager.PoolRewardsDailyDeals2.dataGroups.ToList();
        foreach (var item in listTemp)
        foreach (var reward in poolReward)
            if (reward.reward.resId == item.Value.resId
                && reward.reward.resType == item.Value.resType
                && reward.reward.resId > 0
                && item.Value.resId > 0
                && Mathf.Approximately(reward.reward.resId, item.Value.resId))
            {
                poolReward.Remove(reward);
                break;
            }

        if (config != null)
        {
            var rewards = new List<PoolRewardsDailyDeals2Model>();
            PoolRewardsDailyDeals2Model rewardEveryDay = null;
            foreach (var reward in poolReward)
            {
                if (reward.reward.resType == EnumBase.ResourceTypes.Item)
                {
                    var itemKey = MergeEnum.ItemKey.FromKeyString(reward.reward.resId.ToString());
                    // removed: PlayerDataManager.Gameplay
                    var isNewItem = false; // gameplay removed
                    if (reward.isEveryDay)
                    {
                        // removed: PlayerDataManager.Gameplay.AddNewItem
                        isNewItem = false;
                        if (dataBase.pack2SpawnToDay.Contains(config.ProductId)) continue;

                        dataBase.pack2SpawnToDay.Add(config.ProductId);
                        rewardEveryDay = reward;
                        Save();
                    }

                    if (isNewItem) continue;
                }

                rewards.Add(reward);
            }

            if (rewardEveryDay == null)
            {
                if (rewards.Count > 0)
                {
                    var listWeight = rewards.Select(x => x.weight).ToList();
                    var randomIndex = listWeight.GetIndexInRate();
                    var selectedResource = rewards[randomIndex];
                    dataBase.packDailyDeals2Resource[productId] = selectedResource.reward;
                    Save();
                    return selectedResource.reward;
                }
            }
            else
            {
                return rewardEveryDay.reward;
            }
        }

        return null;
    }

    private bool HaveItemConstain(ItemSave itemCheck, ItemSave itemChecked)
    {
        if (itemCheck.itemSaveType == itemChecked.itemSaveType && itemCheck.idItem == itemChecked.idItem) return true;

        // removed: DataItemCacheManager.CacheItemResultAndRecipe
        return false;
    }

    public bool PurchaseDailyDealsPack2(string productId, int rewardResId)
    {
        if (IsSoldOutDailyDealsPack2(productId, rewardResId))
            return false;

        if (dataBase.packDailyDeals2History.TryGetValue(productId, out var manyBuy))
            dataBase.packDailyDeals2History[productId] = manyBuy + 1;
        else
            dataBase.packDailyDeals2History[productId] = 1;

        Save();
        return true;
    }

    public bool IsSoldOutDailyDealsPack2(string productId, int rewardResId)
    {
        if (!dataBase.packDailyDeals2History.TryGetValue(productId, out var manyBuy))
            return false;

        var maxBuy = GetManyPurchaseLimitDailyDealsPack2(rewardResId);
        return maxBuy > 0 && manyBuy >= maxBuy;
    }

    private static int GetManyPurchaseLimitDailyDealsPack2(int rewardResId)
    {
        return DataManager.PoolRewardsDailyDeals2.dataGroups
            .FirstOrDefault(x => x.reward.resId == rewardResId)?.manyPurchase ?? 0;
    }

    public void AddCountRefreshDailyDealsPack2(string productId)
    {
        if (dataBase.packDailyDeals2RefreshCount.TryGetValue(productId, out var count))
            dataBase.packDailyDeals2RefreshCount[productId] = count + 1;
        else
            dataBase.packDailyDeals2RefreshCount[productId] = 1;

        Save();
    }

    public void RefreshResourceDailyDealsPack2(string productId)
    {
        dataBase.packDailyDeals2Resource.Clear();
        var listHistory = dataBase.packDailyDeals2History.ToList();
        foreach (var history in listHistory)
            if (history.Key == productId)
                dataBase.packDailyDeals2History.Remove(history.Key);

        Save();
    }

    public int GetCountCanBuyInPack2(string productId, int rewardResId)
    {
        var manyBuy = 0;
        if (dataBase.packDailyDeals2History.TryGetValue(productId, out var count)) manyBuy = count;

        var maxBuy = GetManyPurchaseLimitDailyDealsPack2(rewardResId);
        return maxBuy - manyBuy;
    }

    public string GetIdItemInDailyDeals2(ItemSave itemSave)
    {
        var listItem = dataBase.packDailyDeals2Resource.ToList();
        foreach (var item in listItem)
            if (item.Value.resType == EnumBase.ResourceTypes.Item)
                if (itemSave.itemSaveType == MergeEnum.ItemKey.FromKeyString(item.Value.resId.ToString()).type
                    && itemSave.idItem == item.Value.resId)
                {
                    if (dataBase.packDailyDeals2History.TryGetValue(item.Key, out var manyBuy))
                        if (manyBuy >= GetManyPurchaseLimitDailyDealsPack2(item.Value.resId))
                            continue;

                    return item.Key;
                }

        return string.Empty;
    }

    #endregion

    #region Daily Deals Pack

    public Resource GetResourceDailyDealsPack(DailyDealsPackModel model)
    {
        if (dataBase.packDailyDealsResource.TryGetValue(model.ProductId, out var resource)) return resource;

        var result = RandomResourceInDailyDealsPack(model);
        dataBase.packDailyDealsResource[model.ProductId] = result;
        Save();
        TimeManager.RegEventNextDay(NextDay);
        return result;
    }

    private Resource RandomResourceInDailyDealsPack(DailyDealsPackModel model)
    {
        var config = model;
        if (config != null)
        {
            var rewards = new List<Resource>();
            foreach (var reward in config.rewards)
            {
                // removed: PlayerDataManager.Gameplay.IsNewItem
                rewards.Add(reward);
            }

            if (rewards.Count > 0)
            {
                var randomIndex = Random.Range(0, rewards.Count);
                var selectedResource = rewards[randomIndex];
                dataBase.packDailyDealsResource[model.ProductId] = selectedResource;
                Save();
                return selectedResource;
            }
        }

        return config.rewards[0];
    }

    public void PurchaseDailyDealsPack(string productId)
    {
        if (dataBase.packDailyDealsHistory.TryGetValue(productId, out var manyBuy))
            dataBase.packDailyDealsHistory[productId] = manyBuy + 1;
        else
            dataBase.packDailyDealsHistory[productId] = 1;
        EventManager.EmitEvent(nameof(RedDotId.ShopNotif));
        Save();
    }

    public bool IsSoldOutDailyDealsPack(DailyDealsPackModel model)
    {
        if (dataBase.packDailyDealsHistory.TryGetValue(model.ProductId, out var manyBuy))
            return manyBuy >= model.purchaseList[0].purchaseCount && model.purchaseList[0].purchaseCount != -1;

        return false;
    }

    public void AddCountRefreshDailyDealsPack(string productId)
    {
        if (dataBase.packDailyDealsRefreshCount.TryGetValue(productId, out var count))
            dataBase.packDailyDealsRefreshCount[productId] = count + 1;
        else
            dataBase.packDailyDealsRefreshCount[productId] = 1;

        Save();
    }

    public void RefreshResourceDailyDealsPack(string productId)
    {
        dataBase.packDailyDealsResource.Clear();
        var listHistory = dataBase.packDailyDealsHistory.ToList();
        foreach (var history in listHistory)
            if (history.Key == productId)
                dataBase.packDailyDealsHistory.Remove(history.Key);

        Save();
    }

    public int GetCountCanBuyInPack(DailyDealsPackModel model)
    {
        var manyBuy = 0;
        if (dataBase.packDailyDealsHistory.TryGetValue(model.ProductId, out var count)) manyBuy = count;
        var maxBuy = model.purchaseList[0].purchaseCount;
        return maxBuy - manyBuy;
    }

    public int GetManyBuyInDailyDealsPack(string productId)
    {
        var manyBuy = 0;
        if (dataBase.packDailyDealsHistory.TryGetValue(productId, out var count)) manyBuy = count;
        return manyBuy;
    }

    #endregion
}