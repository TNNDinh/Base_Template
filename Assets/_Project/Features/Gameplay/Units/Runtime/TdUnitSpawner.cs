using System;
using System.Collections.Generic;
using Ezg.Feature.MapBuilder;
using Ezg.Feature.Movement;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Ezg.Feature.Units
{
    /// <summary>
    /// Sinh lính tại điểm Spawn của map và cho đi tới điểm Goal.
    /// <para>
    /// Chỉ lo việc tạo và thu hồi lính; đường đi do <see cref="TdMapPathProvider"/> cấp còn
    /// việc di chuyển do <see cref="PathMover"/> lo. Đổi cách đi hay cách dựng lính đều không
    /// phải sửa lớp này.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class TdUnitSpawner : MonoBehaviour
    {
        #region Constants

        private const string CONTAINER_NAME = "SpawnedUnits";
        private const string UNIT_NAME_PREFIX = "Unit_";
        private const float FALLBACK_RADIUS_RATIO = 0.3f;
        private const string COLOR_PROP_URP = "_BaseColor";
        private const string COLOR_PROP_BUILTIN = "_Color";

        #endregion

        #region Fields

        [Title("Nguồn")]
        [SerializeField, Required] private TdMapData _map;

        [Tooltip("Để trống thì dùng khối primitive — đủ để xem thử đường đi.")]
        [SerializeField] private GameObject _unitPrefab;

        [Title("Cách di chuyển")]
        [Tooltip("Auto = NavMesh khi đang Play, waypoint khi xem trước ngoài Play Mode.")]
        [SerializeField] private TdMovementBackend _backend = TdMovementBackend.Auto;

        [Title("Thông số lính")]
        [SerializeField, MinValue(0f)] private float _speed = 4f;
        [SerializeField, MinValue(0f)] private float _turnSpeed = 720f;

        [Tooltip("Nhấc lính lên khỏi mặt đường, vì tuyến chạy ở cao độ mặt lưới.")]
        [SerializeField, MinValue(0f)] private float _heightOffset = 0.6f;

        [SerializeField] private bool _despawnOnArrive = true;
        [SerializeField] private Color _fallbackColor = new Color(0.9f, 0.35f, 0.85f);

        private Transform _container;
        private int _spawnCounter;
        private readonly List<IPathWalker> _alive = new List<IPathWalker>();

        #endregion

        #region Public - Properties

        /// <summary>Map đang dùng.</summary>
        public TdMapData Map => _map;

        /// <summary>Số lính đang sống.</summary>
        public int AliveCount => _alive.Count;

        #endregion

        #region Public - Events

        /// <summary>Bắn khi một lính tới đích, trước khi bị thu hồi.</summary>
        public event Action<IPathWalker> UnitArrived;

        #endregion

        #region Public

        /// <summary>Gán map khác lúc chạy.</summary>
        public void SetMap(TdMapData map) => _map = map;

        /// <summary>
        /// Sinh một lính ở điểm Spawn và cho đi tới Goal.
        /// Trả về <c>null</c> nếu chưa có map hoặc tuyến chưa thông.
        /// </summary>
        [Button(ButtonSizes.Large), PropertyOrder(-1)]
        public IPathWalker Spawn()
        {
            if (_map == null) return null;

            var path = new TdMapPathProvider(_map);
            if (path.WaypointCount < 2) return null;

            GameObject unit = CreateUnit();
            IPathWalker walker = AttachWalker(unit);
            walker.Begin(path);
            walker.Arrived += OnUnitArrived;

            _alive.Add(walker);
            return walker;
        }

        /// <summary>
        /// Gắn cách di chuyển cho lính. Đây là chỗ duy nhất biết tới các bản cài đặt cụ thể;
        /// thêm kiểu di chuyển mới chỉ cần sửa hàm này.
        /// </summary>
        private IPathWalker AttachWalker(GameObject unit)
        {
            if (ResolveBackend() == TdMovementBackend.NavMesh)
            {
                var agent = unit.GetComponent<NavMeshAgent>();
                if (agent == null) agent = unit.AddComponent<NavMeshAgent>();
                agent.speed = _speed;
                agent.angularSpeed = _turnSpeed;
                agent.baseOffset = _heightOffset;

                var navWalker = unit.GetComponent<NavMeshPathWalker>();
                return navWalker != null ? navWalker : unit.AddComponent<NavMeshPathWalker>();
            }

            var mover = unit.GetComponent<PathMover>();
            if (mover == null) mover = unit.AddComponent<PathMover>();

            IMovementMotor motor = new OffsetMovementMotor(
                new TransformMotor(unit.transform, _turnSpeed),
                Vector3.up * _heightOffset);

            mover.Initialize(path: null, motor: motor, speedSource: new ConstantMoveSpeed(_speed));
            return mover;
        }

        /// <summary>Auto thì theo trạng thái Play, vì NavMeshAgent không chạy ngoài Play Mode.</summary>
        private TdMovementBackend ResolveBackend()
        {
            if (_backend != TdMovementBackend.Auto) return _backend;
            return Application.isPlaying ? TdMovementBackend.NavMesh : TdMovementBackend.Waypoint;
        }

        /// <summary>Thu hồi toàn bộ lính đang sống.</summary>
        [Button]
        public void DespawnAll()
        {
            for (int i = _alive.Count - 1; i >= 0; i--) Despawn(_alive[i]);
            _alive.Clear();

            // Dọn nốt phần sót lại nếu ai đó xoá tay trong Hierarchy.
            Transform container = transform.Find(CONTAINER_NAME);
            if (container != null) DestroyObject(container.gameObject);
            _container = null;
            _spawnCounter = 0;
        }

        #endregion

        #region Private

        private GameObject CreateUnit()
        {
            Transform container = GetOrCreateContainer();
            GameObject unit;

            if (_unitPrefab != null)
            {
                unit = Instantiate(_unitPrefab, container);
            }
            else
            {
                unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                unit.transform.SetParent(container, false);

                float radius = _map.CellSize * FALLBACK_RADIUS_RATIO;
                unit.transform.localScale = new Vector3(radius, radius, radius);
                ApplyFallbackColor(unit);

                // Khối primitive kèm collider; lính xem thử không cần va chạm.
                Collider collider = unit.GetComponent<Collider>();
                if (collider != null) DestroyObject(collider);
            }

            unit.name = UNIT_NAME_PREFIX + _spawnCounter++;
            return unit;
        }

        private Transform GetOrCreateContainer()
        {
            if (_container != null) return _container;

            Transform existing = transform.Find(CONTAINER_NAME);
            if (existing != null)
            {
                _container = existing;
                return _container;
            }

            var go = new GameObject(CONTAINER_NAME);
            go.transform.SetParent(transform, false);
            _container = go.transform;
            return _container;
        }

        private void ApplyFallbackColor(GameObject unit)
        {
            var renderer = unit.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(COLOR_PROP_URP, _fallbackColor);
            block.SetColor(COLOR_PROP_BUILTIN, _fallbackColor);
            renderer.SetPropertyBlock(block);
        }

        private void OnUnitArrived(IPathWalker walker)
        {
            UnitArrived?.Invoke(walker);
            _alive.Remove(walker);
            if (_despawnOnArrive) Despawn(walker);
        }

        private void Despawn(IPathWalker walker)
        {
            if (walker == null || walker.Owner == null) return;

            walker.Arrived -= OnUnitArrived;
            DestroyObject(walker.Owner);
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        #endregion
    }
}
