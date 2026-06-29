using System;
using Ezg.Package.Factory;

public abstract class WeeklyPassBaseLogic : DataWithOption<WeeklyPassType>
{
    public WeeklyPassBaseLogic(WeeklyPassType type)
    {
        this.type = type;
    }

    public abstract WeeklyPassRewardModel GetWeeklyPassModel();

    public abstract bool CanClaimReward();

    public abstract void PurchasePass(PurchaseType purchaseType, PurchaseWeeklyPassType weeklyPassPurchaseType);

    public abstract void ClaimPass(PurchaseType purchaseType, PurchaseWeeklyPassType weeklyPassPurchaseType);

    public abstract void ClaimFree();

    public abstract void ClaimPurchased(PurchaseWeeklyPassType weeklyPassPurchaseType);

    public abstract long TimeRemaining();
}

[Serializable]
public enum PurchaseWeeklyPassType
{
    New,
    Renew
}