using System;
using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Điều phối 1 enemy round cho toàn bộ enemy với CHAIN MOVEMENT: khi enemy A muốn vào ô đang bị
    ///     enemy B chiếm mà B CHƯA đi lượt này → xử lý B trước (B có thể nhường ô), rồi A vào; nếu B đã đi
    ///     rồi (hoặc đang trong chuỗi đang xử lý → tránh lặp vô hạn) thì A phải né/đứng yên.
    /// </summary>
    public class ArenaEnemyRound
    {
        private readonly ArenaOccupancy _occ;
        private readonly List<ArenaEnemyUnit> _enemies;
        private readonly Action<ArenaEnemyUnit, GridCell> _onEntered;

        public ArenaEnemyRound(ArenaOccupancy occ, List<ArenaEnemyUnit> enemies,
            Action<ArenaEnemyUnit, GridCell> onEntered = null)
        {
            _occ = occ;
            _enemies = enemies;
            _onEntered = onEntered;
        }

        /// <summary>Chạy 1 enemy round: mỗi enemy hành động đúng 1 lần (theo thứ tự phụ thuộc).</summary>
        public void Resolve()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemies[i].ResolvedThisRound = false;
                _enemies[i].ResolvingNow = false;
            }

            // Duyệt copy phòng khi list bị đổi giữa chừng; enemy chết vẫn đánh dấu resolved để bỏ qua.
            for (int i = 0; i < _enemies.Count; i++)
                EnsureResolved(_enemies[i]);
        }

        private void EnsureResolved(ArenaEnemyUnit e)
        {
            if (e == null) return;
            if (!e.IsAlive)
            {
                e.ResolvedThisRound = true;
                return;
            }

            if (e.ResolvedThisRound || e.ResolvingNow) return; // đã xong hoặc đang trong chuỗi (chặn đệ quy vòng)

            e.ResolvingNow = true;
            e.TakeEnemyTurn(_occ, EnsureResolved, _onEntered);
            e.ResolvingNow = false;
            e.ResolvedThisRound = true;
        }
    }
}
