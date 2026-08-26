using Ezg.Feature.HomeBase;
using Ezg.Feature.MapBuilder.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ezg.Feature.HomeBase.EditorTools
{
    /// <summary>
    /// Dựng căn cứ nhà ba vành vào scene đang mở rồi căn camera nhìn trọn nó.
    /// <para>
    /// Căn cứ đứng độc lập, không dính gì tới map thủ thành. Muốn ghép cổng vào điểm cuối của map
    /// thì gán map cho <see cref="TdHomeBaseBuilder"/> rồi bấm nút căn cổng — đó là việc riêng.
    /// </para>
    /// <para>
    /// Chạy lại được nhiều lần: object cũ được dùng lại chứ không nhân bản.
    /// </para>
    /// </summary>
    public static class TdHomeSceneSetup
    {
        #region Constants

        private const string MENU_PATH = "Tools/Tower Defense/Dựng Home Base vào scene";
        private const string MENU_PATH_CAMERA = "Tools/Tower Defense/Căn camera Home Base";
        private const string HOME_BASE_OBJECT = "HomeBase";
        private const string LAYOUT_FILTER = "t:TdHomeBaseLayout";

        /// <summary>Nới thêm quanh khung hình để camera không cắt sát mép.</summary>
        private const float FRAME_PADDING = 1.06f;

        private const float CAMERA_NEAR = 0.3f;
        private const float CAMERA_FAR_RATIO = 6f;
        private const float CAMERA_FAR_MIN = 400f;

        #endregion

        #region Public

        [MenuItem(MENU_PATH)]
        private static void BuildFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();

            if (LoadFirst<TdHomeBaseLayout>(LAYOUT_FILTER) == null)
            {
                EditorUtility.DisplayDialog(
                    "Thiếu cấu hình",
                    "Không tìm thấy asset TdHomeBaseLayout nào trong project.",
                    "OK");
                return;
            }

            // Đây là thao tác sinh ra hàng nghìn object vào scene đang mở — hỏi trước cho chắc.
            bool proceed = EditorUtility.DisplayDialog(
                "Dựng Home Base",
                $"Dựng căn cứ nhà vào scene \"{scene.name}\"?",
                "Dựng",
                "Huỷ");
            if (!proceed) return;

            BuildIntoActiveScene();
        }

        /// <summary>Căn lại camera theo thông số trong layout, khỏi phải dựng lại cả căn cứ.</summary>
        [MenuItem(MENU_PATH_CAMERA)]
        public static void FrameCameraOnly()
        {
            TdHomeBaseBuilder builder = Object.FindFirstObjectByType<TdHomeBaseBuilder>();
            if (builder == null || builder.Layout == null) return;

            FrameCameras(builder);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Dựng vào scene đang mở, không hỏi han. Tách khỏi menu để script khác gọi lại được.
        /// </summary>
        /// <returns><c>false</c> nếu chưa có asset cấu hình nào.</returns>
        public static bool BuildIntoActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();

            TdHomeBaseLayout layout = LoadFirst<TdHomeBaseLayout>(LAYOUT_FILTER);
            if (layout == null) return false;

            TdHomeBaseBuilder builder = SetUpHomeBase(layout);
            SetUpCameraRig(builder);
            FrameCameras(builder);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = builder.gameObject;

            Debug.Log($"[{nameof(TdHomeSceneSetup)}] Đã dựng Home Base vào scene \"{scene.name}\". "
                    + $"Cổng thành ở {builder.GateWorldPosition}.");
            return true;
        }

        #endregion

        #region Private

        /// <summary>Bảo đảm scene có một căn cứ nhà rồi dựng lại hình cho nó.</summary>
        private static TdHomeBaseBuilder SetUpHomeBase(TdHomeBaseLayout layout)
        {
            TdHomeBaseBuilder builder = Object.FindFirstObjectByType<TdHomeBaseBuilder>();
            if (builder == null)
            {
                var go = new GameObject(HOME_BASE_OBJECT);
                Undo.RegisterCreatedObjectUndo(go, "Create Home Base");
                builder = Undo.AddComponent<TdHomeBaseBuilder>(go);
            }

            Undo.RecordObject(builder, "Set up Home Base");
            Undo.RecordObject(builder.transform, "Set up Home Base");

            ApplySerializedReference(builder, "_layout", layout);
            builder.Rebuild();
            return builder;
        }

        /// <summary>
        /// Gắn rig vuốt/thu-phóng lên camera chính và chép thông số từ layout sang.
        /// <para>
        /// Layout là nơi duy nhất ghi góc nhìn; rig chỉ nhận bản sao để lúc chạy khỏi phải
        /// đọc ngược lại asset cấu hình.
        /// </para>
        /// </summary>
        private static TdHomeCameraRig SetUpCameraRig(TdHomeBaseBuilder builder)
        {
            Camera camera = Camera.main;
            if (camera == null) return null;

            var rig = camera.GetComponent<TdHomeCameraRig>();
            if (rig == null) rig = Undo.AddComponent<TdHomeCameraRig>(camera.gameObject);

            TdHomeBaseLayout layout = builder.Layout;
            var serialized = new SerializedObject(rig);
            serialized.FindProperty("_focusAreaSource").objectReferenceValue = builder;
            serialized.FindProperty("_pitch").floatValue = layout.CameraPitch;
            serialized.FindProperty("_fieldOfView").floatValue = layout.CameraFieldOfView;
            serialized.FindProperty("_minViewSpan").floatValue = layout.CameraMinViewSpan;
            serialized.FindProperty("_maxViewSpan").floatValue = layout.ResolveCameraMaxViewSpan();
            serialized.FindProperty("_startViewSpan").floatValue = layout.ResolveCameraViewSpan();
            serialized.FindProperty("_startFocusOffset").vector2Value =
                new Vector2(layout.CameraTarget.x, layout.CameraTarget.z);
            serialized.FindProperty("_panMargin").floatValue = layout.CameraPanMargin;
            serialized.ApplyModifiedProperties();
            return rig;
        }

        /// <summary>Đưa camera game và Scene View về góc nhìn ghi trong layout.</summary>
        private static void FrameCameras(TdHomeBaseBuilder builder)
        {
            TdHomeBaseLayout layout = builder.Layout;
            Vector3 target = builder.transform.TransformPoint(layout.CameraTarget);
            float span = layout.ResolveCameraViewSpan() * FRAME_PADDING;

            ApplyGameCamera(target, span, layout.CameraPitch, layout.CameraFieldOfView);
            TdSceneViewFramer.Frame(target, span);
        }

        /// <summary>
        /// Đặt camera chính sao cho một dải rộng <paramref name="span"/> lọt vừa khung hình.
        /// <para>
        /// Khoảng lùi tính từ góc mở ống kính và tỉ lệ khung hình, chứ không nhân hệ số áng chừng —
        /// đổi FOV hay đổi tỉ lệ màn hình thì khung nhìn vẫn giữ nguyên chừng ấy mét.
        /// </para>
        /// </summary>
        private static void ApplyGameCamera(Vector3 target, float span, float pitch, float fieldOfView)
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            Undo.RecordObject(camera, "Frame home base camera");
            Undo.RecordObject(camera.transform, "Frame home base camera");

            camera.orthographic = false;
            camera.fieldOfView = fieldOfView;
            camera.clearFlags = CameraClearFlags.Skybox;

            float distance = HomeCameraMath.DistanceForSpan(span, fieldOfView, camera.aspect);
            camera.transform.SetPositionAndRotation(
                HomeCameraMath.PositionFor(target, pitch, distance), HomeCameraMath.Rotation(pitch));
            camera.nearClipPlane = CAMERA_NEAR;
            camera.farClipPlane = Mathf.Max(CAMERA_FAR_MIN, distance * CAMERA_FAR_RATIO);

            EditorUtility.SetDirty(camera);
        }

        /// <summary>
        /// Gán một field private có <c>[SerializeField]</c> qua SerializedObject.
        /// Rẻ hơn là bày thêm setter public chỉ để phục vụ một cái menu editor.
        /// </summary>
        private static void ApplySerializedReference(Object target, string fieldName, Object value)
        {
            if (value == null) return;

            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null) return;

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static T LoadFirst<T>(string filter) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            if (guids.Length == 0) return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        #endregion
    }
}
