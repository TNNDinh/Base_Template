using System;
using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Một thực thể biết tự đi hết một tuyến.
    /// <para>
    /// Che đi việc bên dưới dùng waypoint tự tính hay dùng NavMeshAgent, để phía sinh lính
    /// không phải biết và không phải sửa khi thêm cách di chuyển mới.
    /// </para>
    /// </summary>
    public interface IPathWalker
    {
        /// <summary>GameObject mang thực thể này, để phía gọi thu hồi được.</summary>
        GameObject Owner { get; }

        /// <summary>Đã tới cuối tuyến chưa.</summary>
        bool HasArrived { get; }

        /// <summary>Đang đi hay đang dừng.</summary>
        bool IsRunning { get; }

        /// <summary>
        /// Có cần bên ngoài bơm nhịp qua <see cref="Tick"/> không.
        /// Bản NavMesh trả về <c>false</c> vì agent tự chạy theo vòng lặp vật lý của Unity.
        /// </summary>
        bool NeedsExternalTick { get; }

        /// <summary>Bắn đúng một lần khi tới cuối tuyến.</summary>
        event Action<IPathWalker> Arrived;

        /// <summary>Gắn tuyến và bắt đầu đi.</summary>
        void Begin(IPathProvider path);

        /// <summary>Dừng lại tại chỗ.</summary>
        void Stop();

        /// <summary>Tiến một bước thời gian. Bỏ qua nếu <see cref="NeedsExternalTick"/> là <c>false</c>.</summary>
        void Tick(float deltaTime);
    }
}
