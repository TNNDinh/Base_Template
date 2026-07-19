using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection stage arena. Theo convention ItemMergeCollection: giữ mảng <see cref="dataGroup" />
    ///     (serialize + sửa trong Inspector) và cache lookup dựng lại ở <see cref="Convert" />.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Stage Collection", fileName = "ArenaStageCollection")]
    public class ArenaStageCollection : ScriptableObject
    {
        public ArenaStageModel[] dataGroup;

        private Dictionary<string, ArenaStageModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, ArenaStageModel>();
            if (dataGroup == null) return;
            foreach (var s in dataGroup) _byId[s.id] = s;
        }

        public ArenaStageModel GetById(string id)
        {
            if (_byId == null) Convert();
            return _byId.TryGetValue(id, out var v) ? v : default;
        }

        public IReadOnlyList<ArenaStageModel> All => dataGroup ?? Array.Empty<ArenaStageModel>();
    }

    /// <summary>Collection enemy spawn (kèm STAT enemy). Nhóm theo stageId, sort theo round rồi ring.</summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Spawn Collection", fileName = "ArenaSpawnCollection")]
    public class ArenaSpawnCollection : ScriptableObject
    {
        public ArenaSpawnModel[] dataGroup;

        private Dictionary<string, List<ArenaSpawnModel>> _byStage;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byStage = new Dictionary<string, List<ArenaSpawnModel>>();
            if (dataGroup == null) return;
            foreach (var s in dataGroup)
            {
                if (!_byStage.TryGetValue(s.stageId, out var list))
                {
                    list = new List<ArenaSpawnModel>();
                    _byStage[s.stageId] = list;
                }

                list.Add(s);
            }

            foreach (var kv in _byStage)
                kv.Value.Sort((a, b) => a.round != b.round ? a.round.CompareTo(b.round) : a.ring.CompareTo(b.ring));
        }

        public IReadOnlyList<ArenaSpawnModel> GetByStage(string stageId)
        {
            if (_byStage == null) Convert();
            return _byStage.TryGetValue(stageId, out var v) ? v : (IReadOnlyList<ArenaSpawnModel>)Array.Empty<ArenaSpawnModel>();
        }
    }

    /// <summary>Collection nhiệm vụ sao + bonus. Nhóm theo stageId.</summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Objective Collection", fileName = "ArenaObjectiveCollection")]
    public class ArenaObjectiveCollection : ScriptableObject
    {
        public ArenaObjectiveModel[] dataGroup;

        private Dictionary<string, List<ArenaObjectiveModel>> _byStage;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byStage = new Dictionary<string, List<ArenaObjectiveModel>>();
            if (dataGroup == null) return;
            foreach (var o in dataGroup)
            {
                if (!_byStage.TryGetValue(o.stageId, out var list))
                {
                    list = new List<ArenaObjectiveModel>();
                    _byStage[o.stageId] = list;
                }

                list.Add(o);
            }
        }

        public IReadOnlyList<ArenaObjectiveModel> GetByStage(string stageId)
        {
            if (_byStage == null) Convert();
            return _byStage.TryGetValue(stageId, out var v) ? v : (IReadOnlyList<ArenaObjectiveModel>)Array.Empty<ArenaObjectiveModel>();
        }
    }

    /// <summary>Collection reward resource. Nhóm theo stageId.</summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Reward Collection", fileName = "ArenaRewardCollection")]
    public class ArenaRewardCollection : ScriptableObject
    {
        public ArenaRewardModel[] dataGroup;

        private Dictionary<string, List<ArenaRewardModel>> _byStage;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byStage = new Dictionary<string, List<ArenaRewardModel>>();
            if (dataGroup == null) return;
            foreach (var r in dataGroup)
            {
                if (!_byStage.TryGetValue(r.stageId, out var list))
                {
                    list = new List<ArenaRewardModel>();
                    _byStage[r.stageId] = list;
                }

                list.Add(r);
            }
        }

        public IReadOnlyList<ArenaRewardModel> GetByStage(string stageId)
        {
            if (_byStage == null) Convert();
            return _byStage.TryGetValue(stageId, out var v) ? v : (IReadOnlyList<ArenaRewardModel>)Array.Empty<ArenaRewardModel>();
        }
    }

    /// <summary>
    ///     Collection pattern di chuyển của enemy. Enemy lấy pattern theo <c>id</c> rồi
    ///     <see cref="GetBranches" /> để ra chuỗi bước (x,y) mà di chuyển.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Enemy Move Pattern Collection", fileName = "EnemyMovePatternCollection")]
    public class EnemyMovePatternCollection : ScriptableObject
    {
        public EnemyMovePatternModel[] dataGroup;

        private Dictionary<string, EnemyMovePatternModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, EnemyMovePatternModel>();
            if (dataGroup == null) return;
            foreach (var p in dataGroup) _byId[p.id] = p;
        }

        public EnemyMovePatternModel GetById(string id)
        {
            if (_byId == null) Convert();
            return _byId.TryGetValue(id, out var v) ? v : default;
        }

        public bool Contains(string id)
        {
            if (_byId == null) Convert();
            return _byId.ContainsKey(id);
        }

        /// <summary>Các NHÁNH bước (x,y) đã parse của 1 pattern (rỗng nếu không có id).</summary>
        public List<List<Vector2Int>> GetBranches(string id) => MovePattern.Parse(GetById(id).steps);
    }

    /// <summary>
    ///     Base cho collection stat unit (hero/enemy): mảng <see cref="dataGroup" /> <see cref="UnitStatModel" />,
    ///     tra theo id, tính stat theo level. Hero &amp; enemy = 2 collection riêng (2 CSV riêng) kế thừa class này.
    /// </summary>
    public abstract class UnitStatCollection : ScriptableObject
    {
        public UnitStatModel[] dataGroup;

        private Dictionary<string, UnitStatModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, UnitStatModel>();
            if (dataGroup == null) return;
            foreach (var u in dataGroup) _byId[u.id] = u;
        }

        public UnitStatModel GetById(string id)
        {
            if (_byId == null) Convert();
            return _byId.TryGetValue(id, out var v) ? v : default;
        }

        public bool Contains(string id)
        {
            if (_byId == null) Convert();
            return _byId.ContainsKey(id);
        }

        /// <summary>Stat của unit <paramref name="id" /> tại <paramref name="level" />.</summary>
        public UnitStats StatsAt(string id, int level) => UnitStats.Of(GetById(id), level);
    }

    /// <summary>Collection stat ENEMY (khoá = id prefab, vd "11001"). Data: EnemyStats.csv. Spawn link bằng enemyId.</summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Enemy Stat Collection", fileName = "EnemyStatCollection")]
    public class EnemyStatCollection : UnitStatCollection { }

    /// <summary>Collection vũ khí hero (khoá = id). <see cref="TriggerCells" /> = tập ô nhắm, <see cref="KnockbackSteps" /> = bước đẩy lùi.</summary>
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
