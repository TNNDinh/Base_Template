using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using TigerForge;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;

public class SilverWeeklyPassLogic : WeeklyPassBaseLogic
{
    public SilverWeeklyPassLogic() : base(WeeklyPassType.Silver)
    {
    }

    public override WeeklyPassRewardModel GetWeeklyPassModel()
    {
        return DataManager.SilverWeeklyPass.dataGroup;
    }

    public override bool CanClaimReward()
    {
        return !PlayerDataManager.SilverWeeklyPassData.IsClaimDailyReward() &&
               !PlayerDataManager.SilverWeeklyPassData.IsEndSilverPass();
    }

    public override void PurchasePass(PurchaseType purchaseType, PurchaseWeeklyPassType weeklyPassPurchaseType)
    {
        ClaimPass(purchaseType, weeklyPassPurchaseType);
    }

    public override void ClaimPass(PurchaseType purchaseType, PurchaseWeeklyPassType weeklyPassPurchaseType)
    {
        if (purchaseType == PurchaseType.Free) ClaimFree();

        if (purchaseType == PurchaseType.IAP) ClaimPurchased(weeklyPassPurchaseType);

        EventManager.EmitEvent(EventName.UpdateResource);
        //EventManager.EmitEvent(nameof(EventName.EnergyChanged));
    }

    public override void ClaimFree()
    {
        PlayerDataManager.SilverWeeklyPassData.ClaimDailyReward();
        var listRW = new List<Resource>();
        foreach (var reward in GetWeeklyPassModel().rewardsDaily)
            listRW.Add(new Resource
            {
                resType = reward.dailyRwType,
                resId = reward.dailyRwId,
                resNumber = reward.dailyRwNumber,
                customValue = reward.dailyRwCustomValue.CloneJson()
            });

        RewardsService.ReceiveReward(listRW, isSpawnCurrency: true, source: SourceTracking.Shop,
            sourceDetail: SourceDetailTracking.WeeklyPassSilver);
    }

    public override void ClaimPurchased(PurchaseWeeklyPassType weeklyPassPurchaseType)
    {
        PlayerDataManager.SilverWeeklyPassData.Purchase(weeklyPassPurchaseType);

        var model = GetWeeklyPassModel();
        var allRewards = new List<Resource>();

        foreach (var reward in model.rewardsUpgrade)
            allRewards.Add(new Resource
            {
                resType = reward.upgradeRwType,
                resId = reward.upgradeRwId,
                resNumber = reward.upgradeRwNumber,
                customValue = reward.upgradeRwCustomValue.CloneJson()
            });

        foreach (var reward in model.rewardsInstance)
            allRewards.Add(new Resource
            {
                resType = reward.instanceRwType,
                resId = reward.instanceRwId,
                resNumber = reward.instanceRwNumber,
                customValue = reward.instanceRwCustomValue.CloneJson()
            });

        RewardsService.ReceiveReward(allRewards, PurchaseType.IAP, SourceTracking.Shop,
            SourceDetailTracking.WeeklyPassSilver, isShowPopup: true, isSpawnCurrency: true);
        EventManager.EmitEvent(nameof(EventName.EnergyChanged));
        PlayerResource.AutoRestoreEnergy().Forget();
    }

    public override long TimeRemaining()
    {
        return PlayerDataManager.SilverWeeklyPassData.dataBase.weeklyData.timeEnd - TimeManager.GetNow();
    }
}