using System;
using UnityEngine;

public class PackDurationBuyOnlyOneCollection : ScriptableObject
{
    public PackDurationBuyOnlyOneModel[] dataGroups;
}

[Serializable]
public class PackDurationBuyOnlyOneModel
{
    public int idPack;
    public PackDurationType type;
}