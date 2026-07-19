using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Đấu trường lưới tròn: hero đứng ở TÂM, quanh là <see cref="RadialGridConfig.RingCount" /> vòng ô
    ///     (mỗi ô là annular-sector "bo cạnh ngoài", nhỏ ở trong → to ở ngoài, ghép lại thành hình tròn).
    ///     Mặt phẳng chơi là XY (Z=0) — khớp <see cref="BattleFormation" />. Gắn component vào 1 GameObject
    ///     trong scene; runtime tự dựng lưới ở Awake (hoặc bấm chuột phải → Rebuild trong editor để xem trước).
    /// </summary>
    public class RadialGridArena : MonoBehaviour
    {
        #region Fields

        private const string CellsRootName = "Cells";

        [Header("Cấu hình lưới")]
        [SerializeField] private RadialGridConfig _config = new RadialGridConfig();

        [Header("Hiển thị ô")]
        [Tooltip("Để trống = tự tạo material Sprites/Default + mask bo góc (màu lấy từ vertex color).")]
        [SerializeField] private Material _cellMaterial;
        [Tooltip("Bo góc mềm cho ô bằng mask alpha (map qua UV). Tắt = ô cạnh sắc.")]
        [SerializeField] private bool _roundedCorners = true;

        [Header("Màu ô")]
        [Tooltip("Bật = tô màu theo vòng bằng Gradient (t = vòng trong→ngoài). Tắt = 2 màu xen kẽ.")]
        [SerializeField] private bool _useRingGradient = true;
        [Tooltip("Màu theo vòng: trái = vòng trong cùng, phải = vòng ngoài cùng.")]
        [SerializeField] private Gradient _ringGradient = DefaultRingGradient();
        [SerializeField] private Color _cellColor = new Color(1f, 1f, 1f, 0.14f);
        [Tooltip("Màu xen kẽ theo vòng để lưới dễ đọc (vòng lẻ).")]
        [SerializeField] private Color _cellColorAlt = new Color(1f, 1f, 1f, 0.07f);
        [SerializeField] private string _sortingLayer = "Default";
        [Tooltip("Thứ tự vẽ; âm để lưới nằm DƯỚI hero.")]
        [SerializeField] private int _sortingOrder = -10;

        [Header("Hero ở tâm")]
        [Tooltip("Prefab hero đặt ở tâm arena (tuỳ chọn). Để trống nếu spawn hero bằng hệ thống khác.")]
        [SerializeField] private GameObject _heroPrefab;
        [SerializeField] private bool _buildOnAwake = true;

        private RadialGrid _grid;
        private Transform _cellsRoot;
        private GameObject _hero;
        private readonly List<RadialCellView> _cellViews = new List<RadialCellView>();

        #endregion

        #region Initialize

        private void Awake()
        {
            if (_buildOnAwake) Build();
        }

        private void OnDestroy()
        {
            Clear();
        }

        #endregion

        #region Public

        public RadialGrid Grid => _grid;
        public RadialGridConfig Config => _config;
        public IReadOnlyList<RadialCellView> CellViews => _cellViews;

        /// <summary>Tâm arena ở world space — nơi hero đứng.</summary>
        public Vector3 CenterWorld => transform.position;

        /// <summary>Tâm 1 ô ở world space.</summary>
        public Vector3 CellCenterWorld(in RadialCell cell) => transform.TransformPoint(cell.LocalCenter);

        public RadialCellView GetCellView(int id) =>
            id >= 0 && id < _cellViews.Count ? _cellViews[id] : null;

        public RadialCellView GetCellView(int ring, int sector)
        {
            if (_grid == null) return null;
            return GetCellView(_grid.GetCell(ring, sector).Id);
        }

        /// <summary>Dựng lại toàn bộ lưới + đặt hero ở tâm.</summary>
        [ContextMenu("Rebuild")]
        public void Build()
        {
            Clear();

            _grid = new RadialGrid(_config);
            _cellsRoot = new GameObject(CellsRootName).transform;
            _cellsRoot.SetParent(transform, false);
            _cellsRoot.localPosition = Vector3.zero;

            Material mat = _cellMaterial != null ? _cellMaterial : CreateDefaultMaterial();

            int lastRing = Mathf.Max(1, _config.RingCount - 1);
            var cells = _grid.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                RadialCell cell = cells[i];
                Color color = ColorFor(cell.Ring, lastRing);

                var go = new GameObject($"Cell_{cell.Ring}_{cell.Sector}");
                go.transform.SetParent(_cellsRoot, false);
                go.transform.localPosition = cell.LocalCenter;

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = RadialGridMeshBuilder.Build(cell, _config.ArcSegments, color);

                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.sortingLayerName = _sortingLayer;
                mr.sortingOrder = _sortingOrder;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                var view = go.AddComponent<RadialCellView>();
                view.Setup(cell, mr, mf);
                _cellViews.Add(view);
            }

            SpawnHero();
        }

        /// <summary>Xoá toàn bộ ô + hero đã dựng.</summary>
        [ContextMenu("Clear")]
        public void Clear()
        {
            _cellViews.Clear();
            _grid = null;

            DestroySafe(_hero);
            _hero = null;

            if (_cellsRoot != null)
            {
                DestroySafe(_cellsRoot.gameObject);
                _cellsRoot = null;
            }
            else
            {
                // Dọn tàn dư từ lần build trước (vd sau khi reload domain trong editor).
                var leftover = transform.Find(CellsRootName);
                if (leftover != null) DestroySafe(leftover.gameObject);
            }
        }

        #endregion

        #region Private

        private void SpawnHero()
        {
            if (_heroPrefab == null) return;
            _hero = Instantiate(_heroPrefab, transform);
            _hero.name = "Hero";
            _hero.transform.localPosition = Vector3.zero;
        }

        private Color ColorFor(int ring, int lastRing)
        {
            if (_useRingGradient && _ringGradient != null)
            {
                float t = lastRing <= 0 ? 0f : (float)ring / lastRing;
                return _ringGradient.Evaluate(t);
            }

            return ring % 2 == 0 ? _cellColor : _cellColorAlt;
        }

        private Material CreateDefaultMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = "RadialGridCell (auto)" };

            // Mask bo góc map qua UV (u = phương góc, v = phương bán kính) → ô cong có góc bo mềm.
            if (_roundedCorners) mat.mainTexture = RoundedRectSpriteFactory.GetDefaultTexture();
            return mat;
        }

        private static Gradient DefaultRingGradient()
        {
            // Sáng ở tâm → mờ dần ra ngoài (gợi lại vòng ngắm trong ảnh ref).
            var g = new Gradient();
            g.mode = GradientMode.Blend;
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.85f, 1f, 0.75f), 0f),
                    new GradientColorKey(new Color(0.45f, 0.85f, 0.45f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.34f, 0f),
                    new GradientAlphaKey(0.12f, 1f)
                });
            return g;
        }

        private static void DestroySafe(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        #endregion

        #region Editor Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.5f);

            // Các vòng tròn (ranh giới bán kính).
            for (int ring = 0; ring <= _config.RingCount; ring++)
            {
                float radius = _config.InnerRadius + ring * _config.RingThickness;
                DrawGizmoCircle(radius, 64);
            }

            // Các nan (ranh giới góc giữa các ô).
            float twoPi = Mathf.PI * 2f;
            for (int sector = 0; sector < _config.SectorsPerRing; sector++)
            {
                float a = sector * (twoPi / _config.SectorsPerRing);
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Gizmos.DrawLine(dir * _config.InnerRadius, dir * _config.OuterRadius);
            }
        }

        private static void DrawGizmoCircle(float radius, int segments)
        {
            float step = Mathf.PI * 2f / segments;
            Vector3 prev = new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i * step;
                var cur = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }

        #endregion
    }
}
