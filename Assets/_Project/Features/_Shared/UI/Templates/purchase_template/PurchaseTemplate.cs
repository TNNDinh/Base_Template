using System;
using Ezg.Core.Utils;
using Ezg.Feature.IAP;
using Ezg.Package.Localize.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    public class PurchaseTemplate : MonoBehaviour
    {
        public Text packName;
        public Image iconPack;

        public RewardsShopView rewardsView;

        public GameObject discountGroup;

        public Text discountValueTxt;
        public Text iapDiscountTxt;
        public Text iapTxt;
        public Button purchaseIAPBtn;

        public Button adsPurchaseBtn;
        public Text adsCountTxt;

        public Button freePurchaseBtn;
        public Text freeCountTxt;

        public Button purchaseWithCurrencyBtn;
        public MoneyBarRequireView currencyRequire;

        public GameObject panelFirstTime;
        public MoneyBarRequireView bonusFirstTime;

        public GameObject FreeContainer;
        public GameObject AdsContainer;
        public GameObject NotifObject;
        public GameObject PurchaseWithCurrencyContainer;
        public GameObject PurchaseIapContainer;
        public GameObject discountIap;
        public GameObject hide;
        public GameObject tagBestSeller;

        public Resource CostForPurchaseCurrency;

        [SerializeField] private Transform _buttonSoldOut;

        [SerializeField] private Text textValue;

        private float sale;

        public void ShowDiscountIap(bool isShow)
        {
            discountIap.gameObject.SetActive(sale > 1);
        }

        public void HideAll()
        {
            if (rewardsView != null)
                rewardsView.gameObject.SetActive(false);
            if (iconPack != null) iconPack?.gameObject.SetActive(false);

            packName.gameObject.SetActive(false);
            currencyRequire.gameObject.SetActive(false);
            adsCountTxt.gameObject.SetActive(false);
            freeCountTxt.gameObject.SetActive(false);
            discountValueTxt.gameObject.SetActive(false);
            if (panelFirstTime != null) panelFirstTime.gameObject.SetActive(false);

            if (discountGroup != null)
                discountGroup.SetActive(false);
            if (tagBestSeller != null)
                tagBestSeller.SetActive(false);
        }

        public virtual async void SetData(PurchaseType purchaseType, bool activePack, string packNameIap,
            string localize, string count, bool showProgress, float sale, Resource require, Resource[] rewards,
            Sprite spritePack)
        {
            this.sale = sale;

            // iapTxt.text = IAPManager.Instance.GetPricingLocalize(packNameIap);
            // iapDiscountTxt.text = IAPManager.Instance.GetPriceWithSale(packNameIap, sale);
            discountValueTxt.text = string.Format(Localization.Current.Get("shop", "percent_discount"), sale);
            adsCountTxt.text = count;
            freeCountTxt.text = count;

            currencyRequire.SetData(require);
            packName.text = localize;
            iconPack.sprite = spritePack;

            PurchaseWithCurrencyContainer.SetActive(activePack && purchaseType == PurchaseType.Currency);
            PurchaseIapContainer.SetActive(activePack && purchaseType == PurchaseType.IAP);
            FreeContainer.SetActive(activePack && purchaseType == PurchaseType.Free);
            AdsContainer.SetActive(activePack && purchaseType == PurchaseType.Ads);

            hide.SetActive(!activePack);
            NotifObject.SetActive(!hide.activeSelf);

            rewardsView.InitOrUpdateView(rewards, showProgress);
        }


        public void InitButtons(Action adsAction, Action purchaseCurrencyAction, Action iapAction, Action freeAction)
        {
            purchaseWithCurrencyBtn.onClick.RemoveAllListeners();
            adsPurchaseBtn.onClick.RemoveAllListeners();
            purchaseIAPBtn.onClick.RemoveAllListeners();
            freePurchaseBtn.onClick.RemoveAllListeners();
            purchaseWithCurrencyBtn.onClick.AddListener(() => { purchaseCurrencyAction?.Invoke(); });
            adsPurchaseBtn.onClick.AddListener(() => { adsAction?.Invoke(); });
            purchaseIAPBtn.onClick.AddListener(() => { iapAction?.Invoke(); });
            freePurchaseBtn.onClick.AddListener(() => { freeAction?.Invoke(); });
        }

        #region Set Data

        public PurchaseTemplate SetPurchaseType(PurchaseType purchaseType, bool activePack)
        {
            PurchaseWithCurrencyContainer.SetActive(activePack && purchaseType == PurchaseType.Currency);
            PurchaseIapContainer.SetActive(activePack && purchaseType == PurchaseType.IAP);
            FreeContainer.SetActive(activePack && purchaseType == PurchaseType.Free);
            AdsContainer.SetActive(activePack && purchaseType == PurchaseType.Ads);
            if (hide != null) hide.SetActive(!activePack);

            NotifObject.SetActive(hide != null && !hide.activeSelf);

            return this;
        }

        public void SetPurchased(bool isPurchased)
        {
            hide.SetActive(isPurchased);
            NotifObject.SetActive(!hide.activeSelf);
        }

        public PurchaseTemplate SetBestSeller(bool isBestSeller)
        {
            if (tagBestSeller != null)
                tagBestSeller.SetActive(isBestSeller);
            return this;
        }

        public virtual PurchaseTemplate SetRewards(Resource[] rewards, bool showProgress)
        {
            if (rewardsView != null)
            {
                rewardsView.gameObject.SetActive(true);
                rewardsView.InitOrUpdateView(rewards, showProgress);
            }

            if (textValue != null)
                textValue.text = rewards[0].resNumber > 1 ? rewards[0].resNumber.ToString() : string.Empty;

            return this;
        }

        public PurchaseTemplate SetIconPack(Sprite spritePack, bool isSetNative = false)
        {
            ShowIconPack();
            iconPack.sprite = spritePack;
            if (isSetNative) iconPack.SetNativeSize();

            return this;
        }

        public void ShowIconPack()
        {
            iconPack.gameObject.SetActive(true);
        }

        public PurchaseTemplate SetPackName(string localize)
        {
            packName.gameObject.SetActive(true);
            packName.text = localize;
            return this;
        }

        public PurchaseTemplate SetCurrencyRequire(Resource require)
        {
            if (require.resType == EnumBase.ResourceTypes.None) return this;
            currencyRequire.gameObject.SetActive(true);
            currencyRequire.SetData(require);
            CostForPurchaseCurrency = require;
            return this;
        }

        public PurchaseTemplate SoldOut(bool isSoldOut)
        {
            _buttonSoldOut.gameObject.SetActive(isSoldOut);
            purchaseWithCurrencyBtn.gameObject.SetActive(!isSoldOut);
            adsPurchaseBtn.gameObject.SetActive(!isSoldOut);
            purchaseIAPBtn.gameObject.SetActive(!isSoldOut);
            freePurchaseBtn.gameObject.SetActive(!isSoldOut);
            return this;
        }

        public PurchaseTemplate SetCount(string count)
        {
            adsCountTxt.gameObject.SetActive(true);
            freeCountTxt.gameObject.SetActive(true);

            adsCountTxt.text = count;
            freeCountTxt.text = count;
            return this;
        }

        public PurchaseTemplate SetSale(float numberSaleValue)
        {
            sale = numberSaleValue;
            if (sale == 0) return this;

            if (discountGroup != null)
                discountGroup.SetActive(true);

            discountValueTxt.text =
                string.Format(Localization.Current.Get("shop", "percent_discount"), numberSaleValue);
            discountValueTxt.gameObject.SetActive(true);
            return this;
        }

        public PurchaseTemplate FreeClaim(bool isFreeClaim)
        {
            _buttonSoldOut.gameObject.SetActive(false);
            purchaseWithCurrencyBtn.gameObject.SetActive(false);
            adsPurchaseBtn.gameObject.SetActive(false);
            purchaseIAPBtn.gameObject.SetActive(!isFreeClaim);
            freePurchaseBtn.gameObject.SetActive(isFreeClaim);
            return this;
        }

        public PurchaseTemplate SetProductId(string productId)
        {
            iapTxt.text = InAppManager.Instance.GetPricingLocalize(productId);
            iapDiscountTxt.text = InAppManager.Instance.GetPriceWithSale(productId, sale);
            return this;
        }

        public PurchaseTemplate SetFirstTimeBonus(Resource bonus)
        {
            panelFirstTime.SetActive(true);
            bonusFirstTime.SetData(bonus);
            return this;
        }

        #endregion
    }
}