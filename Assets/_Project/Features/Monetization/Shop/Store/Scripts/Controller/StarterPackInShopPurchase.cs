using Ezg.Core.UI;
using Ezg.Feature.IAP;
using Game.Runtime;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;

namespace Ezg.Feature.Monetization.Shop
{
    public class StarterPackInShopPurchase : PackagePurchaseBaseController
    {
        [SerializeField] private UI_CooldownTimeView cooldown;
        [SerializeField] private IconView _iconView;
        [SerializeField] private Transform _holderIconview;

        protected override void Start()
        {
            // removed: StarterPackService
            if (cooldown)
                cooldown.InitCustomCooldown(PackDurationService.GetDurationPack(PackDurationType.StarterPack));

            if (_iconView && _holderIconview) SpawnRewards();

            base.Start();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.StartListening(EventName.DeActivePackDuration + PackDurationType.StarterPack, CheckActive);
            CheckActive();
        }

        private void OnDisable()
        {
            EventManager.StopListening(EventName.DeActivePackDuration + PackDurationType.StarterPack, CheckActive);
        }

        private void CheckActive()
        {
            gameObject.SetActive(PackDurationService.IsPackActive(PackDurationType.StarterPack));
        }

        private void SpawnRewards()
        {
            foreach (var reward in Config.rewards)
            {
                var obj = Instantiate(_iconView, _holderIconview);
                obj.SetData(reward);
                obj.gameObject.SetActive(true);
            }
        }

        protected override void UpdateView()
        {
            purchaseTemplate.HideAll();
            //purchaseTemplate.SetPackName(GameSystems.Localize(Config.packName));

            var currentPurchase = Config.purchaseList[0];
            var currentPurchaseType = Config.purchaseList[0].purchaseType;
            purchaseTemplate.SetSale(Config.sale);

            //purchaseTemplate.SetRewards(Config.rewards,true).SetPurchaseType(currentPurchaseType, true);

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
            }
        }

        protected override void OnPurchaseIap(UnityAction onSuccess = null)
        {
            // removed: StarterPackService
            InAppManager.Instance.Buy(Config.ProductId, () =>
            {
                UpdateView();
            }, SourceTracking.StarterPack, "pack_" + Config.id);
        }

        // protected override PurchaseTemplate SetReward()
        // {
        //     return purchaseTemplate;
        // }

        protected override void AddRewards(PurchaseType type)
        {
            //base.AddRewards(type);
        }

        protected override void OnPurchaseFree(UnityAction onSuccess = null)
        {
            //EventManager.EmitEvent(global::EventName.NotEnoughPointPurchasePiggyBank);
        }
    }
}