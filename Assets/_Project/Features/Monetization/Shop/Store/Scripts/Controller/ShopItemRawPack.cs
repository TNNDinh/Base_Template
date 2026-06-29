using System;
using Ezg.Core.Utils;
using Ezg.Feature.IAP;
using Ezg.Package.AdsManager;
using Ezg.Package.Audio;
using Game.Runtime;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

public class ShopItemRawPack : MonoBehaviour
{
    [SerializeField] protected PurchaseTemplate purchaseTemplate;
    [SerializeField] private Image background, innerBackground;
    [SerializeField] private bool isUseIconReward;

    [Header("Color Currency")] [SerializeField]
    private Color gemColorBackground;

    [SerializeField] private Color gemColorInnerBackground;

    [Header("Color Coin")] [SerializeField]
    private Color coinColorBackground;

    [SerializeField] private Color coinColorInnerBackground;

    protected PackTemplateModel _config;
    private EnumBase.MoneyTypes _moneyType;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        purchaseTemplate.InitButtons(OnPurchaseAds, OnPurchaseCurrency, OnPurchaseIap, OnPurchaseFree);
        EventManager.StartListening(EventName.PurchasedIapSuccess, UpdateView);
    }

    public void SetData(PackTemplateModel config, EnumBase.MoneyTypes moneyType)
    {
        _config = config;
        _moneyType = moneyType;
        UpdateView();
    }

    protected virtual void UpdateView()
    {
        purchaseTemplate.HideAll();
        var currentPurchase = _config.purchaseList[0];
        var rewards = GetRewards();

        purchaseTemplate
            .SetSale(_config.sale)
            .SetRewards(rewards, false)
            .SetPurchaseType(currentPurchase.purchaseType, true)
            //.SetPackName(_config.packName.Contains("scroll_pack") ? GameSystems.Localize(_config.GetRewards()[0].GetName()) : GameSystems.Localize(_config.packName, LocalizeCategory.Shop))
            .SetIconPack(isUseIconReward
                ? PlayerResource.GetCurrencyImage(rewards[0].resId)
                : PlayerResource.GetIconPack(_moneyType, _config.id), true);

        switch (currentPurchase.purchaseType)
        {
            case PurchaseType.None:
            case PurchaseType.Free:
            case PurchaseType.Ads:
                break;
            case PurchaseType.Currency:
                purchaseTemplate.SetCurrencyRequire(_config.costRequire.GetResource());
                break;
            case PurchaseType.IAP:
                purchaseTemplate.SetProductId(_config.ProductId);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_config.firstTimePurchaseBonus > 0 && !ShopService.IsBuyPack(_config.ProductId))
        {
            var firstCurrency = rewards[0];
            purchaseTemplate.SetFirstTimeBonus(new Resource(firstCurrency.resType, firstCurrency.resId,
                _config.firstTimePurchaseBonus));
        }

        UpdateBackgroundTemplate(rewards[0]);
    }

    private void UpdateBackgroundTemplate(Resource reward)
    {
        switch ((EnumBase.MoneyTypes)reward.resId)
        {
            case EnumBase.MoneyTypes.Gold:
                //background.color = coinColorBackground;
                //innerBackground.color = coinColorInnerBackground;
                break;
            case EnumBase.MoneyTypes.Diamonds:
                // background.color = gemColorBackground;
                //innerBackground.color = gemColorInnerBackground;
                break;
        }
    }

    protected virtual void AddRewards(PurchaseType type)
    {
        // Check first purchase bonus
        if (ShopService.IsBuyPack(_config.ProductId))
        {
            RewardsService.ReceiveRewards(GetRewards(), type, SourceTracking.Shop, _config.packName,
                productId: _config.ProductId, isSpawnCurrency: true);
        }
        else
        {
            var clones = GetRewards();
            foreach (var clone in clones) clone.resNumber += _config.firstTimePurchaseBonus;
            RewardsService.ReceiveRewards(clones, type, SourceTracking.Shop, _config.packName,
                productId: _config.ProductId, isSpawnCurrency: true);
        }
    }

    protected virtual Resource[] GetRewards()
    {
        return _config.GetRewards();
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

        var location = AdsPlacement.Shop(_config.packName);
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

        var cost = _config.costRequire.GetResource();
        PurchaseManager.PurchaseOffline(cost, CallBack, source: nameof(ShopService), sourceId: _config.packName,
            placement: string.IsNullOrEmpty(_config.ProductId) ? _config.packName : _config.ProductId);
    }

    protected virtual void OnPurchaseIap()
    {
        void CallBack()
        {
            AddRewards(PurchaseType.IAP);
            AfterPurchaseIap();
        }

        InAppManager.Instance.Buy(_config.ProductId, CallBack, "shop", "raw_pack");
    }

    protected virtual void AfterPurchaseIap()
    {
        AudioService.Default.PlaySound(DataManager.SoundConfig.PurchaseItem);
        // removed: DiscountGemRawService
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