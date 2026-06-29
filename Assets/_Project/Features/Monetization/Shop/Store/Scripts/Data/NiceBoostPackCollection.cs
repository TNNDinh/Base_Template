using System;
using UnityEngine;

public class NiceBoostPackCollection : ScriptableObject
{
    public NiceBoostPackModel[] dataGroups;
}

[Serializable]
public class NiceBoostPackModel : DailyDealsPackModel
{
}