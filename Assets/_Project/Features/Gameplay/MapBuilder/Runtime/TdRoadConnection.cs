using System;

namespace Ezg.Feature.MapBuilder
{
    /// <summary>
    /// Các hướng mà một ô đường có đường nối tiếp. Bắc là chiều +Z, Đông là chiều +X.
    /// </summary>
    [Flags]
    public enum TdRoadConnection
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,

        All = North | East | South | West,
    }
}
