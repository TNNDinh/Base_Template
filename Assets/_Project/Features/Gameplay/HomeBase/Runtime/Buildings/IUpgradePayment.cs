using Ezg.Core.Utils;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Nơi trừ tài nguyên khi nâng cấp. Nhà chỉ cầm cái giá đi hỏi, còn tiền nằm ở ví nào
    /// là chuyện của bản cài đặt.
    /// </summary>
    public interface IUpgradePayment
    {
        /// <summary>Đủ tài nguyên chưa.</summary>
        /// <param name="reason">Lý do không đủ, rỗng nếu đủ.</param>
        bool CanAfford(UpgradeCost cost, out string reason);

        /// <summary>Trừ tài nguyên. Chỉ gọi khi mọi điều kiện khác đã thoả.</summary>
        /// <returns><c>false</c> nếu không trừ được, khi đó không được lên cấp.</returns>
        bool TrySpend(UpgradeCost cost);
    }

    /// <summary>
    /// Ví miễn phí: nâng cấp không tốn gì. Dùng để test luật lên cấp tách khỏi hệ tài nguyên.
    /// </summary>
    public class FreeUpgradePayment : IUpgradePayment
    {
        #region Public

        public bool CanAfford(UpgradeCost cost, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public bool TrySpend(UpgradeCost cost) => true;

        #endregion
    }
}
