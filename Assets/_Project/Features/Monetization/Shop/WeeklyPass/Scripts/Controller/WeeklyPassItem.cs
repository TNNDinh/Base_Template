using System;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.IAP;
using Ezg.Package.AdsManager;
using Ezg.Package.Audio;
using Ezg.Package.ProgressThread;
using Game.Runtime;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

public class WeeklyPassItem : MonoBehaviour
{
    [SerializeField] protected PurchaseTemplate purchaseTemplate;

    [Header("Color Currency")] [SerializeField]
    private Color gemColorBackground;

    [SerializeField] private Color gemColorInnerBackground;

    [Header("Color Coin")] [SerializeField]
    private Color coinColorBackground;

    [SerializeField] private Color coinColorInnerBackground;

    [SerializeField] private Button buttonInfo;

    [SerializeField] private Text textValueRwUpgared;
    [SerializeField] private Text textValueRwBoost;
    [SerializeField] private Transform parrentTimmer;
    [SerializeField] private Text textCooldownTime;
    [SerializeField] private IconView iconViewRwDaily;
    [SerializeField] private IconView iconViewRwInstant;
    [SerializeField] private Transform objClaimInstantRW;
    [SerializeField] private Transform objClaimDailyRW;
    [SerializeField] private Button renewButton;
    [SerializeField] private Text textRenew;
    protected WeeklyPassRewardModel _config;

    private ProgressThread _progressThread;

    private WeeklyPassBaseLogic _weeklyPassBaseLogic;

    private WeeklyPassType _weeklyPassType;

    // Start is called before the first frame update
    private void Start()
    {
        purchaseTemplate.InitButtons(OnPurchaseAds, OnPurchaseCurrency, OnPurchaseIap, OnPurchaseFree);
        EventManager.StartListening(EventName.PurchasedIapSuccess, UpdateView);
    }

    public void SetData(WeeklyPassRewardModel config, WeeklyPassType type)
    {
        _config = config;
        _weeklyPassType = type;
        _weeklyPassBaseLogic = WeeklyPassFactory.Get(type);
        UpdateView();
        buttonInfo.onClick.AddListener(ShowInfo);
    }

    private void ShowInfo()
    {
        UIManager.Instance.Show(GameEnums.Features.WeeklyPassInfo).Forget();
    }

    protected virtual void UpdateView()
    {
        purchaseTemplate.HideAll();
        var currentPurchase = _config.purchaseList[0];
        var canClaim = _weeklyPassBaseLogic.CanClaimReward();
        var canRenew = !_weeklyPassBaseLogic.CanClaimReward() && _weeklyPassBaseLogic.TimeRemaining() > 0;
        var pType = canClaim ? PurchaseType.Free : canRenew ? PurchaseType.None : currentPurchase.purchaseType;

        purchaseTemplate.ShowIconPack();
        purchaseTemplate
            .SetSale(_config.sale)
            /*.SetRewards( , false)*/
            .SetPurchaseType(pType, true);
        //.SetPackName(_config.packName.Contains("scroll_pack") ? GameSystems.Localize(_config.GetRewards()[0].GetName()) : GameSystems.Localize(_config.packName, LocalizeCategory.Shop));
        /*.SetIconPack(PlayerResource.GetIconPack((EnumBase.MoneyTypes)_reward.resId, _config.id));*/
        // .SetIconPack(_reward.resType == EnumBase.ResourceTypes.Item
        //     ? GameplayService.GetItemImage((MergeEnum.MergeItemTypes)_reward.resId, (int)_reward.resId)
        //     : PlayerResource.GetCurrencyImage(_reward.resId));


        switch (pType)
        {
            case PurchaseType.None:
            case PurchaseType.Free:
                //purchaseTemplate.FreeClaim(true);
                break;
            case PurchaseType.Ads:
                break;
            case PurchaseType.Currency:
                break;
            case PurchaseType.IAP:
                purchaseTemplate.SetProductId(_config.ProductId);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //purchaseTemplate.SoldOut(PlayerDataManager.PlayerShop.IsSoldOutDailyDealsPack(_config));
        if (_config.firstTimePurchaseBonus > 0 && !ShopService.IsBuyPack(_config.ProductId))
        {
            var firstCurrency = _config.GetRewards()[0];
            purchaseTemplate.SetFirstTimeBonus(new Resource(firstCurrency.resType, firstCurrency.resId,
                _config.firstTimePurchaseBonus));
        }

        //UpdateBackgroundTemplate(_reward);
        HandleRenewButton(canRenew);
        UpdateUI();
    }

    private void HandleRenewButton(bool canRenew)
    {
        renewButton.gameObject.SetActive(canRenew);
        renewButton.onClick.RemoveAllListeners();
        renewButton.onClick.AddListener(OnRenew);
        textRenew.text = InAppManager.Instance.GetPricingLocalize(_config.ProductId);
    }

    private void OnRenew()
    {
        OnRenewPack();
    }

    // private void UpdateBackgroundTemplate(Resource reward)
    // {
    //     switch ((EnumBase.MoneyTypes)reward.resId)
    //     {
    //         case EnumBase.MoneyTypes.Coin:
    //             background.color = coinColorBackground;
    //             innerBackground.color = coinColorInnerBackground;
    //             break;
    //         case EnumBase.MoneyTypes.Gem:
    //             background.color = gemColorBackground;
    //             innerBackground.color = gemColorInnerBackground;
    //             break;
    //     }
    // }
    //
    // protected virtual void AddRewards(PurchaseType type)
    // {
    //     // // Check first purchase bonus
    //     // if (ShopService.IsBuyPack(_config.ProductId))
    //     // {
    //     //     RewardsService.ReceiveRewards(new[]
    //     //     {
    //     //         _reward,
    //     //     }, type, nameof(ShopService), _config.packName, productId: _config.ProductId);
    //     // }
    //     // else
    //     // {
    //     //     var clones = new[]
    //     //     {
    //     //         _reward.CloneJson()
    //     //     };
    //     //     foreach (var clone in clones) clone.resNumber += _config.firstTimePurchaseBonus;
    //     //     RewardsService.ReceiveRewards(clones, type, nameof(ShopService), _config.packName,
    //     //         productId: _config.ProductId);
    //     // }
    //     //
    //     // EventManager.EmitEvent(EventName.UpdateResource);
    //     // PlayerDataManager.PlayerShop.PurchaseDailyDealsPack(_config.ProductId);
    //     // purchaseTemplate.SoldOut(PlayerDataManager.PlayerShop.IsSoldOutDailyDealsPack(_config));
    // }

    private void OnPurchaseAds()
    {
        void CallBack()
        {
            //AddRewards(PurchaseType.Ads);
            // PublisherService.NotifyListener(SubjectType.PurchaseTimesInShop);
            // this.PushEvent(EventTracking.ads_reward_offer, source);
            AfterPurchaseAds();
        }

        var location = AdsPlacement.Shop(_config.packName);
        AdsManager.Instance.ShowRewardedVideo(location, CallBack);
    }

    private void OnPurchaseFree()
    {
        //AddRewards(PurchaseType.Free);
        _weeklyPassBaseLogic.PurchasePass(PurchaseType.Free, PurchaseWeeklyPassType.New);
        AfterPurchaseFree();
    }

    private void OnPurchaseCurrency()
    {
        // void CallBack()
        // {
        //     AddRewards(PurchaseType.Currency);
        //
        //     AfterPurchaseCurrency();
        // }
        //
        // var cost = _config.costRequire.GetResource();
        // cost = resourcePrice.CloneJson();
        // PlayerResource.RemoveResource(cost, CallBack, source: nameof(ShopService), sourceId: _config.packName,
        //     placement: string.IsNullOrEmpty(_config.ProductId) ? _config.packName : _config.ProductId);
    }

    private void UpdateUI()
    {
        // int countCanBuy = PlayerDataManager.PlayerShop.GetCountCanBuyInPack(_config);
        // this.manyCanBuyText.gameObject.SetActive(countCanBuy > -1);
        // this.manyCanBuyText.text = $"{countCanBuy} {GameSystems.Localize("left")}";
        objClaimDailyRW.gameObject.SetActive(!_weeklyPassBaseLogic.CanClaimReward() &&
                                             _weeklyPassBaseLogic.TimeRemaining() > 0);
        objClaimInstantRW.gameObject.SetActive(_weeklyPassBaseLogic.TimeRemaining() > 0);
        parrentTimmer.gameObject.SetActive(_weeklyPassBaseLogic.TimeRemaining() > 0);
        var model = _weeklyPassBaseLogic.GetWeeklyPassModel();
        textValueRwUpgared.text =
            $"{DataManager.GeneralConfig.dataGroups.maxEnergy} + {model.rewardsUpgrade[0].upgradeRwNumber}";
        textValueRwBoost.text = $"+{model.rewardsBoost[0].boostRwNumber} %";
        iconViewRwDaily.SetData(new Resource
        {
            resType = model.rewardsDaily[0].dailyRwType,
            resId = model.rewardsDaily[0].dailyRwId,
            resNumber = model.rewardsDaily[0].dailyRwNumber,
            customValue = model.rewardsDaily[0].dailyRwCustomValue.CloneJson()
        });
        iconViewRwInstant.SetData(new Resource
        {
            resType = model.rewardsInstance[0].instanceRwType,
            resId = model.rewardsInstance[0].instanceRwId,
            resNumber = model.rewardsInstance[0].instanceRwNumber,
            customValue = model.rewardsInstance[0].instanceRwCustomValue.CloneJson()
        });

        if (_progressThread != null) _progressThread.Dispose();

        UpdateTime();
        var timeProgressThread = _weeklyPassBaseLogic.TimeRemaining() + 1;
        _progressThread = _progressThread
            .Interval(1f, endTime: timeProgressThread).AddTo(this)
            .Subscribe(UpdateTime).Start();
    }

    private void UpdateTime()
    {
        if (textCooldownTime != null)
            textCooldownTime.text = TimeManager.GetRemainingTimeToString(_weeklyPassBaseLogic.TimeRemaining());
    }

    protected virtual void OnPurchaseIap()
    {
        void CallBack()
        {
            _weeklyPassBaseLogic.PurchasePass(PurchaseType.IAP, PurchaseWeeklyPassType.New);
            //AddRewards(PurchaseType.IAP);
            AfterPurchaseIap();
        }

        InAppManager.Instance.Buy(_config.ProductId, CallBack, "shop", $"weekly_pass_{_weeklyPassType}".ToSnakeCase());
    }

    protected virtual void OnRenewPack()
    {
        void CallBack()
        {
            _weeklyPassBaseLogic.PurchasePass(PurchaseType.IAP, PurchaseWeeklyPassType.Renew);
            //AddRewards(PurchaseType.IAP);
            AfterPurchaseIap();
        }

        InAppManager.Instance.Buy(_config.ProductId, CallBack, "shop", $"weekly_pass_{_weeklyPassType}".ToSnakeCase());
    }

    protected virtual void AfterPurchaseIap()
    {
        UpdateView();
        AudioService.Default.PlaySound(DataManager.SoundConfig.PurchaseItem);
    }

    protected virtual void AfterPurchaseCurrency()
    {
        //UpdateUI();
        UpdateView();
        AudioService.Default.PlaySound(DataManager.SoundConfig.PurchaseItem);
    }

    protected virtual void AfterPurchaseAds()
    {
        UpdateView();
        AudioService.Default.PlaySound(DataManager.SoundConfig.PurchaseItem);
    }

    protected virtual void AfterPurchaseFree()
    {
        UpdateView();
        AudioService.Default.PlaySound(DataManager.SoundConfig.PurchaseItem);
    }
}