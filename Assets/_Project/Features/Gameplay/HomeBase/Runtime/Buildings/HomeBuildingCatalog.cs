using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Danh mục toàn bộ nhà có trong game. Thêm nhà mới là kéo thêm một asset vào đây,
    /// không phải sửa code khởi tạo.
    /// </summary>
    [CreateAssetMenu(fileName = "HomeBuildingCatalog", menuName = "Tower Defense/Buildings/Catalog", order = 0)]
    public class HomeBuildingCatalog : ScriptableObject
    {
        #region Fields

        [Title("Các nhà")]
        [SerializeField] private HomeBuildingDefinition[] _definitions = new HomeBuildingDefinition[0];

        #endregion

        #region Public - Properties

        public HomeBuildingDefinition[] Definitions => _definitions;

        #endregion

        #region Public

        /// <summary>Tìm cấu hình của một loại nhà. Trả <c>null</c> nếu danh mục chưa có.</summary>
        public HomeBuildingDefinition Find(HomeBuildingType type)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (_definitions[i] != null && _definitions[i].Type == type) return _definitions[i];
            }

            return null;
        }

        #endregion
    }
}
