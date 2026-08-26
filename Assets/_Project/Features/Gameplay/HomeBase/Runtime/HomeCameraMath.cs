using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Phép tính hình học cho camera nhìn xuống: lùi bao xa thì lọt khung, và một điểm trên
    /// màn hình rơi vào chỗ nào trên mặt đất.
    /// <para>
    /// Tách riêng vì cả bộ dựng scene lẫn rig lúc chạy đều cần đúng công thức này — hai nơi
    /// tính lệch nhau là khung hình trong Editor một kiểu, lúc chơi một kiểu.
    /// </para>
    /// </summary>
    public static class HomeCameraMath
    {
        #region Public

        /// <summary>
        /// Khoảng lùi để một dải rộng <paramref name="span"/> lọt vừa khung hình theo cả hai chiều.
        /// </summary>
        public static float DistanceForSpan(float span, float fieldOfView, float aspect)
        {
            float halfVertical = fieldOfView * 0.5f * Mathf.Deg2Rad;
            float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * Mathf.Max(0.01f, aspect));
            return span * 0.5f / Mathf.Tan(Mathf.Min(halfVertical, halfHorizontal));
        }

        /// <summary>Vị trí camera khi ngắm <paramref name="focus"/> ở độ nghiêng và khoảng lùi cho trước.</summary>
        public static Vector3 PositionFor(Vector3 focus, float pitch, float distance)
        {
            return focus - Rotation(pitch) * Vector3.forward * distance;
        }

        /// <summary>Góc xoay của camera ở độ nghiêng cho trước.</summary>
        public static Quaternion Rotation(float pitch) => Quaternion.Euler(pitch, 0f, 0f);

        /// <summary>
        /// Điểm trên mặt phẳng ngang <paramref name="groundY"/> mà một điểm trên màn hình chiếu tới.
        /// <para>
        /// Tự dựng tia thay vì gọi <c>Camera.ScreenPointToRay</c> để tính được theo tư thế camera
        /// ĐÍCH chứ không phải tư thế đang trượt mượt tới đó — nếu không, vuốt sẽ trôi theo độ trễ.
        /// </para>
        /// </summary>
        /// <returns><c>false</c> nếu tia song song với mặt đất hoặc hướng ra xa nó.</returns>
        public static bool TryGroundPoint(Vector2 screenPoint, Vector2 screenSize, Vector3 cameraPosition,
                                          float pitch, float fieldOfView, float aspect, float groundY,
                                          out Vector3 groundPoint)
        {
            groundPoint = default;
            if (screenSize.x <= 0f || screenSize.y <= 0f) return false;

            float halfVertical = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            var normalized = new Vector2(
                screenPoint.x / screenSize.x * 2f - 1f,
                screenPoint.y / screenSize.y * 2f - 1f);

            var localDirection = new Vector3(
                normalized.x * halfVertical * aspect,
                normalized.y * halfVertical,
                1f);

            Vector3 direction = Rotation(pitch) * localDirection;
            if (Mathf.Abs(direction.y) < Mathf.Epsilon) return false;

            float travel = (groundY - cameraPosition.y) / direction.y;
            if (travel <= 0f) return false;

            groundPoint = cameraPosition + direction * travel;
            return true;
        }

        #endregion
    }
}
