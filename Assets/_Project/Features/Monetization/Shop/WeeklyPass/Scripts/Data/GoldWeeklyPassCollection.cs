using System;
using UnityEngine;

public class GoldWeeklyPassCollection : ScriptableObject
{
    public GoldWeeklyPassModel dataGroup;
}

[Serializable]
public class GoldWeeklyPassModel : WeeklyPassRewardModel
{
}