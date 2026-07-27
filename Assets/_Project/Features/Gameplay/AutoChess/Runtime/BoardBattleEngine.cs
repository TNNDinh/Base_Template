using System;
using System.Collections.Generic;
using Ezg.Feature.Gameplay.Battle;

namespace Ezg.Feature.Gameplay.AutoChess
{
    /// <summary>
    ///     Driver auto-battler thời gian thực trên bàn cờ 6×8 (kiểu cờ TFT).
    ///     Thay cho vòng turn-based <c>BattleService.RunAuto</c> (SpeedPool): mỗi unit TỰ tiến về
    ///     địch gần nhất, vào tầm thì TỰ đánh; mana đầy thì tung Ultimate. Tái dùng nguyên bộ giải
    ///     combat của Battle: <see cref="Unit" />, <see cref="BattleService.CastSkill" />,
    ///     <see cref="DamageFormula" />, buff/passive — engine này chỉ lo GRID + NHỊP ĐỘ (tick).
    ///     Logic thuần, KHÔNG phụ thuộc UnityEngine; view subscribe qua các event.
    /// </summary>
    public class BoardBattleEngine
    {
        #region Board dims

        public const int Width = 6;  // cột (ngang)
        public const int Height = 8; // hàng (dọc)

        #endregion

        #region Tuning

        /// <summary>SPD quy chiếu: unit có SPD này đánh đúng <see cref="BaseAttackInterval" /> giây/đòn.</summary>
        private const float SpdReference = 100f;

        /// <summary>Giây/đòn ở SPD chuẩn. Interval thực = Base × Ref / SPD (SPD cao → đánh nhanh).</summary>
        private const float BaseAttackInterval = 1.1f;

        private const float MinAttackInterval = 0.25f;
        private const float MaxAttackInterval = 3.0f;

        /// <summary>Giây để bước 1 ô. SPD cũng làm nhanh chân một chút.</summary>
        private const float BaseMoveInterval = 0.40f;

        /// <summary>1 "turn" buff/DoT (đơn vị duration trong CSV) = bấy nhiêu giây thực.</summary>
        private const float BuffTickSeconds = 1.0f;

        /// <summary>Trần thời gian 1 trận (giây) — hết giờ xử hòa/so máu.</summary>
        public const float MaxBattleSeconds = 90f;

        #endregion

        #region State

        private readonly BattleContext _ctx;
        private readonly Dictionary<Unit, UnitBoardState> _state = new Dictionary<Unit, UnitBoardState>();
        private readonly Unit[,] _grid = new Unit[Width, Height];
        private readonly List<Unit> _order = new List<Unit>(); // thứ tự tick ổn định (deterministic)

        private float _buffTimer;

        public float ElapsedTime { get; private set; }
        public BattleContext Context => _ctx;
        public bool IsOver => _ctx.IsOver || ElapsedTime >= MaxBattleSeconds;

        /// <summary>Phe thắng khi trận kết thúc (hết giờ: bên nào còn nhiều máu % hơn).</summary>
        public BattleTeam Winner
        {
            get
            {
                if (_ctx.PlayerAlive && !_ctx.EnemyAlive) return BattleTeam.Player;
                if (_ctx.EnemyAlive && !_ctx.PlayerAlive) return BattleTeam.Enemy;
                return TeamHpRatio(_ctx.PlayerTeam) >= TeamHpRatio(_ctx.EnemyTeam)
                    ? BattleTeam.Player
                    : BattleTeam.Enemy;
            }
        }

        #endregion

        #region Events (cho view Phase 2)

        public event Action<Unit, BoardCell, BoardCell> OnUnitMoved; // unit, from, to
        public event Action<Unit, Unit, SkillModel> OnUnitCast;      // caster, target, skill
        public event Action<Unit> OnUnitDied;

        #endregion

        #region Unit board state

        private class UnitBoardState
        {
            public BoardCell Cell;
            public float AttackCd; // đếm ngược tới đòn kế
            public float MoveCd;   // đếm ngược tới bước kế
            public int RangeCells; // tầm đánh quy ra số ô
            public bool DeathNotified;
        }

        #endregion

        #region Setup

        /// <summary>
        ///     Tạo engine từ 1 <see cref="BattleContext" /> (đã có 2 đội) + vị trí đặt ban đầu của từng unit.
        ///     Đặt sai (trùng ô / ngoài biên) sẽ bị bỏ qua và ném cảnh báo trong log.
        /// </summary>
        public BoardBattleEngine(BattleContext ctx, IDictionary<Unit, BoardCell> placement)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            if (placement == null) throw new ArgumentNullException(nameof(placement));

            foreach (var u in ctx.AllUnits())
            {
                if (!placement.TryGetValue(u, out var cell))
                {
                    BattleLog.Info($"[Board] {u.DisplayName} chưa được đặt vị trí → bỏ qua.");
                    continue;
                }

                if (!InBounds(cell) || _grid[cell.X, cell.Y] != null)
                {
                    BattleLog.Info($"[Board] Vị trí {cell} không hợp lệ/đã có unit → bỏ {u.DisplayName}.");
                    continue;
                }

                _grid[cell.X, cell.Y] = u;
                _state[u] = new UnitBoardState
                {
                    Cell = cell,
                    RangeCells = RangeToCells(u.AttackRange),
                    AttackCd = 0f,
                    MoveCd = 0f
                };
                _order.Add(u);
            }
        }

        /// <summary>Quy tầm đánh (đơn vị world của Battle) ra số ô lưới. &gt;=100 = bắn toàn bàn.</summary>
        private static int RangeToCells(float attackRange)
        {
            if (attackRange >= 100f) return Width + Height; // tầm xa vô hạn (đứng yên bắn)
            var cells = (int)(attackRange / 2.5f + 0.5f);    // ~2.5 world = 1 ô (cận chiến)
            return cells < 1 ? 1 : cells;
        }

        #endregion

        #region Tick

        /// <summary>
        ///     Tiến mô phỏng 1 khung thời gian <paramref name="dt" /> giây. Gọi từ view (Update)
        ///     hoặc từ test bằng bước cố định. Trả về <c>true</c> khi trận đã kết thúc.
        /// </summary>
        public bool Tick(float dt)
        {
            if (dt <= 0f || IsOver) return IsOver;

            ElapsedTime += dt;

            // Tick buff/DoT theo nhịp thực (map 1 duration-turn = BuffTickSeconds).
            _buffTimer += dt;
            while (_buffTimer >= BuffTickSeconds)
            {
                _buffTimer -= BuffTickSeconds;
                TickAllBuffs();
            }

            for (int i = 0; i < _order.Count; i++)
            {
                var u = _order[i];
                if (!u.IsAlive)
                {
                    FreeIfDead(u);
                    continue;
                }

                var sb = _state[u];
                if (sb.AttackCd > 0f) sb.AttackCd -= dt;
                if (sb.MoveCd > 0f) sb.MoveCd -= dt;

                var target = NearestEnemy(u);
                if (target == null) continue;

                var dist = BoardCell.Chebyshev(sb.Cell, _state[target].Cell);
                if (dist <= sb.RangeCells)
                {
                    if (sb.AttackCd <= 0f)
                    {
                        DoCast(u, target);
                        sb.AttackCd = AttackInterval(u);
                    }
                }
                else if (sb.MoveCd <= 0f)
                {
                    StepToward(u, target);
                    sb.MoveCd = MoveInterval(u);
                }
            }

            return IsOver;
        }

        private void TickAllBuffs()
        {
            for (int i = 0; i < _order.Count; i++)
            {
                var u = _order[i];
                if (!u.IsAlive) continue;
                u.TickBuffsTurnStart();
                u.RunPassivesTurnStart(_ctx);
            }
        }

        #endregion

        #region Combat

        /// <summary>1 nhịp đánh: chọn skill (mana đầy → Ultimate, không thì đòn thường) rồi thi triển.</summary>
        private void DoCast(Unit caster, Unit target)
        {
            var skill = ChooseSkill(caster);
            OnUnitCast?.Invoke(caster, target, skill);

            BattleService.CastSkill(caster, skill, target, _ctx);

            if (skill.type == SkillType.Ultimate)
                caster.GainMana(-caster.MaxMana); // xả sạch mana sau khi tung ult
            else
                caster.GainMana(caster.ManaPerAttack); // đòn thường sạc mana

            // dọn unit vừa bị hạ (giải phóng ô cho đồng đội đi qua)
            for (int i = 0; i < _order.Count; i++) FreeIfDead(_order[i]);
        }

        /// <summary>
        ///     Mana đầy &amp; có Ultimate → tung Ultimate; ngược lại đánh Basic.
        ///     (Active-skill tốn mana bị lược để mana bar thuần làm thanh sạc ult — đúng ý "đủ mana xài ult".)
        /// </summary>
        private static SkillModel ChooseSkill(Unit unit)
        {
            if (!string.IsNullOrEmpty(unit.UltimateId) && unit.MaxMana > 0f && unit.Mana >= unit.MaxMana)
            {
                var ult = BattleDatabase.GetSkill(unit.UltimateId);
                if (!string.IsNullOrEmpty(ult.id)) return ult;
            }

            return BattleDatabase.GetSkill(unit.BasicSkillId);
        }

        private static float AttackInterval(Unit u)
        {
            var spd = u.GetStat(StatType.Spd);
            if (spd < 1f) spd = 1f;
            var interval = BaseAttackInterval * SpdReference / spd;
            if (interval < MinAttackInterval) interval = MinAttackInterval;
            if (interval > MaxAttackInterval) interval = MaxAttackInterval;
            return interval;
        }

        private static float MoveInterval(Unit u)
        {
            var spd = u.GetStat(StatType.Spd);
            if (spd < 1f) spd = 1f;
            var interval = BaseMoveInterval * SpdReference / spd;
            if (interval < 0.12f) interval = 0.12f;
            if (interval > 0.9f) interval = 0.9f;
            return interval;
        }

        #endregion

        #region Movement

        /// <summary>Bước 1 ô về phía target: chọn ô kề (8 hướng) trống, giảm Chebyshev nhiều nhất.</summary>
        private void StepToward(Unit u, Unit target)
        {
            var sb = _state[u];
            var from = sb.Cell;
            var goal = _state[target].Cell;
            var bestDist = BoardCell.Chebyshev(from, goal);
            var best = from;

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var next = new BoardCell(from.X + dx, from.Y + dy);
                if (!InBounds(next) || _grid[next.X, next.Y] != null) continue;

                var d = BoardCell.Chebyshev(next, goal);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = next;
                }
            }

            if (best.Equals(from)) return; // bị chặn tứ phía → đứng chờ

            _grid[from.X, from.Y] = null;
            _grid[best.X, best.Y] = u;
            sb.Cell = best;
            OnUnitMoved?.Invoke(u, from, best);
        }

        #endregion

        #region Queries

        /// <summary>Địch còn sống gần nhất theo Chebyshev (hòa: slot nhỏ hơn trước — ổn định).</summary>
        private Unit NearestEnemy(Unit u)
        {
            var enemies = _ctx.EnemiesOf(u);
            var from = _state[u].Cell;
            Unit best = null;
            var bestDist = int.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (!e.IsAlive || !_state.ContainsKey(e)) continue;

                var d = BoardCell.Chebyshev(from, _state[e].Cell);
                if (d < bestDist || (d == bestDist && best != null && e.Slot < best.Slot))
                {
                    bestDist = d;
                    best = e;
                }
            }

            return best;
        }

        private void FreeIfDead(Unit u)
        {
            if (u.IsAlive || !_state.TryGetValue(u, out var sb)) return;

            if (_grid[sb.Cell.X, sb.Cell.Y] == u) _grid[sb.Cell.X, sb.Cell.Y] = null;
            if (!sb.DeathNotified)
            {
                sb.DeathNotified = true;
                OnUnitDied?.Invoke(u);
            }
        }

        public bool TryGetCell(Unit u, out BoardCell cell)
        {
            if (_state.TryGetValue(u, out var sb))
            {
                cell = sb.Cell;
                return true;
            }

            cell = default;
            return false;
        }

        private static bool InBounds(BoardCell c) => c.X >= 0 && c.X < Width && c.Y >= 0 && c.Y < Height;

        private static float TeamHpRatio(List<Unit> team)
        {
            float cur = 0f, max = 0f;
            for (int i = 0; i < team.Count; i++)
            {
                cur += team[i].CurrentHp > 0f ? team[i].CurrentHp : 0f;
                max += team[i].MaxHp;
            }

            return max <= 0f ? 0f : cur / max;
        }

        #endregion
    }
}
