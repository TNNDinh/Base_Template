using static Ezg.Core.Utils.EnumBase;

public struct OnCurrencyChangeEventData
{
    public MoneyTypes MoneyType;
    public long Delta;
    public bool ApplyImmediate;

    public OnCurrencyChangeEventData(MoneyTypes moneyType, long delta, bool applyImmediate = false)
    {
        MoneyType = moneyType;
        Delta = delta;
        ApplyImmediate = applyImmediate;
    }

    public static OnCurrencyChangeEventData DeltaOnly(MoneyTypes moneyType, long delta, bool applyImmediate = false)
    {
        return new OnCurrencyChangeEventData(moneyType, delta, applyImmediate);
    }
}