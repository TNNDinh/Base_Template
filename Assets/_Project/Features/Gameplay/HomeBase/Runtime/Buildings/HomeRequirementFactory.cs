using System;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Dựng đối tượng điều kiện từ một dòng CSV.
    /// <para>
    /// ĐÂY LÀ CHỖ DUY NHẤT biết tới các class điều kiện cụ thể. Thêm loại điều kiện mới thì viết
    /// class cài <see cref="IBuildingRequirement"/>, thêm một mục vào <see cref="HomeRequirementKind"/>,
    /// rồi thêm đúng một nhánh ở đây — phần còn lại của hệ thống nhà không phải sửa gì.
    /// </para>
    /// </summary>
    public static class HomeRequirementFactory
    {
        #region Public

        /// <summary>Dựng điều kiện cho một dòng bảng cấp.</summary>
        /// <param name="context">Asset để báo lỗi trỏ đúng chỗ khi dòng CSV sai.</param>
        /// <returns><c>null</c> nếu dòng không đặt điều kiện gì.</returns>
        public static IBuildingRequirement Create(HomeLevelRow row, UnityEngine.Object context = null)
        {
            if (row == null || row.requireKind == HomeRequirementKind.None) return null;

            switch (row.requireKind)
            {
                case HomeRequirementKind.BuildingLevel:
                    return CreateBuildingLevel(row, context);

                default:
                    Debug.LogWarning($"[{nameof(HomeRequirementFactory)}] Chưa biết dựng điều kiện "
                                   + $"{row.requireKind}. Bỏ qua dòng cấp {row.level}.", context);
                    return null;
            }
        }

        #endregion

        #region Private

        private static IBuildingRequirement CreateBuildingLevel(HomeLevelRow row, UnityEngine.Object context)
        {
            if (!Enum.TryParse(row.requireId, out HomeBuildingType building))
            {
                Debug.LogWarning($"[{nameof(HomeRequirementFactory)}] require_id '{row.requireId}' ở cấp "
                               + $"{row.level} không khớp {nameof(HomeBuildingType)}. Bỏ qua điều kiện này.", context);
                return null;
            }

            return new BuildingLevelRequirement(building, row.requireValue);
        }

        #endregion
    }
}
