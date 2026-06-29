using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class PortalPackCollection : ScriptableObject
{
    public PortalPackModel[] dataGroups;

    public PortalPackModel GetById(int id)
    {
        return dataGroups.FirstOrDefault(x => x.id == id);
    }

    public PortalPackModel GetByHeroId(int heroId)
    {
        return dataGroups.FirstOrDefault(x => x.heroId == heroId);
    }
}