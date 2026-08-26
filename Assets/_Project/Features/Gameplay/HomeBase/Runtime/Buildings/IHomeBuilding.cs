using System;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Một cái nhà trong căn cứ: có cấp, có điều kiện mở khoá và điều kiện nâng cấp.
    /// <para>
    /// UI và hệ thống khác chỉ làm việc qua giao diện này. Nhà chính, nhà lính hay tường thành
    /// khác nhau ở phần riêng của chúng, còn phần chung — mở khoá và lên cấp — thì y hệt.
    /// </para>
    /// </summary>
    public interface IHomeBuilding
    {
        #region Properties

        HomeBuildingType Type { get; }

        /// <summary>Tên hiển thị.</summary>
        string DisplayName { get; }

        /// <summary>Cấp hiện tại. 0 nghĩa là chưa mở khoá.</summary>
        int Level { get; }

        /// <summary>Cấp cao nhất bảng cấu hình cho phép.</summary>
        int MaxLevel { get; }

        bool IsUnlocked { get; }

        bool IsMaxLevel { get; }

        #endregion

        #region Methods

        /// <summary>Đã đủ điều kiện mở khoá chưa.</summary>
        /// <param name="reason">Điều kiện đầu tiên còn thiếu, rỗng nếu đã đủ.</param>
        bool CanUnlock(out string reason);

        /// <summary>Đã đủ điều kiện lên cấp kế tiếp chưa.</summary>
        /// <param name="reason">Điều kiện đầu tiên còn thiếu, rỗng nếu đã đủ.</param>
        bool CanUpgrade(out string reason);

        /// <summary>Mở khoá nhà, đưa lên cấp 1.</summary>
        /// <returns><c>false</c> nếu chưa đủ điều kiện hoặc đã mở rồi.</returns>
        bool TryUnlock();

        /// <summary>Nâng lên cấp kế tiếp.</summary>
        /// <returns><c>false</c> nếu chưa đủ điều kiện hoặc đã kịch cấp.</returns>
        bool TryUpgrade();

        /// <summary>Điều kiện còn thiếu để mở khoá hoặc để lên cấp kế tiếp.</summary>
        string[] DescribeNextRequirements();

        #endregion

        #region Events

        /// <summary>Bắn khi cấp đổi, để UI vẽ lại.</summary>
        event Action<IHomeBuilding> Changed;

        #endregion
    }
}
