using Ezg.Feature.Shared.GameData;
namespace Ezg.Feature.Monetization.Shop
{
    public class FirstPurchase : BaseBundleOffer
    {
        protected override PackTemplateModel PackConfig => DataManager.FirstPurchase.dataGroups;
    }
}