using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Mô hình lưới tròn thuần logic (không MonoBehaviour): sinh danh sách <see cref="RadialCell" />
    ///     từ <see cref="RadialGridConfig" /> và cung cấp tra cứu ô ↔ vị trí. Toạ độ local so với tâm arena.
    /// </summary>
    public class RadialGrid
    {
        private const float TwoPi = Mathf.PI * 2f;

        private readonly List<RadialCell> _cells;

        public RadialGrid(RadialGridConfig config)
        {
            Config = config;
            _cells = Build(config);
        }

        public RadialGridConfig Config { get; }
        public IReadOnlyList<RadialCell> Cells => _cells;

        public int RingCount => Config.RingCount;
        public int SectorsPerRing => Config.SectorsPerRing;
        public int CellCount => _cells.Count;

        /// <summary>Lấy ô theo (ring, sector); sector tự wrap vòng (…, -1 → cuối, SectorsPerRing → 0).</summary>
        public RadialCell GetCell(int ring, int sector)
        {
            ring = Mathf.Clamp(ring, 0, RingCount - 1);
            sector = ((sector % SectorsPerRing) + SectorsPerRing) % SectorsPerRing;
            return _cells[ring * SectorsPerRing + sector];
        }

        public bool TryGetCell(int id, out RadialCell cell)
        {
            if (id < 0 || id >= _cells.Count)
            {
                cell = default;
                return false;
            }

            cell = _cells[id];
            return true;
        }

        /// <summary>Điểm local (so với tâm arena) → ô chứa nó. false nếu ngoài lưới hoặc trong lỗ giữa.</summary>
        public bool TryLocalToCell(Vector3 localPoint, out RadialCell cell)
        {
            cell = default;

            float r = new Vector2(localPoint.x, localPoint.y).magnitude;
            if (r < Config.InnerRadius || r > Config.OuterRadius) return false;

            int ring = Mathf.FloorToInt((r - Config.InnerRadius) / Config.RingThickness);
            ring = Mathf.Clamp(ring, 0, RingCount - 1);

            float ang = Mathf.Atan2(localPoint.y, localPoint.x);
            if (ang < 0f) ang += TwoPi;
            int sector = Mathf.FloorToInt(ang / (TwoPi / SectorsPerRing));
            sector = Mathf.Clamp(sector, 0, SectorsPerRing - 1);

            cell = GetCell(ring, sector);
            return true;
        }

        private static List<RadialCell> Build(RadialGridConfig cfg)
        {
            var list = new List<RadialCell>(cfg.RingCount * cfg.SectorsPerRing);
            float sectorArc = TwoPi / cfg.SectorsPerRing;
            float halfAngularGap = cfg.AngularGapDeg * Mathf.Deg2Rad * 0.5f;
            float halfRadialGap = cfg.RadialGap * 0.5f;

            for (int ring = 0; ring < cfg.RingCount; ring++)
            {
                float baseInner = cfg.InnerRadius + ring * cfg.RingThickness;
                float inner = baseInner + halfRadialGap;
                float outer = baseInner + cfg.RingThickness - halfRadialGap;

                for (int sector = 0; sector < cfg.SectorsPerRing; sector++)
                {
                    float baseStart = sector * sectorArc;
                    float start = baseStart + halfAngularGap;
                    float end = baseStart + sectorArc - halfAngularGap;
                    int id = ring * cfg.SectorsPerRing + sector;
                    list.Add(new RadialCell(ring, sector, id, inner, outer, start, end));
                }
            }

            return list;
        }
    }
}
