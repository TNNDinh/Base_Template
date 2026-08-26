using Ezg.Core.Utils;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Trừ tài nguyên thật qua <see cref="PlayerResource"/>.
    /// </summary>
    public class PlayerResourceUpgradePayment : IUpgradePayment
    {
        #region Constants

        /// <summary>Nguồn ghi vào tracking, để báo cáo doanh thu biết tiền đi đâu.</summary>
        private const string SPEND_SOURCE = "home_base_upgrade";

        #endregion

        #region Public

        public bool CanAfford(UpgradeCost cost, out string reason)
        {
            reason = string.Empty;
            if (cost.IsFree) return true;

            if (PlayerResource.IsEnough(cost.Resource, cost.Amount)) return true;

            // TODO: [HomeBase] - đổi sang khoá localize khi có bảng text cho phần nhà cửa.
            reason = $"Không đủ {cost.Resource} (cần {cost.Amount})";
            return false;
        }

        public bool TrySpend(UpgradeCost cost)
        {
            if (cost.IsFree) return true;
            if (!PlayerResource.IsEnough(cost.Resource, cost.Amount)) return false;

            PlayerResource.RemoveCurrency(cost.Resource, cost.Amount, SPEND_SOURCE);
            return true;
        }

        #endregion
    }
}
