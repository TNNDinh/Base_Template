using System.Linq;
using UnityEngine;
using Ezg.Feature.Shared.Config;

public class UnlockFeatureCollection : ScriptableObject
{
    public UnlockFeatureModel[] dataGroups;

    public void Convert()
    {
        dataGroups = dataGroups.OrderBy(x => x.unlockValue).ToArray();
    }

    public UnlockFeatureModel GetConfigByType(GameEnums.Features feature)
    {
        return dataGroups.FirstOrDefault(x => x.feature == feature);
    }

    public UnlockFeatureModel GetByLevel(int level)
    {
        return dataGroups.FirstOrDefault(x => x.unlockValue == level);
    }

    public UnlockFeatureModel GetClosestUnlockFeatureByLevel(int level)
    {
        var validFeatures = dataGroups
            .Where(x => x.unlockType == UnlockFeatureType.Level && x.unlockValue <= level)
            .OrderByDescending(x => x.unlockValue);

        return validFeatures.FirstOrDefault();
    }
}

public enum UnlockFeatureType
{
    LevelAccount,
    Level
}

public enum FeatureTypes
{
    Feature,
    Booster
}