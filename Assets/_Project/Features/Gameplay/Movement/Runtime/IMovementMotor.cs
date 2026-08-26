using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Áp vị trí và hướng đã tính được lên một thực thể trong scene.
    /// <para>
    /// Tách khỏi phần tính toán để đổi cách di chuyển (Transform, Rigidbody, NavMeshAgent,
    /// hay ECS) chỉ cần thêm class mới, không đụng vào logic bám tuyến.
    /// </para>
    /// </summary>
    public interface IMovementMotor
    {
        /// <summary>Vị trí hiện tại của thực thể.</summary>
        Vector3 Position { get; }

        /// <summary>Đặt thực thể tới vị trí mới.</summary>
        void MoveTo(Vector3 position);

        /// <summary>
        /// Xoay thực thể về hướng đang đi.
        /// Bỏ qua nếu <paramref name="direction"/> gần bằng không.
        /// </summary>
        void LookAlong(Vector3 direction, float deltaTime);
    }
}
