using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.MapBuilder
{
    /// <summary>
    /// Dữ liệu một map thủ thành: lưới ô trên mặt phẳng XZ, điểm spawn quái và điểm đích.
    /// <para>
    /// Spawn và Goal LUÔN tồn tại, luôn nằm trong lưới, luôn khác nhau và luôn là ô
    /// <see cref="TdCellType.Path"/>. Không có API nào xoá được hai ô này — chỉ dời được vị trí
    /// qua <see cref="SetSpawn"/> / <see cref="SetGoal"/>.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "TdMap_New", menuName = "Tower Defense/Map Data", order = 0)]
    public class TdMapData : ScriptableObject
    {
        #region Constants

        public const int MIN_SIZE = 2;
        public const int MAX_SIZE = 128;
        public const int DEFAULT_WIDTH = 20;
        public const int DEFAULT_HEIGHT = 20;
        public const float DEFAULT_CELL_SIZE = 2f;
        public const float MIN_CELL_SIZE = 0.1f;

        #endregion

        #region Fields

        [Header("Kích thước lưới")]
        [SerializeField] private int _width = DEFAULT_WIDTH;
        [SerializeField] private int _height = DEFAULT_HEIGHT;
        [SerializeField] private float _cellSize = DEFAULT_CELL_SIZE;

        [Tooltip("Góc (0,0) của lưới trong world space. Lưới trải theo +X và +Z.")]
        [SerializeField] private Vector3 _origin = Vector3.zero;

        [Header("Điểm cố định (đối diện nhau)")]
        [Tooltip("Trục đặt cặp Spawn/Goal. Dọc = cùng X, lệch Z (mặc định). Ngang = cùng Z, lệch X.")]
        [SerializeField] private TdMapAxis _axis = TdMapAxis.Vertical;

        [Tooltip("Khoảng cách giữa Spawn và Goal, tính bằng số ô.")]
        [SerializeField] private int _separation = DEFAULT_HEIGHT - 1;

        [SerializeField] private Vector2Int _spawn = new Vector2Int(DEFAULT_WIDTH / 2, 0);
        [SerializeField] private Vector2Int _goal = new Vector2Int(DEFAULT_WIDTH / 2, DEFAULT_HEIGHT - 1);

        [SerializeField, HideInInspector] private TdCellType[] _cells;

        [Tooltip("Thứ tự các ô đường theo đúng chiều người dựng map đã vẽ — quyết định chiều đi của quái.")]
        [SerializeField, HideInInspector] private List<Vector2Int> _route = new List<Vector2Int>();

        #endregion

        #region Public - Properties

        /// <summary>Số ô theo trục X.</summary>
        public int Width => _width;

        /// <summary>Số ô theo trục Z.</summary>
        public int Height => _height;

        /// <summary>Cạnh một ô, tính bằng world unit.</summary>
        public float CellSize => _cellSize;

        /// <summary>Góc (0,0) của lưới trong world space.</summary>
        public Vector3 Origin => _origin;

        /// <summary>Tổng số ô.</summary>
        public int CellCount => _width * _height;

        /// <summary>Ô quái xuất hiện. Luôn hợp lệ và luôn là <see cref="TdCellType.Path"/>.</summary>
        public Vector2Int Spawn => _spawn;

        /// <summary>Ô đích quái phải tới. Luôn hợp lệ và luôn là <see cref="TdCellType.Path"/>.</summary>
        public Vector2Int Goal => _goal;

        /// <summary>Trục đặt cặp Spawn/Goal đối diện nhau.</summary>
        public TdMapAxis Axis => _axis;

        /// <summary>Khoảng cách Spawn ↔ Goal, tính bằng số ô.</summary>
        public int Separation => _separation;

        /// <summary>Khoảng cách tối đa lưới hiện tại cho phép trên trục đang chọn.</summary>
        public int MaxSeparation => (_axis == TdMapAxis.Vertical ? _height : _width) - 1;

        /// <summary>
        /// Spawn/Goal có còn đối diện nhau đúng trục và đúng khoảng cách không.
        /// Sau khi dời tay hoặc đổi kích thước lưới thì có thể lệch.
        /// </summary>
        public bool IsOppositeLayout
        {
            get
            {
                if (_axis == TdMapAxis.Vertical)
                {
                    return _spawn.x == _goal.x && Mathf.Abs(_goal.y - _spawn.y) == _separation;
                }

                return _spawn.y == _goal.y && Mathf.Abs(_goal.x - _spawn.x) == _separation;
            }
        }

        /// <summary>Kích thước map trong world space (X = ngang, Z = dọc).</summary>
        public Vector2 WorldSize => new Vector2(_width * _cellSize, _height * _cellSize);

        /// <summary>
        /// Các ô đường theo đúng thứ tự đã vẽ. Đây là nguồn xác định CHIỀU ĐI của map —
        /// khác với BFS chỉ tìm được tuyến ngắn nhất mà không biết người dựng muốn đi lối nào.
        /// Rỗng nghĩa là chưa vẽ theo chuỗi, lúc đó phải rơi về BFS.
        /// </summary>
        public IReadOnlyList<Vector2Int> Route => _route;

        #endregion

        #region Public - Query

        /// <summary>Toạ độ ô có nằm trong lưới không.</summary>
        public bool IsInside(Vector2Int cell) => IsInside(cell.x, cell.y);

        /// <summary>Toạ độ ô có nằm trong lưới không.</summary>
        public bool IsInside(int x, int y) => x >= 0 && x < _width && y >= 0 && y < _height;

        /// <summary>Ô này có phải Spawn hoặc Goal không — hai ô không được phép ghi đè.</summary>
        public bool IsFixedCell(Vector2Int cell) => cell == _spawn || cell == _goal;

        /// <summary>Đọc loại ô. Ngoài lưới trả về <see cref="TdCellType.Blocked"/>.</summary>
        public TdCellType GetCell(Vector2Int cell) => GetCell(cell.x, cell.y);

        /// <summary>Đọc loại ô. Ngoài lưới trả về <see cref="TdCellType.Blocked"/>.</summary>
        public TdCellType GetCell(int x, int y)
        {
            if (!IsInside(x, y)) return TdCellType.Blocked;
            EnsureInitialized();
            return _cells[y * _width + x];
        }

        /// <summary>Quái có đi qua ô này được không.</summary>
        public bool IsWalkable(Vector2Int cell) => GetCell(cell) == TdCellType.Path;

        /// <summary>Người chơi có đặt trụ lên ô này được không.</summary>
        public bool IsBuildable(Vector2Int cell) => GetCell(cell) == TdCellType.Buildable;

        #endregion

        #region Public - Mutate

        /// <summary>
        /// Ghi loại cho một ô.
        /// Trả về <c>false</c> nếu ô nằm ngoài lưới, hoặc là Spawn/Goal và <paramref name="type"/>
        /// không phải <see cref="TdCellType.Path"/> — hai ô đó cố định nên không cho ghi đè.
        /// </summary>
        public bool SetCell(Vector2Int cell, TdCellType type)
        {
            if (!IsInside(cell)) return false;
            if (IsFixedCell(cell) && type != TdCellType.Path) return false;

            EnsureInitialized();
            _cells[cell.y * _width + cell.x] = type;
            return true;
        }

        /// <summary>
        /// Dời điểm spawn. Ô đích tự chuyển thành <see cref="TdCellType.Path"/>.
        /// Trả về <c>false</c> nếu ngoài lưới hoặc trùng Goal.
        /// </summary>
        public bool SetSpawn(Vector2Int cell)
        {
            if (!IsInside(cell) || cell == _goal) return false;
            _spawn = cell;
            ForcePath(cell);
            return true;
        }

        /// <summary>
        /// Dời điểm đích. Ô đích tự chuyển thành <see cref="TdCellType.Path"/>.
        /// Trả về <c>false</c> nếu ngoài lưới hoặc trùng Spawn.
        /// </summary>
        public bool SetGoal(Vector2Int cell)
        {
            if (!IsInside(cell) || cell == _spawn) return false;
            _goal = cell;
            ForcePath(cell);
            return true;
        }

        /// <summary>
        /// Đặt Spawn và Goal đối diện nhau trên <paramref name="axis"/>, cách nhau
        /// <paramref name="separation"/> ô, căn giữa lưới.
        /// <para>
        /// Trục Dọc = cùng X, lệch Z. Trục Ngang = cùng Z, lệch X.
        /// KHÔNG tự vẽ đường nối — nối đường là việc của người dựng map.
        /// </para>
        /// </summary>
        public void ApplyOppositeLayout(TdMapAxis axis, int separation)
        {
            EnsureInitialized();
            _axis = axis;

            int alongLength = axis == TdMapAxis.Vertical ? _height : _width;
            int crossLength = axis == TdMapAxis.Vertical ? _width : _height;

            _separation = Mathf.Clamp(separation, 1, alongLength - 1);

            int start = (alongLength - 1 - _separation) / 2; // căn giữa dọc theo trục
            int cross = crossLength / 2;                     // căn giữa theo trục vuông góc

            if (axis == TdMapAxis.Vertical)
            {
                _spawn = new Vector2Int(cross, start);
                _goal = new Vector2Int(cross, start + _separation);
            }
            else
            {
                _spawn = new Vector2Int(start, cross);
                _goal = new Vector2Int(start + _separation, cross);
            }

            ForcePath(_spawn);
            ForcePath(_goal);
        }

        /// <summary>Đặt lại bố trí đối diện theo trục và khoảng cách đang lưu trong asset.</summary>
        public void ReapplyOppositeLayout() => ApplyOppositeLayout(_axis, _separation);

        /// <summary>
        /// Ghi thêm một ô vào cuối chuỗi đường đã vẽ. Bỏ qua nếu trùng ngay ô cuối
        /// (rê chuột trong cùng một ô bắn nhiều sự kiện).
        /// </summary>
        public void AppendRoute(Vector2Int cell)
        {
            _route ??= new List<Vector2Int>();
            int count = _route.Count;

            if (count > 0 && _route[count - 1] == cell) return;

            // Rê ngược lại ô liền trước = người dùng đang kéo lui, nên thu tuyến về
            // thay vì ghi thêm. Không có bước này thì kéo tới kéo lui sẽ để lại một chuỗi
            // dích dắc và mũi tên chạy giật tới lui ở đó.
            if (count > 1 && _route[count - 2] == cell)
            {
                _route.RemoveAt(count - 1);
                return;
            }

            _route.Add(cell);
        }

        /// <summary>
        /// Dọn chuỗi đường: bỏ ô không còn là đường, bỏ ô trùng, và cắt chuỗi tại chỗ đầu tiên
        /// bị đứt mạch.
        /// <para>
        /// Bắt buộc gọi sau khi xoá ô — nếu không, mũi tên chỉ hướng vẫn chạy qua những ô
        /// vừa bị xoá vì chúng còn nằm trong chuỗi.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> nếu chuỗi có thay đổi.</returns>
        public bool SanitizeRoute()
        {
            if (_route == null || _route.Count == 0) return false;

            int keep = 0;
            for (int i = 0; i < _route.Count; i++)
            {
                Vector2Int cell = _route[i];
                if (GetCell(cell) != TdCellType.Path) break; // ô đã bị xoá -> cắt từ đây

                if (keep > 0)
                {
                    Vector2Int previous = _route[keep - 1];
                    int step = Mathf.Abs(cell.x - previous.x) + Mathf.Abs(cell.y - previous.y);
                    if (step == 0) continue; // trùng ô liền trước
                    if (step != 1) break;    // nhảy cóc -> đứt mạch
                }

                // Thu gọn đoạn đi lui A → B → A thành A. Rê chuột qua lại lúc vẽ sẽ để lại
                // những đoạn như vậy, và lính sẽ giật lùi đúng chỗ đó.
                if (keep > 1 && _route[keep - 2] == cell)
                {
                    keep--;
                    continue;
                }

                _route[keep++] = cell;
            }

            if (keep == _route.Count) return false;

            _route.RemoveRange(keep, _route.Count - keep);
            return true;
        }

        /// <summary>Xoá chuỗi đường đã vẽ (không đụng tới các ô trên lưới).</summary>
        public void ClearRoute() => _route?.Clear();

        /// <summary>Ghi đè chuỗi đường bằng một tuyến dựng sẵn.</summary>
        public void SetRoute(IReadOnlyList<Vector2Int> cells)
        {
            _route ??= new List<Vector2Int>();
            _route.Clear();
            if (cells == null) return;
            for (int i = 0; i < cells.Count; i++) _route.Add(cells[i]);
        }

        /// <summary>Ghi cùng một loại cho toàn bộ lưới (Spawn/Goal vẫn giữ nguyên là Path).</summary>
        public void FillAll(TdCellType type)
        {
            EnsureInitialized();
            for (int i = 0; i < _cells.Length; i++) _cells[i] = type;
            EnsureFixedCells();
        }

        /// <summary>
        /// Đổi kích thước lưới, giữ lại nội dung phần chồng lấn ở góc (0,0).
        /// Spawn/Goal bị kẹp lại vào trong lưới mới.
        /// </summary>
        public void Resize(int width, int height)
        {
            width = Mathf.Clamp(width, MIN_SIZE, MAX_SIZE);
            height = Mathf.Clamp(height, MIN_SIZE, MAX_SIZE);

            EnsureInitialized();
            if (width == _width && height == _height) return;

            var resized = new TdCellType[width * height];
            int copyW = Mathf.Min(width, _width);
            int copyH = Mathf.Min(height, _height);
            for (int y = 0; y < copyH; y++)
            {
                for (int x = 0; x < copyW; x++)
                {
                    resized[y * width + x] = _cells[y * _width + x];
                }
            }

            _cells = resized;
            _width = width;
            _height = height;
            EnsureFixedCells();
        }

        /// <summary>Đặt lại kích thước ô và gốc lưới.</summary>
        public void SetLayout(float cellSize, Vector3 origin)
        {
            _cellSize = Mathf.Max(MIN_CELL_SIZE, cellSize);
            _origin = origin;
        }

        #endregion

        #region Public - World Space

        /// <summary>Tâm của ô trong world space.</summary>
        public Vector3 CellToWorld(Vector2Int cell)
        {
            return _origin + new Vector3((cell.x + 0.5f) * _cellSize, 0f, (cell.y + 0.5f) * _cellSize);
        }

        /// <summary>Góc thấp-trái của ô trong world space.</summary>
        public Vector3 CellCornerToWorld(int x, int y)
        {
            return _origin + new Vector3(x * _cellSize, 0f, y * _cellSize);
        }

        /// <summary>
        /// Điểm world về toạ độ ô. Không đảm bảo nằm trong lưới —
        /// kiểm tra bằng <see cref="IsInside(Vector2Int)"/>.
        /// </summary>
        public Vector2Int WorldToCell(Vector3 world)
        {
            Vector3 local = world - _origin;
            return new Vector2Int(
                Mathf.FloorToInt(local.x / _cellSize),
                Mathf.FloorToInt(local.z / _cellSize));
        }

        #endregion

        #region Private

        /// <summary>Cấp phát mảng ô nếu chưa có hoặc sai kích thước.</summary>
        private void EnsureInitialized()
        {
            if (_cells != null && _cells.Length == CellCount) return;

            var rebuilt = new TdCellType[CellCount];
            if (_cells != null)
            {
                int copy = Mathf.Min(_cells.Length, rebuilt.Length);
                for (int i = 0; i < copy; i++) rebuilt[i] = _cells[i];
            }

            _cells = rebuilt;
        }

        /// <summary>Kẹp Spawn/Goal vào trong lưới, tách chúng ra nếu trùng, và ép cả hai thành Path.</summary>
        private void EnsureFixedCells()
        {
            _spawn = ClampCell(_spawn);
            _goal = ClampCell(_goal);

            if (_spawn == _goal)
            {
                // Đẩy Goal sang ô kế bên để hai điểm không bao giờ chồng nhau.
                _goal = _goal.x < _width - 1
                    ? new Vector2Int(_goal.x + 1, _goal.y)
                    : new Vector2Int(Mathf.Max(0, _goal.x - 1), _goal.y);
            }

            ForcePath(_spawn);
            ForcePath(_goal);
        }

        private Vector2Int ClampCell(Vector2Int cell)
        {
            return new Vector2Int(
                Mathf.Clamp(cell.x, 0, _width - 1),
                Mathf.Clamp(cell.y, 0, _height - 1));
        }

        private void ForcePath(Vector2Int cell)
        {
            if (!IsInside(cell)) return;
            EnsureInitialized();
            _cells[cell.y * _width + cell.x] = TdCellType.Path;
        }

        #endregion

        #region Events

        private void Awake()
        {
            // Asset mới tinh: đặt sẵn Spawn/Goal đối diện nhau. Asset đã có dữ liệu thì bỏ qua.
            bool isFresh = _cells == null || _cells.Length == 0;
            EnsureInitialized();
            if (isFresh) ReapplyOppositeLayout();
        }

        private void OnValidate()
        {
            _width = Mathf.Clamp(_width, MIN_SIZE, MAX_SIZE);
            _height = Mathf.Clamp(_height, MIN_SIZE, MAX_SIZE);
            _cellSize = Mathf.Max(MIN_CELL_SIZE, _cellSize);
            _separation = Mathf.Clamp(_separation, 1, MaxSeparation);

            EnsureInitialized();
            EnsureFixedCells();
        }

        #endregion
    }
}
