namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Nhà chính. Cấp của nó là chốt chặn của cả căn cứ: nhà khác khai
    /// <c>require_kind=BuildingLevel, require_id=TownHall</c> trong CSV là bị nó khoá.
    /// <para>
    /// Không cài thêm gì ngoài phần chung, và đó là đúng — nhà chính không tự làm gì cả,
    /// nó chỉ là con số mà điều kiện của nhà khác soi vào.
    /// </para>
    /// </summary>
    public class TownHallBuilding : HomeBuildingBase
    {
        #region Initialize

        public TownHallBuilding(TownHallDefinition definition, IHomeBuildingState state, IUpgradePayment payment)
            : base(definition, state, payment) { }

        #endregion
    }
}
