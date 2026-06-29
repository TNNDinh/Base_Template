using System;
using UnityEngine;

public class DailyDealsPackCollection : ScriptableObject
{
    public DailyDealsPackModel[] dataGroups;
}

[Serializable]
public class DailyDealsPackModel : PackTemplateModel
{
}