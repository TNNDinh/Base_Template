using System;
using UnityEngine;

public class DailyDealsPack_1Collection : ScriptableObject
{
    public DailyDealsPack_1Model[] dataGroups;
}

[Serializable]
public class DailyDealsPack_1Model : PackTemplateModel
{
    public int manyPurchase = 1;
}