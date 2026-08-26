using System;
using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Bọc một motor khác và cộng thêm một độ lệch cố định vào vị trí.
    /// <para>
    /// Dùng để nhấc thực thể lên khỏi mặt đất khi tuyến chạy ở cao độ 0. Viết theo kiểu decorator
    /// nên thêm được hành vi mà không phải sửa <see cref="TransformMotor"/>.
    /// </para>
    /// </summary>
    public class OffsetMovementMotor : IMovementMotor
    {
        #region Fields

        private readonly IMovementMotor _inner;
        private readonly Vector3 _offset;

        #endregion

        #region Initialize

        public OffsetMovementMotor(IMovementMotor inner, Vector3 offset)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _offset = offset;
        }

        #endregion

        #region Public

        /// <summary>Vị trí trên tuyến, tức đã trừ lại phần lệch.</summary>
        public Vector3 Position => _inner.Position - _offset;

        public void MoveTo(Vector3 position) => _inner.MoveTo(position + _offset);

        public void LookAlong(Vector3 direction, float deltaTime) => _inner.LookAlong(direction, deltaTime);

        #endregion
    }
}
