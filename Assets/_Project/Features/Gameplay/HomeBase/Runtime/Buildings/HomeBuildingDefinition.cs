using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Danh tính của một cái nhà: thuộc loại nào, tên gì, và trỏ tới bảng cấp của nó.
    /// <para>
    /// Số liệu cân bằng — giá, điều kiện, chỉ số — KHÔNG nằm ở đây mà nằm trong file CSV riêng của
    /// từng nhà. Asset này chỉ giữ những thứ CSV không chứa được: tham chiếu tài nguyên và cờ dựng cảnh.
    /// </para>
    /// </summary>
    public abstract class HomeBuildingDefinition : ScriptableObject
    {
        #region Fields

        [Title("Nhận dạng")]
        [SerializeField] private HomeBuildingType _type;

        [Tooltip("Để trống thì lấy luôn tên loại nhà.")]
        [SerializeField] private string _displayName;

        [Tooltip("Có sẵn từ đầu, không cần mở khoá. Nhà chính nên bật cái này.")]
        [SerializeField] private bool _unlockedFromStart;

        #endregion

        #region Public - Properties

        public HomeBuildingType Type => _type;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? _type.ToString() : _displayName;

        public bool UnlockedFromStart => _unlockedFromStart;

        /// <summary>Bảng cấp đọc từ CSV. Lớp con trỏ về đúng asset của mình.</summary>
        public abstract HomeLevelTableCollection Table { get; }

        /// <summary>Cấp cao nhất, lấy từ bảng CSV. Chưa gán bảng thì coi như chưa có cấp nào.</summary>
        public int MaxLevel => Table != null ? Table.MaxLevel : 0;

        #endregion
    }
}
