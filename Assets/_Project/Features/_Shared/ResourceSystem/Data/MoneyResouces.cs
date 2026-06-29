using System;
using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;

[Serializable]
public class MoneyResouces : Resource
{
    public EnumBase.MoneyTypes MoneyType => (EnumBase.MoneyTypes)resId;

    public override Resource Clone()
    {
        return (MoneyResouces)MemberwiseClone();
    }
}