using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection vũ khí hero (khoá = id). <see cref="TriggerCells" /> = tập ô nhắm, <see cref="KnockbackSteps" /> = bước đẩy lùi.
    ///     Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind chắc sau domain reload
    ///     (tránh m_Script=0 → load null → không có data vũ khí → không hiện vùng trigger).
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Weapon Collection", fileName = "WeaponCollection")]
    public class WeaponCollection : ScriptableObject
    {
        public WeaponModel[] dataGroup;

        private Dictionary<string, WeaponModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, WeaponModel>();
            if (dataGroup == null) return;
            foreach (var w in dataGroup) _byId[w.id] = w;
        }

        public WeaponModel GetById(string id)
        {
            if (_byId == null) Convert();
            return _byId.TryGetValue(id, out var v) ? v : default;
        }

        public bool Contains(string id)
        {
            if (_byId == null) Convert();
            return _byId.ContainsKey(id);
        }

        /// <summary>Tập ô nhắm (offset tương đối hướng nhắm) của vũ khí.</summary>
        public List<Vector2Int> TriggerCells(string id) => CellSet.Parse(GetById(id).triggerShape);

        /// <summary>Các bước đẩy lùi (offset) của vũ khí.</summary>
        public List<Vector2Int> KnockbackSteps(string id) => CellSet.Parse(GetById(id).knockback);

        /// <summary>Range (bán kính) = ô xa nhất trong triggerShape — tính tự động.</summary>
        public int RangeOf(string id) => CellSet.MaxForward(GetById(id).triggerShape);

        // ----- Nâng cấp theo level -----

        /// <summary>Cấp tối đa của vũ khí (tối thiểu 1).</summary>
        public int MaxLevel(string id) => Mathf.Max(1, GetById(id).maxLevel);

        /// <summary>Damage hiệu dụng ở <paramref name="level" /> = base + damagePerLevel*(level-1).</summary>
        public float DamageAt(string id, int level)
        {
            var m = GetById(id);
            return m.damage + m.damagePerLevel * Mathf.Max(0, level - 1);
        }

        /// <summary>ComboBonus hiệu dụng ở <paramref name="level" />.</summary>
        public float ComboAt(string id, int level)
        {
            var m = GetById(id);
            return m.comboBonus + m.comboBonusPerLevel * Mathf.Max(0, level - 1);
        }

        /// <summary>Gold để nâng từ <paramref name="level" /> → level+1 = costBase * level.</summary>
        public int UpgradeCost(string id, int level)
        {
            int b = GetById(id).upgradeCostBase;
            return (b > 0 ? b : 100) * Mathf.Max(1, level);
        }

        // ----- Trap -----

        /// <summary>Vũ khí loại TRAP (đặt bẫy) hay không.</summary>
        public bool IsTrap(string id) => GetById(id).weaponType == 1;

        /// <summary>Số round bẫy tồn tại (tối thiểu 1).</summary>
        public int TrapRounds(string id) => Mathf.Max(1, GetById(id).trapRounds);

        /// <summary>Số lần trúng enemy trước khi bẫy mất (tối thiểu 1).</summary>
        public int TrapMaxHits(string id) => Mathf.Max(1, GetById(id).trapMaxHits);
    }
}
