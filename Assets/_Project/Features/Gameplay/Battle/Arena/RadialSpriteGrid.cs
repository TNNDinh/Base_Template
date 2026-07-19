using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Lưới tròn dựng bằng SPRITE: mỗi ô là 1 hình chữ nhật bo góc (SpriteRenderer), xoay hướng
    ///     ra ngoài tâm, scale nhỏ (trong) → to (ngoài) theo vòng, ghép lại thành hình tròn quanh hero.
    ///     Tái dùng <see cref="RadialGrid" /> cho toàn bộ tính toán layout. Mặt phẳng XY (Z=0).
    /// </summary>
    public class RadialSpriteGrid : MonoBehaviour
    {
        #region Fields

        private const string CellsRootName = "Cells";

        [Header("Cấu hình lưới")]
        [SerializeField] private RadialGridConfig _config = new RadialGridConfig();

        [Header("Sprite ô")]
        [Tooltip("Sprite ô (annular-sector / hình chữ nhật bo góc). Để trống = tự sinh runtime.")]
        [SerializeField] private Sprite _cellSprite;

        [Header("Màu ô")]
        [Tooltip("Bật = tô màu theo vòng bằng Gradient (t = vòng trong→ngoài). Tắt = dùng 2 màu xen kẽ.")]
        [SerializeField] private bool _useRingGradient = true;
        [Tooltip("Màu theo vòng: trái = vòng trong cùng, phải = vòng ngoài cùng.")]
        [SerializeField] private Gradient _ringGradient = DefaultRingGradient();
        [SerializeField] private Color _cellColor = new Color(1f, 1f, 1f, 0.16f);
        [Tooltip("Màu xen kẽ theo vòng cho dễ đọc (vòng lẻ).")]
        [SerializeField] private Color _cellColorAlt = new Color(1f, 1f, 1f, 0.08f);
        [SerializeField] private string _sortingLayer = "Default";
        [Tooltip("Thứ tự vẽ; âm để lưới nằm DƯỚI hero.")]
        [SerializeField] private int _sortingOrder = -10;

        [Header("Hero ở tâm")]
        [Tooltip("Prefab hero đặt ở tâm arena (tuỳ chọn).")]
        [SerializeField] private GameObject _heroPrefab;
        [SerializeField] private bool _buildOnAwake = true;

        private RadialGrid _grid;
        private Transform _cellsRoot;
        private GameObject _hero;
        private readonly List<SpriteRenderer> _cells = new List<SpriteRenderer>();

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
        public Vector3 CenterWorld => transform.position;
        public IReadOnlyList<SpriteRenderer> Cells => _cells;

        [ContextMenu("Rebuild")]
        public void Build()
        {
            Clear();

            _grid = new RadialGrid(_config);
            _cellsRoot = new GameObject(CellsRootName).transform;
            _cellsRoot.SetParent(transform, false);
            _cellsRoot.localPosition = Vector3.zero;

            Sprite sprite = _cellSprite != null ? _cellSprite : RoundedRectSpriteFactory.GetDefault();
            float spriteW = sprite.bounds.size.x; // bề rộng world khi scale = 1
            float spriteH = sprite.bounds.size.y; // bề cao world khi scale = 1
            if (spriteW <= 0f) spriteW = 1f;
            if (spriteH <= 0f) spriteH = 1f;

            int lastRing = Mathf.Max(1, _config.RingCount - 1);
            var cells = _grid.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                RadialCell cell = cells[i];

                var go = new GameObject($"Cell_{cell.Ring}_{cell.Sector}");
                go.transform.SetParent(_cellsRoot, false);
                go.transform.localPosition = cell.LocalCenter;

                // Trục Y của sprite hướng ra ngoài (radial); X là phương tiếp tuyến.
                float deg = cell.MidAngleRad * Mathf.Rad2Deg - 90f;
                go.transform.localRotation = Quaternion.Euler(0f, 0f, deg);

                float height = cell.OuterRadius - cell.InnerRadius;                 // theo bán kính
                float angSpan = cell.EndAngleRad - cell.StartAngleRad;
                float width = 2f * cell.MidRadius * Mathf.Sin(angSpan * 0.5f);       // dây cung tại tâm ô
                go.transform.localScale = new Vector3(width / spriteW, height / spriteH, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = ColorFor(cell.Ring, lastRing);
                sr.sortingLayerName = _sortingLayer;
                sr.sortingOrder = _sortingOrder;
                _cells.Add(sr);
            }

            SpawnHero();
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            _cells.Clear();
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
                var leftover = transform.Find(CellsRootName);
                if (leftover != null) DestroySafe(leftover.gameObject);
            }
        }

        #endregion

        #region Private

        private Color ColorFor(int ring, int lastRing)
        {
            if (_useRingGradient && _ringGradient != null)
            {
                float t = lastRing <= 0 ? 0f : (float)ring / lastRing;
                return _ringGradient.Evaluate(t);
            }

            return ring % 2 == 0 ? _cellColor : _cellColorAlt;
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
                    new GradientAlphaKey(0.32f, 0f),
                    new GradientAlphaKey(0.10f, 1f)
                });
            return g;
        }

        private void SpawnHero()
        {
            if (_heroPrefab == null) return;
            _hero = Instantiate(_heroPrefab, transform);
            _hero.name = "Hero";
            _hero.transform.localPosition = Vector3.zero;
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
