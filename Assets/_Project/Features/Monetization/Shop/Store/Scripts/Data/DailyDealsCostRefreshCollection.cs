using System;
using UnityEngine;

public class DailyDealsCostRefreshCollection : ScriptableObject
{
    public DailyDealsCostRefreshModel[] dataGroups;

    public Resource GetCostByIndex(int index)
    {
        var model = Array.Find(dataGroups, x => x.index == index);
        if (model != null) return model.cost;
        return dataGroups[^1].cost;
    }
}

[Serializable]
public class DailyDealsCostRefreshModel
{
    public int index;
    public Resource cost;
}