using System;
using Ezg.Feature.MapBuilder;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Một prefab được đặt tay ở vị trí cố định trong vùng trung tâm.
    /// Trung tâm là bộ mặt của căn cứ nên xếp tay, không rải ngẫu nhiên.
    /// </summary>
    [Serializable]
    public class TdHomePlacement
    {
        #region Fields

        [SerializeField] private GameObject _prefab;

        [Tooltip("Lệch so với tâm căn cứ, trên mặt phẳng ngang.")]
        [SerializeField] private Vector2 _offset;

        [SerializeField] private float _yaw;

        [SerializeField, Min(0.01f)] private float _scale = 1f;

        #endregion

        #region Public - Properties

        public GameObject Prefab => _prefab;

        public Vector3 LocalPosition => new Vector3(_offset.x, 0f, _offset.y);

        public float Yaw => _yaw;

        public float Scale => _scale;

        /// <summary>Bán kính chiếm chỗ, đo thẳng từ mesh và nhân theo tỉ lệ đang đặt.</summary>
        public float PlanarRadius => PrefabBoundsUtility.GetPlanarRadius(_prefab) * _scale;

        #endregion
    }
}
