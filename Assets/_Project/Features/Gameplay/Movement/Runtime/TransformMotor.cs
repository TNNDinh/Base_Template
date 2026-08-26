using System;
using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Áp di chuyển thẳng lên một <see cref="Transform"/>.
    /// Dùng cho quái không cần vật lý; cần va chạm thì viết bản cài đặt Rigidbody riêng.
    /// </summary>
    public class TransformMotor : IMovementMotor
    {
        #region Constants

        private const float MIN_DIRECTION_SQR = 0.000001f;

        #endregion

        #region Fields

        private readonly Transform _transform;
        private readonly float _turnSpeedDegrees;

        #endregion

        #region Initialize

        /// <param name="turnSpeedDegrees">Tốc độ xoay, độ trên giây. Số không hoặc âm = xoay tức thì.</param>
        public TransformMotor(Transform transform, float turnSpeedDegrees)
        {
            _transform = transform != null ? transform : throw new ArgumentNullException(nameof(transform));
            _turnSpeedDegrees = turnSpeedDegrees;
        }

        #endregion

        #region Public

        public Vector3 Position => _transform.position;

        public void MoveTo(Vector3 position) => _transform.position = position;

        public void LookAlong(Vector3 direction, float deltaTime)
        {
            // Chỉ xoay quanh trục đứng: quái đi trên mặt phẳng, ngẩng lên cúi xuống trông sẽ sai.
            direction.y = 0f;
            if (direction.sqrMagnitude < MIN_DIRECTION_SQR) return;

            var target = Quaternion.LookRotation(direction, Vector3.up);
            if (_turnSpeedDegrees <= 0f)
            {
                _transform.rotation = target;
                return;
            }

            _transform.rotation = Quaternion.RotateTowards(
                _transform.rotation, target, _turnSpeedDegrees * deltaTime);
        }

        #endregion
    }
}
