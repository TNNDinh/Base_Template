using System.Collections.Generic;
using Ezg.Package.Factory;
using Ezg.Feature.Shared.Config;

public class PlayerUnlockFeature : DataPlayerBaseGeneric<PlayerUnlockFeatureData>
{
    public bool IsShowedUnlockFeature(GameEnums.Features feature)
    {
        if (dataBase.FeatureDict.ContainsKey(feature)) return dataBase.FeatureDict[feature];

        return false;
    }

    public void AddFeatureShowed(GameEnums.Features feature)
    {
        if (dataBase.FeatureDict.ContainsKey(feature))
        {
            if (dataBase.ListFeatureQueue.Contains(feature))
            {
                dataBase.ListFeatureQueue.Remove(feature);
                Save();
            }

            return;
        }

        dataBase.FeatureDict.Add(feature, true);
        if (dataBase.ListFeatureQueue.Contains(feature)) dataBase.ListFeatureQueue.Remove(feature);

        Save();
    }

    public void ClearAllShowed()
    {
        dataBase.ListFeatureQueue.ForEach(x => dataBase.FeatureDict.TryAdd(x, true));
        dataBase.ListFeatureQueue.Clear();
    }

    public void AddQueueFeature(GameEnums.Features feature, bool isForce = false)
    {
        if (IsShowedUnlockFeature(feature)) return;
        if (dataBase.ListFeatureQueue.Contains(feature) && !isForce) return;
        dataBase.ListFeatureQueue.Add(feature);

        Save();
    }

    public List<GameEnums.Features> GetQueueFeature()
    {
        return dataBase.ListFeatureQueue;
    }
}