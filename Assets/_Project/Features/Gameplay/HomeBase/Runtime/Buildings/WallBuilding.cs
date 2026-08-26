namespace Ezg.Feature.HomeBase
{
    /// <summary>Phần phòng thủ của tường thành, thứ mà lượt chơi cần biết để xử thắng thua.</summary>
    public interface IWallDefense
    {
        /// <summary>Máu tối đa của tường ở cấp hiện tại.</summary>
        int MaxHealth { get; }

        /// <summary>Máu tường ở một cấp bất kỳ, để so trước khi nâng.</summary>
        int GetHealthAt(int level);
    }

    /// <summary>
    /// Tường thành. Nâng cấp là tăng máu tường, số lấy từ cột <c>health</c> trong <c>HomeWall.csv</c>.
    /// <para>
    /// Lượt chơi thắng khi tường còn đứng tới hết trận, nên nó chỉ cần đọc
    /// <see cref="IWallDefense"/> chứ không phải biết gì về hệ thống nhà cửa.
    /// </para>
    /// </summary>
    public class WallBuilding : HomeBuildingBase, IWallDefense
    {
        #region Fields

        private readonly WallDefinition _definition;

        #endregion

        #region Initialize

        public WallBuilding(WallDefinition definition, IHomeBuildingState state, IUpgradePayment payment)
            : base(definition, state, payment)
        {
            _definition = definition;
        }

        #endregion

        #region Public - Properties

        /// <summary>Tường chưa mở khoá thì coi như không có máu.</summary>
        public int MaxHealth => IsUnlocked ? GetHealthAt(Level) : 0;

        #endregion

        #region Public

        public int GetHealthAt(int level)
        {
            HomeWallCollection table = _definition.WallTable;
            HomeWallModel row = table != null ? table.Row(level) : null;
            return row?.health ?? 0;
        }

        #endregion
    }
}
