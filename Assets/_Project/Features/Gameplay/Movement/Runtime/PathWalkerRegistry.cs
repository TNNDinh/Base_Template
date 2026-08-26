using System.Collections.Generic;

namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Sổ đăng ký các <see cref="IPathWalker"/> đang bật.
    /// <para>
    /// Có sổ này thì trình xem trước trong Editor khỏi phải quét cả scene mỗi frame để tìm
    /// thứ cần tick.
    /// </para>
    /// </summary>
    public static class PathWalkerRegistry
    {
        #region Fields

        private static readonly List<IPathWalker> Walkers = new List<IPathWalker>();

        #endregion

        #region Public - Properties

        /// <summary>Danh sách đang bật. Chỉ đọc; đừng giữ tham chiếu qua nhiều frame.</summary>
        public static IReadOnlyList<IPathWalker> Active => Walkers;

        #endregion

        #region Public

        public static void Register(IPathWalker walker)
        {
            if (walker == null || Walkers.Contains(walker)) return;
            Walkers.Add(walker);
        }

        public static void Unregister(IPathWalker walker)
        {
            if (walker == null) return;
            Walkers.Remove(walker);
        }

        #endregion
    }
}
