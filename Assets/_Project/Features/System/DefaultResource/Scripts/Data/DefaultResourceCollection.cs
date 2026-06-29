using System;
using UnityEngine;

[Serializable]
public class DefaultResourceCollection : ScriptableObject
{
    public DefaultResourceModel[] dataGroups;

    public DefaultResourceModel[] GetAll()
    {
        return dataGroups;
    }
}