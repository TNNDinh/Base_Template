using System.Linq;
using Game.Runtime;
using TigerForge;
using Ezg.Feature.Shared.GameData;

public class BundlePackController : PackagePurchaseBaseController
{
    public IconView MainReward;

    public int PackId;

    protected override void Start()
    {
        Config = DataManager.BundlePack.dataGroups[PackId];
        base.Start();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EventManager.StartListening(EventName.PurchasedIapSuccess, UpdateView);
        gameObject.SetActive(!ShopService.IsBuyPack(DataManager.BundlePack.dataGroups[PackId].ProductId));
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventName.PurchasedIapSuccess, UpdateView);
    }

    protected override PurchaseTemplate SetReward()
    {
        purchaseTemplate
            .SetRewards(GetPackageRewards().CompileRewards().Skip(1).ToArray(), false);
        return purchaseTemplate;
    }

    protected override void UpdateView()
    {
        base.UpdateView();
        gameObject.SetActive(!ShopService.IsBuyPack(DataManager.BundlePack.dataGroups[PackId].ProductId));

        purchaseTemplate.SetSale(Config.sale);
        MainReward.SetData(GetPackageRewards()[0]);
    }
}