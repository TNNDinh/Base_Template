using Ezg.Feature.MapBuilder;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Ezg.Feature.MapBuilder.EditorTools
{
    /// <summary>
    /// Bảng công cụ dựng map nằm ngay trong Scene View — không phải mở cửa sổ riêng.
    /// Hiện/ẩn qua menu overlay của Scene View (dấu ☰ góc trên trái) hoặc phím <c>`</c>.
    /// </summary>
    [Overlay(typeof(SceneView), OVERLAY_ID, "TD Map Builder", defaultDisplay = true)]
    public class TdMapPaintOverlay : IMGUIOverlay
    {
        #region Constants

        private const string OVERLAY_ID = "td-map-builder-overlay";
        private const float PANEL_WIDTH = 250f;

        #endregion

        #region Public

        public override void OnGUI()
        {
            // Scene không phải scene dựng map thì overlay không được chiếm chỗ và không được
            // để lộ nút nào — bấm nhầm sẽ sửa map của scene khác.
            if (!TdMapEditSession.HasSceneContext)
            {
                EditorGUILayout.LabelField(
                    "Scene này không có TdMapRenderer.",
                    EditorStyles.miniLabel,
                    GUILayout.Width(PANEL_WIDTH));
                return;
            }

            TdMapData map = TdMapEditSession.Map;

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(PANEL_WIDTH)))
            {
                if (map == null)
                {
                    EditorGUILayout.HelpBox(
                        "TdMapRenderer trong scene chưa gán Map Data.",
                        MessageType.Info);

                    if (GUILayout.Button("Mở cửa sổ Map Builder")) TdMapBuilderWindow.Open();
                    return;
                }

                DrawPaintToggle();
                EditorGUILayout.Space(2f);
                DrawTools();
                EditorGUILayout.Space(2f);
                DrawQuickActions(map);
                EditorGUILayout.Space(2f);
                DrawUnitPreview();
                EditorGUILayout.Space(2f);
                DrawStatus(map);
            }
        }

        #endregion

        #region Private

        private void DrawPaintToggle()
        {
            bool painting = TdMapEditSession.PaintEnabled;

            var color = GUI.backgroundColor;
            GUI.backgroundColor = painting ? new Color(0.4f, 0.9f, 0.45f) : color;

            if (GUILayout.Button(painting ? "● ĐANG VẼ — bấm để tắt" : "○ Vẽ đang TẮT — bấm để bật",
                    GUILayout.Height(26f)))
            {
                TdMapEditSession.PaintEnabled = !painting;
            }

            GUI.backgroundColor = color;

            if (painting)
            {
                EditorGUILayout.LabelField(
                    "Click trong Scene View đang dùng để vẽ, không chọn được object.",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawTools()
        {
            using (new EditorGUI.DisabledScope(!TdMapEditSession.PaintEnabled))
            {
                TdMapEditSession.Target = (TdPaintTarget)GUILayout.SelectionGrid(
                    (int)TdMapEditSession.Target,
                    new[] { "Đường", "Xoá", "Chặn" },
                    3,
                    GUILayout.Height(24f));

                TdMapEditSession.Tool = (TdPaintTool)GUILayout.SelectionGrid(
                    (int)TdMapEditSession.Tool,
                    new[] { "Tự do", "Thẳng", "C.nhật", "Chuỗi" },
                    4,
                    GUILayout.Height(24f));

                EditorGUILayout.LabelField(GetToolHint(), EditorStyles.miniLabel);

                TdMapEditSession.EnforceCorridorWidth = EditorGUILayout.ToggleLeft(
                    "Cấm 2 làn sát nhau (tuỳ chọn)",
                    TdMapEditSession.EnforceCorridorWidth);
            }
        }

        private static string GetToolHint()
        {
            switch (TdMapEditSession.Tool)
            {
                case TdPaintTool.Line: return "Bấm ô đầu → rê → thả. Shift đổi chiều bẻ góc.";
                case TdPaintTool.Rect: return "Bấm và rê để quét một vùng.";
                case TdPaintTool.Chain: return "Click nối tiếp từ ô đã nhớ. Shift đổi chiều bẻ góc.";
                default: return "Rê để vẽ. Shift để xoá.";
            }
        }

        private void DrawQuickActions(TdMapData map)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Nối L ngang")) TdMapEditSession.ConnectEndpoints(true);
                if (GUILayout.Button("Nối L dọc")) TdMapEditSession.ConnectEndpoints(false);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Xoá đường")) TdMapEditSession.ClearPath();
                if (GUILayout.Button("Dựng hình")) TdMapEditSession.RebuildRenderers();
            }
        }

        /// <summary>Nút chạy thử lính ngay trong Scene View, không cần vào Play Mode.</summary>
        private void DrawUnitPreview()
        {
            EditorGUILayout.LabelField("Chạy thử lính", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var color = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);

                if (GUILayout.Button("▶ Spawn lính", GUILayout.Height(26f))
                    && !TdUnitPreviewDriver.SpawnOne())
                {
                    Debug.LogWarning("Chưa spawn được lính: map chưa gán hoặc đường Spawn → Goal chưa thông.");
                }

                GUI.backgroundColor = color;

                if (GUILayout.Button("Xoá lính", GUILayout.Height(26f), GUILayout.Width(70f)))
                {
                    TdUnitPreviewDriver.DespawnAll();
                }
            }

            int alive = TdUnitPreviewDriver.AliveCount;
            EditorGUILayout.LabelField(
                alive > 0 ? $"Đang chạy: {alive} lính" : "Chưa có lính nào",
                EditorStyles.miniLabel);
        }

        private void DrawStatus(TdMapData map)
        {
            TdMapValidation validation = TdMapEditSession.Validation;

            EditorGUILayout.LabelField(
                $"Spawn {map.Spawn} → Goal {map.Goal}",
                EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"Ô đã nhớ: {TdMapEditSession.ChainAnchor}",
                    EditorStyles.miniLabel);

                if (GUILayout.Button("Về Spawn", EditorStyles.miniButton, GUILayout.Width(70f)))
                {
                    TdMapEditSession.ResetChainAnchor();
                }
            }

            string status = validation.IsValid
                ? $"Đường thông — {validation.RouteLength} ô. Lạc: {validation.OrphanCellCount}."
                : validation.Message;

            bool enforcing = TdMapEditSession.EnforceCorridorWidth;
            if (enforcing && validation.WideSpotCount > 0)
            {
                status += $"\n{validation.WideSpotCount} ô đường dính nhau (tô đỏ trong scene).";
            }

            bool ok = enforcing ? validation.IsClean : validation.IsValid;
            EditorGUILayout.HelpBox(status, ok ? MessageType.Info : MessageType.Warning);

            if (GUILayout.Button("Mở cửa sổ Map Builder (đủ tuỳ chọn)")) TdMapBuilderWindow.Open();
        }

        #endregion
    }
}
