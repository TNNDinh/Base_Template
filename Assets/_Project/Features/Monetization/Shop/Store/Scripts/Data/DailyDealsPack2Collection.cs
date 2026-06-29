using System;
using UnityEngine;

public class DailyDealsPack2Collection : ScriptableObject
{
    public DailyDealsPack2Model[] dataGroups;
}

[Serializable]
public class DailyDealsPack2Model : PackTemplateModel
{
}