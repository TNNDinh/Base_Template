using System.Collections.Generic;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Tra cứu các nhà đang có trong căn cứ. Điều kiện mở khoá hỏi qua đây để biết
    /// nhà chính đang cấp mấy, mà không phải giữ tham chiếu thẳng tới nhà chính.
    /// </summary>
    public interface IHomeBuildingRegistry
    {
        /// <summary>Toàn bộ nhà, kể cả nhà chưa mở khoá.</summary>
        IReadOnlyList<IHomeBuilding> All { get; }

        /// <summary>Lấy một nhà theo loại.</summary>
        bool TryGet(HomeBuildingType type, out IHomeBuilding building);

        /// <summary>Cấp của một nhà. Trả 0 nếu chưa mở khoá hoặc không có nhà đó.</summary>
        int GetLevel(HomeBuildingType type);
    }
}
