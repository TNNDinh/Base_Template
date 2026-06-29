using System.Linq;
using UnityEngine;
using Ezg.Feature.Shared.Config;

[CreateAssetMenu(fileName = "UnlockFeatureViewCollection", menuName = "ScriptableObjects/UnlockFeatureViewCollection",
    order = 2)]
public class UnlockFeatureViewCollection : ScriptableObject
{
    public UnlockFeatureViewModel[] dataGroups;

    public UnlockFeatureViewModel GetConfigByType(GameEnums.Features feature)
    {
        return dataGroups.FirstOrDefault(x => x.feature == feature);
    }
}