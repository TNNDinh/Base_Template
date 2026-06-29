using BlackFace.Libraries.Modules.UIModule;
using Ezg.Package.Localize;
using Game.Runtime;
using TigerForge;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;

public class RemoveAdsController : PackagePurchaseBaseController
{
    public int PackId;

    protected override void Start()
    {
        Config = DataManager.RemoveAdsPack.dataGroups[PackId];
        base.Start();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EventManager.StartListening(EventName.PurchasedIapSuccess, UpdateView);
        gameObject.SetActive(!ShopService.IsRemoveAds());
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventName.PurchasedIapSuccess, UpdateView);
    }

    protected override void UpdateView()
    {
        base.UpdateView();
        gameObject.SetActive(!ShopService.IsRemoveAds());
        if (!gameObject.activeSelf)
            UIManager.Instance.CloseFeature(GameEnums.Features.RemoveAdsPack);

        purchaseTemplate.ShowIconPack();
        purchaseTemplate.SetSale(Config.sale);
    }

    protected override Resource[] GetPackageRewards()
    {
        return Config.rewards.GetFinalRewardsBonus();
    }

    protected override void AddRewards(PurchaseType type)
    {
        //base.AddRewards(type);
        EventManager.EmitEvent(nameof(EventName.PurchaseRemoveAdsSuccess));
        GameSystems.ShowSimpleMessage("remove_ads_purchased", LocalizeCategory.Shop);
        RewardsService.ReceiveRewards(GetPackageRewards(), type, nameof(RemoveAdsController));
        UpdateView();
    }
}