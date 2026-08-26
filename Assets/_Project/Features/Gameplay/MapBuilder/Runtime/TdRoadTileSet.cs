using UnityEngine;

namespace Ezg.Feature.MapBuilder
{
    /// <summary>
    /// Bộ mảnh ghép để dựng đường từ prefab thật thay vì khối primitive.
    /// <para>
    /// Bộ đường modular không có một mảnh dùng chung cho mọi ô: mỗi ô phải chọn hình dạng
    /// theo số hàng xóm rồi xoay cho khớp. Lớp này giữ prefab của từng hình dạng, cùng góc
    /// xoay bù cho trường hợp prefab được tạo theo hướng khác quy ước.
    /// </para>
    /// <para>
    /// Quy ước hướng gốc, khi góc bù bằng 0:
    /// <list type="bullet">
    /// <item>Thẳng: nối Bắc–Nam (dọc trục Z)</item>
    /// <item>Góc: nối Đông và Nam</item>
    /// <item>Chữ T: nối Đông, Tây, Nam (thiếu Bắc)</item>
    /// <item>Ngã tư: nối cả bốn hướng</item>
    /// <item>Cụt: nối Nam</item>
    /// </list>
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "TdRoadTileSet_New", menuName = "Tower Defense/Road Tile Set", order = 1)]
    public class TdRoadTileSet : ScriptableObject
    {
        #region Constants

        /// <summary>Bước lưới mặc định, đo từ prefab đường của Synty Farm.</summary>
        public const float DEFAULT_TILE_PITCH = 11.91f;

        #endregion

        #region Fields

        [Header("Kích thước")]
        [Tooltip("Cạnh một ô của bộ prefab này. Nên đặt CellSize của map bằng đúng giá trị đây.")]
        [SerializeField] private float _tilePitch = DEFAULT_TILE_PITCH;

        [Header("Mảnh đường")]
        [Tooltip("Nối Bắc–Nam.")]
        [SerializeField] private GameObject _straight;

        [Tooltip("Nối Đông và Nam.")]
        [SerializeField] private GameObject _corner;

        [Tooltip("Nối Đông, Tây, Nam.")]
        [SerializeField] private GameObject _tSection;

        [Tooltip("Nối cả bốn hướng.")]
        [SerializeField] private GameObject _intersection;

        [Tooltip("Chỉ nối Nam. Dùng cho ô đường đứng một mình hoặc đầu mút.")]
        [SerializeField] private GameObject _end;

        [Header("Góc xoay bù (độ)")]
        [Tooltip("Cộng thêm vào góc tính được, dùng khi prefab không theo đúng hướng quy ước.")]
        [SerializeField] private float _straightOffset;
        [SerializeField] private float _cornerOffset;
        [SerializeField] private float _tSectionOffset;
        [SerializeField] private float _intersectionOffset;
        [SerializeField] private float _endOffset;

        [Header("Nền cỏ")]
        [Tooltip("Prefab lát nền, rải kín vùng map. Kích thước và pivot đo thẳng từ mesh.")]
        [SerializeField] private GameObject _groundTile;

        [Tooltip("Phóng to miếng nền. Miếng nền là mặt phẳng màu trơn nên phóng lên không lộ, "
               + "mà số object rải ra giảm theo bình phương.")]
        [SerializeField, Min(0.01f)] private float _groundTileScale = 1f;

        #endregion

        #region Public - Properties

        public float TilePitch => _tilePitch;

        public GameObject GroundTile => _groundTile;

        public float GroundTileScale => _groundTileScale;

        /// <summary>Cạnh một miếng nền theo X và Z, đo thẳng từ mesh và nhân theo hệ số phóng.</summary>
        public Vector2 GroundFootprint => PrefabBoundsUtility.GetFootprint(_groundTile) * _groundTileScale;

        /// <summary>Pivot miếng nền lệch khỏi tâm mesh bao nhiêu, đã tính hệ số phóng.</summary>
        public Vector3 GroundPivotOffset => PrefabBoundsUtility.GetPivotOffset(_groundTile) * _groundTileScale;

        /// <summary>Bộ này có đủ mảnh tối thiểu để dựng đường không.</summary>
        public bool IsUsable => _straight != null && _corner != null;

        #endregion

        #region Public

        /// <summary>
        /// Chọn mảnh và góc xoay cho một ô đường, dựa trên các hướng có đường nối tiếp.
        /// </summary>
        /// <param name="connections">Cờ hướng nối, xem <see cref="TdRoadConnection"/>.</param>
        /// <param name="prefab">Prefab được chọn, có thể <c>null</c> nếu bộ chưa gán đủ.</param>
        /// <param name="yRotation">Góc xoay quanh trục đứng, tính bằng độ.</param>
        public void Resolve(TdRoadConnection connections, out GameObject prefab, out float yRotation)
        {
            switch (CountBits(connections))
            {
                case 0:
                    prefab = _end != null ? _end : _straight;
                    yRotation = _endOffset;
                    return;

                case 1:
                    prefab = _end != null ? _end : _straight;
                    yRotation = RotationFromSouth(connections) + _endOffset;
                    return;

                case 2:
                    ResolveTwoWay(connections, out prefab, out yRotation);
                    return;

                case 3:
                    prefab = _tSection != null ? _tSection : _intersection;
                    // Mảnh gốc thiếu hướng Bắc, nên xoay theo đúng hướng đang thiếu.
                    yRotation = RotationFromNorth(Missing(connections)) + _tSectionOffset;
                    return;

                default:
                    prefab = _intersection != null ? _intersection : _tSection;
                    yRotation = _intersectionOffset;
                    return;
            }
        }

        #endregion

        #region Private

        private void ResolveTwoWay(TdRoadConnection connections, out GameObject prefab, out float yRotation)
        {
            bool northSouth = connections == (TdRoadConnection.North | TdRoadConnection.South);
            bool eastWest = connections == (TdRoadConnection.East | TdRoadConnection.West);

            if (northSouth || eastWest)
            {
                prefab = _straight;
                yRotation = (northSouth ? 0f : 90f) + _straightOffset;
                return;
            }

            prefab = _corner;
            yRotation = CornerRotation(connections) + _cornerOffset;
        }

        /// <summary>Mảnh góc gốc nối Đông+Nam; xoay 90 độ mỗi lần theo chiều kim đồng hồ.</summary>
        private static float CornerRotation(TdRoadConnection connections)
        {
            if (connections == (TdRoadConnection.East | TdRoadConnection.South)) return 0f;
            if (connections == (TdRoadConnection.South | TdRoadConnection.West)) return 90f;
            if (connections == (TdRoadConnection.West | TdRoadConnection.North)) return 180f;
            return 270f; // Bắc + Đông
        }

        /// <summary>Góc cần xoay để hướng Nam của mảnh gốc trỏ về <paramref name="target"/>.</summary>
        private static float RotationFromSouth(TdRoadConnection target)
        {
            if ((target & TdRoadConnection.South) != 0) return 0f;
            if ((target & TdRoadConnection.West) != 0) return 90f;
            if ((target & TdRoadConnection.North) != 0) return 180f;
            return 270f; // Đông
        }

        /// <summary>Góc cần xoay để hướng Bắc của mảnh gốc trỏ về <paramref name="target"/>.</summary>
        private static float RotationFromNorth(TdRoadConnection target)
        {
            if ((target & TdRoadConnection.North) != 0) return 0f;
            if ((target & TdRoadConnection.East) != 0) return 90f;
            if ((target & TdRoadConnection.South) != 0) return 180f;
            return 270f; // Tây
        }

        private static TdRoadConnection Missing(TdRoadConnection connections)
        {
            return ~connections & TdRoadConnection.All;
        }

        private static int CountBits(TdRoadConnection connections)
        {
            int count = 0;
            int bits = (int)connections;
            while (bits != 0)
            {
                bits &= bits - 1;
                count++;
            }

            return count;
        }

        #endregion
    }
}
