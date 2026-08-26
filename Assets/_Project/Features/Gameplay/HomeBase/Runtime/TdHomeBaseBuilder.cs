using System.Collections.Generic;
using Ezg.Feature.MapBuilder;
using Sirenix.OdinInspector;
using UnityEngine;
using SysRandom = System.Random;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Dựng căn cứ nhà theo ba vành đồng tâm: lõi ở giữa, vành farm bao quanh, ngoài cùng là
    /// tường thành có một cổng. Cổng chính là điểm cuối của map thủ thành — ngoài cổng là map.
    /// <para>
    /// Mọi thứ dựng trong không gian cục bộ của chính object này, nên dời object là cả căn cứ
    /// dời theo — khác với <see cref="TdMapRenderer"/> vốn bám theo gốc lưới ghi trong dữ liệu map.
    /// </para>
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class TdHomeBaseBuilder : MonoBehaviour, ICameraFocusArea
    {
        #region Constants

        private const string CONTAINER_NAME = "GeneratedHomeBase";
        private const string GROUP_GROUND = "Ground";
        private const string GROUP_WALL = "Wall";
        private const string GROUP_AVENUE = "Avenue";
        private const string GROUP_CENTER = "Center";
        private const string GROUP_FARM = "Farm";

        /// <summary>Bỏ qua đoạn tường ngắn hơn ngần này lần một mảnh — dựng vào chỉ thừa ra.</summary>
        private const float MIN_RUN_RATIO = 0.5f;

        private const int CIRCLE_GIZMO_SEGMENTS = 48;

        #endregion

        #region Fields

        [Title("Cấu hình")]
        [SerializeField, Required] private TdHomeBaseLayout _layout;

        [Title("Nối với map thủ thành")]
        [Tooltip("Map mà cổng thành phải khớp vào điểm cuối. Để trống thì căn cứ đứng độc lập.")]
        [SerializeField] private TdMapData _map;

        [Tooltip("Đẩy cổng ra xa điểm cuối một đoạn, tính theo hướng ra ngoài cổng.")]
        [SerializeField] private float _gateGapFromGoal;

        private Transform _container;

        // Chỗ đã bị chiếm trong một lần dựng: tâm trên mặt ngang và bán kính tương ứng.
        private readonly List<Vector3> _occupiedCenters = new List<Vector3>();
        private readonly List<float> _occupiedRadii = new List<float>();

        #endregion

        #region Public - Properties

        /// <summary>Cấu hình đang dùng.</summary>
        public TdHomeBaseLayout Layout => _layout;

        /// <summary>Map đang nối vào cổng.</summary>
        public TdMapData Map => _map;

        /// <summary>Vị trí cổng so với tâm căn cứ.</summary>
        public Vector3 GateLocalPosition => _layout != null ? _layout.GateLocalPosition : Vector3.zero;

        /// <summary>Vị trí cổng trong world.</summary>
        public Vector3 GateWorldPosition => transform.TransformPoint(GateLocalPosition);

        /// <summary>Tâm vùng camera được phép lia — chính là tâm căn cứ.</summary>
        public Vector3 AreaCenter => transform.position;

        /// <summary>Nửa cạnh vùng camera được phép lia — lấy theo tường thành.</summary>
        public float AreaHalfExtent => _layout != null ? _layout.WallHalfExtent : 0f;

        #endregion

        #region Public

        /// <summary>Gán cấu hình khác rồi dựng lại.</summary>
        public void SetLayout(TdHomeBaseLayout layout)
        {
            _layout = layout;
            Rebuild();
        }

        /// <summary>Gán map cần nối vào cổng.</summary>
        public void SetMap(TdMapData map) => _map = map;

        /// <summary>Xoá hình cũ và dựng lại toàn bộ căn cứ.</summary>
        [Button(ButtonSizes.Large), PropertyOrder(-2)]
        public void Rebuild()
        {
            Clear();
            if (_layout == null) return;

            // Prefab có thể vừa bị sửa; đo lại từ đầu cho chắc.
            PrefabBoundsUtility.ClearCache();
            _occupiedCenters.Clear();
            _occupiedRadii.Clear();

            Transform container = GetOrCreateContainer();
            BuildGround(container);
            BuildWall(container);
            BuildAvenue(container);
            BuildCenter(container);
            BuildFarm(container);
        }

        /// <summary>Xoá toàn bộ hình đã dựng.</summary>
        [Button, PropertyOrder(-2)]
        public void Clear()
        {
            Transform existing = transform.Find(CONTAINER_NAME);
            while (existing != null)
            {
                DestroyObject(existing.gameObject);
                existing = transform.Find(CONTAINER_NAME);
            }

            _container = null;
        }

        /// <summary>
        /// Dời cả căn cứ sao cho cổng thành trùng điểm cuối của map.
        /// <para>
        /// Dời căn cứ chứ không dời map: gốc lưới nằm trong asset map, đụng vào là scene dựng map
        /// lệch theo. Căn cứ chỉ là một object trong scene nên dời thoải mái.
        /// </para>
        /// </summary>
        /// <returns><c>false</c> nếu chưa gán map hoặc cấu hình.</returns>
        [Button(ButtonSizes.Large), PropertyOrder(-1)]
        public bool AlignGateToMapGoal()
        {
            if (_layout == null || _map == null) return false;

            WarnIfGateFacesWrongWay();

            Vector3 goal = _map.CellToWorld(_map.Goal);
            Vector3 outward = transform.rotation * _layout.GateSide.ToDirection();
            Vector3 gateOffset = transform.rotation * GateLocalPosition;

            transform.position = goal + outward * _gateGapFromGoal - gateOffset;
            return true;
        }

        #endregion

        #region Private - Ground

        /// <summary>Lát nền kín cả căn cứ, lan thêm một vành ra ngoài tường.</summary>
        private void BuildGround(Transform parent)
        {
            GameObject prefab = _layout.GroundPrefab;
            if (prefab == null) return;

            float scale = _layout.GroundScale;
            Vector2 tile = PrefabBoundsUtility.GetFootprint(prefab) * scale;
            if (tile.x <= 0f || tile.y <= 0f) return;

            float side = (_layout.WallHalfExtent + _layout.GroundPadding) * 2f;
            int columns = Mathf.CeilToInt(side / tile.x);
            int rows = Mathf.CeilToInt(side / tile.y);

            Transform group = CreateGroup(parent, GROUP_GROUND);
            float startX = -columns * tile.x * 0.5f;
            float startZ = -rows * tile.y * 0.5f;

            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var center = new Vector3(
                        startX + (x + 0.5f) * tile.x,
                        0f,
                        startZ + (z + 0.5f) * tile.y);

                    Spawn(prefab, group, center, 0f, scale, $"Ground_{x}_{z}");
                }
            }
        }

        #endregion

        #region Private - Wall

        /// <summary>Dựng tường vuông quanh căn cứ, chừa một lỗ mở ở cạnh có cổng.</summary>
        private void BuildWall(Transform parent)
        {
            GameObject segment = _layout.WallSegmentPrefab;
            if (segment == null) return;

            float segmentLength = LongestSide(segment) * _layout.WallScale;
            if (segmentLength <= 0f) return;

            Transform group = CreateGroup(parent, GROUP_WALL);
            bool runsAlongZ = PrefabBoundsUtility.RunsAlongZ(segment);
            float half = _layout.WallHalfExtent;

            for (int i = 0; i < 4; i++)
            {
                var side = (TdCompass)i;
                Vector3 outward = side.ToDirection();
                Vector3 tangent = side.ToTangent();
                Vector3 start = outward * half - tangent * half;
                Vector3 end = outward * half + tangent * half;

                if (side != _layout.GateSide)
                {
                    BuildWallRun(group, start, end, segment, segmentLength, runsAlongZ, $"Wall_{side}");
                    continue;
                }

                BuildGateSide(group, side, start, end, segment, segmentLength, runsAlongZ);
            }

            BuildWallPosts(group, half);
        }

        /// <summary>Cạnh có cổng: hai đoạn tường hai bên, cổng nằm chính giữa.</summary>
        private void BuildGateSide(Transform group, TdCompass side, Vector3 start, Vector3 end,
                                   GameObject segment, float segmentLength, bool runsAlongZ)
        {
            Vector3 tangent = side.ToTangent();
            Vector3 middle = (start + end) * 0.5f;
            float gateHalf = _layout.ResolveGateWidth() * 0.5f;

            BuildWallRun(group, start, middle - tangent * gateHalf, segment, segmentLength, runsAlongZ, $"Wall_{side}_A");
            BuildWallRun(group, middle + tangent * gateHalf, end, segment, segmentLength, runsAlongZ, $"Wall_{side}_B");

            GameObject gate = _layout.GatePrefab;
            if (gate == null) return;

            float yaw = YawAlong(tangent, PrefabBoundsUtility.RunsAlongZ(gate)) + _layout.WallYawOffset;
            Spawn(gate, group, middle, yaw, _layout.WallScale, "Gate");
        }

        /// <summary>Rải mảnh tường đều trên một đoạn thẳng, không co giãn prefab.</summary>
        private void BuildWallRun(Transform group, Vector3 start, Vector3 end, GameObject segment,
                                  float segmentLength, bool runsAlongZ, string prefix)
        {
            float length = Vector3.Distance(start, end);
            if (length < segmentLength * MIN_RUN_RATIO) return;

            Vector3 direction = (end - start) / length;
            int count = Mathf.Max(1, Mathf.RoundToInt(length / segmentLength));
            float step = length / count;
            float yaw = YawAlong(direction, runsAlongZ) + _layout.WallYawOffset;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = start + direction * (step * (i + 0.5f));
                Spawn(segment, group, position, yaw, _layout.WallScale, $"{prefix}_{i}");
            }
        }

        private void BuildWallPosts(Transform group, float half)
        {
            GameObject post = _layout.WallPostPrefab;
            if (post == null) return;

            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? -half : half;
                float z = (i & 2) == 0 ? -half : half;
                Spawn(post, group, new Vector3(x, 0f, z), 0f, _layout.WallScale, $"Post_{i}");
            }
        }

        #endregion

        #region Private - Avenue

        /// <summary>Đường thẳng nối từ cổng vào tới rìa lõi.</summary>
        private void BuildAvenue(Transform parent)
        {
            GameObject prefab = _layout.AvenuePrefab;
            if (prefab == null) return;

            float pitch = LongestSide(prefab);
            if (pitch <= 0f) return;

            TdCompass side = _layout.GateSide;
            Vector3 outward = side.ToDirection();
            float from = _layout.CenterRadius;
            float to = _layout.WallHalfExtent;
            float length = to - from;
            if (length <= 0f) return;

            // Giữ nguyên bước của prefab chứ không chia đều: co giãn bước là đường hở khe.
            // Neo từ cổng đi vào, thừa ra phía sân trong thì thôi — chỗ đó khuất dưới công trình.
            int count = Mathf.Max(1, Mathf.CeilToInt(length / pitch));

            // Mảnh đường thẳng gốc nối Bắc–Nam, nên cạnh Đông/Tây phải xoay ngang.
            float yaw = side.IsNorthSouth() ? 0f : 90f;
            Transform group = CreateGroup(parent, GROUP_AVENUE);

            for (int i = 0; i < count; i++)
            {
                Vector3 position = outward * (to - pitch * (i + 0.5f)) + Vector3.up * _layout.AvenueLift;
                Spawn(prefab, group, position, yaw, 1f, $"Avenue_{i}");
            }
        }

        #endregion

        #region Private - Content

        private void BuildCenter(Transform parent)
        {
            TdHomePlacement[] pieces = _layout.CenterPieces;
            if (pieces == null || pieces.Length == 0) return;

            Transform group = CreateGroup(parent, GROUP_CENTER);
            for (int i = 0; i < pieces.Length; i++)
            {
                TdHomePlacement piece = pieces[i];
                if (piece == null || piece.Prefab == null) continue;

                Vector3 position = piece.LocalPosition;
                Spawn(piece.Prefab, group, position, piece.Yaw, piece.Scale, $"{piece.Prefab.name}_{i}");
                Occupy(position, piece.PlanarRadius);
            }
        }

        /// <summary>
        /// Rải vành farm bằng cách bốc chỗ ngẫu nhiên rồi loại chỗ đụng nhau.
        /// Cùng seed thì ra cùng bố cục, nên dựng lại bao nhiêu lần cũng giống hệt.
        /// </summary>
        private void BuildFarm(Transform parent)
        {
            TdHomeScatterEntry[] entries = _layout.FarmScatter;
            if (entries == null || entries.Length == 0 || _layout.FarmItemCount <= 0) return;

            float totalWeight = 0f;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].IsUsable) totalWeight += entries[i].Weight;
            }

            if (totalWeight <= 0f) return;

            var random = new SysRandom(_layout.Seed);
            Transform group = CreateGroup(parent, GROUP_FARM);

            float inner = _layout.FarmInnerRadius;
            float outer = _layout.FarmOuterRadius;
            int placed = 0;

            for (int i = 0; i < _layout.FarmItemCount; i++)
            {
                TdHomeScatterEntry entry = PickEntry(entries, totalWeight, random.NextDouble());
                if (entry == null) continue;

                float scale = entry.PickScale(random.NextDouble());
                float radius = entry.ResolveRadius(scale);

                if (!TryFindSpot(random, inner, outer, radius, out Vector3 position)) continue;

                float yaw = entry.PickYaw(random.NextDouble());
                Spawn(entry.Prefab, group, position, yaw, scale, $"{entry.Prefab.name}_{placed}");
                Occupy(position, radius);
                placed++;
            }
        }

        private static TdHomeScatterEntry PickEntry(TdHomeScatterEntry[] entries, float totalWeight, double random01)
        {
            float target = (float)random01 * totalWeight;
            for (int i = 0; i < entries.Length; i++)
            {
                TdHomeScatterEntry entry = entries[i];
                if (entry == null || !entry.IsUsable) continue;

                target -= entry.Weight;
                if (target <= 0f) return entry;
            }

            return null;
        }

        /// <summary>Bốc thử vài chỗ trong vành farm cho tới khi được một chỗ trống.</summary>
        private bool TryFindSpot(SysRandom random, float inner, float outer, float radius, out Vector3 position)
        {
            for (int attempt = 0; attempt < _layout.PlacementAttempts; attempt++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;

                // Nội suy theo bình phương bán kính, nếu không vật thể sẽ dồn cục vào vành trong.
                float distance = Mathf.Sqrt(Mathf.Lerp(inner * inner, outer * outer, (float)random.NextDouble()));
                position = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);

                if (IsOnAvenue(position, radius)) continue;
                if (IsOccupied(position, radius)) continue;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        /// <summary>Chỗ này có lấn vào hành lang từ cổng vào lõi không.</summary>
        private bool IsOnAvenue(Vector3 position, float radius)
        {
            Vector3 outward = _layout.GateSide.ToDirection();
            if (Vector3.Dot(position, outward) <= 0f) return false;

            float lateral = Mathf.Abs(Vector3.Dot(position, _layout.GateSide.ToTangent()));
            return lateral < _layout.AvenueClearance * 0.5f + radius;
        }

        private bool IsOccupied(Vector3 position, float radius)
        {
            for (int i = 0; i < _occupiedCenters.Count; i++)
            {
                float minDistance = radius + _occupiedRadii[i];
                if ((position - _occupiedCenters[i]).sqrMagnitude < minDistance * minDistance) return true;
            }

            return false;
        }

        private void Occupy(Vector3 position, float radius)
        {
            _occupiedCenters.Add(position);
            _occupiedRadii.Add(radius);
        }

        #endregion

        #region Private - Helpers

        /// <summary>Góc để trục dài của prefab nằm dọc theo <paramref name="direction"/>.</summary>
        private static float YawAlong(Vector3 direction, bool prefabRunsAlongZ)
        {
            return prefabRunsAlongZ
                ? Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
                : Mathf.Atan2(-direction.z, direction.x) * Mathf.Rad2Deg;
        }

        private static float LongestSide(GameObject prefab)
        {
            Vector2 size = PrefabBoundsUtility.GetFootprint(prefab);
            return Mathf.Max(size.x, size.y);
        }

        /// <summary>
        /// Đặt một prefab sao cho TÂM MESH của nó rơi đúng <paramref name="localPosition"/>.
        /// <para>
        /// Bộ Synty có nhiều prefab đặt pivot ở mép chứ không ở giữa — mảnh hàng rào pivot nằm
        /// hẳn một đầu, luống rau pivot ở góc. Đặt thẳng theo pivot là cả hàng lệch đi nửa mảnh,
        /// nên phải trừ đi độ lệch, xoay và phóng đúng như bản thể sắp dựng.
        /// </para>
        /// </summary>
        private GameObject Spawn(GameObject prefab, Transform parent, Vector3 localPosition,
                                 float yaw, float scale, string objectName)
        {
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 pivot = rotation * (PrefabBoundsUtility.GetPivotOffset(prefab) * scale);

            GameObject instance = InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = localPosition - new Vector3(pivot.x, 0f, pivot.z);
            instance.transform.localRotation = rotation;
            if (!Mathf.Approximately(scale, 1f)) instance.transform.localScale *= scale;
            instance.name = objectName;
            return instance;
        }

        private Transform CreateGroup(Transform parent, string groupName)
        {
            Transform existing = parent.Find(groupName);
            if (existing != null) return existing;

            var go = new GameObject(groupName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private Transform GetOrCreateContainer()
        {
            if (_container != null) return _container;

            var go = new GameObject(CONTAINER_NAME);
            go.transform.SetParent(transform, false);
            _container = go.transform;
            return _container;
        }

        /// <summary>Cảnh báo khi cổng không quay về phía map — lính sẽ đi xuyên tường.</summary>
        private void WarnIfGateFacesWrongWay()
        {
            TdCompass expected = ExpectedGateSide();
            if (expected == _layout.GateSide) return;

            Debug.LogWarning(
                $"[{nameof(TdHomeBaseBuilder)}] Điểm cuối map nằm ở cạnh {expected} nhưng cổng đang mở ở cạnh "
                + $"{_layout.GateSide}. Đổi Gate Side trong {_layout.name} thành {expected} thì map mới khớp cổng.",
                this);
        }

        /// <summary>Cạnh mà cổng phải mở để quay ra map, suy từ vị trí điểm cuối trên lưới.</summary>
        private TdCompass ExpectedGateSide()
        {
            Vector2Int goal = _map.Goal;
            int fromLeft = goal.x;
            int fromRight = _map.Width - 1 - goal.x;
            int fromBottom = goal.y;
            int fromTop = _map.Height - 1 - goal.y;
            int nearest = Mathf.Min(Mathf.Min(fromLeft, fromRight), Mathf.Min(fromBottom, fromTop));

            // Map trải về phía đối diện cạnh chứa điểm cuối, nên cổng phải quay đúng về phía đó.
            if (nearest == fromTop) return TdCompass.South;
            if (nearest == fromBottom) return TdCompass.North;
            if (nearest == fromRight) return TdCompass.West;
            return TdCompass.East;
        }

        private GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (instance != null) return instance;
            }
#endif
            return Instantiate(prefab, parent);
        }

        private static void DestroyObject(Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        #endregion

        #region Events

        private void OnDrawGizmosSelected()
        {
            if (_layout == null) return;

            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            float side = _layout.WallHalfExtent * 2f;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(side, 0.1f, side));

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.7f);
            DrawCircleGizmo(_layout.CenterRadius);
            DrawCircleGizmo(_layout.FarmInnerRadius);
            DrawCircleGizmo(_layout.FarmOuterRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GateLocalPosition, _layout.ResolveGateWidth() * 0.5f);
        }

        private static void DrawCircleGizmo(float radius)
        {
            Vector3 previous = new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= CIRCLE_GIZMO_SEGMENTS; i++)
            {
                float angle = i / (float)CIRCLE_GIZMO_SEGMENTS * Mathf.PI * 2f;
                var current = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }

        #endregion
    }
}
