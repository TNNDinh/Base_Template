// Compat: các EventName const còn được code dùng-chung tham chiếu sau khi feature gameplay bị gỡ.
// Giữ lại dưới dạng string const để không phá vỡ listener/emitter còn sống.
public static partial class EventName
{
    public const string CheatHideUIUA = "CheatHideUIUA";
    public const string CheatShowUIUA = "CheatShowUIUA";
    public const string OnUnlimitedEnergy = "OnUnlimitedEnergy";
    public const string SetDataScreenItemInfo = "SetDataScreenItemInfo";
    public const string ClearAllTut = "ClearAllTut";
    public const string SelectItem = "SelectItem";
    public const string BuildUpGoalDone = "BuildUpGoalDone";
    public const string UpLevelInData = "UpLevelInData";
    public const string OnUpLevel = "OnUpLevel";
    public const string EndDiscountGemRaw = "EndDiscountGemRaw";
    public const string GetDailyDealReward = "GetDailyDealReward";
    public const string DeductEnergy = "DeductEnergy";
    public const string CloseScreenGameplay = "CloseScreenGameplay";
}
