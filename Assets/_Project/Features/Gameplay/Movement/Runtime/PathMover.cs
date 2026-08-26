using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Lớp ghép nối duy nhất chạm tới Unity: mỗi frame bảo <see cref="IPathFollower"/> tính vị trí,
    /// rồi nhờ <see cref="IMovementMotor"/> áp lên thực thể.
    /// <para>
    /// Không tự tính toán gì, nên đổi cách bám tuyến hay cách di chuyển đều chỉ là chuyện
    /// tiêm bản cài đặt khác qua <see cref="Initialize"/> — thường do spawner gọi.
    /// </para>
    /// </summary>
    // ExecuteAlways để OnEnable/OnDisable chạy cả ngoài Play Mode — trình xem trước trong Editor
    // dựa vào sổ đăng ký này để biết có mover nào mà tick.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PathMover : MonoBehaviour, IPathWalker
    {
        #region Constants

        private const float DEFAULT_SPEED = 3f;
        private const float DEFAULT_TURN_SPEED = 720f;

        #endregion

        #region Fields

        [Title("Mặc định khi đặt tay trong scene")]
        [Tooltip("Spawner tiêm tuyến qua Initialize thì bỏ trống mục này.")]
        [SerializeField] private Transform[] _inspectorWaypoints;

        [SerializeField, MinValue(0f)] private float _speed = DEFAULT_SPEED;

        [Tooltip("Độ trên giây. Số không = xoay tức thì.")]
        [SerializeField, MinValue(0f)] private float _turnSpeed = DEFAULT_TURN_SPEED;

        [SerializeField] private bool _autoStart = true;

        private IPathFollower _follower;
        private IMovementMotor _motor;
        private IMoveSpeedSource _activeSpeed;
        private ConstantMoveSpeed _defaultSpeedSource;
        private bool _isRunning;

        /// <summary>Follower vừa báo tới đích; sẽ thông báo ra ngoài ở cuối Tick.</summary>
        private bool _pendingArrived;


        #endregion

        #region Public - Properties

        /// <summary>
        /// GameObject mang mover này, hoặc <c>null</c> nếu đã bị huỷ.
        /// Truy cập <c>gameObject</c> trên component đã huỷ sẽ ném lỗi, mà phía giữ tham chiếu
        /// (sổ đăng ký, danh sách lính của spawner) hoàn toàn có thể còn cầm nó.
        /// </summary>
        public GameObject Owner => this == null ? null : gameObject;

        /// <summary>Tự tính vị trí nên phải có người bơm nhịp từ ngoài.</summary>
        public bool NeedsExternalTick => true;

        /// <summary>Đã tới cuối tuyến chưa.</summary>
        public bool HasArrived => _follower != null && _follower.HasReachedEnd;

        /// <summary>Đang chạy hay đang dừng.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Đổi tốc độ lúc chạy. Chỉ dùng được với nguồn tốc độ mặc định.</summary>
        public float Speed
        {
            get => _defaultSpeedSource?.Speed ?? _speed;
            set
            {
                _speed = Mathf.Max(0f, value);
                if (_defaultSpeedSource != null) _defaultSpeedSource.Speed = _speed;
            }
        }

        #endregion

        #region Public - Events

        /// <summary>Bắn một lần khi thực thể chạm waypoint cuối.</summary>
        public event Action<IPathWalker> Arrived;

        #endregion

        #region Initialize

        /// <summary>
        /// Tiêm phụ thuộc và bắt đầu đi. Bỏ trống <paramref name="motor"/> hoặc
        /// <paramref name="speedSource"/> thì dùng bản mặc định dựng từ các field trong Inspector.
        /// </summary>
        public void Initialize(IPathProvider path, IMovementMotor motor = null, IMoveSpeedSource speedSource = null)
        {
            // Giữ lại phụ thuộc đã tiêm trước đó: spawner tiêm motor/tốc độ trước rồi mới
            // gọi Begin(path), nếu ghi đè bằng bản mặc định thì mất luôn offset chiều cao.
            _motor = motor ?? _motor ?? new TransformMotor(transform, _turnSpeed);
            _activeSpeed = speedSource ?? _activeSpeed ?? CreateDefaultSpeedSource();

            if (_follower != null) _follower.ReachedEnd -= OnReachedEnd;

            _follower = new WaypointPathFollower(_activeSpeed);
            _follower.ReachedEnd += OnReachedEnd;
            _follower.Bind(path);

            _pendingArrived = false;
            _isRunning = path != null && path.WaypointCount >= 2;
            if (_isRunning) _motor.MoveTo(_follower.Position);
        }

        private void Awake()
        {
            if (!_autoStart || _follower != null) return;

            IPathProvider path = BuildInspectorPath();
            if (path != null) Initialize(path);
        }

        private void OnEnable() => PathWalkerRegistry.Register(this);

        private void OnDisable() => PathWalkerRegistry.Unregister(this);

        private void OnDestroy()
        {
            if (_follower != null) _follower.ReachedEnd -= OnReachedEnd;
        }

        #endregion

        #region Public

        /// <summary>Cho đi lại từ đầu tuyến hiện tại.</summary>
        [Button]
        public void Restart()
        {
            if (_follower == null) return;

            _follower.Restart();
            _motor.MoveTo(_follower.Position);
            _pendingArrived = false;
            _isRunning = true;
        }

        /// <summary>Tạm dừng hoặc chạy tiếp.</summary>
        public void SetRunning(bool running) => _isRunning = running && !HasArrived;

        /// <summary>Gắn tuyến và bắt đầu đi, dùng motor và tốc độ mặc định.</summary>
        public void Begin(IPathProvider path) => Initialize(path);

        /// <summary>Dừng tại chỗ.</summary>
        public void Stop() => _isRunning = false;

        /// <summary>
        /// Tiến một bước thời gian. Play mode gọi từ <c>Update</c>; ngoài play mode thì
        /// trình xem trước trong Editor gọi thẳng vào đây.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isRunning || _follower == null || deltaTime <= 0f) return;

            _follower.Tick(deltaTime);

            // Follower có thể vừa chạm đích. KHÔNG bắn sự kiện ở đây: người nghe thường huỷ
            // luôn GameObject, và hai dòng dưới sẽ chạm vào Transform đã bị huỷ.
            _motor.MoveTo(_follower.Position);
            _motor.LookAlong(_follower.Forward, deltaTime);

            if (!_pendingArrived) return;

            _pendingArrived = false;
            _isRunning = false;
            Arrived?.Invoke(this);
        }

        #endregion

        #region Private

        private void Update()
        {
            // Edit mode không chạy Update; lúc đó trình xem trước trong Editor gọi Tick trực tiếp.
            if (Application.isPlaying) Tick(Time.deltaTime);
        }

        private IMoveSpeedSource CreateDefaultSpeedSource()
        {
            _defaultSpeedSource = new ConstantMoveSpeed(_speed);
            return _defaultSpeedSource;
        }

        private IPathProvider BuildInspectorPath()
        {
            if (_inspectorWaypoints == null || _inspectorWaypoints.Length < 2) return null;

            var points = new Vector3[_inspectorWaypoints.Length];
            for (int i = 0; i < _inspectorWaypoints.Length; i++)
            {
                if (_inspectorWaypoints[i] == null) return null;
                points[i] = _inspectorWaypoints[i].position;
            }

            return new ArrayPathProvider(points);
        }

        private void OnReachedEnd() => _pendingArrived = true;

        #endregion
    }
}
