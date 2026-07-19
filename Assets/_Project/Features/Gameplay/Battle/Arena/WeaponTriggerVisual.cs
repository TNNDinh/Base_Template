using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Visual TẦM ĐÁNH của vũ khí trên lưới tròn: tô VÀNG MỜ các ô <see cref="WeaponModel.triggerShape" />
    ///     và TỰ XOAY quanh hero (chiều kim đồng hồ) như cơ chế. Các ô dựng 1 lần ở hướng nhắm gốc (facing 0)
    ///     rồi xoay cả parent — annular-sector đồng dạng qua phép xoay nên vẫn khít lưới. Gắn vào 1 GameObject
    ///     đặt tại TÂM arena (trùng <see cref="RadialGridArena" />). Tap dừng xoay = <see cref="SetSpinning" />(false).
    /// </summary>
    public class WeaponTriggerVisual : MonoBehaviour
    {
        #region Fields

        private const string TriggerRootName = "TriggerVisual";

        [Header("Lưới (khớp arena)")]
        [SerializeField] private RadialGridConfig _config = new RadialGridConfig();

        [Header("Vũ khí")]
        [SerializeField] private WeaponCollection _weapons;
        [SerializeField] private string _weaponId = "wp_wood_sword";

        [Header("Hiển thị")]
        [SerializeField] private Color _color = new Color(1f, 0.92f, 0.15f, 0.45f); // vàng mờ
        [SerializeField] private string _sortingLayer = "Default";
        [Tooltip("Vẽ trên lưới nền, dưới hero.")]
        [SerializeField] private int _sortingOrder = -5;

        [Header("Xoay")]
        [Tooltip("Tốc độ xoay (độ/giây). Xoay theo chiều kim đồng hồ.")]
        [SerializeField] private float _spinSpeedDeg = 60f;
        [Tooltip("Góc nhắm ban đầu (độ) khi bắt đầu mỗi round player — mặc định 90° (hướng lên).")]
        [SerializeField] private float _resetAngleDeg = 90f;
        [SerializeField] private bool _spinning = true;
        [SerializeField] private bool _buildOnAwake = true;

        private Transform _root;
        private Material _mat;
        private float _angle;

        #endregion

        #region Initialize

        private void Awake()
        {
            if (_buildOnAwake) Build();
        }

        private void Update()
        {
            if (!_spinning || _root == null) return;
            _angle -= _spinSpeedDeg * Time.deltaTime; // âm = chiều kim đồng hồ (mặt phẳng XY)
            _root.localRotation = Quaternion.Euler(0f, 0f, _angle);
        }

        private void OnDestroy() => Clear();

        #endregion

        #region Public

        public bool Spinning => _spinning;

        public void SetSpinning(bool on) => _spinning = on;

        /// <summary>Đưa hướng nhắm về VỊ TRÍ BAN ĐẦU (<see cref="_resetAngleDeg" />, mặc định 90°) — gọi khi bắt đầu mỗi round player.</summary>
        public void ResetAim()
        {
            _angle = _resetAngleDeg;
            if (_root != null) _root.localRotation = Quaternion.Euler(0f, 0f, _angle);
        }

        /// <summary>
        ///     Sector hướng nhắm hiện tại (theo góc xoay), để executor biết trigger đang chỉ đâu khi tap dừng.
        ///     Ô lateral x của trigger được vẽ ở grid sector (x + _angle/step) sau khi xoay parent theo _angle,
        ///     nên facing = round(_angle/step) — PHẢI khớp dấu với phép xoay để vùng vàng == ô bị đánh.
        /// </summary>
        public int CurrentFacingSector()
        {
            float step = 360f / Mathf.Max(1, _config.SectorsPerRing);
            int s = Mathf.RoundToInt(_angle / step);
            int n = _config.SectorsPerRing;
            return ((s % n) + n) % n;
        }

        public void SetWeapon(string weaponId)
        {
            _weaponId = weaponId;
            Build();
        }

        [ContextMenu("Rebuild")]
        public void Build()
        {
            Clear();
            if (_weapons == null) return;

            _root = new GameObject(TriggerRootName).transform;
            _root.SetParent(transform, false);
            _root.localPosition = Vector3.zero;
            _root.localRotation = Quaternion.Euler(0f, 0f, _angle);

            var grid = new RadialGrid(_config);
            var cells = _weapons.TriggerCells(_weaponId);
            if (_mat == null) _mat = CreateMaterial();

            for (int i = 0; i < cells.Count; i++)
            {
                int ring = cells[i].y - 1;         // forward 1..range → ring 0..range-1
                int sector = cells[i].x;           // facing 0: sector = lệch ngang
                if (ring < 0 || ring >= _config.RingCount) continue;

                RadialCell cell = grid.GetCell(ring, sector);
                var go = new GameObject($"Trig_{ring}_{grid.SectorsPerRing}");
                go.transform.SetParent(_root, false);
                go.transform.localPosition = cell.LocalCenter;

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = RadialGridMeshBuilder.Build(cell, _config.ArcSegments, _color);

                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = _mat;
                mr.sortingLayerName = _sortingLayer;
                mr.sortingOrder = _sortingOrder;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            if (_root != null)
            {
                DestroySafe(_root.gameObject);
                _root = null;
                return;
            }

            var leftover = transform.Find(TriggerRootName);
            if (leftover != null) DestroySafe(leftover.gameObject);
        }

        #endregion

        #region Private

        private Material CreateMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = "WeaponTrigger (auto)" };
            mat.mainTexture = RoundedRectSpriteFactory.GetDefaultTexture(); // bo góc mềm
            return mat;
        }

        private static void DestroySafe(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        #endregion
    }
}
