using System;
using Ezg.Feature.MapBuilder;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Một loại vật thể được rải ngẫu nhiên trong vành farm.
    /// Bán kính chiếm chỗ mặc định đo thẳng từ mesh nên thêm prefab mới không phải căn tay.
    /// </summary>
    [Serializable]
    public class TdHomeScatterEntry
    {
        #region Fields

        [SerializeField] private GameObject _prefab;

        [Tooltip("Càng lớn càng hay được chọn so với các mục khác.")]
        [SerializeField, Min(0f)] private float _weight = 1f;

        [Tooltip("Bán kính chiếm chỗ. Để 0 thì tự đo từ mesh.")]
        [SerializeField, Min(0f)] private float _radius;

        [Tooltip("Nới thêm quanh bán kính để vật thể không dính sát nhau.")]
        [SerializeField, Min(0f)] private float _padding = 1.5f;

        [SerializeField] private bool _randomYaw = true;

        [Tooltip("Chỉ xoay theo bội số 90 độ. Bật cho luống rau, ruộng — thứ phải thẳng hàng.")]
        [SerializeField] private bool _snapYawToQuarter;

        [SerializeField] private Vector2 _scaleRange = Vector2.one;

        #endregion

        #region Public - Properties

        public GameObject Prefab => _prefab;

        public float Weight => _weight;

        public bool IsUsable => _prefab != null && _weight > 0f;

        #endregion

        #region Public

        /// <summary>Bán kính chiếm chỗ đã tính cả phần nới, theo tỉ lệ <paramref name="scale"/>.</summary>
        public float ResolveRadius(float scale)
        {
            float baseRadius = _radius > 0f ? _radius : PrefabBoundsUtility.GetPlanarRadius(_prefab);
            return baseRadius * scale + _padding;
        }

        /// <summary>Tỉ lệ ngẫu nhiên trong khoảng đã đặt.</summary>
        public float PickScale(double random01)
        {
            float min = Mathf.Max(0.01f, Mathf.Min(_scaleRange.x, _scaleRange.y));
            float max = Mathf.Max(min, Mathf.Max(_scaleRange.x, _scaleRange.y));
            return Mathf.Lerp(min, max, (float)random01);
        }

        /// <summary>Góc xoay ngẫu nhiên theo cấu hình.</summary>
        public float PickYaw(double random01)
        {
            if (!_randomYaw) return 0f;
            if (_snapYawToQuarter) return Mathf.Floor((float)random01 * 4f) * 90f;
            return (float)random01 * 360f;
        }

        #endregion
    }
}
