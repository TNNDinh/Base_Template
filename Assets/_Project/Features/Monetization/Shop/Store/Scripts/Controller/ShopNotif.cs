using Ezg.Feature.RedDot;

public class ShopNotif : RedDotBadge
{
    public override void Execute()
    {
        base.Execute();
        IsActive = ShopService.CanClaimDailyDeal();
    }
}