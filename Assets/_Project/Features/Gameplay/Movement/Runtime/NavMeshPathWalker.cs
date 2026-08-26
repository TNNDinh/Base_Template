using System;
using UnityEngine;
using UnityEngine.AI;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Đi hết tuyến bằng <see cref="NavMeshAgent"/>: đặt đích lần lượt qua từng waypoint,
    /// còn việc lái và né nhau để agent lo.
    /// <para>
    /// CHỈ chạy trong Play Mode — Unity không mô phỏng NavMeshAgent ngoài Play Mode. Muốn xem
    /// trước ngay trong Scene View thì dùng <see cref="PathMover"/>.
    /// </para>
    /// <para>
    /// Cần một NavMesh đã bake phủ tuyến, nếu không agent sẽ không đặt được lên mặt đường.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public class NavMeshPathWalker : MonoBehaviour, IPathWalker
    {
        #region Constants

        private const float DEFAULT_SPEED = 4f;

        /// <summary>Khoảng cách coi như đã tới waypoint để chuyển sang waypoint kế.</summary>
        private const float DEFAULT_ARRIVE_DISTANCE = 0.25f;

        /// <summary>Bán kính tối đa khi tìm điểm trên NavMesh gần vị trí spawn.</summary>
        private const float SAMPLE_RADIUS = 4f;

        #endregion

        #region Fields

        [SerializeField, Min(0f)] private float _speed = DEFAULT_SPEED;
        [SerializeField, Min(0.01f)] private float _arriveDistance = DEFAULT_ARRIVE_DISTANCE;

        private NavMeshAgent _agent;
        private IPathProvider _path;
        private int _targetIndex;
        private bool _isRunning;
        private bool _hasArrived;

        #endregion

        #region Public - Properties

        /// <summary>GameObject mang walker này, hoặc <c>null</c> nếu đã bị huỷ.</summary>
        public GameObject Owner => this == null ? null : gameObject;

        public bool HasArrived => _hasArrived;

        public bool IsRunning => _isRunning;

        /// <summary>Agent tự chạy theo vòng lặp của Unity nên không cần ai bơm nhịp.</summary>
        public bool NeedsExternalTick => false;

        #endregion

        #region Public - Events

        public event Action<IPathWalker> Arrived;

        #endregion

        #region Initialize

        private void Awake() => CacheAgent();

        private void OnEnable() => PathWalkerRegistry.Register(this);

        private void OnDisable() => PathWalkerRegistry.Unregister(this);

        #endregion

        #region Public

        public void Begin(IPathProvider path)
        {
            CacheAgent();

            _path = path;
            _targetIndex = 0;
            _hasArrived = false;
            _isRunning = false;

            if (_path == null || _path.WaypointCount < 2) return;

            if (!TryPlaceOnNavMesh(_path.GetWaypoint(0)))
            {
                Debug.LogWarning(
                    $"[{nameof(NavMeshPathWalker)}] Không đặt được lính lên NavMesh quanh {_path.GetWaypoint(0)}. "
                    + "Kiểm tra xem NavMesh đã bake phủ tuyến chưa.", this);
                return;
            }

            _agent.speed = _speed;
            _isRunning = true;
            SetTarget(1);
        }

        public void Stop()
        {
            _isRunning = false;
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
        }

        /// <summary>Không dùng: agent tự chạy. Giữ để thoả <see cref="IPathWalker"/>.</summary>
        public void Tick(float deltaTime) { }

        #endregion

        #region Private

        private void Update()
        {
            if (!_isRunning || _agent == null || !_agent.isOnNavMesh) return;
            if (_agent.pathPending) return;
            if (_agent.remainingDistance > _arriveDistance) return;

            AdvanceWaypoint();
        }

        private void AdvanceWaypoint()
        {
            int next = _targetIndex + 1;
            if (next < _path.WaypointCount)
            {
                SetTarget(next);
                return;
            }

            _isRunning = false;
            _hasArrived = true;
            _agent.isStopped = true;
            Arrived?.Invoke(this);
        }

        private void SetTarget(int index)
        {
            _targetIndex = index;
            _agent.isStopped = false;
            _agent.SetDestination(_path.GetWaypoint(index));
        }

        /// <summary>
        /// Đặt agent lên điểm gần <paramref name="position"/> nhất còn nằm trên NavMesh.
        /// Đặt thẳng vào một điểm ngoài NavMesh sẽ khiến agent không di chuyển được.
        /// </summary>
        private bool TryPlaceOnNavMesh(Vector3 position)
        {
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, SAMPLE_RADIUS, NavMesh.AllAreas)) return false;

            return _agent.Warp(hit.position);
        }

        private void CacheAgent()
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
        }

        #endregion
    }
}
