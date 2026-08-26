using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Nhà chính. Không có chỉ số riêng — giá trị của nó nằm ở chỗ cấp của nó được các nhà khác
    /// lấy làm điều kiện, khai trong cột <c>require_*</c> của CSV bên kia.
    /// </summary>
    [CreateAssetMenu(fileName = "TownHallDefinition", menuName = "Tower Defense/Buildings/Town Hall", order = 10)]
    public class TownHallDefinition : HomeBuildingDefinition
    {
        #region Fields

        [Title("Bảng cấp")]
        [Tooltip("Asset sinh ra từ HomeTownHall.csv.")]
        [SerializeField, Required] private HomeTownHallCollection _table;

        #endregion

        #region Public - Properties

        public override HomeLevelTableCollection Table => _table;

        #endregion
    }
}
