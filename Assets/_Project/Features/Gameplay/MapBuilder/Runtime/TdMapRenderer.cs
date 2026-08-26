using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.MapBuilder
{
    /// <summary>
    /// Dựng hình 3D của một <see cref="TdMapData"/> trong scene.
    /// Chưa gán prefab thì fallback sang khối primitive tô màu — vẫn nhìn được map ngay.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class TdMapRenderer : MonoBehaviour
    {
        #region Constants

        private const string CONTAINER_NAME = "GeneratedMap";
        private const string COLOR_PROP_URP = "_BaseColor";
        private const string COLOR_PROP_BUILTIN = "_Color";
        private const string GROUND_NAME = "Ground";
        private const float MARKER_HEIGHT_RATIO = 0.75f;

        #endregion

        #region Fields

        [Title("Dữ liệu")]
        [SerializeField, Required] private TdMapData _map;

        [Title("Bộ mảnh đường")]
        [Tooltip("Gán bộ prefab đường thật. Để trống thì rơi về khối primitive như cũ.")]
        [SerializeField] private TdRoadTileSet _roadTileSet;

        [Title("Prefab ô (để trống = dùng khối primitive)")]
        [SerializeField] private GameObject _pathPrefab;
        [SerializeField] private GameObject _buildablePrefab;
        [SerializeField] private GameObject _blockedPrefab;

        [Title("Prefab điểm cố định")]
        [SerializeField] private GameObject _spawnMarkerPrefab;
        [SerializeField] private GameObject _goalMarkerPrefab;

        [Title("Nền cỏ")]
        [Tooltip("Dựng một mặt cỏ liền phủ hết map. Khe hở giữa các tile đường sẽ lộ ra mặt cỏ này.")]
        [SerializeField] private bool _drawGround = true;
        [SerializeField] private GameObject _groundPrefab;

        [Title("Tuỳ chọn")]
        [Tooltip("Dựng cả tile cho ô Đất xây. Tắt thì để mặt cỏ lo — nhẹ hơn nhiều.")]
        [SerializeField] private bool _renderBuildableCells;
        [SerializeField] private bool _renderBlockedCells = true;
        [SerializeField, Range(0.02f, 2f)] private float _tileThickness = 0.1f;

        [Tooltip("Phần cạnh ô mà tile chiếm. 1 = kín ô (hai làn sát nhau sẽ dính liền), "
               + "nhỏ hơn 1 = chừa đường chỉ cỏ mảnh quanh mỗi tile.")]
        [SerializeField, Range(0.5f, 1f)] private float _tileFillRatio = 0.88f;

        [Title("Màu fallback")]
        [SerializeField] private Color _pathColor = new Color(0.72f, 0.55f, 0.33f);
        [SerializeField] private Color _buildableColor = new Color(0.38f, 0.62f, 0.32f);
        [SerializeField] private Color _groundColor = new Color(0.33f, 0.58f, 0.29f);
        [SerializeField] private Color _blockedColor = new Color(0.28f, 0.28f, 0.32f);
        [SerializeField] private Color _spawnColor = new Color(0.25f, 0.6f, 1f);
        [SerializeField] private Color _goalColor = new Color(0.95f, 0.3f, 0.3f);

        private Transform _container;
        private MaterialPropertyBlock _propertyBlock;

        // Cache tuyến của một lần Rebuild: ô nào nằm trên tuyến, và cặp ô nào nối tiếp nhau.
        private readonly List<Vector2Int> _routeCache = new List<Vector2Int>();
        private readonly HashSet<int> _routeCells = new HashSet<int>();
        private readonly HashSet<long> _routeEdges = new HashSet<long>();

        #endregion

        #region Public - Properties

        /// <summary>Map đang được dựng.</summary>
        public TdMapData Map => _map;

        #endregion

        #region Public

        /// <summary>Gán map mới rồi dựng lại ngay.</summary>
        public void SetMap(TdMapData map)
        {
            _map = map;
            Rebuild();
        }

        /// <summary>Xoá hình cũ và dựng lại toàn bộ map.</summary>
        [Button(ButtonSizes.Large), PropertyOrder(-1)]
        public void Rebuild()
        {
            Clear();
            if (_map == null) return;

            BuildRouteCache();

            Transform container = GetOrCreateContainer();
            if (_drawGround) SpawnGround(container);

            bool useTileSet = _roadTileSet != null && _roadTileSet.IsUsable;

            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    TdCellType type = _map.GetCell(cell);
                    if (!ShouldRenderCell(type)) continue;

                    if (useTileSet && type == TdCellType.Path) SpawnRoadPiece(container, cell);
                    else SpawnTile(container, cell, type);
                }
            }

            SpawnMarker(container, _map.Spawn, _spawnMarkerPrefab, _spawnColor, "Spawn");
            SpawnMarker(container, _map.Goal, _goalMarkerPrefab, _goalColor, "Goal");
        }

        /// <summary>Xoá toàn bộ hình đã dựng.</summary>
        [Button]
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
        /// Tô lại màu cho các khối fallback đã dựng sẵn.
        /// <para>
        /// MaterialPropertyBlock KHÔNG được ghi vào file scene, nên mở lại scene là tile trắng hết.
        /// Hàm này chạy lại ở <c>OnEnable</c> để phục hồi màu mà không phải dựng lại toàn bộ.
        /// Tile dùng prefab thì bỏ qua — màu do material của prefab quyết định.
        /// </para>
        /// </summary>
        [Button]
        public void RefreshColors()
        {
            if (_map == null) return;

            Transform container = transform.Find(CONTAINER_NAME);
            if (container == null) return;

            foreach (Transform child in container)
            {
                if (child.name == GROUND_NAME)
                {
                    if (_groundPrefab == null) ApplyColor(child.gameObject, _groundColor);
                    continue;
                }

                if (child.name == "Spawn")
                {
                    if (_spawnMarkerPrefab == null) ApplyColor(child.gameObject, _spawnColor);
                    continue;
                }

                if (child.name == "Goal")
                {
                    if (_goalMarkerPrefab == null) ApplyColor(child.gameObject, _goalColor);
                    continue;
                }

                // Suy ra loại ô từ vị trí thay vì parse tên — vị trí là nguồn đáng tin hơn.
                TdCellType type = _map.GetCell(_map.WorldToCell(child.position));
                if (GetPrefab(type) != null) continue;

                ApplyColor(child.gameObject, GetColor(type));
            }
        }

        /// <summary>
        /// Lấy waypoint world-space cho quái chạy từ Spawn tới Goal.
        /// </summary>
        /// <returns><c>false</c> nếu map chưa gán hoặc đường bị đứt.</returns>
        public bool TryGetRoute(List<Vector3> result) => TdMapPath.TryBuildWorldRoute(_map, result);

        #endregion

        #region Initialize

        private void OnEnable() => RefreshColors();

        #endregion

        #region Private

        /// <summary>
        /// Đặt một mảnh đường thật cho ô, chọn hình dạng và góc xoay theo các hướng có đường kề bên.
        /// </summary>
        private void SpawnRoadPiece(Transform parent, Vector2Int cell)
        {
            TdRoadConnection connections = GetConnections(cell);
            _roadTileSet.Resolve(connections, out GameObject prefab, out float yRotation);
            if (prefab == null) return;

            GameObject piece = InstantiatePrefab(prefab, parent);
            piece.transform.SetPositionAndRotation(
                _map.CellToWorld(cell),
                Quaternion.Euler(0f, yRotation, 0f));
            piece.name = $"Road_{cell.x}_{cell.y}";
        }

        /// <summary>
        /// Những hướng mà ô này nối tiếp sang ô đường khác.
        /// <para>
        /// Bám theo tuyến mũi tên chứ không theo kề ô trên lưới. Bộ prefab đường chỉ rộng
        /// một ô, nên nếu tính theo kề ô thì một mảng đường 2 ô rộng sẽ ra toàn ngã ba ngã tư
        /// và ghép ra hình răng cưa. Theo tuyến thì mỗi ô có tối đa hai nối, cho ra đường đơn
        /// gấp khúc — đúng thứ bộ prefab này dựng được.
        /// </para>
        /// </summary>
        private TdRoadConnection GetConnections(Vector2Int cell)
        {
            bool onRoute = IsOnRoute(cell);

            TdRoadConnection result = TdRoadConnection.None;
            if (Connects(cell, Vector2Int.up, onRoute)) result |= TdRoadConnection.North;
            if (Connects(cell, Vector2Int.right, onRoute)) result |= TdRoadConnection.East;
            if (Connects(cell, Vector2Int.down, onRoute)) result |= TdRoadConnection.South;
            if (Connects(cell, Vector2Int.left, onRoute)) result |= TdRoadConnection.West;
            return result;
        }

        private bool Connects(Vector2Int cell, Vector2Int direction, bool onRoute)
        {
            Vector2Int neighbour = cell + direction;
            if (!IsRoad(neighbour)) return false;

            // Ô ngoài tuyến (nhánh vẽ lẻ) không có cạnh nào trên tuyến, đành theo kề ô.
            if (!onRoute || !IsOnRoute(neighbour)) return true;

            return _routeEdges.Contains(EdgeKey(cell, neighbour));
        }

        private bool IsRoad(Vector2Int cell) => _map.IsInside(cell) && _map.GetCell(cell) == TdCellType.Path;

        private bool ShouldRenderCell(TdCellType type)
        {
            switch (type)
            {
                case TdCellType.Buildable: return _renderBuildableCells;
                case TdCellType.Blocked: return _renderBlockedCells;
                default: return true;
            }
        }

        /// <summary>Mặt cỏ liền phủ hết map, nằm dưới các tile đường.</summary>
        private void SpawnGround(Transform parent)
        {
            Vector2 size = _map.WorldSize;
            Vector3 center = _map.Origin + new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);

            if (_roadTileSet != null && _roadTileSet.GroundTile != null)
            {
                SpawnGroundTiles(parent);
                return;
            }

            if (_groundPrefab != null)
            {
                GameObject prefabInstance = InstantiatePrefab(_groundPrefab, parent);
                prefabInstance.transform.position = center;
                prefabInstance.name = GROUND_NAME;
                return;
            }

            GameObject ground = CreatePrimitive(parent, GROUND_NAME);
            ground.transform.position = center + new Vector3(0f, _tileThickness * 0.25f, 0f);
            ground.transform.localScale = new Vector3(size.x, _tileThickness * 0.5f, size.y);
            ApplyColor(ground, _groundColor);
        }

        /// <summary>Rải prefab nền kín vùng map. Bước rải là kích thước thật của miếng nền, không liên quan bước lưới đường.</summary>
        private void SpawnGroundTiles(Transform parent)
        {
            Vector2 tile = _roadTileSet.GroundFootprint;
            if (tile.x <= 0f || tile.y <= 0f) return;

            Vector3 pivot = _roadTileSet.GroundPivotOffset;
            Vector2 size = _map.WorldSize;
            int columns = Mathf.CeilToInt(size.x / tile.x);
            int rows = Mathf.CeilToInt(size.y / tile.y);

            var root = new GameObject(GROUND_NAME);
            root.transform.SetParent(parent, false);

            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < columns; x++)
                {
                    GameObject piece = InstantiatePrefab(_roadTileSet.GroundTile, root.transform);
                    piece.transform.localScale *= _roadTileSet.GroundTileScale;

                    // Trừ pivot để TÂM MESH rơi đúng ô — miếng nền của Synty pivot nằm ở góc.
                    piece.transform.position = _map.Origin
                                             + new Vector3((x + 0.5f) * tile.x, 0f, (z + 0.5f) * tile.y)
                                             - new Vector3(pivot.x, 0f, pivot.z);
                    piece.name = $"Ground_{x}_{z}";
                }
            }
        }

        private Transform GetOrCreateContainer()
        {
            if (_container != null) return _container;

            var go = new GameObject(CONTAINER_NAME);
            go.transform.SetParent(transform, false);
            _container = go.transform;
            return _container;
        }

        private void SpawnTile(Transform parent, Vector2Int cell, TdCellType type)
        {
            GameObject prefab = GetPrefab(type);
            Vector3 position = _map.CellToWorld(cell);

            if (prefab != null)
            {
                GameObject instance = InstantiatePrefab(prefab, parent);
                instance.transform.position = position;
                instance.name = $"{type}_{cell.x}_{cell.y}";
                return;
            }

            CreateFallbackTile(parent, cell, type, $"{type}_{cell.x}_{cell.y}");
        }

        private void SpawnMarker(Transform parent, Vector2Int cell, GameObject prefab, Color color, string label)
        {
            Vector3 position = _map.CellToWorld(cell);

            if (prefab != null)
            {
                GameObject instance = InstantiatePrefab(prefab, parent);
                instance.transform.position = position;
                instance.name = label;
                return;
            }

            float size = _map.CellSize * _tileFillRatio;
            float height = _map.CellSize * MARKER_HEIGHT_RATIO;
            GameObject marker = CreatePrimitive(parent, label);
            marker.transform.position = position + new Vector3(0f, height * 0.5f + _tileThickness, 0f);
            marker.transform.localScale = new Vector3(size * 0.5f, height, size * 0.5f);
            ApplyColor(marker, color);
        }

        /// <summary>
        /// Dựng một tile khối hộp cho ô.
        /// <para>
        /// Mỗi ô mặc định chừa khe hở quanh mình. Với ô đường, khe về phía nào có ô đường nối tiếp
        /// cùng làn thì được nới ra sát mép ô để hai tile chạm nhau — đường dọc/ngang nhìn liền mạch.
        /// Khe về phía làn nằm song song thì giữ nguyên, lộ nền cỏ bên dưới.
        /// </para>
        /// </summary>
        private void CreateFallbackTile(Transform parent, Vector2Int cell, TdCellType type, string tileName)
        {
            float cellSize = _map.CellSize;
            float half = cellSize * _tileFillRatio * 0.5f;
            float margin = cellSize * 0.5f - half;

            float maxX = half + MergeExtent(cell, Vector2Int.right, type, margin);
            float minX = -half - MergeExtent(cell, Vector2Int.left, type, margin);
            float maxZ = half + MergeExtent(cell, Vector2Int.up, type, margin);
            float minZ = -half - MergeExtent(cell, Vector2Int.down, type, margin);

            GameObject tile = CreatePrimitive(parent, tileName);
            tile.transform.position = _map.CellToWorld(cell)
                                    + new Vector3((maxX + minX) * 0.5f, _tileThickness * 0.5f, (maxZ + minZ) * 0.5f);
            tile.transform.localScale = new Vector3(maxX - minX, _tileThickness, maxZ - minZ);
            ApplyColor(tile, GetColor(type));
        }

        /// <summary>Phần nới thêm về một hướng: bằng <paramref name="margin"/> nếu lấp khe, 0 nếu giữ khe.</summary>
        private float MergeExtent(Vector2Int cell, Vector2Int direction, TdCellType type, float margin)
        {
            if (type != TdCellType.Path) return 0f;

            Vector2Int neighbour = cell + direction;
            if (!_map.IsInside(neighbour) || _map.GetCell(neighbour) != TdCellType.Path) return 0f;

            // Bám theo tuyến mũi tên: chỉ nối liền hai ô đi nối tiếp nhau TRÊN TUYẾN.
            // Đường gấp lại chạy song song chính nó sẽ có hai ô kề nhau nhưng không nối tiếp
            // trên tuyến — đúng chỗ đó phải hở ra dải cỏ.
            if (IsOnRoute(cell) && IsOnRoute(neighbour))
            {
                return _routeEdges.Contains(EdgeKey(cell, neighbour)) ? margin : 0f;
            }

            // Ô nằm ngoài tuyến (nhánh cụt, ô vẽ lẻ) thì dùng luật hình học.
            return TdMapPath.ShouldMergeTiles(_map, cell, neighbour) ? margin : 0f;
        }

        /// <summary>Dựng lại cache tuyến cho lần Rebuild này.</summary>
        private void BuildRouteCache()
        {
            _routeCells.Clear();
            _routeEdges.Clear();
            if (!TdMapPath.TryGetEffectiveRoute(_map, _routeCache)) return;

            for (int i = 0; i < _routeCache.Count; i++)
            {
                _routeCells.Add(CellIndex(_routeCache[i]));
                if (i > 0) _routeEdges.Add(EdgeKey(_routeCache[i - 1], _routeCache[i]));
            }
        }

        private bool IsOnRoute(Vector2Int cell) => _routeCells.Contains(CellIndex(cell));

        private int CellIndex(Vector2Int cell) => cell.y * _map.Width + cell.x;

        /// <summary>Khoá của một cạnh, không phụ thuộc thứ tự hai đầu.</summary>
        private long EdgeKey(Vector2Int a, Vector2Int b)
        {
            int ia = CellIndex(a);
            int ib = CellIndex(b);
            if (ia > ib) (ia, ib) = (ib, ia);
            return (long)ia * _map.CellCount + ib;
        }

        private GameObject CreatePrimitive(Transform parent, string primitiveName)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = primitiveName;
            go.transform.SetParent(parent, false);
            return go;
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

        /// <summary>Tô màu qua MaterialPropertyBlock để không sinh material rác trong scene.</summary>
        private void ApplyColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            _propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(COLOR_PROP_URP, color);
            _propertyBlock.SetColor(COLOR_PROP_BUILTIN, color);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private GameObject GetPrefab(TdCellType type)
        {
            switch (type)
            {
                case TdCellType.Path: return _pathPrefab;
                case TdCellType.Buildable: return _buildablePrefab;
                default: return _blockedPrefab;
            }
        }

        private Color GetColor(TdCellType type)
        {
            switch (type)
            {
                case TdCellType.Path: return _pathColor;
                case TdCellType.Buildable: return _buildableColor;
                default: return _blockedColor;
            }
        }

        private static void DestroyObject(Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        #endregion
    }
}
