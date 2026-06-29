using System;
using Ezg.Feature.IAP;
using Ezg.Package.AdsManager;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using Ezg.Feature.Shared.Systems;

namespace Game.Runtime
{
    public class PackagePurchaseBaseController : MonoBehaviour
    {
        [SerializeField] protected PurchaseTemplate purchaseTemplate;

        protected virtual PackTemplateModel Config { get; set; } // => DataManager.RemoveAdsPack.dataGroups;

        protected virtual void Start()
        {
            purchaseTemplate.InitButtons(() => OnPurchaseAds(), () => OnPurchaseCurrency(), () => OnPurchaseIap(),
                () => OnPurchaseFree());
            EventManager.StartListening(EventName.PurchasedIapSuccess, UpdateView);
            UpdateView();
        }

        protected virtual void OnEnable()
        {
        }

        public PurchaseTemplate GetPurchaseTemplate()
        {
            return purchaseTemplate;
        }

        public void InitData(PackTemplateModel data)
        {
            Config = data;
            UpdateView();
        }

        protected virtual void UpdateView()
        {
            purchaseTemplate.HideAll();
            purchaseTemplate.SetPackName(GameSystems.Localize(Config.packName));

            var currentPurchase = Config.purchaseList[0];

            SetReward().SetPurchaseType(currentPurchase.purchaseType, true);
            ;

            switch (currentPurchase.purchaseType)
            {
                case PurchaseType.None:
                case PurchaseType.Free:
                case PurchaseType.Ads:
                case PurchaseType.Currency:
                    purchaseTemplate.SetCurrencyRequire(Config.costRequire.GetResource());
                    break;
                case PurchaseType.IAP:
                    purchaseTemplate.SetProductId(Config.ProductId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual PurchaseTemplate SetReward()
        {
            purchaseTemplate
                .SetRewards(GetPackageRewards().CompileRewards().ToArray(), false);
            return purchaseTemplate;
        }

        protected virtual void OnPurchaseAds(UnityAction onSuccess = null)
        {
            var location = Config.packName;
            AdsManager.Instance.ShowRewardedVideo(location, () =>
            {
                AddRewards(PurchaseType.Ads);
                onSuccess?.Invoke();
                UpdateView();
            });
        }

        protected virtual void OnPurchaseFree(UnityAction onSuccess = null)
        {
            AddRewards(PurchaseType.Free);
            onSuccess?.Invoke();
            UpdateView();
        }

        protected virtual void OnPurchaseCurrency(UnityAction onSuccess = null)
        {
            //var cost = Config.costRequire.GetResource();
            var cost = purchaseTemplate.CostForPurchaseCurrency;
            PurchaseManager.PurchaseOffline(cost, () =>
                {
                    AddRewards(PurchaseType.Currency);
                    onSuccess?.Invoke();
                    UpdateView();
                }, sourceId: Config.packName,
                placement: string.IsNullOrEmpty(Config.ProductId) ? Config.packName : Config.ProductId);
        }

        protected virtual void OnPurchaseIap(UnityAction onSuccess = null)
        {
            InAppManager.Instance.Buy(Config.ProductId, () =>
            {
                AddRewards(PurchaseType.IAP);
                onSuccess?.Invoke();
                UpdateView();
            }, Config.packName, Config.packName);
        }

        protected virtual Resource[] GetPackageRewards()
        {
            return Config.GetRewards();
        }

        protected virtual void AddRewards(PurchaseType type)
        {
            var rewards = GetPackageRewards();
            RewardsService.ReceiveRewards(rewards, type, Config.packName, Config.packName,
                onClose: () => { /* removed: SpawnCurrencyManager (gameplay removed) */ });
        }
    }
}