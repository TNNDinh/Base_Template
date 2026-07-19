using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>1 ô trên lưới tròn theo (ring, sector). ring 0 = trong cùng; sector wrap quanh vòng.</summary>
    public struct GridCell
    {
        public int ring;
        public int sector;

        public GridCell(int ring, int sector)
        {
            this.ring = ring;
            this.sector = sector;
        }

        public override string ToString() => $"({ring},{sector})";
    }

    /// <summary>
    ///     Theo dõi ô nào đang bị chiếm trên lưới tròn (để enemy né nhau + chặn rìa). Thuần logic,
    ///     không phụ thuộc view — dùng chung với <see cref="RadialGrid" /> (cùng ringCount/sectorsPerRing).
    /// </summary>
    public class ArenaOccupancy
    {
        private readonly int _ringCount;
        private readonly int _sectorsPerRing;
        private readonly Dictionary<int, object> _occupants = new Dictionary<int, object>();

        private bool _hasHero;
        private GridCell _heroCell;

        public ArenaOccupancy(int ringCount, int sectorsPerRing)
        {
            _ringCount = ringCount;
            _sectorsPerRing = sectorsPerRing;
        }

        public int SectorsPerRing => _sectorsPerRing;
        public int RingCount => _ringCount;

        /// <summary>Đưa sector về [0, sectorsPerRing) (vòng tròn).</summary>
        public int Wrap(int sector) => ((sector % _sectorsPerRing) + _sectorsPerRing) % _sectorsPerRing;

        /// <summary>Ô nằm trong lưới (ring hợp lệ). ring &lt; 0 (quá tâm) hoặc ≥ ringCount (quá rìa) = ngoài.</summary>
        public bool InBounds(GridCell c) => c.ring >= 0 && c.ring < _ringCount;

        /// <summary>Đặt ô hero đứng (ô trung tâm). Enemy không được dẫm lên ô này.</summary>
        public void SetHeroCell(GridCell c)
        {
            _hasHero = true;
            _heroCell = c;
        }

        public bool IsHeroCell(GridCell c) =>
            _hasHero && c.ring == _heroCell.ring && Wrap(c.sector) == Wrap(_heroCell.sector);

        /// <summary>Ô trống để bước vào = trong lưới, KHÔNG phải ô hero, và chưa bị chiếm.</summary>
        public bool IsFree(GridCell c) => InBounds(c) && !IsHeroCell(c) && !_occupants.ContainsKey(Key(c));

        public object Occupant(GridCell c) => _occupants.TryGetValue(Key(c), out var o) ? o : null;

        public void Set(GridCell c, object who)
        {
            if (InBounds(c)) _occupants[Key(c)] = who;
        }

        public void Clear(GridCell c) => _occupants.Remove(Key(c));

        public void Move(GridCell from, GridCell to, object who)
        {
            Clear(from);
            Set(to, who);
        }

        public void Reset() => _occupants.Clear();

        private int Key(GridCell c) => c.ring * _sectorsPerRing + Wrap(c.sector);
    }
}
