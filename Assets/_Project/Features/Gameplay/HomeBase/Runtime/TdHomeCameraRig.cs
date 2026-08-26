using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Camera màn nhà: vuốt để lia, chụm hai ngón để thu/phóng, có quán tính và có giới hạn
    /// không cho lia ra khỏi căn cứ.
    /// <para>
    /// Lia theo kiểu "dính tay": điểm mặt đất nằm dưới ngón lúc đặt xuống sẽ bám theo ngón,
    /// nên vuốt bao xa là cảnh trôi đúng bấy nhiêu, không phụ thuộc camera đang cao hay thấp.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class TdHomeCameraRig : MonoBehaviour
    {
        #region Constants

        /// <summary>Vuốt ngắn hơn ngần này pixel thì coi như tay đứng yên, khỏi sinh quán tính rác.</summary>
        private const float PAN_DEAD_ZONE = 0.5f;

        /// <summary>Chặn quán tính lại khi đã chậm tới mức này, để camera dừng hẳn.</summary>
        private const float MOMENTUM_CUTOFF = 0.05f;

        #endregion

        #region Fields

        [Title("Vùng được phép lia")]
        [Tooltip("Object cấp vùng giới hạn — phải cài ICameraFocusArea. Để trống thì tự tìm trong scene.")]
        [SerializeField] private MonoBehaviour _focusAreaSource;

        [Tooltip("Cho lia ra ngoài rìa vùng thêm ngần này mét.")]
        [SerializeField, MinValue(0f)] private float _panMargin = 12f;

        [Title("Góc nhìn")]
        [SerializeField, Range(20f, 89f)] private float _pitch = 50f;
        [SerializeField, Range(15f, 80f)] private float _fieldOfView = 40f;

        [Title("Thu phóng")]
        [Tooltip("Bề ngang vùng lọt khung khi phóng sát nhất.")]
        [SerializeField, MinValue(1f)] private float _minViewSpan = 45f;

        [Tooltip("Bề ngang vùng lọt khung khi lùi xa nhất.")]
        [SerializeField, MinValue(1f)] private float _maxViewSpan = 180f;

        [Tooltip("Bề ngang lúc mới vào màn.")]
        [SerializeField, MinValue(1f)] private float _startViewSpan = 105f;

        [Tooltip("Lúc mới vào màn thì ngắm lệch khỏi tâm vùng bao nhiêu, trên mặt phẳng ngang.")]
        [SerializeField] private Vector2 _startFocusOffset;

        [Tooltip("Chụm 1 pixel thì đổi bao nhiêu phần khung nhìn. Thu phóng theo cấp số nhân "
               + "nên phóng sát hay lùi xa đều nhạy như nhau.")]
        [SerializeField, MinValue(0f)] private float _zoomSensitivity = 0.0035f;

        [Title("Độ mượt")]
        [SerializeField, MinValue(0f)] private float _panSmoothTime = 0.08f;
        [SerializeField, MinValue(0f)] private float _zoomSmoothTime = 0.12f;

        [Tooltip("Quán tính sau khi nhấc tay. 0 là dừng khựng ngay.")]
        [SerializeField, Range(0f, 1f)] private float _momentum = 0.85f;

        [Title("Nhập liệu")]
        [Tooltip("Bỏ qua cử chỉ bắt đầu trên UI, để bấm nút không kéo cả cảnh theo.")]
        [SerializeField] private bool _ignoreGesturesOverUi = true;

        private Camera _camera;
        private ICameraFocusArea _area;
        private IHomeCameraGestureSource _gestures;

        private Vector3 _focus;
        private Vector3 _focusTarget;
        private Vector3 _focusVelocity;
        private Vector3 _panVelocityPerSecond;

        private float _viewSpan;
        private float _viewSpanTarget;
        private float _viewSpanVelocity;

        #endregion

        #region Public - Properties

        /// <summary>Điểm mặt đất camera đang ngắm.</summary>
        public Vector3 Focus => _focus;

        /// <summary>Bề ngang vùng đang lọt khung, tính bằng mét.</summary>
        public float ViewSpan => _viewSpan;

        #endregion

        #region Public

        /// <summary>Đổi nguồn cử chỉ. Dùng để cắm nguồn giả lập khi test.</summary>
        public void SetGestureSource(IHomeCameraGestureSource source) => _gestures = source;

        /// <summary>Đổi vùng giới hạn lúc chạy.</summary>
        public void SetFocusArea(ICameraFocusArea area)
        {
            _area = area;
            SnapTo(AreaCenter + new Vector3(_startFocusOffset.x, 0f, _startFocusOffset.y), _viewSpanTarget);
        }

        /// <summary>Nhảy thẳng tới góc nhìn mới, không trượt mượt.</summary>
        public void SnapTo(Vector3 focus, float viewSpan)
        {
            _viewSpanTarget = Mathf.Clamp(viewSpan, _minViewSpan, MaxSpan);
            _viewSpan = _viewSpanTarget;
            _viewSpanVelocity = 0f;

            _focusTarget = ClampFocus(focus, _viewSpanTarget);
            _focus = _focusTarget;
            _focusVelocity = Vector3.zero;
            _panVelocityPerSecond = Vector3.zero;

            ApplyToCamera();
        }

        /// <summary>Đưa camera về góc nhìn mặc định lúc mới vào màn.</summary>
        [Button]
        public void ResetView()
        {
            SnapTo(AreaCenter + new Vector3(_startFocusOffset.x, 0f, _startFocusOffset.y), _startViewSpan);
        }

        #endregion

        #region Initialize

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _area = ResolveArea();
            _gestures ??= BuildDefaultGestureSource();
        }

        private void OnEnable() => ResetView();

        private IHomeCameraGestureSource BuildDefaultGestureSource()
        {
            var source = new CompositeGestureSource(new TouchGestureSource(), new PointerGestureSource());
            return _ignoreGesturesOverUi ? new UiBlockingGestureSource(source) : (IHomeCameraGestureSource)source;
        }

        #endregion

        #region Private

        private void LateUpdate()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f) return;

            HomeCameraGesture gesture = _gestures.Read();

            ApplyZoom(gesture);
            ApplyPan(gesture, deltaTime);

            _focusTarget = ClampFocus(_focusTarget, _viewSpanTarget);

            _viewSpan = Mathf.SmoothDamp(_viewSpan, _viewSpanTarget, ref _viewSpanVelocity, _zoomSmoothTime, Mathf.Infinity, deltaTime);
            _focus = Vector3.SmoothDamp(_focus, _focusTarget, ref _focusVelocity, _panSmoothTime, Mathf.Infinity, deltaTime);

            ApplyToCamera();
        }

        /// <summary>
        /// Thu phóng quanh chính điểm đang chụm: chỗ mặt đất dưới hai ngón phải đứng yên,
        /// nếu không cảnh sẽ trượt đi mỗi lần phóng.
        /// </summary>
        private void ApplyZoom(HomeCameraGesture gesture)
        {
            if (Mathf.Approximately(gesture.ZoomPixels, 0f)) return;

            bool hasAnchor = TryGroundPoint(gesture.PointerPosition, _focusTarget, _viewSpanTarget, out Vector3 before);

            float scale = Mathf.Exp(-gesture.ZoomPixels * _zoomSensitivity);
            _viewSpanTarget = Mathf.Clamp(_viewSpanTarget * scale, _minViewSpan, MaxSpan);

            if (!hasAnchor) return;
            if (!TryGroundPoint(gesture.PointerPosition, _focusTarget, _viewSpanTarget, out Vector3 after)) return;

            _focusTarget += before - after;
        }

        private void ApplyPan(HomeCameraGesture gesture, float deltaTime)
        {
            if (gesture.PanPixels.sqrMagnitude > PAN_DEAD_ZONE * PAN_DEAD_ZONE)
            {
                Vector3 shift = GroundShiftFor(gesture);
                _focusTarget -= shift;
                _panVelocityPerSecond = -shift / deltaTime;
                return;
            }

            if (gesture.IsActive)
            {
                // Ngón còn đặt trên màn nhưng đứng yên: giữ nguyên, không trôi.
                _panVelocityPerSecond = Vector3.zero;
                return;
            }

            if (_panVelocityPerSecond.sqrMagnitude < MOMENTUM_CUTOFF * MOMENTUM_CUTOFF)
            {
                _panVelocityPerSecond = Vector3.zero;
                return;
            }

            _focusTarget += _panVelocityPerSecond * deltaTime;
            _panVelocityPerSecond *= Mathf.Pow(1f - _momentum, deltaTime);
        }

        /// <summary>Quãng mặt đất trôi qua dưới ngón tay trong khung hình này.</summary>
        private Vector3 GroundShiftFor(HomeCameraGesture gesture)
        {
            Vector2 current = gesture.PointerPosition;
            Vector2 previous = current - gesture.PanPixels;

            if (!TryGroundPoint(current, _focusTarget, _viewSpanTarget, out Vector3 now)) return Vector3.zero;
            if (!TryGroundPoint(previous, _focusTarget, _viewSpanTarget, out Vector3 then)) return Vector3.zero;

            return now - then;
        }

        private bool TryGroundPoint(Vector2 screenPoint, Vector3 focus, float viewSpan, out Vector3 groundPoint)
        {
            float aspect = _camera.aspect;
            float distance = HomeCameraMath.DistanceForSpan(viewSpan, _fieldOfView, aspect);
            Vector3 cameraPosition = HomeCameraMath.PositionFor(focus, _pitch, distance);
            var screenSize = new Vector2(_camera.pixelWidth, _camera.pixelHeight);

            return HomeCameraMath.TryGroundPoint(
                screenPoint, screenSize, cameraPosition, _pitch, _fieldOfView, aspect, GroundHeight, out groundPoint);
        }

        private void ApplyToCamera()
        {
            float distance = HomeCameraMath.DistanceForSpan(_viewSpan, _fieldOfView, _camera.aspect);

            _camera.orthographic = false;
            _camera.fieldOfView = _fieldOfView;
            _camera.transform.SetPositionAndRotation(
                HomeCameraMath.PositionFor(_focus, _pitch, distance),
                HomeCameraMath.Rotation(_pitch));
        }

        /// <summary>Giữ tâm ngắm trong vùng cho phép, và càng lùi xa thì càng ít chỗ để lia.</summary>
        private Vector3 ClampFocus(Vector3 focus, float viewSpan)
        {
            Vector3 center = AreaCenter;
            float limit = Mathf.Max(0f, AreaHalfExtent + _panMargin - viewSpan * 0.5f);

            return new Vector3(
                Mathf.Clamp(focus.x, center.x - limit, center.x + limit),
                center.y,
                Mathf.Clamp(focus.z, center.z - limit, center.z + limit));
        }

        private ICameraFocusArea ResolveArea()
        {
            if (_focusAreaSource is ICameraFocusArea assigned) return assigned;

            foreach (var candidate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidate is ICameraFocusArea found) return found;
            }

            return null;
        }

        private Vector3 AreaCenter => _area?.AreaCenter ?? Vector3.zero;

        private float AreaHalfExtent => _area?.AreaHalfExtent ?? _maxViewSpan * 0.5f;

        private float GroundHeight => AreaCenter.y;

        /// <summary>Không cho lùi xa quá mức tối thiểu, kể cả khi số cấu hình bị đặt ngược.</summary>
        private float MaxSpan => Mathf.Max(_minViewSpan, _maxViewSpan);

        #endregion

        #region Events

        private void OnValidate()
        {
            if (_focusAreaSource != null && _focusAreaSource is not ICameraFocusArea)
            {
                Debug.LogWarning(
                    $"[{nameof(TdHomeCameraRig)}] {_focusAreaSource.GetType().Name} không cài "
                    + $"{nameof(ICameraFocusArea)}, bỏ qua. Gán object có cài giao diện này vào.", this);
                _focusAreaSource = null;
            }

            _maxViewSpan = Mathf.Max(_minViewSpan, _maxViewSpan);
            _startViewSpan = Mathf.Clamp(_startViewSpan, _minViewSpan, _maxViewSpan);
        }

        #endregion
    }
}
