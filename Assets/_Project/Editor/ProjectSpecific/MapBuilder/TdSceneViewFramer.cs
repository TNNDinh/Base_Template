using Ezg.Feature.MapBuilder;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ezg.Feature.MapBuilder.EditorTools
{
    /// <summary>
    /// Căn Scene View về góc hơi top-down nhìn xuống map.
    /// <para>
    /// Unity KHÔNG lưu vị trí camera Scene View vào file scene, nên mỗi lần mở scene chứa
    /// <see cref="TdMapRenderer"/> lớp này tự căn lại — khỏi phải xoay tay, và tự thoát chế độ 2D
    /// (ở 2D mode Scene View ép rotation về 0 nên không nhìn nghiêng được).
    /// </para>
    /// </summary>
    [InitializeOnLoad]
    public static class TdSceneViewFramer
    {
        #region Constants

        private const string PREF_PITCH = "TdMapBuilder.ScenePitch";
        private const string PREF_AUTO_FRAME = "TdMapBuilder.AutoFrame";

        public const float DEFAULT_PITCH = 55f;
        public const float MIN_PITCH = 25f;
        public const float MAX_PITCH = 89f;

        /// <summary>Hệ số khoảng cách so với cạnh dài của map — càng nhỏ càng zoom sát.</summary>
        private const float ZOOM_RATIO = 0.62f;

        /// <summary>Số tick thử lại nếu Scene View hoặc map chưa sẵn sàng ngay sau khi mở scene.</summary>
        private const int RETRY_TICKS = 10;

        /// <summary>Khoảng cách camera game so với cạnh dài của map.</summary>
        private const float GAME_CAMERA_DISTANCE_RATIO = 1.15f;

        private const float GAME_CAMERA_NEAR = 0.3f;
        private const float GAME_CAMERA_FAR_RATIO = 4f;
        private const float GAME_CAMERA_FAR_MIN = 500f;

        #endregion

        #region Initialize

        static TdSceneViewFramer() => Subscribe();

        /// <summary>
        /// Đăng ký lại sau mỗi lần domain reload. Dùng kèm static ctor vì static ctor chỉ chạy khi
        /// có ai đó chạm vào class, còn attribute này thì Unity gọi thẳng sau khi load xong.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Subscribe()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        #endregion

        #region Public - Properties

        /// <summary>Độ nghiêng camera, tính bằng độ. 90 = nhìn thẳng từ trên xuống.</summary>
        public static float Pitch
        {
            get => Mathf.Clamp(EditorPrefs.GetFloat(PREF_PITCH, DEFAULT_PITCH), MIN_PITCH, MAX_PITCH);
            set => EditorPrefs.SetFloat(PREF_PITCH, Mathf.Clamp(value, MIN_PITCH, MAX_PITCH));
        }

        /// <summary>Có tự căn Scene View khi mở scene có map không.</summary>
        public static bool AutoFrameOnSceneOpen
        {
            get => EditorPrefs.GetBool(PREF_AUTO_FRAME, true);
            set => EditorPrefs.SetBool(PREF_AUTO_FRAME, value);
        }

        #endregion

        #region Public

        /// <summary>
        /// Đưa Scene View về đúng góc nhìn map. Trả về <c>false</c> nếu chưa có map hoặc chưa mở Scene View.
        /// </summary>
        public static bool Frame(TdMapData map)
        {
            if (map == null) return false;

            Vector3 center = map.Origin + new Vector3(map.WorldSize.x * 0.5f, 0f, map.WorldSize.y * 0.5f);
            float span = Mathf.Max(map.WorldSize.x, map.WorldSize.y);
            return Frame(center, span);
        }

        /// <summary>Đưa Scene View về nhìn xuống một vùng bất kỳ.</summary>
        public static bool Frame(Vector3 center, float span)
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) return false;

            // Phải tắt 2D trước: ở 2D mode Scene View khoá rotation về identity.
            view.in2DMode = false;
            view.orthographic = false;
            view.LookAt(center, Quaternion.Euler(Pitch, 0f, 0f), span * ZOOM_RATIO, false, true);
            view.Repaint();
            return true;
        }

        /// <summary>
        /// Căn camera chính trong scene về khung nhìn map, cùng góc nghiêng với Scene View.
        /// <para>
        /// Đổi CellSize là kích thước world đổi theo, camera đặt tay sẽ lệch ngay — nên để
        /// tính lại thay vì chỉnh số bằng tay mỗi lần.
        /// </para>
        /// </summary>
        /// <returns><c>false</c> nếu chưa có map hoặc scene không có camera chính.</returns>
        public static bool FrameGameCamera(TdMapData map)
        {
            if (map == null) return false;

            Vector3 center = map.Origin + new Vector3(map.WorldSize.x * 0.5f, 0f, map.WorldSize.y * 0.5f);
            float span = Mathf.Max(map.WorldSize.x, map.WorldSize.y);
            return FrameGameCamera(center, span);
        }

        /// <summary>
        /// Căn camera chính vào một vùng bất kỳ. Dùng khi khung hình phải ôm cả map lẫn căn cứ nhà.
        /// </summary>
        /// <param name="center">Tâm vùng cần nhìn.</param>
        /// <param name="span">Cạnh dài nhất của vùng.</param>
        public static bool FrameGameCamera(Vector3 center, float span)
        {
            Camera camera = Camera.main;
            if (camera == null) return false;

            var rotation = Quaternion.Euler(Pitch, 0f, 0f);
            Vector3 position = center - rotation * Vector3.forward * (span * GAME_CAMERA_DISTANCE_RATIO);

            Undo.RecordObject(camera.transform, "Frame TD Map Camera");
            Undo.RecordObject(camera, "Frame TD Map Camera");

            camera.transform.SetPositionAndRotation(position, rotation);
            camera.nearClipPlane = GAME_CAMERA_NEAR;
            camera.farClipPlane = Mathf.Max(GAME_CAMERA_FAR_MIN, span * GAME_CAMERA_FAR_RATIO);

            EditorUtility.SetDirty(camera);
            return true;
        }

        /// <summary>Căn theo map của <see cref="TdMapRenderer"/> đầu tiên tìm thấy trong scene đang mở.</summary>
        public static bool FrameActiveScene()
        {
            TdMapData map = FindMapInLoadedScenes();
            return Frame(map);
        }

        #endregion

        #region Private

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!AutoFrameOnSceneOpen) return;
            if (mode != OpenSceneMode.Single) return;

            // Căn ngay trong chính callback: lúc này scene đã load xong nên hầu như luôn thành công.
            // KHÔNG dựa vào delayCall làm đường chính — delayCall đứng im khi cửa sổ Unity mất focus.
            if (FrameActiveScene()) return;

            // Dự phòng: Scene View chưa mở/chưa sẵn sàng thì thử lại ở các tick sau.
            ScheduleFrame(RETRY_TICKS);
        }

        private static void ScheduleFrame(int retriesLeft)
        {
            EditorApplication.delayCall += () =>
            {
                if (FrameActiveScene()) return;
                if (retriesLeft > 0) ScheduleFrame(retriesLeft - 1);
            };
        }

        private static TdMapData FindMapInLoadedScenes()
        {
            var renderers = Object.FindObjectsByType<TdMapRenderer>(FindObjectsSortMode.None);
            foreach (TdMapRenderer renderer in renderers)
            {
                if (renderer.Map != null) return renderer.Map;
            }

            return null;
        }

        #endregion
    }
}
