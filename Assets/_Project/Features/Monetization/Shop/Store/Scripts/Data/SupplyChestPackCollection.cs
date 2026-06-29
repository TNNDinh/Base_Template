using System;
using UnityEngine;

public class SupplyChestPackCollection : ScriptableObject
{
    public SupplyChestPackModel[] dataGroups;
}

[Serializable]
public class SupplyChestPackModel : DailyDealsPackModel
{
}