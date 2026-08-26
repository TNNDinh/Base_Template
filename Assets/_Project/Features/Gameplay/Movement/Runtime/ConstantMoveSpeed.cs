using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Tốc độ cố định, đổi được lúc chạy.
    /// Bản cài đặt đơn giản nhất của <see cref="IMoveSpeedSource"/>; hiệu ứng làm chậm hay
    /// tăng tốc sau này chỉ cần thêm bản cài đặt khác, không đụng tới người dùng interface.
    /// </summary>
    public class ConstantMoveSpeed : IMoveSpeedSource
    {
        #region Fields

        private float _speed;

        #endregion

        #region Initialize

        public ConstantMoveSpeed(float speed) => Speed = speed;

        #endregion

        #region Public - Properties

        /// <summary>Tốc độ đặt vào, luôn bị kẹp về không âm.</summary>
        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Max(0f, value);
        }

        public float CurrentSpeed => _speed;

        #endregion
    }
}
