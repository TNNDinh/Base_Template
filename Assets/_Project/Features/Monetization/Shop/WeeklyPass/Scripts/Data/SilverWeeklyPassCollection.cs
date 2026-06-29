using System;
using UnityEngine;

public class SilverWeeklyPassCollection : ScriptableObject
{
    public SilverWeeklyPassModel dataGroup;
}

[Serializable]
public class SilverWeeklyPassModel : WeeklyPassRewardModel
{
}

[Serializable]
public class WeeklyPassRewardModel : PackTemplateModel
{
    public RewardInstanceModel[] rewardsInstance;
    public RewardDailyModel[] rewardsDaily;
    public RewardBoostModel[] rewardsBoost;
    public RewardUpgradeModel[] rewardsUpgrade;
    public long timePassInDays;
}

[Serializable]
public class RewardInstanceModel
{
    public int instanceRwType;
    public int instanceRwId;
    public long instanceRwNumber;
    public float[] instanceRwCustomValue;
}

[Serializable]
public class RewardDailyModel
{
    public int dailyRwType;
    public int dailyRwId;
    public long dailyRwNumber;
    public float[] dailyRwCustomValue;
}

[Serializable]
public class RewardBoostModel
{
    public int boostRwType;
    public int boostRwId;
    public long boostRwNumber;
    public float[] boostRwCustomValue;
}

[Serializable]
public class RewardUpgradeModel
{
    public int upgradeRwType;
    public int upgradeRwId;
    public long upgradeRwNumber;
    public float[] upgradeRwCustomValue;
}