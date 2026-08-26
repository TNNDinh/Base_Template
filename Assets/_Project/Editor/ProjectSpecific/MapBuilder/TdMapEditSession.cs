using System;
using System.Collections.Generic;
using Ezg.Feature.MapBuilder;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ezg.Feature.MapBuilder.EditorTools
{
    /// <summary>Cách rê chuột để vẽ.</summary>
    public enum TdPaintTool
    {
        /// <summary>Rê tới đâu tô tới đó, tự lấp ô bị nhảy khi rê nhanh.</summary>
        Freehand,

        /// <summary>Bấm ở ô đầu, thả ở ô cuối — nối bằng tuyến gấp khúc chữ L.</summary>
        Line,

        /// <summary>Quét một vùng chữ nhật.</summary>
        Rect,

        /// <summary>Click từng phát, nối tiếp từ ô đã nhớ lần trước.</summary>
        Chain,
    }

    /// <summary>Loại ô mà cọ ghi xuống.</summary>
    public enum TdPaintTarget
    {
        Path,
        Erase,
        Blocked,
    }

    /// <summary>Đang chờ click để đặt điểm cố định nào (một phát rồi tự tắt).</summary>
    public enum TdPlaceMode
    {
        None,
        Spawn,
        Goal,
    }

    /// <summary>
    /// Trạng thái dựng map dùng chung cho cả cửa sổ Map Builder lẫn overlay trong Scene View.
    /// <para>
    /// Lớp này tự đăng ký <see cref="SceneView.duringSceneGui"/> nên vẽ được ngay trong scene
    /// mà KHÔNG cần mở cửa sổ nào. Map được tự nhận từ <see cref="TdMapRenderer"/> có trong scene.
    /// </para>
    /// </summary>
    [InitializeOnLoad]
    public static class TdMapEditSession
    {
        #region Constants

        private const string PREF_PAINT_ENABLED = "TdMapBuilder.PaintEnabled";
        private const string PREF_LOCK_ENDPOINTS = "TdMapBuilder.LockEndpoints";
        private const string PREF_SHOW_ROUTE = "TdMapBuilder.ShowRoute";
        private const string PREF_ENFORCE_WIDTH = "TdMapBuilder.EnforceCorridorWidth";
        private const string PREF_LAST_MAP = "TdMapBuilder.LastMapGuid";

        /// <summary>Trên ngưỡng này chỉ vẽ viền ô, bỏ phần tô nền cho đỡ giật Scene View.</summary>
        private const int MAX_FILLED_CELLS = 4096;

        public static readonly Vector2Int INVALID_CELL = new Vector2Int(-1, -1);

        #endregion

        #region Fields

        private static TdMapData _map;

        private static bool _isPainting;        // đang rê ở chế độ Tự do
        private static bool _isDragOperation;   // đang kéo khung Đường thẳng / Chữ nhật
        private static Vector2Int _dragStart = INVALID_CELL;
        private static Vector2Int _dragEnd = INVALID_CELL;
        private static Vector2Int _lastPaintedCell = INVALID_CELL;
        private static Vector2Int _hoverCell = INVALID_CELL;

        /// <summary>Nét vẽ hiện tại đã chạm luật "không cho 2 làn dính nhau" và bị dừng.</summary>
        /// <summary>Ô đã nối gần nhất — điểm bắt đầu cho lần click kế tiếp ở chế độ Nối chuỗi.</summary>
        private static Vector2Int _chainAnchor = INVALID_CELL;

        private static float _routeLength;

        private static bool _strokeBlocked;

        private static string _lastBlockMessage;

        private static readonly List<Vector2Int> StrokeBuffer = new List<Vector2Int>();
        private static readonly List<Vector2Int> PreviewBuffer = new List<Vector2Int>();
        private static readonly List<Vector2Int> RouteBuffer = new List<Vector2Int>();
        private static readonly List<Vector2Int> WideSpotBuffer = new List<Vector2Int>();

        private static Vector3[] _routePoints;
        private static bool _routeDirty = true;

        // Map lấy từ scene đang mở, cache lại vì OnSceneGui chạy liên tục còn quét scene thì đắt.
        private static TdMapData _sceneMapCache;
        private static bool _sceneMapDirty = true;

        private static TdMapValidation _validation;
        private static bool _validationDirty = true;

        #endregion

        #region Initialize

        static TdMapEditSession() => Hook();

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;

            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            EditorApplication.hierarchyChanged -= InvalidateSceneMap;
            EditorApplication.hierarchyChanged += InvalidateSceneMap;
        }

        #endregion

        #region Public - Events

        /// <summary>Bắn khi map hoặc thiết lập đổi, để cửa sổ và overlay vẽ lại.</summary>
        public static event Action Changed;

        #endregion

        #region Public - Properties

        /// <summary>
        /// Map đang dựng. Chưa chọn thì tự lấy từ <see cref="TdMapRenderer"/> trong scene,
        /// không có nữa thì lấy map dùng lần trước.
        /// </summary>
        public static TdMapData Map
        {
            get
            {
                // Scene có renderer thì map của scene đó là nguồn duy nhất — chuyển scene là
                // đổi map theo, không dính map của scene cũ.
                TdMapData sceneMap = SceneMap;
                if (sceneMap != null)
                {
                    if (_map != sceneMap) MarkDirty();
                    _map = sceneMap;
                    return _map;
                }

                if (_map == null) _map = LoadLastMap();
                return _map;
            }
            set
            {
                if (_map == value) return;
                _map = value;
                ResetStroke();
                MarkDirty();
                RememberLastMap();
                RaiseChanged();
            }
        }

        public static TdPaintTool Tool { get; set; } = TdPaintTool.Freehand;

        public static TdPaintTarget Target { get; set; } = TdPaintTarget.Path;

        public static TdPlaceMode PlaceMode { get; set; } = TdPlaceMode.None;

        /// <summary>
        /// Bật thì click chuột trong Scene View dùng để vẽ map thay vì chọn object.
        /// Mặc định TẮT để không cướp thao tác chọn object của scene.
        /// </summary>
        public static bool PaintEnabled
        {
            get => EditorPrefs.GetBool(PREF_PAINT_ENABLED, false);
            set
            {
                if (PaintEnabled == value) return;
                EditorPrefs.SetBool(PREF_PAINT_ENABLED, value);
                if (!value) ResetStroke();
                RaiseChanged();
            }
        }

        public static bool LockEndpoints
        {
            get => EditorPrefs.GetBool(PREF_LOCK_ENDPOINTS, true);
            set => EditorPrefs.SetBool(PREF_LOCK_ENDPOINTS, value);
        }

        public static bool ShowRoute
        {
            get => EditorPrefs.GetBool(PREF_SHOW_ROUTE, true);
            set => EditorPrefs.SetBool(PREF_SHOW_ROUTE, value);
        }

        /// <summary>
        /// Tuỳ chọn: bật thì không cho vẽ ô đường nào làm hai làn dính thành mảng rộng 2 ô.
        /// <para>
        /// MẶC ĐỊNH TẮT — hai làn sát nhau là hợp lệ; dải cỏ ngăn cách được thể hiện bằng khe hở
        /// mảnh giữa các tile khi dựng hình (xem <see cref="TdMapRenderer"/>), không phải bằng
        /// cách chừa hẳn một ô.
        /// </para>
        /// </summary>
        public static bool EnforceCorridorWidth
        {
            get => EditorPrefs.GetBool(PREF_ENFORCE_WIDTH, false);
            set
            {
                if (EnforceCorridorWidth == value) return;
                EditorPrefs.SetBool(PREF_ENFORCE_WIDTH, value);
                RaiseChanged();
            }
        }

        /// <summary>
        /// Scene đang mở có chứa <see cref="TdMapRenderer"/> không.
        /// Không có thì công cụ dựng map phải im lặng hoàn toàn — đây không phải scene dựng map.
        /// </summary>
        public static bool HasSceneContext => SceneMap != null;

        /// <summary>Map lấy từ scene đang mở, hoặc <c>null</c>.</summary>
        private static TdMapData SceneMap
        {
            get
            {
                if (!_sceneMapDirty) return _sceneMapCache;

                _sceneMapCache = ResolveMapFromScene();
                _sceneMapDirty = false;
                return _sceneMapCache;
            }
        }

        /// <summary>Ô đang nằm dưới con trỏ, hoặc <see cref="INVALID_CELL"/>.</summary>
        public static Vector2Int HoverCell => _hoverCell;

        /// <summary>Lý do thao tác vẽ gần nhất bị từ chối, rỗng nếu không có.</summary>
        public static string LastBlockMessage => _lastBlockMessage;

        /// <summary>
        /// Ô mà lần nối kế tiếp sẽ xuất phát. Chưa nối lần nào thì lấy Spawn làm gốc.
        /// </summary>
        public static Vector2Int ChainAnchor
        {
            get
            {
                TdMapData map = Map;
                if (map == null) return INVALID_CELL;

                // Ô đã nhớ có thể vừa bị xoá — lúc đó phải lùi về ô cuối chuỗi.
                if (_chainAnchor != INVALID_CELL && map.GetCell(_chainAnchor) == TdCellType.Path)
                {
                    return _chainAnchor;
                }

                IReadOnlyList<Vector2Int> route = map.Route;
                return route != null && route.Count > 0 ? route[route.Count - 1] : map.Spawn;
            }
        }

        /// <summary>Quên ô đã nhớ, lần nối sau bắt đầu lại từ Spawn.</summary>
        public static void ResetChainAnchor()
        {
            _chainAnchor = INVALID_CELL;
            RaiseChanged();
        }

        /// <summary>
        /// Kết quả kiểm tra map, cache lại. Overlay và cửa sổ vẽ lại rất nhiều lần nên
        /// không chạy BFS mỗi lần repaint — chỉ tính lại sau khi map thay đổi.
        /// </summary>
        public static TdMapValidation Validation
        {
            get
            {
                if (!_validationDirty) return _validation;

                _validation = TdMapPath.Validate(Map);
                _validationDirty = false;
                return _validation;
            }
        }

        #endregion

        #region Public - Map Operations

        /// <summary>Ghi cùng một loại cho toàn bộ lưới.</summary>
        public static void FillAll(TdCellType type)
        {
            if (Map == null) return;
            Record("Fill TD Map");
            Map.FillAll(type);
            Commit();
        }

        /// <summary>Xoá mọi ô đường về Đất xây (Spawn/Goal giữ nguyên).</summary>
        public static void ClearPath()
        {
            if (Map == null) return;
            Record("Clear TD Map Path");

            for (int y = 0; y < Map.Height; y++)
            {
                for (int x = 0; x < Map.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (Map.GetCell(cell) == TdCellType.Path) Map.SetCell(cell, TdCellType.Buildable);
                }
            }

            Map.ClearRoute();
            _chainAnchor = INVALID_CELL;
            Commit();
        }

        /// <summary>Vẽ sẵn một tuyến chữ L nối Spawn → Goal.</summary>
        public static void ConnectEndpoints(bool horizontalFirst)
        {
            if (Map == null) return;
            Record("Connect TD Map Endpoints");
            TdMapPath.BuildElbow(Map.Spawn, Map.Goal, horizontalFirst, StrokeBuffer);
            for (int i = 0; i < StrokeBuffer.Count; i++)
            {
                Vector2Int cell = StrokeBuffer[i];
                if (EnforceCorridorWidth && TdMapPath.WouldWiden(Map, cell)) continue;
                Map.SetCell(cell, TdCellType.Path);
            }

            Map.SetRoute(StrokeBuffer);
            _chainAnchor = Map.Goal;
            Commit();
        }

        /// <summary>Dựng lại mọi <see cref="TdMapRenderer"/> trong scene đang dùng map này.</summary>
        /// <returns>Số renderer đã dựng lại.</returns>
        public static int RebuildRenderers()
        {
            var renderers = UnityEngine.Object.FindObjectsByType<TdMapRenderer>(FindObjectsSortMode.None);
            int rebuilt = 0;

            foreach (TdMapRenderer renderer in renderers)
            {
                if (renderer.Map != Map) continue;
                Undo.RegisterFullObjectHierarchyUndo(renderer.gameObject, "Rebuild TD Map");
                renderer.Rebuild();
                rebuilt++;
            }

            return rebuilt;
        }

        /// <summary>Ghi nhận thay đổi lên asset và báo cho UI vẽ lại.</summary>
        public static void Commit()
        {
            if (Map != null)
            {
                Map.SanitizeRoute();
                EditorUtility.SetDirty(Map);
            }

            MarkDirty();
            RaiseChanged();
        }

        /// <summary>Đăng ký undo trước khi sửa map.</summary>
        public static void Record(string label)
        {
            if (Map != null) Undo.RecordObject(Map, label);
        }

        #endregion

        #region Private - Scene GUI

        private static void OnSceneGui(SceneView sceneView)
        {
            // Scene không có renderer map thì không vẽ gì cả, không nuốt chuột, không hiện mũi tên.
            if (!HasSceneContext) return;

            TdMapData map = Map;
            if (map == null) return;

            DrawGrid(map);
            DrawWideSpots(map);
            DrawEndpoints(map);
            if (ShowRoute) DrawRoute(map);
            DrawDragPreview(map);

            if (PaintEnabled) HandlePaintInput(map);

            // Mũi tên chạy cần repaint liên tục; ngoài ra chế độ vẽ cũng cần để thấy ô hover.
            if (PaintEnabled || (ShowRoute && _routePoints != null)) sceneView.Repaint();
        }

        private static void DrawGrid(TdMapData map)
        {
            bool fill = map.CellCount <= MAX_FILLED_CELLS;
            var corners = new Vector3[4];

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    GetCellCorners(map, cell, corners);

                    Color face = fill ? GetCellColor(map.GetCell(cell)) : Color.clear;
                    Color outline = cell == _hoverCell
                        ? new Color(1f, 1f, 0.2f, 0.95f)
                        : new Color(0f, 0f, 0f, 0.25f);

                    Handles.DrawSolidRectangleWithOutline(corners, face, outline);
                }
            }
        }

        /// <summary>Tô đỏ những ô đường bị dính rộng 2 ô để thấy ngay chỗ thiếu dải ngăn cách.</summary>
        private static void DrawWideSpots(TdMapData map)
        {
            if (!EnforceCorridorWidth || Validation.WideSpotCount == 0) return;

            TdMapPath.FindWideSpots(map, WideSpotBuffer);

            var corners = new Vector3[4];
            var face = new Color(1f, 0.15f, 0.15f, 0.5f);
            var outline = new Color(1f, 0.1f, 0.1f, 0.95f);

            for (int i = 0; i < WideSpotBuffer.Count; i++)
            {
                GetCellCorners(map, WideSpotBuffer[i], corners);
                Handles.DrawSolidRectangleWithOutline(corners, face, outline);
            }
        }

        private static void DrawEndpoints(TdMapData map)
        {
            DrawMarker(map, map.Spawn, new Color(0.25f, 0.6f, 1f), "SPAWN");
            DrawMarker(map, map.Goal, new Color(0.95f, 0.3f, 0.3f), "GOAL");
        }

        private static void DrawMarker(TdMapData map, Vector2Int cell, Color color, string label)
        {
            Vector3 center = map.CellToWorld(cell);
            float radius = map.CellSize * 0.35f;

            using (new Handles.DrawingScope(color))
            {
                Handles.DrawWireDisc(center + Vector3.up * 0.02f, Vector3.up, radius);
                Handles.DrawWireDisc(center + Vector3.up * 0.02f, Vector3.up, radius * 0.6f);
                Handles.Label(center + Vector3.up * (map.CellSize * 0.5f), label, EditorStyles.whiteBoldLabel);
            }
        }

        private static void DrawRoute(TdMapData map)
        {
            RefreshRouteCache(map);
            if (_routePoints == null || _routePoints.Length < 2) return;

            using (new Handles.DrawingScope(new Color(1f, 0.85f, 0.1f, 0.45f)))
            {
                Handles.DrawAAPolyLine(4f, _routePoints);
            }

            DrawFlowArrows(map);
        }

        /// <summary>Mũi tên vàng chạy dọc tuyến để thấy rõ chiều đi.</summary>
        private static void DrawFlowArrows(TdMapData map)
        {
            if (_routeLength <= 0.001f) return;

            float spacing = map.CellSize * 2f;
            float speed = map.CellSize * 2.5f;
            float phase = (float)(EditorApplication.timeSinceStartup * speed % spacing);
            float size = map.CellSize * 0.22f;

            using (new Handles.DrawingScope(new Color(1f, 0.95f, 0.2f)))
            {
                for (float travelled = phase; travelled < _routeLength; travelled += spacing)
                {
                    if (!SampleRoute(travelled, out Vector3 position, out Vector3 direction)) continue;

                    Handles.ConeHandleCap(
                        0,
                        position,
                        Quaternion.LookRotation(direction, Vector3.up),
                        size,
                        EventType.Repaint);
                }
            }
        }

        /// <summary>Lấy vị trí và hướng tại một khoảng cách dọc theo tuyến.</summary>
        private static bool SampleRoute(float distance, out Vector3 position, out Vector3 direction)
        {
            position = Vector3.zero;
            direction = Vector3.forward;
            if (_routePoints == null || _routePoints.Length < 2) return false;

            float walked = 0f;
            for (int i = 1; i < _routePoints.Length; i++)
            {
                Vector3 from = _routePoints[i - 1];
                Vector3 to = _routePoints[i];
                float segment = Vector3.Distance(from, to);
                if (segment <= 0.0001f) continue;

                if (walked + segment >= distance)
                {
                    float t = (distance - walked) / segment;
                    position = Vector3.Lerp(from, to, t);
                    direction = (to - from).normalized;
                    return true;
                }

                walked += segment;
            }

            return false;
        }

        /// <summary>Chạy lại BFS chỉ khi map vừa đổi — OnSceneGui gọi liên tục nên không tính mỗi frame.</summary>
        private static void RefreshRouteCache(TdMapData map)
        {
            if (!_routeDirty) return;
            _routeDirty = false;

            // Chuỗi người dùng vẽ là nguồn ưu tiên vì nó mang CHIỀU ĐI;
            // BFS chỉ dùng khi chưa vẽ theo chuỗi (map cũ, hoặc vẽ lẻ tẻ).
            IReadOnlyList<Vector2Int> source = map.Route;
            if (source == null || source.Count < 2)
            {
                if (!TdMapPath.TryBuildRoute(map, RouteBuffer) || RouteBuffer.Count < 2)
                {
                    _routePoints = null;
                    _routeLength = 0f;
                    return;
                }

                source = RouteBuffer;
            }

            if (_routePoints == null || _routePoints.Length != source.Count)
            {
                _routePoints = new Vector3[source.Count];
            }

            for (int i = 0; i < source.Count; i++)
            {
                _routePoints[i] = map.CellToWorld(source[i]) + Vector3.up * 0.05f;
            }

            _routeLength = 0f;
            for (int i = 1; i < _routePoints.Length; i++)
            {
                _routeLength += Vector3.Distance(_routePoints[i - 1], _routePoints[i]);
            }
        }

        /// <summary>Tô vàng các ô mà thao tác kéo Đường thẳng / Chữ nhật sắp ghi vào.</summary>
        private static void DrawDragPreview(TdMapData map)
        {
            if (!_isDragOperation) return;

            BuildDragCells(PreviewBuffer, Event.current.shift);

            var corners = new Vector3[4];
            var face = new Color(1f, 0.9f, 0.2f, 0.4f);
            var outline = new Color(1f, 0.85f, 0.1f, 0.95f);

            for (int i = 0; i < PreviewBuffer.Count; i++)
            {
                Vector2Int cell = PreviewBuffer[i];
                if (!map.IsInside(cell)) continue;

                GetCellCorners(map, cell, corners);
                Handles.DrawSolidRectangleWithOutline(corners, face, outline);
            }

            Handles.Label(
                map.CellToWorld(_dragEnd) + Vector3.up * (map.CellSize * 0.4f),
                PreviewBuffer.Count + " ô",
                EditorStyles.whiteBoldLabel);
        }

        #endregion

        #region Private - Paint Input

        private static void HandlePaintInput(TdMapData map)
        {
            Event evt = Event.current;

            // Chặn click chọn object khi đang ở chế độ vẽ.
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            bool hasCell = TryGetCellUnderMouse(map, out Vector2Int cell);
            _hoverCell = hasCell ? cell : INVALID_CELL;

            if (evt.alt) return; // Alt = xoay camera, nhường cho Scene View.

            switch (evt.type)
            {
                case EventType.MouseDown when evt.button == 0 && hasCell:
                    BeginStroke(map, cell, evt.shift);
                    evt.Use();
                    break;

                case EventType.MouseDrag when evt.button == 0 && hasCell:
                    ContinueStroke(map, cell, evt.shift);
                    evt.Use();
                    break;

                case EventType.MouseUp when evt.button == 0:
                    EndStroke(map, hasCell ? cell : _dragEnd, evt.shift);
                    evt.Use();
                    break;

                case EventType.MouseLeaveWindow:
                    ResetStroke();
                    break;
            }
        }

        private static void BeginStroke(TdMapData map, Vector2Int cell, bool shift)
        {
            // Đặt Spawn/Goal: một click là xong, không kéo.
            if (PlaceMode != TdPlaceMode.None)
            {
                PlaceEndpoint(map, cell);
                return;
            }

            if (Tool == TdPaintTool.Chain)
            {
                ChainTo(map, cell, shift);
                return;
            }

            _dragStart = cell;
            _dragEnd = cell;

            if (Tool != TdPaintTool.Freehand)
            {
                _isDragOperation = true; // Đường thẳng / Chữ nhật: chỉ preview, ghi lúc thả.
                return;
            }

            Record("Paint TD Map");
            _isPainting = true;
            _strokeBlocked = false;
            _lastBlockMessage = null;

            if (!PaintCell(map, cell, shift)) BlockStroke();
            _lastPaintedCell = cell;
            Commit();
        }

        /// <summary>
        /// Nối từ ô đã nhớ tới ô vừa click bằng tuyến chữ L, rồi nhớ luôn ô vừa click
        /// làm gốc cho lần nối sau.
        /// </summary>
        private static void ChainTo(TdMapData map, Vector2Int cell, bool flipElbow)
        {
            Vector2Int from = ChainAnchor;
            if (!map.IsInside(from)) from = map.Spawn;
            if (cell == from) return;

            bool horizontalFirst = TdMapPath.PreferHorizontalFirst(from, cell);
            if (flipElbow) horizontalFirst = !horizontalFirst;
            TdMapPath.BuildElbow(from, cell, horizontalFirst, StrokeBuffer);

            if (WouldAnyWiden(map, StrokeBuffer))
            {
                BlockStroke();
                RaiseChanged();
                return;
            }

            Record("Chain TD Map");
            for (int i = 0; i < StrokeBuffer.Count; i++) PaintCell(map, StrokeBuffer[i], false);

            _chainAnchor = cell;
            _lastBlockMessage = null;
            Commit();
        }

        private static void ContinueStroke(TdMapData map, Vector2Int cell, bool shift)
        {
            if (Tool == TdPaintTool.Chain) return; // Chain chỉ phản ứng với click, không kéo.

            if (Tool != TdPaintTool.Freehand)
            {
                _dragEnd = cell;
                return;
            }

            if (!_isPainting) return;

            // Nét đã chạm luật độ rộng thì dừng hẳn tới khi thả chuột — nếu cứ vẽ tiếp,
            // luật 2x2 sẽ cho lọt ô cách quãng và để lại một hàng đường răng lược.
            if (_strokeBlocked)
            {
                _lastPaintedCell = cell;
                return;
            }

            // MouseDrag bắn rời rạc: rê nhanh sẽ nhảy cóc vài ô. Nối bù bằng tuyến chữ L
            // (liền theo 4 hướng) để nét vẽ không bị đứt.
            if (_lastPaintedCell != INVALID_CELL && cell != _lastPaintedCell)
            {
                TdMapPath.BuildElbow(
                    _lastPaintedCell,
                    cell,
                    TdMapPath.PreferHorizontalFirst(_lastPaintedCell, cell),
                    StrokeBuffer);

                for (int i = 0; i < StrokeBuffer.Count; i++)
                {
                    if (PaintCell(map, StrokeBuffer[i], shift)) continue;
                    BlockStroke();
                    break;
                }
            }
            else if (!PaintCell(map, cell, shift))
            {
                BlockStroke();
            }

            _lastPaintedCell = cell;
            Commit();
        }

        private static void EndStroke(TdMapData map, Vector2Int cell, bool shift)
        {
            if (_isDragOperation && map.IsInside(cell))
            {
                _dragEnd = cell;
                BuildDragCells(StrokeBuffer, shift);

                // Nguyên khối: chỉ cần một ô vi phạm là bỏ cả thao tác, thay vì ghi
                // được một phần rồi để lại hình răng lược.
                if (WouldAnyWiden(map, StrokeBuffer))
                {
                    BlockStroke();
                    ResetStroke();
                    RaiseChanged();
                    return;
                }

                Record(Tool == TdPaintTool.Line ? "Line TD Map" : "Rect TD Map");
                for (int i = 0; i < StrokeBuffer.Count; i++) PaintCell(map, StrokeBuffer[i], false);
                Commit();
            }

            ResetStroke();
        }

        /// <summary>Có ô nào trong danh sách làm đường phình rộng không (thử trên bản sao trạng thái).</summary>
        private static bool WouldAnyWiden(TdMapData map, List<Vector2Int> cells)
        {
            if (Target != TdPaintTarget.Path || !EnforceCorridorWidth) return false;

            for (int i = 0; i < cells.Count; i++)
            {
                if (TdMapPath.WouldWiden(map, cells[i])) return true;
            }

            return false;
        }

        private static void BlockStroke()
        {
            _strokeBlocked = true;
            _lastBlockMessage = "Chỗ này sẽ làm hai làn đường dính vào nhau. "
                              + "Hai làn phải chừa ít nhất một ô cỏ ở giữa.";
        }

        private static void ResetStroke()
        {
            _isPainting = false;
            _isDragOperation = false;
            _strokeBlocked = false;
            _lastPaintedCell = INVALID_CELL;
        }

        /// <summary>Liệt kê các ô mà thao tác kéo hiện tại sẽ ghi vào.</summary>
        private static void BuildDragCells(List<Vector2Int> result, bool flipElbow)
        {
            if (Tool == TdPaintTool.Line)
            {
                bool horizontalFirst = TdMapPath.PreferHorizontalFirst(_dragStart, _dragEnd);
                if (flipElbow) horizontalFirst = !horizontalFirst;
                TdMapPath.BuildElbow(_dragStart, _dragEnd, horizontalFirst, result);
                return;
            }

            result.Clear();
            int minX = Mathf.Min(_dragStart.x, _dragEnd.x);
            int maxX = Mathf.Max(_dragStart.x, _dragEnd.x);
            int minY = Mathf.Min(_dragStart.y, _dragEnd.y);
            int maxY = Mathf.Max(_dragStart.y, _dragEnd.y);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++) result.Add(new Vector2Int(x, y));
            }
        }

        /// <returns><c>false</c> nếu ô bị luật độ rộng từ chối.</returns>
        private static bool PaintCell(TdMapData map, Vector2Int cell, bool eraseModifier)
        {
            TdCellType type = eraseModifier ? TdCellType.Buildable : TargetToCellType(Target);

            // Chặn ngay lúc vẽ: không cho hai làn đường dính lại thành một dải rộng.
            if (type == TdCellType.Path && EnforceCorridorWidth && TdMapPath.WouldWiden(map, cell)) return false;

            if (!map.SetCell(cell, type)) return true;

            // Thứ tự vẽ chính là chiều đi của map, nên ô đường nào ghi xuống cũng nối vào cuối chuỗi.
            if (type == TdCellType.Path)
            {
                map.AppendRoute(cell);
                _chainAnchor = cell;
            }

            return true;
        }

        private static void PlaceEndpoint(TdMapData map, Vector2Int cell)
        {
            if (LockEndpoints) return;

            Record("Move TD Map Endpoint");
            bool ok = PlaceMode == TdPlaceMode.Spawn ? map.SetSpawn(cell) : map.SetGoal(cell);
            if (!ok) return;

            PlaceMode = TdPlaceMode.None;
            Commit();
        }

        private static bool TryGetCellUnderMouse(TdMapData map, out Vector2Int cell)
        {
            cell = default;

            var plane = new Plane(Vector3.up, map.Origin);
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (!plane.Raycast(ray, out float distance)) return false;

            Vector2Int candidate = map.WorldToCell(ray.GetPoint(distance));
            if (!map.IsInside(candidate)) return false;

            cell = candidate;
            return true;
        }

        #endregion

        #region Private - Helpers

        private static TdCellType TargetToCellType(TdPaintTarget target)
        {
            switch (target)
            {
                case TdPaintTarget.Path: return TdCellType.Path;
                case TdPaintTarget.Blocked: return TdCellType.Blocked;
                default: return TdCellType.Buildable;
            }
        }

        private static void GetCellCorners(TdMapData map, Vector2Int cell, Vector3[] buffer)
        {
            float size = map.CellSize;
            Vector3 corner = map.CellCornerToWorld(cell.x, cell.y);
            buffer[0] = corner;
            buffer[1] = corner + new Vector3(size, 0f, 0f);
            buffer[2] = corner + new Vector3(size, 0f, size);
            buffer[3] = corner + new Vector3(0f, 0f, size);
        }

        private static Color GetCellColor(TdCellType type)
        {
            switch (type)
            {
                case TdCellType.Path: return new Color(0.78f, 0.58f, 0.32f, 0.55f);
                case TdCellType.Buildable: return new Color(0.35f, 0.68f, 0.35f, 0.35f);
                default: return new Color(0.15f, 0.15f, 0.2f, 0.45f);
            }
        }

        private static TdMapData ResolveMapFromScene()
        {
            var renderers = UnityEngine.Object.FindObjectsByType<TdMapRenderer>(FindObjectsSortMode.None);
            foreach (TdMapRenderer renderer in renderers)
            {
                if (renderer.Map != null) return renderer.Map;
            }

            return null;
        }

        private static TdMapData LoadLastMap()
        {
            string guid = EditorPrefs.GetString(PREF_LAST_MAP, string.Empty);
            if (string.IsNullOrEmpty(guid)) return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<TdMapData>(path);
        }

        private static void RememberLastMap()
        {
            string guid = _map == null
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_map));
            EditorPrefs.SetString(PREF_LAST_MAP, guid);
        }

        private static void InvalidateSceneMap() => _sceneMapDirty = true;

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            _sceneMapDirty = true;
            if (mode != OpenSceneMode.Single) return;

            // Scene mới có thể dùng map khác — bỏ cache để lần đọc sau tự nhận lại.
            _map = null;
            ResetStroke();
            MarkDirty();
            RaiseChanged();
        }

        private static void OnUndoRedo()
        {
            MarkDirty();
            RaiseChanged();
            SceneView.RepaintAll();
        }

        /// <summary>Đánh dấu tuyến đường và kết quả validate cần tính lại.</summary>
        private static void MarkDirty()
        {
            _routeDirty = true;
            _validationDirty = true;
        }

        private static void RaiseChanged()
        {
            Changed?.Invoke();
            SceneView.RepaintAll();
        }

        #endregion
    }
}
