using System;
using UnityEngine;

public class PoolRewardsDailyDeals2Collection : ScriptableObject
{
    public PoolRewardsDailyDeals2Model[] dataGroups;
}

[Serializable]
public class PoolRewardsDailyDeals2Model
{
    public Resource reward;
    public int manyPurchase = 1;
    public bool isEveryDay;
    public int seat = -1;
    public float weight = 1f;
}