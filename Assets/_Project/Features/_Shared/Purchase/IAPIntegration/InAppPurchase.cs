using System;
using System.Collections.Generic;
using TigerForge;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.IAP
{
    public class InAppPurchase : IPurchasing
    {
        public InAppPurchase()
        {
            OnTransactionRestored += TransactionRestored;
            OnPurchaseFailed += PurchaseFailed;
            OnPurchaseComplete += PurchaseComplete;
            OnPurchaseCompleteBeforeCallback += PurchaseCompleteBeforeCallBack;
        }

        public Action<bool> OnTransactionRestored { get; set; }
        public Action<string> OnPurchaseFailed { get; set; }
        public Action<string> OnPurchaseCompleteBeforeCallback { get; set; }
        public Action<string> OnPurchaseComplete { get; set; }

        public List<string> GetConsumableProducts()
        {
            var list = new List<string>();
            list.AddRange(ShopService.GetAllProductId());
            return list;
        }

        public List<string> GetNonConsumableProducts()
        {
            return new List<string>();
        }

        public void RestoreItem()
        {
        }

        private void TransactionRestored(bool success)
        {
            GameSystems.ShowSimpleMessage(success ? "restore_success" : "restore_fail");
        }

        private void PurchaseFailed(string message)
        {
            GameSystems.ShowSimpleMessage("purchase_fail");
        }

        private void PurchaseCompleteBeforeCallBack(string productId)
        {
            //ShopService.BuyPack(productId);
        }

        private void PurchaseComplete(string productId)
        {
            //AdsManager.countTimeShowInterstitialAds = 0;
            //DataPlayer.GetModule<PlayerTracking>().Purchase();
            //FirebaseUtility.PushData(isForce: true);
            //AppsFlyerPurchaseEvent(product);

            // QuestManager.ActiveQuest(EnumBase.QuestTypes.PurchaseInShop);
            ShopService.BuyPack(productId);
            EventManager.EmitEvent(EventName.PurchasedIapSuccess);
        }
    }
}