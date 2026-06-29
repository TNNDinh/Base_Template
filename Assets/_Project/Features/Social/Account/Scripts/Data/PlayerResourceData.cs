using System;
using System.Collections.Generic;
using Ezg.Core.Utils;
using Ezg.Package.Factory;
using Ezg.Feature.Shared.Systems;

[Serializable]
public class PlayerResourceData : DataBase
{
    /// <summary>
    ///     Thời gian cuối cùng cộng năng lượng
    /// </summary>
    public long LastTimeRestoreEnergy;

    public long StartTimeInfinityEnergy;
    public long EndTimeInfinityEnergy;

    public long StartTimeInfinityX2Gold;
    public long EndTimeInfinityX2Gold;

    /// <summary>
    ///     Level của các equipment đã được nâng
    /// </summary>
    public Dictionary<EnumBase.EquipmentTypes, int> EquipmentLevel;

    public Dictionary<EnumBase.MoneyTypes, long> Monies { get; set; }

    /// <summary>
    ///     Danh sách ID các hero đã sở hữu
    /// </summary>
    public List<int> HeroIdOwned { get; set; }

    /// <summary>
    ///     Danh sách tài nguyên dùng để upgrade hero
    /// </summary>
    public Dictionary<int, Dictionary<EnumBase.MoneyTypes, long>> HeroMoneyUpgrade { get; set; } = new();

    /// <summary>
    ///     Cấp độ hero
    /// </summary>
    public Dictionary<int, int> HeroesLevel { get; set; } = new();

    public Dictionary<int, int> HeroStarRankLevel { get; set; } = new();

    public Dictionary<int, int> HeroShardLevel { get; set; } = new();

    public Dictionary<int, bool> IsWaitToUpStarRank { get; set; } = new();

    /// <summary>
    ///     Danh sách id skill sở hữu
    /// </summary>
    public List<int> SkillIdOwned { get; set; }

    /// <summary>
    ///     Danh sách id passive sở hữu, bao gồm
    /// </summary>
    public List<int> PassiveIdOwned { get; set; }

    /// <summary>
    ///     Danh sách các item đang có
    /// </summary>
    public List<Resource> Items { get; set; }

    /// <summary>
    ///     Danh sách các item đã được trang bị
    /// </summary>
    public Dictionary<EnumBase.EquipmentTypes, Guid> ItemEquipments { get; set; } = new();

    /// <summary>
    ///     Danh sách Jewel đã trang bị
    /// </summary>
    public Dictionary<EnumBase.EquipmentTypes, Dictionary<int, Guid>> JewelEquipments { get; set; }

    /// <summary>
    ///     Danh sách ID các pet đã sở hữu
    /// </summary>
    public List<int> PetIdOwned { get; set; }
}