using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ResourceExchangeCollection : ScriptableObject
{
    public ResourceExchangeModel[] dataGroups;

    public ResourceExchangeModel[] GetAll()
    {
        return dataGroups;
    }

    public ResourceExchangeModel GetResourceExchange(int sourceId, int type)
    {
        return dataGroups.FirstOrDefault(x => x.sourceId == sourceId && x.sourceType == type);
    }

    public List<ResourceExchangeDetailModel> GetRewards(int sourceId, int type)
    {
        return dataGroups.FirstOrDefault(x => x.sourceId == sourceId && x.sourceType == type).rewards.ToList();
    }
}