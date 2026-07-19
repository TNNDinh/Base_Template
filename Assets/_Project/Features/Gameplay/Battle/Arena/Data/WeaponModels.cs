using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Config 1 vũ khí của hero (CSV <c>Weapons.csv</c>). Hero trang bị tối đa 3 vũ khí.
    ///     <para><b>triggerShape</b>: tập ô "pick" tương đối so với hướng nhắm — mỗi ô "x y" với
    ///     <c>x</c> = lệch ngang (−trái/+phải so với tâm hướng nhắm), <c>y</c> = tiến ra ngoài (1..range →
    ///     ring y−1). Ngăn bằng ';'. Vd quạt 3×3: <c>-1 1;0 1;1 1;-1 2;0 2;1 2;-1 3;0 3;1 3</c>. Trigger tự
    ///     xoay quanh hero; user tap → dừng → mọi enemy nằm trong tập ô (đã xoay theo góc tap) bị nhắm.</para>
    ///     <para><b>knockback</b>: các bước đẩy lùi "x y" (x = ngang, y = +ra ngoài/−vào tâm), đẩy TỪNG Ô.</para>
    /// </summary>
    [Serializable]
    public struct WeaponModel
    {
        public string id;
        public string name;
        public int rarity;
        public float damage;
        public string triggerShape;    // tập ô nhắm (xem mô tả). RANGE = ô xa nhất (max forward) — tính từ đây, không lưu riêng.
        public string knockback;       // các bước đẩy lùi (xem mô tả)
        public float comboBonus;       // hệ số damage CỘNG DỒN mỗi lần chuyển sang enemy kế (vd 0.2 = +20%/lần)
        public float collisionDamage;  // damage gây cho enemy bị TÔNG TRÚNG khi 1 enemy bị đẩy lùi vào nó
        public string modelKey;        // prefab vũ khí (vd "41000") gắn vào bone b_weapon của hero

        // ----- Nâng cấp (level) -----
        public int maxLevel;             // cấp tối đa (≤1 = không nâng được)
        public float damagePerLevel;     // +damage mỗi cấp
        public float comboBonusPerLevel; // +comboBonus mỗi cấp
        public int upgradeCostBase;      // gold để lên cấp (nhân theo cấp hiện tại)

        // ----- Loại vũ khí đặc biệt (TRAP) -----
        public int weaponType;   // 0 = Normal (lao đánh), 1 = Trap (đặt bẫy lên ô trigger)
        public int trapRounds;   // TRAP: bẫy tồn tại bao nhiêu enemy round
        public int trapMaxHits;  // TRAP: bẫy mất sau bao nhiêu lần trúng enemy
    }

    /// <summary>Parse chuỗi "x y;x y;..." → danh sách offset ô (GIỮ NGUYÊN, không tách đơn vị — đây là tập ô pick).</summary>
    public static class CellSet
    {
        public static List<Vector2Int> Parse(string raw)
        {
            var cells = new List<Vector2Int>();
            if (string.IsNullOrWhiteSpace(raw)) return cells;

            var parts = raw.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                var pair = parts[i].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length != 2) continue;
                if (!int.TryParse(pair[0], out var x) || !int.TryParse(pair[1], out var y)) continue;
                cells.Add(new Vector2Int(x, y));
            }

            return cells;
        }

        /// <summary>Range = ô xa nhất (forward/y lớn nhất) trong tập ô. 0 nếu rỗng.</summary>
        public static int MaxForward(List<Vector2Int> cells)
        {
            int m = 0;
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].y > m) m = cells[i].y;
            return m;
        }

        public static int MaxForward(string raw) => MaxForward(Parse(raw));
    }
}
