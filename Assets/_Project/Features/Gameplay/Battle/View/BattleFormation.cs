using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Bố trí đội hình 6 slot mỗi phe trên mặt phẳng 2D (XY, Z=0): lưới 3 cột × 2 hàng
    ///     (hàng 0 = tiền tuyến, hàng 1 = hậu tuyến). Player bên -X, enemy bên +X.
    /// </summary>
    public static class BattleFormation
    {
        public const int Cols = 3;
        public const int Rows = 2;

        private const float ColSpacing = 2.8f;   // khoảng cách 3 cột theo trục dọc (Y)
        private const float RowSpacing = 1.8f;    // khoảng cách tiền/hậu tuyến (X)
        private const float TeamOffsetX = 3.0f;   // khoảng cách 2 phe theo trục X

        /// <summary>Vị trí world của 1 slot (0..5) theo phe. Game 2D: dàn trên mặt phẳng XY (Z=0).</summary>
        public static Vector3 SlotPosition(BattleTeam team, int slot)
        {
            var row = slot / Cols; // 0 = front, 1 = back
            var col = slot % Cols;

            var sign = team == BattleTeam.Player ? -1f : 1f;
            var x = sign * (TeamOffsetX + row * RowSpacing); // hàng sau lùi ra xa tâm
            var y = (col - (Cols - 1) / 2f) * ColSpacing;    // 3 cột dàn theo trục dọc
            return new Vector3(x, y, 0f);
        }
    }
}
