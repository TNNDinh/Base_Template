using System;
using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Tuyến lấy thẳng từ một mảng điểm. Dùng cho tuyến đặt tay trong scene và cho test.
    /// </summary>
    public class ArrayPathProvider : IPathProvider
    {
        #region Fields

        private readonly Vector3[] _waypoints;

        #endregion

        #region Initialize

        public ArrayPathProvider(Vector3[] waypoints)
        {
            _waypoints = waypoints ?? throw new ArgumentNullException(nameof(waypoints));
        }

        #endregion

        #region Public

        public int WaypointCount => _waypoints.Length;

        public Vector3 GetWaypoint(int index) => _waypoints[index];

        #endregion
    }
}
