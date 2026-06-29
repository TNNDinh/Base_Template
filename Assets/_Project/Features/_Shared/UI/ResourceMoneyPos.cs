using UnityEngine;
using UnityEngine.Serialization;
using Ezg.Feature.Shared;
using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

public class ResourceMoneyPos
{
    public int resType;
    [FormerlySerializedAs("resourceType")] public EnumBase.MoneyTypes moneyType;
    public Transform pos;
}
