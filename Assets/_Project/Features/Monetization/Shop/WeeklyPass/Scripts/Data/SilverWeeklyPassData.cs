using System;

public class SilverWeeklyPassData
{
    public WeeklyData weeklyData = new();
}

[Serializable]
public class WeeklyData
{
    public long timeEnd;
    public long lastTimeClaimDailyReward;
}