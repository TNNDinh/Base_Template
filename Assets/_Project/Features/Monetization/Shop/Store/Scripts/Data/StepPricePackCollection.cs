using System;
using Ezg.Core.Extensions;
using UnityEngine;
using Ezg.Feature.Shared.Systems;

public class StepPricePackCollection : ScriptableObject
{
    public StepPricePackModel[] dataGroups;

    public int GetStepPriceByProductId(string productId, int step)
    {
        foreach (var pack in dataGroups)
            if (pack.ProductId == productId)
            {
                var manyStep = step < pack.stepPrices.Length ? step : pack.stepPrices.Length - 1;
                return pack.stepPrices[manyStep].stepPrices;
            }

        var manyStepDefault = step < dataGroups[^1].stepPrices.Length ? step : dataGroups[^1].stepPrices.Length - 1;
        return dataGroups[^1].stepPrices[manyStepDefault].stepPrices;
    }
}

[Serializable]
public class StepPricePackModel
{
    public string googleProductId;
    public string appleProductId;
    public StepPricePackData[] stepPrices;

    public string ProductId
    {
        get
        {
            string productId;

            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXEditor)
                productId = /*GameSystems.IsPremium ? googleProductIdPremium :*/ googleProductId;
            else
                productId = appleProductId;

            if (string.IsNullOrEmpty(productId))
            {
                var type = GetType();
                var typeName = type.Name.ToSnakeCase();
                productId = $"{typeName}_{"id"}";
            }

            return productId;
        }
    }
}

[Serializable]
public class StepPricePackData
{
    public int stepPrices;
}