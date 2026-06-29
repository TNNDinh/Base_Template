using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;

public class WeeklyPassService
{
    public static void CheckAllWeeklyPassReset()
    {
        CheckSilverPass();
        CheckGoldPass();
    }

    public static long BonusEnergy()
    {
        long bonusEnergy = 0;
        if (!PlayerDataManager.SilverWeeklyPassData.IsEndSilverPass())
            bonusEnergy += DataManager.SilverWeeklyPass.dataGroup.rewardsUpgrade[0].upgradeRwNumber;
        if (!PlayerDataManager.GoldWeeklyPassData.IsEndPass())
            bonusEnergy += DataManager.GoldWeeklyPass.dataGroup.rewardsUpgrade[0].upgradeRwNumber;
        return bonusEnergy;
    }

    public static float BonusTimeRestoreEnergy()
    {
        float bonusTime = 0;
        if (!PlayerDataManager.SilverWeeklyPassData.IsEndSilverPass())
            bonusTime += DataManager.GeneralConfig.GetData().restoreEnergyTime *
                DataManager.SilverWeeklyPass.dataGroup.rewardsBoost[0].boostRwNumber / 100f;
        if (!PlayerDataManager.GoldWeeklyPassData.IsEndPass())
            bonusTime += DataManager.GeneralConfig.GetData().restoreEnergyTime *
                DataManager.GoldWeeklyPass.dataGroup.rewardsUpgrade[0].upgradeRwNumber / 100f;
        return bonusTime;
    }

    private static void CheckSilverPass()
    {
        if (PlayerDataManager.SilverWeeklyPassData.IsEndSilverPass())
            PlayerDataManager.SilverWeeklyPassData.ResetPass();
        if (TimeManager.IsNextDay(PlayerDataManager.SilverWeeklyPassData.dataBase.weeklyData
                .lastTimeClaimDailyReward) &&
            !PlayerDataManager.SilverWeeklyPassData.IsEndSilverPass())
            PlayerDataManager.SilverWeeklyPassData.ResetDailyReward();
    }

    private static void CheckGoldPass()
    {
        if (PlayerDataManager.GoldWeeklyPassData.IsEndPass()) PlayerDataManager.GoldWeeklyPassData.ResetPass();
        if (TimeManager.IsNextDay(PlayerDataManager.GoldWeeklyPassData.dataBase.weeklyData.lastTimeClaimDailyReward) &&
            !PlayerDataManager.GoldWeeklyPassData.IsEndPass())
            PlayerDataManager.GoldWeeklyPassData.ResetDailyReward();
    }
}