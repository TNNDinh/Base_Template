using System;
using System.Collections.Generic;
using System.Linq;
using Ezg.Core.Utils;
using Ezg.Feature.RedDot;
using Ezg.Feature.Shared;
using TigerForge;
using UnityEngine;
using Random = UnityEngine.Random;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;

public static class ShopService
{
    public static ShopController Controller;

    /// <summary>
    ///     Trigger yêu cầu login lúc purchase xong, chỉ 1 lần mỗi session
    /// </summary>
    private static bool _suggestLogin;

    private static readonly List<EnumBase.MoneyTypes> _listScrollReshuffle = new()
    {
        // EnumBase.MoneyTypes.ScrollAmulet,
        // EnumBase.MoneyTypes.ScrollArmor,
        // EnumBase.MoneyTypes.ScrollBoots,
        // EnumBase.MoneyTypes.ScrollGloves,
        // EnumBase.MoneyTypes.ScrollHelmet,
        // EnumBase.MoneyTypes.ScrollRing,
    };

    public static void ValidNextDay()
    {
        if (TimeManager.IsNextDay(PlayerDataManager.PlayerShop.dataBase.lastTimeCheckinDaily)) NextDay();

        TimeManager.RegEventNextDay(NextDay);

        PlayerDataManager.PlayerShop.Init();
    }

    private static void NextDay()
    {
        PlayerDataManager.PlayerShop.dataBase.lastTimeCheckinDaily = TimeManager.GetNow();
        //PlayerDataManager.DailyFreeReward.ResetDaily();
        //DataManager.EnergyBoost.dataGroups.ForEach(x =>
        //{
        //    PlayerDataManager.PlayerShop.RemoveProductId(x.ProductId);
        //});

        var index = Random.Range(0, _listScrollReshuffle.Count);
        //PlayerDataManager.PlayerShop.SetCurrentScrollReshuffle(_listScrollReshuffle[index]);
        PlayerDataManager.PlayerShop.NextDay();
        EventManager.EmitEvent(EventName.ShopUpdated);
        EventManager.EmitEvent(nameof(RedDotId.ShopNotif));
    }

    public static bool IsRemoveAds()
    {
        //Debug.Log(IsBuyPack(DataManager.RemoveAdsPack.dataGroups.ProductId));
        return IsBuyPack(DataManager.RemoveAdsPack.dataGroups[0].ProductId) ||
               IsBuyPack(DataManager.RemoveAdsPack.dataGroups[1].ProductId);
    }

    public static void BuyPack(string productId)
    {
        PlayerDataManager.PlayerShop.BuyPack(productId);
    }

    public static void RemovePack(string productId)
    {
        PlayerDataManager.PlayerShop.RemoveProductId(productId);
    }

    public static bool IsBuyPack(string productId)
    {
        return PlayerDataManager.PlayerShop.IsBuyPack(productId);
    }

    public static PackTemplateModel[] GetAllPack(EnumBase.MoneyTypes moneyTypes)
    {
        switch (moneyTypes)
        {
            case EnumBase.MoneyTypes.Gold:
                var listCoin = new List<PackTemplateModel>();
                var totalPercentCoin = GetTotalPercentBonusCoin();
                foreach (var coinPack in DataManager.CoinPack.dataGroups)
                {
                    var clone = (PackTemplateModel)coinPack.Clone();
                    if (totalPercentCoin != 0)
                        foreach (var reward in clone.rewards)
                            reward.resNumber = (long)(reward.resNumber + reward.resNumber * totalPercentCoin);

                    listCoin.Add(clone);
                }

                return listCoin.ToArray();
            case EnumBase.MoneyTypes.Diamonds:
                return DataManager.GemPack.dataGroups;
            // case EnumBase.MoneyTypes.HeroOrb:
            //     return DataManager.HeroOrbPack.dataGroups;
            //var listHeroOrb = new List<PackTemplateModel>();
            //for (int i = 0; i < DataManager.HeroOrbPack.dataGroups.Length; i++)
            //{
            //    var clone = (PackTemplateModel)DataManager.HeroOrbPack.dataGroups[i].CloneJson();
            //    foreach (var reward in clone.GetRewards())
            //    {
            //        reward.resNumber += GetRawBonusHeroOrb(i);
            //    }

            //    listHeroOrb.Add(clone);
            //}

            //return listHeroOrb.ToArray();
            // case EnumBase.MoneyTypes.ScrollRandom:
            // case EnumBase.MoneyTypes.ScrollHelmet:
            // case EnumBase.MoneyTypes.ScrollBoots:
            // case EnumBase.MoneyTypes.ScrollArmor:
            // case EnumBase.MoneyTypes.ScrollGloves:
            // case EnumBase.MoneyTypes.ScrollAmulet:
            // case EnumBase.MoneyTypes.ScrollRing:
            //     return DataManager.ScrollPack.dataGroups;
            //var listScroll = new List<PackTemplateModel>();
            //for (int i = 0; i < DataManager.ScrollPack.dataGroups.Length; i++)
            //{
            //    var clone = (PackTemplateModel)DataManager.ScrollPack.dataGroups[i].CloneJson();
            //    foreach (var reward in clone.GetRewards())
            //    {
            //        reward.resNumber += GetRawBonusScroll(i);
            //        reward.resId = (int)PlayerDataManager.PlayerShop.GetCurrentScrollReshuffle;
            //    }  

            //    listScroll.Add(clone);
            //}

            //return listScroll.ToArray();
            default:
                throw new ArgumentOutOfRangeException(nameof(moneyTypes), moneyTypes, null);
        }
    }

    public static List<string> GetAllProductId()
    {
        var list = new List<string>();
        list.AddRange(DataManager.CoinPack.dataGroups.Select(x => x.ProductId).ToList());
        list.AddRange(DataManager.GemPack.dataGroups.Select(x => x.ProductId).ToList());
        list.AddRange(DataManager.RemoveAdsPack.dataGroups.Select(x => x.ProductId));
        list.AddRange(DataManager.BundlePack.dataGroups.Select(x => x.ProductId));
        // removed: DataManager.BattlePassPackage, ChainPack, LuckySpinPack, PiggyBank
        list.Add(DataManager.FirstPurchase.dataGroups.ProductId);
        list.Add(DataManager.LuxuriousOffer.dataGroups.ProductId);
        list.Add(DataManager.StandardDiamond.dataGroups.ProductId);
        list.AddRange(DataManager.DailyDealsPack.dataGroups.Select(x => x.ProductId).ToList());
        list.Add(DataManager.SilverWeeklyPass.dataGroup.ProductId);
        list.Add(DataManager.GoldWeeklyPass.dataGroup.ProductId);
        // removed: DataManager.VideoBonusesPurchase, EnergyPack, EnergyTrilogyPack
        list.AddRange(DataManager.PackDuration.dataGroup.Select(x => x.ProductId).ToList());
        // removed: DataManager.StarterPack, OpenningPack, InfinityPack, SpeedPackage
        list = list.Where(x => !string.IsNullOrEmpty(x)).ToList();

        return list;
    }

    public static float GetTotalPercentBonusCoin()
    {
        return 0;
        //return //GetPercentCoinScaleStage() +
        //       BattleManager.GetStatsHeroSelected(false).GetStat(RPGStatType.GoldGain).StatValue;
    }

    private static float GetPercentCoinScaleStage()
    {
        return 0;
    }

    private static int GetRawBonusHeroOrb(int index)
    {
        return 0;
    }

    private static int GetRawBonusScroll(int index)
    {
        return 0;
    }

    public static bool IsActivePack(PackTemplateModel packModel)
    {
        var countPurchase = PlayerDataManager.PlayerShop.GetNumberBuyPack(packModel.ProductId);

        var totalPurchase = packModel.purchaseList.Sum(packData => packData.purchaseCount);

        return countPurchase < totalPurchase;
    }

    public static int GetPackBuyedCount(PackTemplateModel packModel)
    {
        return PlayerDataManager.PlayerShop.GetNumberBuyPack(packModel.ProductId);
    }

    /// <summary>
    ///     Trả về pack data va so luot mua con lai
    /// </summary>
    /// <param name="packModel"></param>
    /// <returns></returns>
    public static (PackData, int) GetCurrentPack(PackTemplateModel packModel)
    {
        var countPurchase = PlayerDataManager.PlayerShop.GetNumberBuyPack(packModel.ProductId);

        var currentIndex = 0;
        foreach (var packData in packModel.purchaseList)
        {
            if (countPurchase < packData.purchaseCount) break;
            currentIndex++;
            countPurchase -= packData.purchaseCount;
        }

        var result = packModel.purchaseList[Mathf.Clamp(currentIndex, 0, packModel.purchaseList.Length - 1)];
        return (result, result.purchaseCount - countPurchase);
    }

    /// <summary>
    ///     Yêu cầu user login sau khi purchase nếu chưa login
    /// </summary>
    public static void SuggestLogin()
    {
        //if (!ProfileManager.IsLogon() && !_suggestLogin)
        //{
        //    _suggestLogin = true;
        //    GameSystems.ShowMessage("suggest_login_content", "suggest_login_title", () =>
        //    {
        //        ProfileManager.Login().Forget();
        //    });
        //}
    }


    public static bool CanClaimDailyDeal()
    {
        return PlayerDataManager.PlayerShop.GetCountCanBuyInPack(DataManager.DailyDealsPack.dataGroups[0]) > 0;
    }

    public static List<PackTemplateModel> GetAllPackTemplates()
    {
        var list = new List<PackTemplateModel>();
        list.AddRange(DataManager.GemPack.dataGroups);
        list.AddRange(DataManager.BundlePack.dataGroups);
        list.Add(DataManager.FirstPurchase.dataGroups);
        list.Add(DataManager.LuxuriousOffer.dataGroups);
        list.Add(DataManager.StandardDiamond.dataGroups);
        // removed: DataManager.StarterPack, OpenningPack, EnergyPack, EnergyTrilogyPack, VideoBonusesPurchase, PiggyBank
        list.Add(DataManager.SilverWeeklyPass.dataGroup);
        list.Add(DataManager.GoldWeeklyPass.dataGroup);
        list.AddRange(DataManager.PackDuration.dataGroup);
        // removed: DataManager.InfinityPack
        list.AddRange(DataManager.NiceBoostPack.dataGroups);
        list.AddRange(DataManager.SupplyChestPack.dataGroups);
        list.AddRange(DataManager.DailyDealsPack_1.dataGroups);
        list.AddRange(DataManager.DailyDealsPack.dataGroups);
        return list.Where(x => x != null && !string.IsNullOrEmpty(x.ProductId)).ToList();
    }

    public static Resource[] ActivatePackByResId(int resId)
    {
        var packs = GetAllPackTemplates();
        var index = resId - 1;
        if (index < 0 || index >= packs.Count)
        {
            Debug.LogError(
                $"[ShopService] ActivatePackByResId: resId {resId} out of range (total packs: {packs.Count})");
            return Array.Empty<Resource>();
        }

        var pack = packs[index];
        BuyPack(pack.ProductId);
        GameSystems.ShowSimpleMessage(pack.packName);
        return pack.GetRewards();
    }
}