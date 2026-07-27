namespace Ezg.Feature.Gameplay.AutoChess
{
    /// <summary>
    ///     Toạ độ 1 ô trên bàn cờ (cột X, hàng Y). Struct thuần, KHÔNG phụ thuộc UnityEngine
    ///     để engine chạy/test được ngoài runtime — mirror cách <c>Battle.Unit</c> giữ logic sạch.
    /// </summary>
    public readonly struct BoardCell
    {
        public readonly int X;
        public readonly int Y;

        public BoardCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(BoardCell o) => X == o.X && Y == o.Y;
        public override bool Equals(object o) => o is BoardCell c && Equals(c);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X},{Y})";

        /// <summary>Khoảng cách Chebyshev (đi chéo tính 1 ô) — hợp với di chuyển 8 hướng.</summary>
        public static int Chebyshev(BoardCell a, BoardCell b)
        {
            var dx = a.X - b.X;
            if (dx < 0) dx = -dx;
            var dy = a.Y - b.Y;
            if (dy < 0) dy = -dy;
            return dx > dy ? dx : dy;
        }
    }
}
