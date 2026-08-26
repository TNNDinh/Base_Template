using System.Collections.Generic;
using Ezg.Feature.Movement;
using UnityEngine;

namespace Ezg.Feature.MapBuilder
{
    /// <summary>
    /// Adapter đưa tuyến của một <see cref="TdMapData"/> ra dưới dạng <see cref="IPathProvider"/>.
    /// <para>
    /// Chiều phụ thuộc cố ý đi một hướng: MapBuilder biết tới abstraction của tầng Movement,
    /// còn tầng Movement không biết gì về map thủ thành.
    /// </para>
    /// <para>
    /// Waypoint được chốt một lần lúc khởi tạo — quái đang chạy giữa chừng mà map bị sửa
    /// thì nó vẫn đi theo tuyến cũ thay vì nhảy vị trí. Muốn cập nhật thì gọi <see cref="Refresh"/>.
    /// </para>
    /// </summary>
    public class TdMapPathProvider : IPathProvider
    {
        #region Fields

        private readonly TdMapData _map;
        private readonly List<Vector2Int> _cellBuffer = new List<Vector2Int>();
        private readonly List<Vector3> _waypoints = new List<Vector3>();

        #endregion

        #region Initialize

        public TdMapPathProvider(TdMapData map)
        {
            _map = map;
            Refresh();
        }

        #endregion

        #region Public - Properties

        public int WaypointCount => _waypoints.Count;

        /// <summary>Ô lưới tương ứng từng waypoint, cùng thứ tự.</summary>
        public IReadOnlyList<Vector2Int> Cells => _cellBuffer;

        #endregion

        #region Public

        public Vector3 GetWaypoint(int index) => _waypoints[index];

        /// <summary>Đọc lại tuyến từ map. Gọi sau khi map bị sửa.</summary>
        public void Refresh()
        {
            _waypoints.Clear();
            if (_map == null) return;

            if (!TdMapPath.TryGetEffectiveRoute(_map, _cellBuffer)) return;

            for (int i = 0; i < _cellBuffer.Count; i++)
            {
                _waypoints.Add(_map.CellToWorld(_cellBuffer[i]));
            }
        }

        #endregion
    }
}
