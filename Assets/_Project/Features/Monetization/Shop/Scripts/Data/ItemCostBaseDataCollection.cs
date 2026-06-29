using System;
using UnityEngine;

public class ItemCostBaseDataCollection : ScriptableObject
{
    public ItemCostBaseDataModel[] dataGroup;

    public ItemCostBaseDataModel GetByType(MergeEnum.MergeItemTypes type)
    {
        var data = Array.Find(dataGroup, x => x.type == type);
        if (data == null) data = dataGroup[0];
        return data;
    }
}

[Serializable]
public class ItemCostBaseDataModel
{
    public MergeEnum.MergeItemTypes type;
    public int cost;
}