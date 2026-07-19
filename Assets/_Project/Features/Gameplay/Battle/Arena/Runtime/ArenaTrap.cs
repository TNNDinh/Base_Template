using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     1 cái bẫy đặt trên 1 ô lưới (do vũ khí loại TRAP tạo ra). Enemy bước vào ô → dính damage + đẩy lùi.
    ///     Bẫy mất khi hết <see cref="RoundsLeft" /> (số enemy round) HOẶC hết <see cref="HitsLeft" /> (số enemy trúng).
    ///     Thuần logic; view móc qua <see cref="OnTriggered" />/<see cref="OnExpired" />.
    /// </summary>
    public class ArenaTrap
    {
        public GridCell Cell;
        public float Damage;
        public List<Vector2Int> Knockback;
        public float CollisionDamage; // damage cho enemy bị TÔNG khi con dính bẫy bị đẩy vào nó
        public int RoundsLeft;
        public int HitsLeft;
        public bool Alive = true;

        public Action OnTriggered; // view: chớp sáng khi dính enemy
        public Action OnExpired;   // view: dọn bẫy
    }
}
