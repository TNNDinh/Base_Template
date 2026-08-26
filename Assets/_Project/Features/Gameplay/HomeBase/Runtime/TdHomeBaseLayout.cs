using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Cấu hình căn cứ nhà: ba vành đồng tâm — trung tâm, vành farm, tường thành —
    /// cùng bộ prefab dựng nên từng vành.
    /// <para>
    /// Đây là dữ liệu thuần. Việc dựng hình do <see cref="TdHomeBaseBuilder"/> lo, nên đổi số
    /// hay đổi prefab không phải đụng tới code.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "TdHomeBaseLayout_New", menuName = "Tower Defense/Home Base Layout", order = 2)]
    public class TdHomeBaseLayout : ScriptableObject
    {
        #region Fields

        [Title("Bán kính các vành")]
        [Tooltip("Vùng lõi: nhà chính và các công trình xếp tay.")]
        [SerializeField, MinValue(1f)] private float _centerRadius = 38f;

        [Tooltip("Vành farm bắt đầu từ bán kính này.")]
        [SerializeField, MinValue(1f)] private float _farmInnerRadius = 46f;

        [Tooltip("Nửa cạnh của tường thành. Tường là hình vuông nên đây là khoảng từ tâm ra mỗi cạnh.")]
        [SerializeField, MinValue(1f)] private float _wallHalfExtent = 96f;

        [Tooltip("Chừa trống giữa vành farm và tường, để tường không bị nhà đè lên.")]
        [SerializeField, MinValue(0f)] private float _wallClearance = 12f;

        [Title("Cổng thành")]
        [Tooltip("Cạnh nào mở cổng. Cổng chính là điểm cuối của map thủ thành.")]
        [SerializeField] private TdCompass _gateSide = TdCompass.South;

        [Tooltip("Bề rộng lỗ mở trên tường. Để 0 thì lấy đúng bề rộng prefab cổng.")]
        [SerializeField, MinValue(0f)] private float _gateWidth;

        [SerializeField] private GameObject _gatePrefab;
        [SerializeField] private GameObject _wallSegmentPrefab;
        [SerializeField] private GameObject _wallPostPrefab;

        [Tooltip("Cộng thêm vào góc xoay của mảnh tường, dùng khi prefab quay sai chiều.")]
        [SerializeField] private float _wallYawOffset;

        [Tooltip("Phóng to mảnh tường. Hàng rào gốc chỉ cao hơn 1m nên phải nống lên mới ra dáng tường thành.")]
        [SerializeField, MinValue(0.01f)] private float _wallScale = 1f;

        [Title("Nền")]
        [SerializeField] private GameObject _groundPrefab;

        [Tooltip("Phóng to miếng nền. Nền là mặt phẳng màu trơn nên phóng lên không lộ, "
               + "mà số object rải ra giảm theo bình phương.")]
        [SerializeField, MinValue(0.01f)] private float _groundScale = 1f;

        [Tooltip("Nền lan ra ngoài tường bao nhiêu.")]
        [SerializeField, MinValue(0f)] private float _groundPadding = 18f;

        [Title("Đường vào")]
        [Tooltip("Mảnh đường thẳng nối từ cổng vào tới lõi. Để trống thì không dựng đường.")]
        [SerializeField] private GameObject _avenuePrefab;

        [Tooltip("Nhấc đường lên khỏi nền để không bị chọi mặt (z-fighting).")]
        [SerializeField, MinValue(0f)] private float _avenueLift = 0.03f;

        [Tooltip("Bề rộng hành lang chừa trống hai bên đường vào.")]
        [SerializeField, MinValue(0f)] private float _avenueClearance = 16f;

        [Title("Camera")]
        [Tooltip("Bề ngang vùng lọt khung hình, tính bằng mét. Càng nhỏ camera càng sà xuống gần. "
               + "Để 0 thì lùi ra nhìn trọn cả căn cứ.")]
        [SerializeField, MinValue(0f)] private float _cameraViewSpan;

        [Tooltip("Độ nghiêng camera. 90 là nhìn thẳng từ trên xuống; game mobile thường 45–55.")]
        [SerializeField, Range(20f, 89f)] private float _cameraPitch = 50f;

        [Tooltip("Góc mở ống kính. Nhỏ thì hình dẹt lại, đỡ méo — hợp góc nhìn xây nhà.")]
        [SerializeField, Range(15f, 80f)] private float _cameraFieldOfView = 40f;

        [Tooltip("Điểm camera ngắm lệch khỏi tâm căn cứ, trên mặt phẳng ngang.")]
        [SerializeField] private Vector2 _cameraTargetOffset;

        [Tooltip("Bề ngang lọt khung khi người chơi phóng sát nhất.")]
        [SerializeField, MinValue(1f)] private float _cameraMinViewSpan = 45f;

        [Tooltip("Bề ngang lọt khung khi lùi xa nhất. Để 0 thì lấy trọn cả nền.")]
        [SerializeField, MinValue(0f)] private float _cameraMaxViewSpan;

        [Tooltip("Cho lia camera ra ngoài rìa tường thêm ngần này mét.")]
        [SerializeField, MinValue(0f)] private float _cameraPanMargin = 12f;

        [Title("Nội dung")]
        [Tooltip("Công trình lõi, xếp tay theo toạ độ.")]
        [SerializeField] private TdHomePlacement[] _centerPieces = new TdHomePlacement[0];

        [Tooltip("Vật thể rải ngẫu nhiên trong vành farm.")]
        [SerializeField] private TdHomeScatterEntry[] _farmScatter = new TdHomeScatterEntry[0];

        [Tooltip("Số vật thể muốn rải trong vành farm.")]
        [SerializeField, MinValue(0)] private int _farmItemCount = 110;

        [Tooltip("Số lần thử chỗ cho mỗi vật thể trước khi bỏ qua.")]
        [SerializeField, MinValue(1)] private int _placementAttempts = 24;

        [Tooltip("Đổi số này là ra bố cục farm khác, nhưng vẫn lặp lại y hệt mỗi lần dựng.")]
        [SerializeField] private int _seed = 20260823;

        #endregion

        #region Public - Properties

        public float CenterRadius => _centerRadius;

        public float FarmInnerRadius => Mathf.Max(_farmInnerRadius, _centerRadius);

        /// <summary>Vành farm dừng lại trước tường một khoảng, để tường luôn thoáng.</summary>
        public float FarmOuterRadius => Mathf.Max(FarmInnerRadius, _wallHalfExtent - _wallClearance);

        public float WallHalfExtent => _wallHalfExtent;

        public TdCompass GateSide => _gateSide;

        public GameObject GatePrefab => _gatePrefab;

        public GameObject WallSegmentPrefab => _wallSegmentPrefab;

        public GameObject WallPostPrefab => _wallPostPrefab;

        public float WallYawOffset => _wallYawOffset;

        public float WallScale => _wallScale;

        public GameObject GroundPrefab => _groundPrefab;

        public float GroundScale => _groundScale;

        public float GroundPadding => _groundPadding;

        public GameObject AvenuePrefab => _avenuePrefab;

        public float AvenueLift => _avenueLift;

        public float AvenueClearance => _avenueClearance;

        public float CameraPitch => _cameraPitch;

        public float CameraFieldOfView => _cameraFieldOfView;

        public float CameraMinViewSpan => _cameraMinViewSpan;

        public float CameraPanMargin => _cameraPanMargin;

        /// <summary>Điểm camera ngắm, so với tâm căn cứ.</summary>
        public Vector3 CameraTarget => new Vector3(_cameraTargetOffset.x, 0f, _cameraTargetOffset.y);

        public TdHomePlacement[] CenterPieces => _centerPieces;

        public TdHomeScatterEntry[] FarmScatter => _farmScatter;

        public int FarmItemCount => _farmItemCount;

        public int PlacementAttempts => _placementAttempts;

        public int Seed => _seed;

        /// <summary>Vị trí cổng so với tâm căn cứ.</summary>
        public Vector3 GateLocalPosition => _gateSide.ToDirection() * _wallHalfExtent;

        #endregion

        #region Public

        /// <summary>
        /// Bề ngang vùng cần lọt khung. Không đặt thì lấy trọn cả nền, kể cả vành cỏ ngoài tường.
        /// </summary>
        public float ResolveCameraViewSpan()
        {
            if (_cameraViewSpan > 0f) return _cameraViewSpan;
            return WholeBaseSpan;
        }

        /// <summary>Bề ngang tối đa người chơi lùi ra được. Không đặt thì cho nhìn trọn căn cứ.</summary>
        public float ResolveCameraMaxViewSpan()
        {
            float span = _cameraMaxViewSpan > 0f ? _cameraMaxViewSpan : WholeBaseSpan;
            return Mathf.Max(_cameraMinViewSpan, span);
        }

        /// <summary>
        /// Bề rộng lỗ mở trên tường. Ưu tiên số đặt tay, không có thì đo từ prefab cổng.
        /// </summary>
        public float ResolveGateWidth()
        {
            if (_gateWidth > 0f) return _gateWidth;

            Vector2 size = MapBuilder.PrefabBoundsUtility.GetFootprint(_gatePrefab);
            float measured = Mathf.Max(size.x, size.y) * _wallScale;
            return measured > 0f ? measured : _avenueClearance;
        }

        #endregion

        #region Private

        /// <summary>Bề ngang trọn cả nền, kể cả vành cỏ ngoài tường.</summary>
        private float WholeBaseSpan => (_wallHalfExtent + _groundPadding) * 2f;

        #endregion
    }
}
