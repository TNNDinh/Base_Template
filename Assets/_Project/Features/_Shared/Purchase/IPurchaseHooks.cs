using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.IAP
{
    /// <summary>
    ///     Project-specific side effects invoked by <see cref="PurchaseManager" />.
    ///     Keeping these out of the manager lets the purchase logic be shared across projects:
    ///     each project supplies its own implementation and registers it via
    ///     <see cref="PurchaseManager.Hooks" />.
    /// </summary>
    public interface IPurchaseHooks
    {
        /// <summary>Shown when the player cannot afford a purchase (e.g. a localized toast).</summary>
        void ShowInsufficientResourceMessage();

        /// <summary>
        ///     Called right after a currency/resource has been deducted, so the project can react
        ///     (refresh UI, fire sync/energy events, …).
        /// </summary>
        void OnCurrencySpent(EnumBase.MoneyTypes moneyType);

        /// <summary>
        ///     Called when a purchase failed for lack of funds and the caller allowed opening the shop.
        ///     The implementation decides whether/which shop to open for the given currency.
        /// </summary>
        void OnInsufficientFunds(EnumBase.MoneyTypes moneyType, UIManager.UIGroupName groupName);

        /// <summary>
        ///     Called after a rewarded ad used as payment completed successfully. Carries the analytics
        ///     context so the project can record the reward (bookkeeping, tracking events, …).
        /// </summary>
        void OnAdsRewardGranted(string source, string sourceId, string placement, List<Resource> rewardsTracking);
    }

    /// <summary>
    ///     No-op fallback so <see cref="PurchaseManager" /> never null-references before a project
    ///     registers its own <see cref="IPurchaseHooks" />.
    /// </summary>
    public sealed class NullPurchaseHooks : IPurchaseHooks
    {
        public void ShowInsufficientResourceMessage()
        {
        }

        public void OnCurrencySpent(EnumBase.MoneyTypes moneyType)
        {
        }

        public void OnInsufficientFunds(EnumBase.MoneyTypes moneyType, UIManager.UIGroupName groupName)
        {
        }

        public void OnAdsRewardGranted(string source, string sourceId, string placement, List<Resource> rewardsTracking)
        {
        }
    }
}