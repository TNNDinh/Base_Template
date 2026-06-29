using System;
using UnityEngine;

[Serializable]
public class GeneralConfigCollection : ScriptableObject
{
    public GeneralConfigModel dataGroups;

    public GeneralConfigModel GetData()
    {
        return dataGroups;
    }
}