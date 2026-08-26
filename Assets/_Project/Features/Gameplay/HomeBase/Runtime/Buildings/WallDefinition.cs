using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>Tường thành. Chỉ số duy nhất là máu tường, khai trong <c>HomeWall.csv</c>.</summary>
    [CreateAssetMenu(fileName = "WallDefinition", menuName = "Tower Defense/Buildings/Wall", order = 12)]
    public class WallDefinition : HomeBuildingDefinition
    {
        #region Fields

        [Title("Bảng cấp")]
        [Tooltip("Asset sinh ra từ HomeWall.csv.")]
        [SerializeField, Required] private HomeWallCollection _table;

        #endregion

        #region Public - Properties

        public override HomeLevelTableCollection Table => _table;

        /// <summary>Bảng đã ép sẵn kiểu, để đọc cột máu tường.</summary>
        public HomeWallCollection WallTable => _table;

        #endregion
    }
}
