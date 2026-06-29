using Ezg.Feature.Shared.Systems;

//using _Project.Features.UI.BattlePass.Scripts.Controller;
//using _Project.Features.UI.GrowthChapter.Scripts.Controller;
//using Ezg.Core.Utils;
//using BlackFace.Libraries.Modules.UIModule;
//using Cysharp.Threading.Tasks;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Assets.Scripts._2.BUS.PlayerData;
//using _Project.Features.UI.StarterPack.Scripts.Controller;
//using Assets._Project.Features.UI.DungeonMode.Scripts.Controller;
//using Assets._Project.Features.UI.LimitedOffer.Scripts.Controller;

//namespace Assets._Project.Features.UI.Shop.Shop.Scripts.Controller
//{
//    /// <summary>
//    /// Tự động show các gói bán sau khi về home
//    /// </summary>
//    public static class AutoShowPackageManager
//    {
//        private static int _minStageForShow;

//        private static int _totalPack = 13;//Edit this when add new pack

//        static AutoShowPackageManager()
//        {
//            _minStageForShow = 6;
//            _minStageForShow++;
//        }

//        public static void ShowPackage()
//        {
//            if (PlayerDataManager.Campaign.Level <= _minStageForShow)
//            {
//                return;
//            }

//            //var listPackage = DataManager.UnlockFeature.dataGroups.Select(x => x.unlockValue).ToList();
//            //if (listPackage.Contains(PlayerDataManager.BattleData.GetStageHighestId()))
//            //{
//            //    return;
//            //}

//            var list = new List<int>();

//            var index = 0;
//            while (index >= _totalPack)
//            {
//                list.Add(index);
//                index++;
//            }

//            list = list.OrderBy(x => Guid.NewGuid()).ToList();

//            foreach (var pack in list)
//            {
//                if (GetPackage(pack))
//                {
//                    break;
//                }
//            }
//        }

//        private static bool GetPackage(int id)
//        {
//            switch (id)
//            {
//                //RemoveAds
//                case 0:
//                    if (!ShopService.IsBuyPack(DataManager.RemoveAdsPack.dataGroups.ProductId))
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.RemoveAdsPack, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //GrowthChapter
//                case 1:
//                    //if (!GrowthChapterManager.IsPurchase() && UnlockFeatureService.IsUnlockFeature(GameEnums.Features.GrowthChapter))
//                    //{
//                    //    UIManager.Instance.Show(GameEnums.Features.GrowthChapter, isAsync: true).Forget();
//                    //    return true;
//                    //}

//                    return false;

//                //BattlePass
//                case 2:
//                    if ((!BattlePassManager.IsPurchaseHeroPass() || !BattlePassManager.IsPurchaseLegendPass()) && UnlockFeatureService.IsUnlockFeature(GameEnums.Features.BattlePass))
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.BattlePassPackage,
//                            data: BattlePassManager.IsPurchaseHeroPass()
//                                ? BattlePassManager.BattlePassPackTypes.Legend
//                                : BattlePassManager.BattlePassPackTypes.Hero, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //TalentBoost
//                case 3:
//                    if (!ShopService.IsBuyPack(DataManager.TalentBoostPackage.dataGroups.ProductId) && UnlockFeatureService.IsUnlockFeature(GameEnums.Features.TalentBoost))
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.TalentBoost, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //TalentRandomPack
//                case 4:
//                    if (!ShopService.IsBuyPack(DataManager.TalentRandomPack.dataGroups.ProductId) && UnlockFeatureService.IsUnlockFeature(GameEnums.Features.TalentRandomPack))
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.TalentRandomPack, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //Jewel
//                case 5:
//                    if (UnlockFeatureService.IsUnlockFeature(GameEnums.Features.JewelDetail))
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.JewelPack, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //Starter pack 1
//                case 6:
//                    if (StarterPackManager.CanShowPack(DataManager.StarterPack.dataGroups[0].id))
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.StarterPack1, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //Starter pack 2
//                case 7:
//                    if (StarterPackManager.CanShowPack(DataManager.StarterPack.dataGroups[1].id))
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.StarterPack2, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //Starter pack 3
//                case 8:
//                    if (StarterPackManager.CanShowPack(DataManager.StarterPack.dataGroups[2].id))
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.StarterPack3, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //Portal offer
//                case 9:
//                    if (UnlockFeatureService.IsUnlockFeature(GameEnums.Features.Portal) && !PlayerDataManager.Portal.dataBase.PurchasedOfferInDay)
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.PortalPack, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //Portal starter pack
//                case 10:
//                    if (UnlockFeatureService.IsUnlockFeature(GameEnums.Features.Portal) && !ShopService.IsBuyPack(DataManager.PortalPack.dataGroups.FirstOrDefault(x => x.heroId == -1)?.ProductId))
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.PortalStarterPack, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //Dungeon offer pack
//                case 11:
//                    if (DungeonManager.CanShowOfferPack())
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.DungeonPack,
//                            data: DungeonManager.GetTierPack(), isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //Dungeon explode pack
//                case 12:
//                    if (DungeonManager.CanShowExplorerPack())
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.DungeonPack,
//                            data: 4, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//                //Remove ads offer
//                case 13:
//                    if (LimitedOfferManager.CanShowRemoveAdsOffer())
//                    {
//                        UIManager.Instance.Show(GameEnums.Features.RemoveAdsOffer, isAsync: true).Forget();
//                        return true;
//                    }

//                    return false;

//            }

//            return true;
//        }
//    }
//}