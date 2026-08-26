using System;
using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Tính vị trí dọc theo một tuyến theo thời gian.
    /// <para>
    /// Thuần C#, không đụng tới Transform hay MonoBehaviour, nên chạy test được mà không cần scene.
    /// Phần áp kết quả lên thực thể là việc của <see cref="IMovementMotor"/>.
    /// </para>
    /// </summary>
    public interface IPathFollower
    {
        /// <summary>Đã tới waypoint cuối chưa.</summary>
        bool HasReachedEnd { get; }

        /// <summary>Waypoint đang hướng tới.</summary>
        int TargetWaypointIndex { get; }

        /// <summary>Vị trí hiện tại trên tuyến.</summary>
        Vector3 Position { get; }

        /// <summary>Hướng đang đi, đã chuẩn hoá. Bằng <see cref="Vector3.zero"/> khi chưa có tuyến.</summary>
        Vector3 Forward { get; }

        /// <summary>Quãng đường đã đi kể từ đầu tuyến.</summary>
        float DistanceTravelled { get; }

        /// <summary>Bắn đúng một lần khi vừa tới waypoint cuối.</summary>
        event Action ReachedEnd;

        /// <summary>Gắn tuyến mới và đưa về waypoint đầu.</summary>
        void Bind(IPathProvider path);

        /// <summary>Quay lại đầu tuyến hiện tại.</summary>
        void Restart();

        /// <summary>Tiến thêm <paramref name="deltaTime"/> giây.</summary>
        void Tick(float deltaTime);
    }
}
