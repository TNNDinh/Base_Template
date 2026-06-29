using Ezg.Feature.Shared.GameData;
namespace Ezg.Feature.Monetization.Shop
{
    public class StandardDiamond : BaseBundleOffer
    {
        protected override PackTemplateModel PackConfig => DataManager.StandardDiamond.dataGroups;
    }
}