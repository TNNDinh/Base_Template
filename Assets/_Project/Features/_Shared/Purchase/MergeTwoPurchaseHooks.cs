using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using Ezg.Core.Utils;
using Ezg.Feature.Meta.HomeScene;
using Ezg.Feature.Shared;
using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.IAP
{
    /// <summary>
    ///     Merge Two implementation of <see cref="IPurchaseHooks" />. Holds every project-specific
    ///     reaction to a purchase so <see cref="PurchaseManager" /> itself stays portable.
    ///     Self-registers into <see cref="PurchaseManager.Hooks" /> before the first scene loads.
    /// </summary>
    public sealed class MergeTwoPurchaseHooks : IPurchaseHooks
    {
        #region Fields

        /// <summary>Shop scroll index to snap to when the player runs out of diamonds.</summary>
        private const int SHOP_DIAMONDS_SNAP_INDEX = 8;

        #endregion

        #region Initialize

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            PurchaseManager.Hooks = new MergeTwoPurchaseHooks();
        }

        #endregion

        #region Public Methods

        public void ShowInsufficientResourceMessage()
        {
            GameSystems.ShowSimpleMessage("not_enough_resource");
        }

        public void OnCurrencySpent(EnumBase.MoneyTypes moneyType)
        {
            switch (moneyType)
            {
                case EnumBase.MoneyTypes.Energy:
                    EventManager.EmitEvent(EventName.DeductEnergy);
                    break;
                case EnumBase.MoneyTypes.Diamonds:
                    EventManager.EmitEvent(EventName.ForceSyncData);
                    break;
            }
        }

        public void OnInsufficientFunds(EnumBase.MoneyTypes moneyType, UIManager.UIGroupName groupName)
        {
            if (moneyType != EnumBase.MoneyTypes.Diamonds) return;

            if (!UIManager.Instance.IsFeatureActiving(GameEnums.Features.Shop))
                HomeSceneManager.OpenShopAndSnapTo(SHOP_DIAMONDS_SNAP_INDEX, groupName);
        }

        public void OnAdsRewardGranted(string source, string sourceId, string placement, List<Resource> rewardsTracking)
        {
            PlayerDataManager.PlayerShop.dataBase.IAACount++;
            PlayerDataManager.PlayerShop.Save();

            //if (rewardsTracking != null)
            //{
            //    var index = 0;
            //    FirebaseEvent.ads_reward.Send(new FirebaseEventConfig()
            //    {
            //        source = source,
            //        source_detail = sourceId,
            //        placement = placement,
            //        item_archieve = JsonConvert.SerializeObject(rewardsTracking.ToDictionary(
            //            x => (index++ + ".") + (x.resType == EnumBase.ResourceTypes.Money
            //                ? ((EnumBase.MoneyTypes)x.resId).ToString()
            //                : "item_" + x.resId), x => x.resNumber)),
            //    }).Forget();
            //}
        }

        #endregion
    }
}