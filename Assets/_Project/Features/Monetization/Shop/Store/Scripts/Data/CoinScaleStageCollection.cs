using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class CoinScaleStageCollection : ScriptableObject
{
    public CoinScaleStageModel[] dataGroups;

    public CoinScaleStageModel GetByStage(int stageId)
    {
        return dataGroups.FirstOrDefault(x => x.rangeStageId[0] <= stageId && stageId <= x.rangeStageId[1]);
    }
}