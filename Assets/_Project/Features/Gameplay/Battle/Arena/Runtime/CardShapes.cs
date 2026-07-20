using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Dựng tập ô mục tiêu của thẻ trên lưới cực (tâm = hero), xoay theo hướng nhắm (<c>aimSector</c>).
    ///     Thuần logic — test được không cần scene.
    /// </summary>
    public static class CardShapes
    {
        /// <summary>Tập ô bị nhắm cho <paramref name="shape" />. <paramref name="ring" /> &lt; 0 = ring 1 mặc định.</summary>
        public static List<GridCell> Cells(CardTargetShape shape, int aimSector, ArenaOccupancy occ,
            int ring = -1, int shapeSize = 0)
        {
            var cells = new List<GridCell>();
            if (occ == null) return cells;
            int rings = occ.RingCount;
            int sectors = occ.SectorsPerRing;

            switch (shape)
            {
                case CardTargetShape.RadialLine: // 1 tia xuyên tâm: mọi ring của sector nhắm
                    for (int r = 0; r < rings; r++) cells.Add(new GridCell(r, occ.Wrap(aimSector)));
                    break;

                case CardTargetShape.Arc: // quạt: sector nhắm ± shapeSize, mọi ring
                    for (int d = -shapeSize; d <= shapeSize; d++)
                        for (int r = 0; r < rings; r++)
                            cells.Add(new GridCell(r, occ.Wrap(aimSector + d)));
                    break;

                case CardTargetShape.Ring: // 1 vòng ring: toàn sector của ring mục tiêu
                    int rr = ring < 0 ? 1 : ring;
                    if (rr >= 0 && rr < rings)
                        for (int s = 0; s < sectors; s++) cells.Add(new GridCell(rr, s));
                    break;

                case CardTargetShape.Board: // toàn sàn
                    for (int r = 0; r < rings; r++)
                        for (int s = 0; s < sectors; s++) cells.Add(new GridCell(r, s));
                    break;

                // None → rỗng (self-target)
            }

            return cells;
        }
    }
}
