using System;
using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Bám tuyến theo kiểu đi thẳng từ waypoint này sang waypoint kế tiếp.
    /// <para>
    /// Mỗi tick tiêu một "ngân sách quãng đường" bằng tốc độ nhân delta time, và tiêu hết
    /// qua nhiều waypoint nếu cần — nhờ vậy tốc độ cao hay ô lưới nhỏ cũng không bị bỏ sót
    /// waypoint như cách chỉ nhích một đoạn mỗi frame.
    /// </para>
    /// </summary>
    public class WaypointPathFollower : IPathFollower
    {
        #region Constants

        /// <summary>Ngưỡng coi như đã chạm waypoint, tránh kẹt do sai số dấu phẩy động.</summary>
        private const float ARRIVE_EPSILON = 0.0001f;

        #endregion

        #region Fields

        private readonly IMoveSpeedSource _speedSource;

        private IPathProvider _path;
        private int _targetIndex;
        private Vector3 _position;
        private Vector3 _forward;
        private float _distanceTravelled;
        private bool _hasReachedEnd;

        #endregion

        #region Initialize

        /// <param name="speedSource">Nguồn tốc độ, đọc lại mỗi tick.</param>
        public WaypointPathFollower(IMoveSpeedSource speedSource)
        {
            _speedSource = speedSource ?? throw new ArgumentNullException(nameof(speedSource));
        }

        #endregion

        #region Public - Properties

        public bool HasReachedEnd => _hasReachedEnd;

        public int TargetWaypointIndex => _targetIndex;

        public Vector3 Position => _position;

        public Vector3 Forward => _forward;

        public float DistanceTravelled => _distanceTravelled;

        #endregion

        #region Public - Events

        public event Action ReachedEnd;

        #endregion

        #region Public

        public void Bind(IPathProvider path)
        {
            _path = path;
            Restart();
        }

        public void Restart()
        {
            _targetIndex = 0;
            _distanceTravelled = 0f;
            _forward = Vector3.zero;
            _hasReachedEnd = false;

            if (!HasUsablePath())
            {
                _position = Vector3.zero;
                return;
            }

            _position = _path.GetWaypoint(0);
            _targetIndex = 1;
            _forward = DirectionTo(_path.GetWaypoint(1));
        }

        public void Tick(float deltaTime)
        {
            if (_hasReachedEnd || deltaTime <= 0f || !HasUsablePath()) return;

            float budget = Mathf.Max(0f, _speedSource.CurrentSpeed) * deltaTime;
            if (budget <= 0f) return;

            ConsumeBudget(budget);
        }

        #endregion

        #region Private

        /// <summary>Đi hết ngân sách quãng đường của tick này, có thể vượt qua nhiều waypoint.</summary>
        private void ConsumeBudget(float budget)
        {
            while (budget > 0f && _targetIndex < _path.WaypointCount)
            {
                Vector3 target = _path.GetWaypoint(_targetIndex);
                Vector3 offset = target - _position;
                float distance = offset.magnitude;

                if (distance > ARRIVE_EPSILON) _forward = offset / distance;

                if (distance > budget)
                {
                    _position += _forward * budget;
                    _distanceTravelled += budget;
                    return;
                }

                // Chạm waypoint: nuốt trọn đoạn còn lại rồi nhắm tới waypoint kế.
                _position = target;
                _distanceTravelled += distance;
                budget -= distance;
                _targetIndex++;
            }

            if (_targetIndex >= _path.WaypointCount) CompleteRoute();
        }

        private void CompleteRoute()
        {
            if (_hasReachedEnd) return;

            _hasReachedEnd = true;
            _position = _path.GetWaypoint(_path.WaypointCount - 1);
            ReachedEnd?.Invoke();
        }

        private bool HasUsablePath() => _path != null && _path.WaypointCount >= 2;

        private Vector3 DirectionTo(Vector3 target)
        {
            Vector3 offset = target - _position;
            return offset.sqrMagnitude > ARRIVE_EPSILON * ARRIVE_EPSILON ? offset.normalized : Vector3.zero;
        }

        #endregion
    }
}
