using Ezg.Feature.Shared.GameData;
namespace Ezg.Feature.Monetization.Shop
{
    public class LuxuriousOffer : BaseBundleOffer
    {
        protected override PackTemplateModel PackConfig => DataManager.LuxuriousOffer.dataGroups;
    }
}