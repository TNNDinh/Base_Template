using Ezg.Core.Utils;

namespace Ezg.Feature.HomeBase
{
    /// <summary>Giá của một lần nâng cấp: trừ loại tài nguyên nào, bao nhiêu.</summary>
    public readonly struct UpgradeCost
    {
        #region Public - Properties

        public EnumBase.MoneyTypes Resource { get; }

        public long Amount { get; }

        /// <summary>Không tốn gì. Bảng giá thiếu dòng cũng rơi vào trường hợp này.</summary>
        public bool IsFree => Amount <= 0;

        #endregion

        #region Initialize

        public UpgradeCost(EnumBase.MoneyTypes resource, long amount)
        {
            Resource = resource;
            Amount = amount;
        }

        #endregion

        #region Public

        public override string ToString() => IsFree ? "mi\u1ec5n ph\u00ed" : $"{Amount} {Resource}";

        #endregion
    }
}
