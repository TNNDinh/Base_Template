using Ezg.Feature.IAP;
using Ezg.Package.AdsManager;
using Ezg.Package.Audio;
using Ezg.Package.Localize;
using Game.Runtime;
using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Monetization.Shop
{
    public class BaseBundleOffer : MonoBehaviour
    {
        [SerializeField] private PurchaseTemplate purchaseTemplate;

        protected virtual PackTemplateModel PackConfig => null;

        private void Start()
        {
            purchaseTemplate.InitButtons(OnPurchaseAds, OnPurchaseCurrency, OnPurchaseIap, OnPurchaseFree);
            UpdateView();
            purchaseTemplate.SetProductId(PackConfig.ProductId);
        }

        private void OnEnable()
        {
            EventManager.StartListening(EventName.PurchasedIapSuccess, UpdateView);
        }

        private void OnDisable()
        {
            EventManager.StopListening(EventName.PurchasedIapSuccess, UpdateView);
        }

        private void UpdateView()
        {
            purchaseTemplate.HideAll();
            var currentPurchase = PackConfig.purchaseList[0];
            purchaseTemplate
                .SetRewards(PackConfig.GetRewards(), false)
                .SetPurchaseType(currentPurchase.purchaseType, true)
                .SetBestSeller(PackConfig.isBestSeller)
                .SetPackName(GameSystems.Localize(PackConfig.packName, LocalizeCategory.Shop));

            if (!ShopService.IsActivePack(PackConfig)) gameObject.SetActive(false);
        }

        protected virtual void AddRewards(PurchaseType type)
        {
            RewardsService.ReceiveRewards(PackConfig.GetRewards(), type, SourceTracking.Shop, PackConfig.packName,
                productId: PackConfig.ProductId, /*onClose: () => ClosePopup(PackConfig.GetRewards())*/
                isSpawnCurrency: true);

            // void ClosePopup(Resource[] rewards)
            // {
            //     SpawnCurrencyManager.SpawnCurrency(rewards, UIManager.Instance.GetLastFeatureController().transform);
            // }
        }

        private void OnPurchaseAds()
        {
            void CallBack()
            {
                AddRewards(PurchaseType.Ads);
                // PublisherService.NotifyListener(SubjectType.PurchaseTimesInShop);
                // this.PushEvent(EventTracking.ads_reward_offer, source);
                AfterPurchaseAds();
            }

            var location = AdsPlacement.Shop(PackConfig.packName);
            AdsManager.Instance.ShowRewardedVideo(location, CallBack);
        }

        private void OnPurchaseFree()
        {
            AddRewards(PurchaseType.Free);
            AfterPurchaseFree();
        }

        private void OnPurchaseCurrency()
        {
            void CallBack()
            {
                AddRewards(PurchaseType.Currency);

                AfterPurchaseCurrency();
            }

            var cost = PackConfig.costRequire.GetResource();
            PurchaseManager.PurchaseOffline(cost, CallBack, source: nameof(ShopService), sourceId: PackConfig.packName,
                placement: string.IsNullOrEmpty(PackConfig.ProductId) ? PackConfig.packName : PackConfig.ProductId);
        }

        protected virtual void OnPurchaseIap()
        {
            void CallBack()
            {
                AddRewards(PurchaseType.IAP);
                AfterPurchaseIap();
            }

            InAppManager.Instance.Buy(PackConfig.ProductId, CallBack, "shop", PackConfig.packName);
        }

        protected virtual void AfterPurchaseIap()
        {
            AudioService.Default.PlaySound(DataManager.SoundConfig.PurchaseItem);
        }

        protected virtual void AfterPurchaseCurrency()
        {
            AudioService.Default.PlaySound(DataManager.SoundConfig.PurchaseItem);
        }

        protected virtual void AfterPurchaseAds()
        {
            AudioService.Default.PlaySound(DataManager.SoundConfig.PurchaseItem);
        }

        protected virtual void AfterPurchaseFree()
        {
            AudioService.Default.PlaySound(DataManager.SoundConfig.PurchaseItem);
        }
    }
}