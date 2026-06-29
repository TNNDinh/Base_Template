using System;
using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Package.AdsManager;
using UnityEngine;
using UnityEngine.Events;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.IAP
{
    /// <summary>
    ///     Centralised entry point for every purchase flow in the game:
    ///     spending resources / currencies offline, watching a rewarded ad as payment,
    ///     and store IAP.
    ///     The class only owns the *transaction* logic (enough-check, deduct, save, ads, IAP).
    ///     Every project-specific side effect (showing the "not enough" toast, opening the shop,
    ///     firing sync/energy events, granting the ad bookkeeping reward) is delegated to
    ///     <see cref="IPurchaseHooks" /> so the same class can be reused across projects.
    ///     A project wires its behaviour once at start-up via <see cref="Hooks" />.
    /// </summary>
    public static class PurchaseManager
    {
        #region Fields

        /// <summary>
        ///     Project-specific side effects. Defaults to a no-op implementation so the
        ///     manager is safe to call before a project registers its own hooks.
        /// </summary>
        public static IPurchaseHooks Hooks { get; set; } = new NullPurchaseHooks();

        #endregion

        #region Public Methods

        /// <summary>
        ///     Spends a bundle of resources offline. Fails atomically when any item is insufficient.
        /// </summary>
        /// <returns>True when the whole bundle was charged; otherwise false.</returns>
        public static bool PurchaseOffline(List<Resource> resource, UnityAction successAction = null,
            UnityAction unSuccessAction = null,
            bool isShowMes = true,
            string source = "",
            string sourceId = "",
            string placement = "")
        {
            if (!PlayerResource.IsEnough(resource))
            {
                if (isShowMes) Hooks.ShowInsufficientResourceMessage();

                unSuccessAction?.Invoke();
                return false;
            }

            PlayerResource.RemoveResource(resource, source: source, sourceId: sourceId, placement: placement);
            successAction?.Invoke();
            PlayerDataManager.PlayerResource.Save();
            NotifyCurrencySpent(resource);
            return true;
        }

        /// <summary>
        ///     Spends a single resource offline, or shows a rewarded ad when the resource is the Ads pseudo-currency.
        /// </summary>
        /// <returns>True when charged (or the ad was started); otherwise false.</returns>
        public static bool PurchaseOffline(Resource resource, UnityAction successAction = null,
            UnityAction unSuccessAction = null,
            bool isShowMes = true,
            string source = "",
            string sourceId = "",
            string placement = "",
            bool isOpenShop = true, UIManager.UIGroupName groupName = UIManager.UIGroupName.Main_Container)
        {
            if (IsAds(resource))
                return ShowRewardedAd(placement,
                    () => successAction?.Invoke(),
                    () => unSuccessAction?.Invoke());

            if (!PlayerResource.IsEnough(resource))
            {
                if (isShowMes) Hooks.ShowInsufficientResourceMessage();

                if (isOpenShop) Hooks.OnInsufficientFunds((EnumBase.MoneyTypes)resource.resId, groupName);

                unSuccessAction?.Invoke();
                return false;
            }

            PlayerResource.RemoveResource(resource, source: source, sourceId: sourceId, placement: placement);
            successAction?.Invoke();
            PlayerDataManager.PlayerResource.Save();
            if (resource.resType == EnumBase.ResourceTypes.Money)
                Hooks.OnCurrencySpent((EnumBase.MoneyTypes)resource.resId);

            return true;
        }

        /// <summary>
        ///     Spends an amount of a single currency offline, or shows a rewarded ad when <paramref name="moneyType" /> is Ads.
        /// </summary>
        /// <returns>True when charged (or the ad was started); otherwise false.</returns>
        public static bool PurchaseOffline(EnumBase.MoneyTypes moneyType, long price,
            Action successAction = null,
            Action unSuccessAction = null,
            bool isShowMes = true,
            string source = "",
            string sourceId = "",
            string placement = "",
            List<Resource> rewardsTracking = null,
            bool notifyResourceUi = true,
            bool isOpenShop = true, UIManager.UIGroupName groupName = UIManager.UIGroupName.Main_Container)
        {
            if (moneyType == EnumBase.MoneyTypes.Ads)
            {
                successAction += () => Hooks.OnAdsRewardGranted(source, sourceId, placement, rewardsTracking);
                return ShowRewardedAd(placement, successAction, unSuccessAction);
            }

            if (!PlayerResource.IsEnough(moneyType, price))
            {
                if (isOpenShop) Hooks.OnInsufficientFunds(moneyType, groupName);

                if (isShowMes) Hooks.ShowInsufficientResourceMessage();

                unSuccessAction?.Invoke();
                return false;
            }

            PlayerResource.RemoveCurrency(moneyType, price, source, sourceId,
                placement, notifyResourceUi);
            successAction?.Invoke();
            PlayerDataManager.PlayerResource.Save();
            Hooks.OnCurrencySpent(moneyType);
            return true;
        }

        /// <summary>
        ///     Starts a store in-app purchase. Result is delivered through <paramref name="callback" />.
        /// </summary>
        public static void PurchaseOnline(string productId, Action callback, string source = "", string sourceId = "")
        {
            InAppManager.Instance.Buy(productId, callback, source, sourceId);
        }

        #endregion

        #region Private Methods

        /// <summary>True when the resource describes the Ads pseudo-currency.</summary>
        private static bool IsAds(Resource resource)
        {
            return resource.resType == EnumBase.ResourceTypes.Money && resource.resId == (int)EnumBase.MoneyTypes.Ads;
        }

        /// <summary>
        ///     Shows a rewarded video as payment. Returns false (and invokes <paramref name="onFail" />)
        ///     when no ads provider is available.
        /// </summary>
        private static bool ShowRewardedAd(string placement, Action onSuccess, Action onFail)
        {
            if (AdsManager.Instance == null)
            {
                Debug.LogWarning("[PurchaseManager] AdsManager.Instance is null, cannot show rewarded video.");
                onFail?.Invoke();
                return false;
            }

            AdsManager.Instance.ShowRewardedVideo(placement, onSuccess, onFail);
            return true;
        }

        /// <summary>Fires the per-currency spent hook for every money entry in the bundle.</summary>
        private static void NotifyCurrencySpent(List<Resource> resources)
        {
            for (var i = 0; i < resources.Count; i++)
            {
                var res = resources[i];
                if (res.resType == EnumBase.ResourceTypes.Money) Hooks.OnCurrencySpent((EnumBase.MoneyTypes)res.resId);
            }
        }

        #endregion
    }
}