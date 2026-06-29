using System.Collections.Generic;
using Ezg.Feature.Shared.GameData;

public enum BundleOfferType
{
    FirstPurchase,
    StandardDiamond,
    LuxuriousOffer
}

public static class BundleOfferService
{
    private static readonly List<PackTemplateModel> listBundle = new();

    static BundleOfferService()
    {
        listBundle.Add(DataManager.FirstPurchase.dataGroups);
        listBundle.Add(DataManager.StandardDiamond.dataGroups);
        listBundle.Add(DataManager.LuxuriousOffer.dataGroups);
    }

    public static List<PackTemplateModel> GetBundleActive()
    {
        var list = new List<PackTemplateModel>();
        foreach (var bundle in listBundle)
        {
            var isActive = ShopService.IsActivePack(bundle);
            if (isActive) list.Add(bundle);
        }

        return list;
    }
}