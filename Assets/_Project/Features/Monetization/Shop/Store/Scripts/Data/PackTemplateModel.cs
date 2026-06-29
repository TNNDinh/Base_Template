using System;
using System.Linq;
using Ezg.Core.Extensions;
using Ezg.Feature.Shared;
using UnityEngine;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

[Serializable]
public class PackRewards : Resource
{
    public float bonus;
    public int stageBonus;
}

[Serializable]
public class PackTemplateModel : ICloneable
{
    public int id;
    public PackRewards[] rewards;
    public PackData[] purchaseList;
    public float iapCost;
    public RewardsService.CostRequire costRequire;
    public int firstTimePurchaseBonus;
    public float sale;
    public string googleProductId;
    public string googleProductIdPremium;
    public string appleProductId;
    public string packName;
    public bool isBestSeller;

    public string ProductId
    {
        get
        {
            string productId;

            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXEditor)
                productId = GameSystems.IsPremium ? googleProductIdPremium : googleProductId;
            else
                productId = appleProductId;

            if (string.IsNullOrEmpty(productId))
            {
                var type = GetType();
                var typeName = type.Name.ToSnakeCase();
                productId = $"{typeName}_{id}";
            }

            return productId;
        }
    }

    public object Clone()
    {
        var packTemplate = new PackTemplateModel
        {
            id = id,
            rewards = rewards.Select(r => new PackRewards
            {
                resType = r.resType, resId = r.resId, resNumber = r.resNumber, bonus = r.bonus,
                stageBonus = r.stageBonus
            }).ToArray(),
            firstTimePurchaseBonus = firstTimePurchaseBonus,
            isBestSeller = isBestSeller,
            purchaseList = purchaseList
                .Select(packData => new PackData(packData.purchaseType, packData.purchaseCount))
                .ToArray(),
            iapCost = iapCost,
            costRequire = new RewardsService.CostRequire
                { costType = costRequire.costType, costId = costRequire.costId, costNumber = costRequire.costNumber },
            sale = sale,
            googleProductId = googleProductId,
            googleProductIdPremium = googleProductIdPremium,
            appleProductId = appleProductId,
            packName = packName
        };

        return packTemplate;
    }

    public Resource[] GetRewards()
    {
        var isScrollPack = packName.StartsWith("scroll_pack_");
        var scrollReshuffle = isScrollPack ? (int)PlayerDataManager.PlayerShop.GetCurrentScrollReshuffle : 0;
        var result = new Resource[rewards.Length];
        for (var i = 0; i < rewards.Length; i++)
        {
            var r = rewards[i];
            var resNumber = (long)(r.bonus > 0
                ? r.resNumber + (r.stageBonus == 0 ? 0 : r.bonus * (PlayerDataManager.Campaign.Level / r.stageBonus))
                : r.resNumber);
            result[i] = new Resource
            {
                resId = isScrollPack ? scrollReshuffle : r.resId,
                resType = r.resType,
                resNumber = resNumber
            };
        }

        return result;
    }
}

[Serializable]
public struct PackData
{
    public PurchaseType purchaseType;
    public int purchaseCount;

    public PackData(PurchaseType purchaseType, int purchaseCount)
    {
        this.purchaseType = purchaseType;
        this.purchaseCount = purchaseCount;
    }
}