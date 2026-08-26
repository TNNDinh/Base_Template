using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Nguồn cấp tuyến đường dưới dạng danh sách waypoint world-space.
    /// <para>
    /// Cố tình không biết gì về ai đi trên tuyến hay đi kiểu gì — nhờ vậy tuyến có thể đến từ
    /// map thủ thành, từ một mảng đặt tay, hay từ dữ liệu test.
    /// </para>
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>Số waypoint. Nhỏ hơn 2 nghĩa là không có tuyến để đi.</summary>
        int WaypointCount { get; }

        /// <summary>Waypoint thứ <paramref name="index"/> trong world space.</summary>
        Vector3 GetWaypoint(int index);
    }
}
