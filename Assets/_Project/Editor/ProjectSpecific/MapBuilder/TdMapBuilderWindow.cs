using System.IO;
using Ezg.Feature.MapBuilder;
using UnityEditor;
using UnityEngine;

namespace Ezg.Feature.MapBuilder.EditorTools
{
    /// <summary>
    /// Cửa sổ dựng map thủ thành — bản đầy đủ tuỳ chọn.
    /// <para>
    /// Việc vẽ trong Scene View do <see cref="TdMapEditSession"/> lo và chạy được cả khi cửa sổ này
    /// đóng; cửa sổ chỉ là lớp UI. Muốn gọn hơn thì dùng overlay ngay trong Scene View
    /// (<see cref="TdMapPaintOverlay"/>).
    /// </para>
    /// </summary>
    public class TdMapBuilderWindow : EditorWindow
    {
        #region Constants

        private const string MENU_PATH = "Tools/Tower Defense/Map Builder";
        private const string DEFAULT_ASSET_DIR = "Assets/_Project/Features/Gameplay/MapBuilder/Maps";
        private const float PANEL_LABEL_WIDTH = 110f;

        #endregion

        #region Fields

        private int _draftWidth = TdMapData.DEFAULT_WIDTH;
        private int _draftHeight = TdMapData.DEFAULT_HEIGHT;
        private float _draftCellSize = TdMapData.DEFAULT_CELL_SIZE;
        private Vector3 _draftOrigin = Vector3.zero;
        private TdMapAxis _draftAxis = TdMapAxis.Vertical;
        private int _draftSeparation = TdMapData.DEFAULT_HEIGHT - 1;

        private Vector2 _scroll;

        #endregion

        #region Initialize

        [MenuItem(MENU_PATH)]
        public static void Open()
        {
            var window = GetWindow<TdMapBuilderWindow>("Map Builder");
            window.minSize = new Vector2(320f, 480f);
            window.Show();
        }

        /// <summary>Mở cửa sổ và chọn sẵn một map.</summary>
        public static void OpenWith(TdMapData map)
        {
            Open();
            TdMapEditSession.Map = map;
        }

        private void OnEnable()
        {
            TdMapEditSession.Changed -= OnSessionChanged;
            TdMapEditSession.Changed += OnSessionChanged;
            PullDraftFromMap();
        }

        private void OnDisable() => TdMapEditSession.Changed -= OnSessionChanged;

        #endregion

        #region Private - Window GUI

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUIUtility.labelWidth = PANEL_LABEL_WIDTH;

            DrawMapSelector();

            if (TdMapEditSession.Map == null)
            {
                EditorGUILayout.HelpBox(
                    "Chọn một Map Data có sẵn hoặc bấm \"Tạo map mới\" để bắt đầu.",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(6f);
            DrawGridSettings();

            EditorGUILayout.Space(6f);
            DrawLayoutPanel();

            EditorGUILayout.Space(6f);
            DrawBrushPanel();

            EditorGUILayout.Space(6f);
            DrawBulkActions();

            EditorGUILayout.Space(6f);
            DrawValidationPanel();

            EditorGUILayout.Space(6f);
            DrawSceneActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawMapSelector()
        {
            EditorGUILayout.LabelField("Map", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var picked = (TdMapData)EditorGUILayout.ObjectField(
                    TdMapEditSession.Map, typeof(TdMapData), false);

                if (picked != TdMapEditSession.Map)
                {
                    TdMapEditSession.Map = picked;
                    PullDraftFromMap();
                }

                if (GUILayout.Button("Tạo map mới", GUILayout.Width(100f))) CreateNewMap();
            }
        }

        private void DrawGridSettings()
        {
            TdMapData map = TdMapEditSession.Map;
            EditorGUILayout.LabelField("Lưới", EditorStyles.boldLabel);

            _draftWidth = EditorGUILayout.IntSlider("Rộng (X)", _draftWidth, TdMapData.MIN_SIZE, TdMapData.MAX_SIZE);
            _draftHeight = EditorGUILayout.IntSlider("Cao (Z)", _draftHeight, TdMapData.MIN_SIZE, TdMapData.MAX_SIZE);
            _draftCellSize = Mathf.Max(TdMapData.MIN_CELL_SIZE, EditorGUILayout.FloatField("Cạnh ô", _draftCellSize));
            _draftOrigin = EditorGUILayout.Vector3Field("Gốc lưới", _draftOrigin);

            bool dirty = _draftWidth != map.Width
                         || _draftHeight != map.Height
                         || !Mathf.Approximately(_draftCellSize, map.CellSize)
                         || _draftOrigin != map.Origin;

            using (new EditorGUI.DisabledScope(!dirty))
            {
                if (GUILayout.Button("Áp dụng kích thước"))
                {
                    TdMapEditSession.Record("Resize TD Map");
                    map.Resize(_draftWidth, _draftHeight);
                    map.SetLayout(_draftCellSize, _draftOrigin);
                    TdMapEditSession.Commit();
                    PullDraftFromMap();
                }
            }

            if (dirty)
            {
                EditorGUILayout.HelpBox(
                    "Thu nhỏ lưới sẽ cắt mất phần ô nằm ngoài. Spawn/Goal tự kẹp vào trong lưới mới.",
                    MessageType.Warning);
            }
        }

        private void DrawLayoutPanel()
        {
            TdMapData map = TdMapEditSession.Map;
            EditorGUILayout.LabelField("Bố trí Spawn / Goal", EditorStyles.boldLabel);

            _draftAxis = (TdMapAxis)EditorGUILayout.EnumPopup("Trục", _draftAxis);
            EditorGUILayout.LabelField(
                _draftAxis == TdMapAxis.Vertical ? "Cùng X, lệch Z (dưới ↔ trên)" : "Cùng Z, lệch X (trái ↔ phải)",
                EditorStyles.miniLabel);

            int maxSeparation = (_draftAxis == TdMapAxis.Vertical ? map.Height : map.Width) - 1;
            _draftSeparation = EditorGUILayout.IntSlider("Cách nhau (ô)", _draftSeparation, 1, maxSeparation);

            if (GUILayout.Button("Đặt lại 2 điểm đối diện"))
            {
                TdMapEditSession.Record("Layout TD Map Endpoints");
                map.ApplyOppositeLayout(_draftAxis, _draftSeparation);
                TdMapEditSession.Commit();
                PullDraftFromMap();
            }

            if (!map.IsOppositeLayout)
            {
                EditorGUILayout.HelpBox(
                    "Spawn/Goal hiện không còn đối diện đúng khoảng cách (do dời tay hoặc đổi kích thước lưới). "
                    + "Bấm \"Đặt lại 2 điểm đối diện\" để căn lại.",
                    MessageType.Warning);
            }
        }

        private void DrawBrushPanel()
        {
            EditorGUILayout.LabelField("Cọ vẽ", EditorStyles.boldLabel);

            TdMapEditSession.PaintEnabled = EditorGUILayout.ToggleLeft(
                "Bật vẽ trong Scene View", TdMapEditSession.PaintEnabled);

            using (new EditorGUI.DisabledScope(!TdMapEditSession.PaintEnabled))
            {
                TdMapEditSession.Target = (TdPaintTarget)GUILayout.SelectionGrid(
                    (int)TdMapEditSession.Target,
                    new[] { "Đường", "Xoá", "Ô chặn" },
                    3,
                    GUILayout.Height(28f));

                TdMapEditSession.Tool = (TdPaintTool)GUILayout.SelectionGrid(
                    (int)TdMapEditSession.Tool,
                    new[] { "Tự do", "Đường thẳng", "Chữ nhật", "Nối chuỗi" },
                    4,
                    GUILayout.Height(28f));

                EditorGUILayout.HelpBox(GetToolHint(), MessageType.None);

                TdMapEditSession.EnforceCorridorWidth = EditorGUILayout.ToggleLeft(
                    "Tuỳ chọn: cấm 2 làn sát nhau (chừa hẳn 1 ô)",
                    TdMapEditSession.EnforceCorridorWidth);
            }

            EditorGUILayout.Space(4f);
            DrawChainAnchor();

            EditorGUILayout.Space(4f);
            DrawEndpointPlacement();
        }

        private void DrawChainAnchor()
        {
            Vector2Int anchor = TdMapEditSession.ChainAnchor;
            int routeLength = TdMapEditSession.Map.Route?.Count ?? 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"Ô đã nhớ: {anchor}   ({routeLength} ô trong chuỗi)",
                    EditorStyles.miniLabel);

                if (GUILayout.Button("Về Spawn", EditorStyles.miniButton, GUILayout.Width(80f)))
                {
                    TdMapEditSession.ResetChainAnchor();
                }
            }
        }

        private static string GetToolHint()
        {
            switch (TdMapEditSession.Tool)
            {
                case TdPaintTool.Line:
                    return "Bấm ở ô đầu, rê tới ô cuối rồi thả. Nối bằng tuyến chữ L, "
                         + "trục lệch nhiều hơn đi trước. Giữ Shift để đổi chiều bẻ góc.";

                case TdPaintTool.Rect:
                    return "Bấm và rê để quét một vùng chữ nhật, thả ra là ghi.";

                case TdPaintTool.Chain:
                    return "Click từng ô: mỗi lần click nối tiếp từ ô đã nhớ lần trước rồi nhớ ô mới. "
                         + "Chưa nối lần nào thì xuất phát từ Spawn. Giữ Shift để đổi chiều bẻ góc.";

                default:
                    return "Giữ chuột trái và rê. Rê nhanh vẫn liền nét — ô bị nhảy được lấp tự động. "
                         + "Giữ Shift để xoá thay vì tô.";
            }
        }

        private void DrawEndpointPlacement()
        {
            TdMapData map = TdMapEditSession.Map;
            EditorGUILayout.LabelField($"Spawn: {map.Spawn}    Goal: {map.Goal}", EditorStyles.miniLabel);

            TdMapEditSession.LockEndpoints = EditorGUILayout.ToggleLeft(
                "Khoá Spawn/Goal (không cho dời)", TdMapEditSession.LockEndpoints);

            using (new EditorGUI.DisabledScope(TdMapEditSession.LockEndpoints))
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPlaceToggle(TdPlaceMode.Spawn, "Đặt lại Spawn");
                DrawPlaceToggle(TdPlaceMode.Goal, "Đặt lại Goal");
            }

            if (TdMapEditSession.PlaceMode != TdPlaceMode.None)
            {
                EditorGUILayout.HelpBox(
                    $"Bấm vào một ô trong Scene View để đặt {TdMapEditSession.PlaceMode}. Đặt xong tự tắt.",
                    MessageType.Info);
            }
        }

        private void DrawPlaceToggle(TdPlaceMode mode, string label)
        {
            bool active = TdMapEditSession.PlaceMode == mode;
            if (GUILayout.Toggle(active, label, EditorStyles.miniButton) == active) return;

            TdMapEditSession.PlaceMode = active ? TdPlaceMode.None : mode;
        }

        private void DrawBulkActions()
        {
            EditorGUILayout.LabelField("Thao tác nhanh", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Phủ Đất xây")) TdMapEditSession.FillAll(TdCellType.Buildable);
                if (GUILayout.Button("Phủ Ô chặn")) TdMapEditSession.FillAll(TdCellType.Blocked);
            }

            if (GUILayout.Button("Xoá hết đường (giữ Spawn/Goal)")) TdMapEditSession.ClearPath();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Nối nhanh chữ L (ngang trước)")) TdMapEditSession.ConnectEndpoints(true);
                if (GUILayout.Button("Nối nhanh chữ L (dọc trước)")) TdMapEditSession.ConnectEndpoints(false);
            }
        }

        private void DrawValidationPanel()
        {
            EditorGUILayout.LabelField("Kiểm tra", EditorStyles.boldLabel);

            TdMapEditSession.ShowRoute = EditorGUILayout.ToggleLeft(
                "Hiện tuyến đường trong Scene", TdMapEditSession.ShowRoute);

            TdMapValidation validation = TdMapEditSession.Validation;
            EditorGUILayout.HelpBox(validation.Message, validation.IsClean ? MessageType.Info : MessageType.Error);
            EditorGUILayout.LabelField(
                $"Ô đường: {validation.PathCellCount}    Lạc: {validation.OrphanCellCount}    Tuyến: {validation.RouteLength} ô",
                EditorStyles.miniLabel);

            if (TdMapEditSession.EnforceCorridorWidth && validation.WideSpotCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{validation.WideSpotCount} ô đường dính thành dải rộng 2 ô (tô đỏ trong Scene View).",
                    MessageType.Warning);
            }
        }

        private void DrawSceneActions()
        {
            EditorGUILayout.LabelField("Dựng hình", EditorStyles.boldLabel);

            if (GUILayout.Button("Dựng lại TdMapRenderer trong scene"))
            {
                int rebuilt = TdMapEditSession.RebuildRenderers();
                ShowNotification(new GUIContent(rebuilt > 0
                    ? $"Đã dựng lại {rebuilt} renderer."
                    : "Không tìm thấy TdMapRenderer nào dùng map này."));
            }

            EditorGUILayout.LabelField(
                "Thêm component TdMapRenderer vào một GameObject rỗng và gán map này.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(6f);
            DrawViewAngle();
        }

        private void DrawViewAngle()
        {
            EditorGUILayout.LabelField("Góc nhìn Scene View", EditorStyles.boldLabel);

            float pitch = EditorGUILayout.Slider(
                "Độ nghiêng",
                TdSceneViewFramer.Pitch,
                TdSceneViewFramer.MIN_PITCH,
                TdSceneViewFramer.MAX_PITCH);

            if (!Mathf.Approximately(pitch, TdSceneViewFramer.Pitch))
            {
                TdSceneViewFramer.Pitch = pitch;
                TdSceneViewFramer.Frame(TdMapEditSession.Map);
            }

            TdSceneViewFramer.AutoFrameOnSceneOpen = EditorGUILayout.ToggleLeft(
                "Tự căn khi mở scene",
                TdSceneViewFramer.AutoFrameOnSceneOpen);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Căn Scene View") && !TdSceneViewFramer.Frame(TdMapEditSession.Map))
                {
                    ShowNotification(new GUIContent("Chưa mở Scene View."));
                }

                if (GUILayout.Button("Căn camera game")
                    && !TdSceneViewFramer.FrameGameCamera(TdMapEditSession.Map))
                {
                    ShowNotification(new GUIContent("Scene không có Main Camera."));
                }
            }

            EditorGUILayout.LabelField("90 = nhìn thẳng từ trên xuống.", EditorStyles.miniLabel);
        }

        #endregion

        #region Private - Map Operations

        private void PullDraftFromMap()
        {
            TdMapData map = TdMapEditSession.Map;
            if (map == null) return;

            _draftWidth = map.Width;
            _draftHeight = map.Height;
            _draftCellSize = map.CellSize;
            _draftOrigin = map.Origin;
            _draftAxis = map.Axis;
            _draftSeparation = map.Separation;
        }

        private void CreateNewMap()
        {
            if (!Directory.Exists(DEFAULT_ASSET_DIR)) Directory.CreateDirectory(DEFAULT_ASSET_DIR);

            string path = EditorUtility.SaveFilePanelInProject(
                "Tạo Map Data",
                "TdMap_New",
                "asset",
                "Chọn nơi lưu map mới",
                DEFAULT_ASSET_DIR);

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<TdMapData>();
            asset.FillAll(TdCellType.Buildable);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            TdMapEditSession.Map = asset;
            PullDraftFromMap();
        }

        private void OnSessionChanged()
        {
            Repaint();
        }

        #endregion
    }
}
