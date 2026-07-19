using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Orchestrator trận arena (thuần logic, view móc qua callback). 1 <see cref="RunEnemyRound" /> =
    ///     1 ENEMY ROUND: (1) nếu round này có cấu hình spawn → spawn enemy trước; (2) rồi toàn bộ enemy
    ///     hành động (di chuyển pattern / telegraph / tấn công) qua <see cref="ArenaEnemyRound" />.
    ///     Đọc data từ các collection: stage/spawn + stat enemy + pattern di chuyển.
    /// </summary>
    public class ArenaCombat
    {
        private readonly RadialGridConfig _config;
        private readonly ArenaOccupancy _occ;
        private readonly List<ArenaEnemyUnit> _enemies = new List<ArenaEnemyUnit>();
        private readonly ArenaEnemyRound _round;

        private readonly string _stageId;
        private readonly ArenaSpawnCollection _spawns;
        private readonly EnemyStatCollection _enemyStats;
        private readonly EnemyMovePatternCollection _patterns;

        private readonly Dictionary<int, ArenaTrap> _traps = new Dictionary<int, ArenaTrap>();

        private int _roundNo;
        private int _spawnSeq;

        public ArenaCombat(RadialGridConfig config, string stageId,
            ArenaSpawnCollection spawns, EnemyStatCollection enemyStats, EnemyMovePatternCollection patterns,
            GridCell? heroCell = null)
        {
            _config = config;
            _stageId = stageId;
            _spawns = spawns;
            _enemyStats = enemyStats;
            _patterns = patterns;

            _occ = new ArenaOccupancy(config.RingCount, config.SectorsPerRing);
            if (heroCell.HasValue) _occ.SetHeroCell(heroCell.Value);
            _round = new ArenaEnemyRound(_occ, _enemies, OnEnemyEnteredCell);

            // Round SPAWN cuối của stage — để biết khi nào không còn quái nào sẽ xuất hiện nữa (win sớm).
            if (_spawns != null)
            {
                var list = _spawns.GetByStage(_stageId);
                for (int i = 0; i < list.Count; i++)
                    if (list[i].round > LastSpawnRound) LastSpawnRound = list[i].round;
            }
        }

        /// <summary>Round mà stage còn spawn quái lần cuối (0 = không có spawn). Qua round này = không còn quái mới.</summary>
        public int LastSpawnRound { get; private set; }

        /// <summary>Số enemy còn sống hiện tại.</summary>
        public int AliveCount
        {
            get
            {
                int c = 0;
                for (int i = 0; i < _enemies.Count; i++)
                    if (_enemies[i] != null && _enemies[i].IsAlive) c++;
                return c;
            }
        }

        public int RoundNo => _roundNo;
        public ArenaOccupancy Occupancy => _occ;
        public IReadOnlyList<ArenaEnemyUnit> Enemies => _enemies;

        /// <summary>Enemy vừa spawn (view: instantiate model tại ô). Fire trước khi enemy hành động.</summary>
        public Action<ArenaEnemyUnit, ArenaSpawnModel> OnEnemySpawned;

        /// <summary>Bẫy vừa đặt (view: dựng model bẫy rơi xuống ô).</summary>
        public Action<ArenaTrap> OnTrapPlaced;

        /// <summary>Enemy kích hoạt 1 skill (không phải Spawn) → controller áp effect (heal/damage hero) + view.</summary>
        public Action<ArenaEnemyUnit, ArenaSkillModel> OnEnemyUseSkill;

        /// <summary>Chạy 1 enemy round: SPAWN → ENEMY HÀNH ĐỘNG → SKILL/regen enemy → già bẫy.</summary>
        public void RunEnemyRound()
        {
            _roundNo++;
            SpawnForRound(_roundNo);
            _round.Resolve();
            _enemies.RemoveAll(e => !e.IsAlive); // dọn enemy đã chết (đã tự nhả ô khi Kill)
            EnemySkillsPhase();
            TickTraps();
        }

        /// <summary>Mỗi enemy round: passive regen + giảm cooldown + bắn skill trigger=EveryNRounds.</summary>
        private void EnemySkillsPhase()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!e.IsAlive) continue;
                e.Regen();
                e.Skills?.TickCooldowns();
                FireEnemySkills(e, SkillTrigger.EveryNRounds);
            }
        }

        // ----- Skill hooks (gắn vào unit lúc spawn) -----
        private void OnEnemyKilled(ArenaEnemyUnit e) => FireEnemySkills(e, SkillTrigger.OnDeath);

        private void OnEnemyDamaged(ArenaEnemyUnit e)
        {
            FireEnemySkills(e, SkillTrigger.OnDamaged);
            FireEnemySkills(e, SkillTrigger.OnLowHp);
        }

        /// <summary>Bắn các skill của enemy khớp <paramref name="trigger" />: Spawn → sinh con; còn lại → controller áp effect.</summary>
        private void FireEnemySkills(ArenaEnemyUnit e, SkillTrigger trigger)
        {
            if (e == null || e.Skills == null) return;
            var fired = e.Skills.Fire(trigger, e.CurrentHp / e.MaxHp, _roundNo);
            for (int i = 0; i < fired.Count; i++)
            {
                var s = fired[i];
                if ((ArenaSkillEffect)s.effect == ArenaSkillEffect.Spawn)
                    SpawnNear(e.Cell, s.spawnId, s.spawnCount);
                else
                    OnEnemyUseSkill?.Invoke(e, s);
            }
        }

        /// <summary>Sinh <paramref name="count" /> con <paramref name="enemyId" /> vào các ô TRỐNG gần <paramref name="center" /> (không đủ ô = spawn vừa đủ).</summary>
        private void SpawnNear(GridCell center, string enemyId, int count)
        {
            if (string.IsNullOrEmpty(enemyId) || count <= 0 || _enemyStats == null) return;
            UnitStats childStats = _enemyStats.StatsAt(enemyId, 1);
            var approach = _patterns != null ? _patterns.GetBranches("approach_straight") : null;
            var wander = _patterns != null ? _patterns.GetBranches("wander_circle") : null;
            var model = new ArenaSpawnModel { enemyId = enemyId, level = 1 };

            int need = count;
            var cells = NearbyCells(center);
            for (int i = 0; i < cells.Count && need > 0; i++)
            {
                if (!_occ.IsFree(cells[i])) continue;
                var child = SpawnOne(enemyId, childStats, cells[i], approach, wander, model);
                child.ResolvedThisRound = true; // con vừa sinh KHÔNG hành động trong lượt đang xử lý
                need--;
            }
        }

        #region Trap

        /// <summary>Đặt 1 bẫy lên ô (ghi đè bẫy cũ cùng ô). Nếu ô ĐANG có enemy → nổ ngay (damage + đẩy lùi). Trả về bẫy.</summary>
        public ArenaTrap PlaceTrap(GridCell cell, float damage, List<Vector2Int> knockback, int rounds, int maxHits, float collisionDamage)
        {
            if (!_occ.InBounds(cell) || _occ.IsHeroCell(cell)) return null;
            int key = TrapKey(cell);
            if (_traps.TryGetValue(key, out var old)) { old.Alive = false; old.OnExpired?.Invoke(); } // đặt chồng ô → bẫy CŨ mất hẳn (không stack nhiều bẫy 1 ô)
            var trap = new ArenaTrap
            {
                Cell = cell, Damage = damage, Knockback = knockback, CollisionDamage = collisionDamage,
                RoundsLeft = Mathf.Max(1, rounds), HitsLeft = Mathf.Max(1, maxHits)
            };
            _traps[key] = trap;
            OnTrapPlaced?.Invoke(trap);

            // Thả trúng ô đang có enemy → kích hoạt NGAY.
            var occupant = _occ.Occupant(cell) as ArenaEnemyUnit;
            if (occupant != null && occupant.IsAlive) TriggerTrap(trap, occupant);
            return trap;
        }

        /// <summary>
        ///     Kích hoạt bẫy lên 1 enemy: dính damage + đẩy lùi (chạm enemy khác → enemy đó ăn CollisionDamage,
        ///     con bị đẩy VỀ LẠI ô cũ — dùng chung rule <see cref="ApplyKnockback" />). Trừ 1 lượt, hết thì dọn bẫy.
        /// </summary>
        private void TriggerTrap(ArenaTrap trap, ArenaEnemyUnit e)
        {
            if (trap == null || !trap.Alive || e == null) return;

            int safety = 64; // chặn loop vô hạn (an toàn); thực tế dừng vì hết hit / con hoặc blocker chết / văng thoát
            while (e.IsAlive && safety-- > 0)
            {
                // 1 phát nổ: trừ máu con + trừ 1 hit (bẫy MẤT ngay nếu hết hit).
                e.TakeDamage(trap.Damage, _occ);
                trap.OnTriggered?.Invoke();
                bool trapGone = --trap.HitsLeft <= 0;
                if (trapGone) RemoveTrap(trap);

                // LUÔN đẩy lùi sau khi nổ — kể cả lần nổ cuối làm bẫy mất ("trap mất xong bị đẩy lui").
                bool blocked = false;
                if (e.IsAlive && trap.Knockback != null && trap.Knockback.Count > 0)
                    blocked = ApplyKnockback(e, trap.Knockback, trap.CollisionDamage);

                // Nổ LẠI chỉ khi: bẫy CÒN + con bị blocker chặn (đẩy lại vào ô bẫy) + con còn sống.
                if (trapGone || !blocked || !e.IsAlive) break;
            }
        }

        /// <summary>
        ///     Enemy vừa BƯỚC VÀO 1 ô (gọi từ resolver, ĐỒNG BỘ trong lượt di chuyển) → nếu ô có bẫy thì
        ///     enemy dính damage + bị đẩy lùi NGAY (cập nhật occupancy) — nên enemy khác chọn ô di chuyển sau đó
        ///     thấy đúng ô trống/bị chiếm, không lấy nhầm ô đã bị đẩy vào.
        /// </summary>
        private void OnEnemyEnteredCell(ArenaEnemyUnit e, GridCell cell)
        {
            if (e == null || !e.IsAlive) return;
            FireEnemySkills(e, SkillTrigger.OnMove); // enemy vừa di chuyển → skill OnMove

            if (_traps.TryGetValue(TrapKey(cell), out var trap) && trap.Alive) TriggerTrap(trap, e);
        }

        /// <summary>Cuối mỗi enemy round: giảm số round tồn tại; hết thì dọn bẫy.</summary>
        private void TickTraps()
        {
            if (_traps.Count == 0) return;
            List<ArenaTrap> expired = null;
            foreach (var kv in _traps)
            {
                if (--kv.Value.RoundsLeft > 0) continue;
                (expired ??= new List<ArenaTrap>()).Add(kv.Value);
            }

            if (expired == null) return;
            for (int i = 0; i < expired.Count; i++) RemoveTrap(expired[i]);
        }

        private void RemoveTrap(ArenaTrap t)
        {
            if (!t.Alive) return;
            t.Alive = false;
            _traps.Remove(TrapKey(t.Cell));
            t.OnExpired?.Invoke();
        }

        private int TrapKey(GridCell c) => c.ring * _config.SectorsPerRing + _occ.Wrap(c.sector);

        #endregion

        /// <summary>Enemy sống đang đứng ở ô (null nếu không có).</summary>
        public ArenaEnemyUnit EnemyAt(GridCell cell)
        {
            var e = _occ.Occupant(cell) as ArenaEnemyUnit;
            return e != null && e.IsAlive ? e : null;
        }

        /// <summary>
        ///     Đẩy lùi 1 enemy theo chuỗi bước (mỗi bước 1 ô). Chạm RÌA/ô hero → dừng tại ô cuối hợp lệ.
        ///     Chạm enemy khác → enemy đó nhận <paramref name="collisionDamage" />, con bị đẩy VỀ LẠI ô cũ.
        ///     Trả về TRUE nếu bị CHẶN bởi enemy (đã về ô cũ) — dùng cho bẫy lặp (nổ lại khi bị chặn).
        /// </summary>
        public bool ApplyKnockback(ArenaEnemyUnit unit, List<Vector2Int> steps, float collisionDamage)
        {
            if (unit == null || !unit.IsAlive || steps == null || steps.Count == 0) return false;

            GridCell origin = unit.Cell;
            _occ.Clear(origin); // nhấc ra để dò đường
            GridCell cur = origin;
            bool collided = false;

            for (int i = 0; i < steps.Count; i++)
            {
                var next = new GridCell(cur.ring + steps[i].y, _occ.Wrap(cur.sector + steps[i].x));
                if (!_occ.InBounds(next) || _occ.IsHeroCell(next)) break; // rìa / hero → dừng

                var blocker = _occ.Occupant(next) as ArenaEnemyUnit;
                if (blocker != null && blocker.IsAlive)
                {
                    blocker.TakeDamage(collisionDamage, _occ); // enemy bị tông mất máu
                    collided = true;
                    break;
                }

                cur = next;
            }

            if (collided) cur = origin; // bị chặn → về lại ô cũ
            _occ.Set(cur, unit);
            if (cur.ring != origin.ring || cur.sector != origin.sector) unit.SetCellForced(cur);
            return collided;
        }

        private void SpawnForRound(int round)
        {
            if (_spawns == null) return;
            var list = _spawns.GetByStage(_stageId);
            for (int i = 0; i < list.Count; i++)
                if (list[i].round == round)
                    SpawnGroup(list[i]);
        }

        private void SpawnGroup(ArenaSpawnModel s)
        {
            UnitStats stats = _enemyStats != null ? _enemyStats.StatsAt(s.enemyId, s.level) : default;
            stats.hp *= s.hpMul <= 0f ? 1f : s.hpMul;
            stats.atk *= s.atkMul <= 0f ? 1f : s.atkMul;
            stats.def *= s.defMul <= 0f ? 1f : s.defMul;

            var approach = _patterns != null ? _patterns.GetBranches(s.patternId) : null;
            var wander = _patterns != null && !string.IsNullOrEmpty(s.wanderPatternId)
                ? _patterns.GetBranches(s.wanderPatternId)
                : null;

            int count = Mathf.Max(1, s.count);
            for (int i = 0; i < count; i++)
            {
                var cell = FindSpawnCell(s, i, count);
                if (!cell.HasValue) continue; // hết ô trống trên vòng spawn
                SpawnOne(s.enemyId, stats, cell.Value, approach, wander, s);
            }
        }

        /// <summary>Tạo 1 enemy tại ô: áp passive (buff chỉ số + regen/dmgReduce từ skill list), gắn skill runner + hook, chiếm ô, fire view.</summary>
        private ArenaEnemyUnit SpawnOne(string enemyId, UnitStats stats, GridCell cell,
            List<List<Vector2Int>> approach, List<List<Vector2Int>> wander, ArenaSpawnModel model)
        {
            var passives = ArenaUpgradeService.AggregatePassives(stats.skills);
            stats.hp *= 1f + passives.maxHpPct;
            stats.atk *= 1f + passives.atkPct;
            int attackRangeRings = Mathf.Max(1, Mathf.RoundToInt(stats.attackRange));

            var e = new ArenaEnemyUnit(enemyId, stats, cell, approach, wander, attackRangeRings);
            e.SpawnOrder = _spawnSeq++;
            e.DmgReduce = Mathf.Clamp01(passives.dmgReducePct);
            e.RegenPct = passives.regenPct;
            e.Skills = new ArenaSkillRunner(stats.skills);
            e.OnKilled = OnEnemyKilled;       // OnDeath skill
            e.OnDamagedHook = OnEnemyDamaged; // OnDamaged/OnLowHp skill

            e.OccupySpawn(_occ);
            _enemies.Add(e);
            OnEnemySpawned?.Invoke(e, model);
            return e;
        }

        /// <summary>Ô tâm + 8 ô lân cận (thứ tự ưu tiên gần) để sinh con (skill Spawn).</summary>
        private List<GridCell> NearbyCells(GridCell c)
        {
            return new List<GridCell>
            {
                c,
                new GridCell(c.ring, _occ.Wrap(c.sector - 1)),
                new GridCell(c.ring, _occ.Wrap(c.sector + 1)),
                new GridCell(c.ring - 1, c.sector),
                new GridCell(c.ring + 1, c.sector),
                new GridCell(c.ring - 1, _occ.Wrap(c.sector - 1)),
                new GridCell(c.ring - 1, _occ.Wrap(c.sector + 1)),
                new GridCell(c.ring + 1, _occ.Wrap(c.sector - 1)),
                new GridCell(c.ring + 1, _occ.Wrap(c.sector + 1))
            };
        }

        /// <summary>Tìm ô spawn trống trên vòng <c>s.ring</c>. sector ≥ 0 = quanh vị trí đó; sector &lt; 0 = rải đều.</summary>
        private GridCell? FindSpawnCell(ArenaSpawnModel s, int index, int count)
        {
            int sectors = _config.SectorsPerRing;
            int start = s.sector >= 0
                ? s.sector + index
                : index * Mathf.Max(1, sectors / Mathf.Max(1, count));

            for (int k = 0; k < sectors; k++)
            {
                int sec = ((start + k) % sectors + sectors) % sectors;
                var cell = new GridCell(s.ring, sec);
                if (_occ.IsFree(cell)) return cell;
            }

            return null;
        }
    }
}
