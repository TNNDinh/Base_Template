using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ezg.Feature.MapBuilder
{
    /// <summary>
    /// Kết quả kiểm tra một <see cref="TdMapData"/>.
    /// </summary>
    public readonly struct TdMapValidation
    {
        /// <summary>Map đi được từ Spawn tới Goal hay không.</summary>
        public readonly bool IsValid;

        /// <summary>Mô tả ngắn để hiện lên UI editor.</summary>
        public readonly string Message;

        /// <summary>Số ô Path đang có trên lưới.</summary>
        public readonly int PathCellCount;

        /// <summary>Số ô Path không nối được tới Spawn (đường lạc).</summary>
        public readonly int OrphanCellCount;

        /// <summary>Độ dài tuyến đường ngắn nhất Spawn → Goal, tính theo số ô.</summary>
        public readonly int RouteLength;

        /// <summary>Số ô nằm trong chỗ đường bị dính rộng 2 ô (thiếu dải ngăn cách).</summary>
        public readonly int WideSpotCount;

        /// <summary>Map vừa thông đường vừa không có chỗ đường dính.</summary>
        public bool IsClean => IsValid && WideSpotCount == 0;

        public TdMapValidation(bool isValid, string message, int pathCellCount, int orphanCellCount,
            int routeLength, int wideSpotCount)
        {
            IsValid = isValid;
            Message = message;
            PathCellCount = pathCellCount;
            OrphanCellCount = orphanCellCount;
            RouteLength = routeLength;
            WideSpotCount = wideSpotCount;
        }
    }

    /// <summary>
    /// Tìm đường và kiểm tra tính hợp lệ của map thủ thành.
    /// BFS 4 hướng trên các ô <see cref="TdCellType.Path"/>.
    /// </summary>
    public static class TdMapPath
    {
        #region Constants

        private static readonly Vector2Int[] NEIGHBOURS =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
        };

        private const int UNVISITED = -1;

        /// <summary>Giá trị "không giả định ô nào là Path" cho <see cref="IsBlockAllPath"/>.</summary>
        private static readonly Vector2Int NO_ASSUMPTION = new Vector2Int(int.MinValue, int.MinValue);

        /// <summary>Bộ đệm dùng lại cho phép đếm chỗ đường dính, tránh cấp phát mỗi lần Validate.</summary>
        private static readonly List<Vector2Int> WideSpotScratch = new List<Vector2Int>();

        #endregion

        #region Public

        /// <summary>
        /// Dựng danh sách waypoint (tuyến ngắn nhất) từ Spawn tới Goal.
        /// Ghi kết quả vào <paramref name="result"/> để tránh cấp phát mới ở phía gọi.
        /// </summary>
        /// <returns><c>true</c> nếu có đường nối Spawn → Goal.</returns>
        public static bool TryBuildRoute(TdMapData map, List<Vector2Int> result)
        {
            result?.Clear();
            if (map == null || result == null) return false;

            int[] cameFrom = RunBfs(map, out int visitedCount);
            if (cameFrom == null) return false;

            int goalIndex = map.Goal.y * map.Width + map.Goal.x;
            if (cameFrom[goalIndex] == UNVISITED) return false;

            // Lần ngược từ Goal về Spawn rồi đảo lại.
            int cursor = goalIndex;
            while (cursor != UNVISITED)
            {
                result.Add(new Vector2Int(cursor % map.Width, cursor / map.Width));
                if (cursor == cameFrom[cursor]) break; // Spawn tự trỏ vào chính nó.
                cursor = cameFrom[cursor];
            }

            result.Reverse();
            return true;
        }

        /// <summary>
        /// Dựng waypoint dưới dạng world position (tâm ô), dùng để cho quái chạy.
        /// </summary>
        /// <returns><c>true</c> nếu có đường nối Spawn → Goal.</returns>
        public static bool TryBuildWorldRoute(TdMapData map, List<Vector3> result)
        {
            result?.Clear();
            if (map == null || result == null) return false;

            var cells = new List<Vector2Int>();
            if (!TryBuildRoute(map, cells)) return false;

            for (int i = 0; i < cells.Count; i++) result.Add(map.CellToWorld(cells[i]));
            return true;
        }

        /// <summary>
        /// Dựng tuyến gấp khúc chữ L nối hai ô: đi hết một trục rồi mới rẽ sang trục còn lại.
        /// <para>
        /// Kết quả luôn liền mạch theo 4 hướng (không có bước chéo), nên vừa dùng được cho
        /// công cụ vẽ đường thẳng, vừa dùng để lấp các ô bị nhảy khi rê chuột nhanh.
        /// </para>
        /// </summary>
        /// <param name="horizontalFirst">Đi theo trục X trước rồi mới tới Z.</param>
        public static void BuildElbow(Vector2Int from, Vector2Int to, bool horizontalFirst, List<Vector2Int> result)
        {
            if (result == null) return;
            result.Clear();

            int x = from.x;
            int y = from.y;
            result.Add(new Vector2Int(x, y));

            if (horizontalFirst)
            {
                while (x != to.x) { x += to.x > x ? 1 : -1; result.Add(new Vector2Int(x, y)); }
                while (y != to.y) { y += to.y > y ? 1 : -1; result.Add(new Vector2Int(x, y)); }
            }
            else
            {
                while (y != to.y) { y += to.y > y ? 1 : -1; result.Add(new Vector2Int(x, y)); }
                while (x != to.x) { x += to.x > x ? 1 : -1; result.Add(new Vector2Int(x, y)); }
            }
        }

        /// <summary>Trục nào nên đi trước để tuyến chữ L trông tự nhiên: trục có độ lệch lớn hơn.</summary>
        public static bool PreferHorizontalFirst(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(to.x - from.x) >= Mathf.Abs(to.y - from.y);
        }

        /// <summary>
        /// Tìm những chỗ đường bị dính thành mảng rộng từ 2 ô: mọi khối 2x2 mà cả 4 ô đều là Path.
        /// <para>
        /// Hai làn đường chạy song song sát nhau sẽ tạo ra khối 2x2 và bị bắt ở đây — đúng luật
        /// "hai đường phải cách nhau ít nhất một ô không đi được".
        /// Góc rẽ chữ L chỉ chiếm 3 trong 4 ô của khối 2x2 nên KHÔNG bị tính là vi phạm.
        /// </para>
        /// </summary>
        /// <param name="result">Nhận danh sách ô thuộc các khối vi phạm (không trùng lặp).</param>
        /// <returns>Số ô vi phạm.</returns>
        public static int FindWideSpots(TdMapData map, List<Vector2Int> result)
        {
            result?.Clear();
            if (map == null || result == null) return 0;

            for (int y = 0; y < map.Height - 1; y++)
            {
                for (int x = 0; x < map.Width - 1; x++)
                {
                    if (!IsBlockAllPath(map, x, y, NO_ASSUMPTION)) continue;

                    AddUnique(result, new Vector2Int(x, y));
                    AddUnique(result, new Vector2Int(x + 1, y));
                    AddUnique(result, new Vector2Int(x, y + 1));
                    AddUnique(result, new Vector2Int(x + 1, y + 1));
                }
            }

            return result.Count;
        }

        /// <summary>
        /// Tuyến đang hiển thị: ưu tiên chuỗi người dùng đã vẽ, không có thì rơi về BFS.
        /// Đây chính là tuyến mà mũi tên chỉ hướng chạy theo, nên việc dựng hình cũng phải
        /// bám vào nó để đường nối liền đúng chỗ mũi tên đi qua.
        /// </summary>
        public static bool TryGetEffectiveRoute(TdMapData map, List<Vector2Int> result)
        {
            result?.Clear();
            if (map == null || result == null) return false;

            IReadOnlyList<Vector2Int> drawn = map.Route;
            if (drawn != null && drawn.Count >= 2)
            {
                for (int i = 0; i < drawn.Count; i++) result.Add(drawn[i]);
                OrientFromSpawn(map, result);
                SnapEndsToEndpoints(map, result);
                return true;
            }

            return TryBuildRoute(map, result);
        }

        /// <summary>
        /// Xoay chiều tuyến để nó luôn chạy Spawn → Goal.
        /// <para>
        /// Người dựng map có thể vẽ từ đầu nào cũng được; quái thì luôn phải xuất phát ở Spawn.
        /// Không chuẩn hoá thì mũi tên chỉ ngược và lính spawn nhầm ở đầu kia.
        /// </para>
        /// </summary>
        private static void OrientFromSpawn(TdMapData map, List<Vector2Int> route)
        {
            if (route.Count < 2) return;

            Vector2Int first = route[0];
            Vector2Int last = route[route.Count - 1];
            if (first == map.Spawn || last == map.Goal) return;

            bool shouldReverse = last == map.Spawn
                                 || first == map.Goal
                                 || SqrDistance(last, map.Spawn) < SqrDistance(first, map.Spawn);

            if (shouldReverse) route.Reverse();
        }

        /// <summary>
        /// Nối thêm ô Spawn/Goal vào hai đầu nếu tuyến vẽ còn thiếu mà lại kề ngay bên.
        /// <para>
        /// Người dựng map hay dừng tay sát điểm cuối chứ không click đúng vào nó; không bù thì
        /// lính đứng lại cách đích một ô.
        /// </para>
        /// </summary>
        private static void SnapEndsToEndpoints(TdMapData map, List<Vector2Int> route)
        {
            if (route.Count == 0) return;

            if (route[0] != map.Spawn && IsAdjacent(route[0], map.Spawn)) route.Insert(0, map.Spawn);

            int last = route.Count - 1;
            if (route[last] != map.Goal && IsAdjacent(route[last], map.Goal)) route.Add(map.Goal);
        }

        private static bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        private static int SqrDistance(Vector2Int a, Vector2Int b)
        {
            int dx = a.x - b.x;
            int dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Khi dựng hình: có nên nối liền tile của hai ô đường kề nhau không.
        /// <para>
        /// Mọi ô mặc định chừa một khe hở nhỏ quanh mình. Khe giữa hai ô được LẤP khi chúng nối
        /// tiếp nhau trên cùng một làn đơn — nhờ vậy đường dọc và đường ngang trông liền mạch.
        /// Khe được GIỮ khi hai ô thuộc một mảng 2x2 toàn đường (hai làn nằm sát nhau), lúc đó
        /// khe hở lộ ra nền cỏ bên dưới.
        /// </para>
        /// </summary>
        public static bool ShouldMergeTiles(TdMapData map, Vector2Int cell, Vector2Int neighbour)
        {
            if (map == null) return false;
            if (!map.IsInside(neighbour)) return false;
            if (map.GetCell(cell) != TdCellType.Path) return false;
            if (map.GetCell(neighbour) != TdCellType.Path) return false;

            int minX = Mathf.Min(cell.x, neighbour.x);
            int maxX = Mathf.Max(cell.x, neighbour.x);
            int minY = Mathf.Min(cell.y, neighbour.y);
            int maxY = Mathf.Max(cell.y, neighbour.y);

            // Ngoài mảng 2x2 thì luôn nối liền: đây là đường đơn, không có chuyện hai làn kề nhau.
            bool insideBlock = false;
            for (int ox = maxX - 1; ox <= minX && !insideBlock; ox++)
            {
                for (int oy = maxY - 1; oy <= minY && !insideBlock; oy++)
                {
                    insideBlock = IsBlockAllPath(map, ox, oy, NO_ASSUMPTION);
                }
            }

            if (!insideBlock) return true;

            // Đang ở trong mảng đường dày 2 ô. Chỉ khe CẮT NGANG giữa hai làn mới để hở;
            // khe nối tiếp dọc theo làn vẫn phải lấp, nếu không mỗi làn bị vỡ vụn thành từng ô.
            // Trục nào có dải đường dài hơn thì đó là chiều đi của làn.
            bool alongX = cell.y == neighbour.y;
            int runAlong = RunLength(map, cell, alongX);

            // Lấy MIN chứ không phải Max: ở chỗ nhánh đâm vào một làn dài, ô thuộc làn đó có dải
            // vuông góc rất dài và sẽ áp đảo Max, khiến khe nối bị cắt rời oan. Hai làn thật sự
            // chạy song song thì cả hai ô đều có dải vuông góc ngắn, nên Min vẫn bắt đúng.
            int runAcross = Mathf.Min(RunLength(map, cell, !alongX), RunLength(map, neighbour, !alongX));

            return runAlong >= runAcross;
        }

        /// <summary>Độ dài dải ô đường liên tục đi qua <paramref name="cell"/> theo một trục.</summary>
        private static int RunLength(TdMapData map, Vector2Int cell, bool alongX)
        {
            var step = alongX ? new Vector2Int(1, 0) : new Vector2Int(0, 1);
            int length = 1;

            for (Vector2Int c = cell + step; map.GetCell(c) == TdCellType.Path; c += step) length++;
            for (Vector2Int c = cell - step; map.GetCell(c) == TdCellType.Path; c -= step) length++;

            return length;
        }

        /// <summary>
        /// Đặt Path vào ô này có làm đường phình thành mảng rộng 2 ô không.
        /// Dùng để chặn ngay lúc vẽ thay vì để lỗi lọt tới bước kiểm tra.
        /// </summary>
        public static bool WouldWiden(TdMapData map, Vector2Int cell)
        {
            if (map == null) return false;
            if (map.GetCell(cell) == TdCellType.Path) return false; // đã là đường sẵn, không đổi gì

            // Ô này nằm trong tối đa 4 khối 2x2 khác nhau.
            for (int dy = -1; dy <= 0; dy++)
            {
                for (int dx = -1; dx <= 0; dx++)
                {
                    if (IsBlockAllPath(map, cell.x + dx, cell.y + dy, cell)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra map: Spawn có nối tới Goal không, có bao nhiêu ô đường lạc.
        /// </summary>
        public static TdMapValidation Validate(TdMapData map)
        {
            if (map == null) return new TdMapValidation(false, "Chưa gán map.", 0, 0, 0, 0);

            int pathCells = CountPathCells(map);
            int wideSpots = FindWideSpots(map, WideSpotScratch);

            int[] cameFrom = RunBfs(map, out int reachable);
            if (cameFrom == null)
            {
                return new TdMapValidation(false, "Map rỗng hoặc kích thước không hợp lệ.", pathCells, 0, 0, wideSpots);
            }

            int orphans = pathCells - reachable;
            int goalIndex = map.Goal.y * map.Width + map.Goal.x;
            bool goalReached = cameFrom[goalIndex] != UNVISITED;

            if (!goalReached)
            {
                return new TdMapValidation(
                    false,
                    $"Đường bị đứt: không đi được từ Spawn {map.Spawn} tới Goal {map.Goal}.",
                    pathCells, orphans, 0, wideSpots);
            }

            var route = new List<Vector2Int>();
            TryBuildRoute(map, route);

            var message = new StringBuilder("Đường thông");
            if (orphans > 0) message.Append($", còn {orphans} ô đường lạc không nối tới Spawn");
            if (wideSpots > 0)
            {
                message.Append($", có {wideSpots} ô đường dính rộng 2 ô — hai làn phải cách nhau ít nhất 1 ô");
            }

            if (orphans == 0 && wideSpots == 0) message.Append(", không có ô lạc và không chỗ nào bị dính");
            message.Append('.');

            return new TdMapValidation(true, message.ToString(), pathCells, orphans, route.Count, wideSpots);
        }

        #endregion

        #region Private

        /// <summary>
        /// BFS từ Spawn trên các ô Path.
        /// Trả về mảng cha của từng ô (<see cref="UNVISITED"/> nếu chưa tới được), hoặc <c>null</c> nếu map hỏng.
        /// </summary>
        private static int[] RunBfs(TdMapData map, out int visitedCount)
        {
            visitedCount = 0;
            int width = map.Width;
            int height = map.Height;
            if (width <= 0 || height <= 0) return null;

            var cameFrom = new int[width * height];
            for (int i = 0; i < cameFrom.Length; i++) cameFrom[i] = UNVISITED;

            Vector2Int spawn = map.Spawn;
            if (!map.IsWalkable(spawn)) return cameFrom;

            int spawnIndex = spawn.y * width + spawn.x;
            cameFrom[spawnIndex] = spawnIndex; // gốc tự trỏ vào chính nó
            visitedCount = 1;

            var queue = new Queue<int>();
            queue.Enqueue(spawnIndex);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int cx = current % width;
                int cy = current / width;

                for (int n = 0; n < NEIGHBOURS.Length; n++)
                {
                    int nx = cx + NEIGHBOURS[n].x;
                    int ny = cy + NEIGHBOURS[n].y;
                    if (!map.IsInside(nx, ny)) continue;
                    if (map.GetCell(nx, ny) != TdCellType.Path) continue;

                    int next = ny * width + nx;
                    if (cameFrom[next] != UNVISITED) continue;

                    cameFrom[next] = current;
                    visitedCount++;
                    queue.Enqueue(next);
                }
            }

            return cameFrom;
        }

        /// <summary>
        /// Khối 2x2 có gốc (<paramref name="originX"/>, <paramref name="originY"/>) có toàn ô Path không.
        /// <paramref name="assumePath"/> được coi như đã là Path dù thực tế chưa — dùng cho
        /// <see cref="WouldWiden"/> để thử trước khi ghi.
        /// </summary>
        private static bool IsBlockAllPath(TdMapData map, int originX, int originY, Vector2Int assumePath)
        {
            for (int y = originY; y <= originY + 1; y++)
            {
                for (int x = originX; x <= originX + 1; x++)
                {
                    if (!map.IsInside(x, y)) return false;
                    if (x == assumePath.x && y == assumePath.y) continue;
                    if (map.GetCell(x, y) != TdCellType.Path) return false;
                }
            }

            return true;
        }

        private static void AddUnique(List<Vector2Int> list, Vector2Int cell)
        {
            if (!list.Contains(cell)) list.Add(cell);
        }

        private static int CountPathCells(TdMapData map)
        {
            int count = 0;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.GetCell(x, y) == TdCellType.Path) count++;
                }
            }

            return count;
        }

        #endregion
    }
}
