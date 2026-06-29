using Ezg.Core.Utils;
using Ezg.Package.Factory;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;

public class PlayerSilverWeeklyPassData : DataPlayerBaseGeneric<SilverWeeklyPassData>
{
    public bool IsEndSilverPass()
    {
        return dataBase.weeklyData.timeEnd <= TimeManager.GetNow();
    }


    public void Purchase(PurchaseWeeklyPassType type)
    {
        if (dataBase.weeklyData.timeEnd == 0) dataBase.weeklyData.timeEnd = TimeManager.GetNow();
        long timeRemaining = 0;
        if (type == PurchaseWeeklyPassType.New) timeRemaining = TimeManager.GetRemainingTimeToNextDay();
        dataBase.weeklyData.timeEnd += DataManager.SilverWeeklyPass.dataGroup.timePassInDays * 86400 - timeRemaining;
        Save();
    }

    public bool IsClaimDailyReward()
    {
        return dataBase.weeklyData.lastTimeClaimDailyReward > 0 &&
               !TimeManager.IsNextDay(dataBase.weeklyData.lastTimeClaimDailyReward);
    }

    public void ClaimDailyReward()
    {
        dataBase.weeklyData.lastTimeClaimDailyReward = TimeManager.GetNow();
        Save();
    }

    public void ResetDailyReward()
    {
        dataBase.weeklyData.lastTimeClaimDailyReward = 0;
        Save();
    }

    public void ResetPass()
    {
        dataBase.weeklyData.timeEnd = 0;
        dataBase.weeklyData.lastTimeClaimDailyReward = 0;
        Save();
    }
}