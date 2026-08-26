using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Danh tính một loại lính: tên, hình, prefab, và trỏ tới bảng cấp CSV của riêng nó.
    /// Chỉ số nằm hết trong file CSV, asset này chỉ giữ thứ CSV không chứa được.
    /// </summary>
    [CreateAssetMenu(fileName = "TroopDefinition", menuName = "Tower Defense/Buildings/Troop", order = 13)]
    public class TroopDefinition : ScriptableObject
    {
        #region Fields

        [Title("Nhận dạng")]
        [SerializeField] private TroopType _type;

        [Tooltip("Để trống thì lấy luôn tên loại lính.")]
        [SerializeField] private string _displayName;

        [SerializeField] private Sprite _icon;

        [SerializeField] private GameObject _prefab;

        [Title("Bảng cấp")]
        [Tooltip("Asset sinh ra từ file CSV của chính loại lính này, ví dụ TroopSwordsman.csv.")]
        [SerializeField, Required] private TroopTableCollection _table;

        #endregion

        #region Public - Properties

        public TroopType Type => _type;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? _type.ToString() : _displayName;

        public Sprite Icon => _icon;

        public GameObject Prefab => _prefab;

        public TroopTableCollection Table => _table;

        public int MaxLevel => _table != null ? _table.MaxLevel : 0;

        #endregion

        #region Public

        /// <summary>Chỉ số ở một cấp.</summary>
        public TroopStats StatsAt(int level) => _table != null ? _table.StatsAt(level) : default;

        /// <summary>Giá để lên tới một cấp.</summary>
        public UpgradeCost CostAt(int level) => _table != null ? _table.GetCost(level) : default;

        #endregion
    }
}
