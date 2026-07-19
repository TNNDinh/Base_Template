using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Trạng thái turn-based của 1 enemy.</summary>
    public enum EnemyState
    {
        Approaching, // đang tiến vào theo pattern
        Preparing,   // đã vào tầm → giơ đòn (telegraph), sẽ đánh ở enemy round kế
        Attacking,   // phát động tấn công (round này)
        Wandering    // đi loanh quanh sau khi đánh, rồi quay lại tiến vào
    }

    /// <summary>
    ///     Enemy runtime turn-based. Mỗi <see cref="TakeEnemyTurn" /> (1 enemy round) xử lý theo state:
    ///     tiến vào theo pattern (né vật cản bằng nhánh phụ / đứng yên), vào tầm thì giơ đòn, round kế
    ///     phát động, xong đi loanh quanh rồi tiến vào lại. THUẦN LOGIC trên (ring, sector) — mọi hiệu ứng
    ///     view móc qua callback (<see cref="OnMoved" />/<see cref="OnPrepare" />/<see cref="OnAttack" />…),
    ///     nên test được không cần scene.
    /// </summary>
    public class ArenaEnemyUnit
    {
        private const int DefaultWanderRounds = 2;

        private readonly List<List<Vector2Int>> _approach; // nhánh[0] = đường chính, còn lại = né vật cản
        private readonly List<List<Vector2Int>> _wander;
        private readonly int _attackRangeRings;

        private int _stepIndex;
        private int _wanderLeft;

        public ArenaEnemyUnit(string enemyId, UnitStats stats, GridCell spawn,
            List<List<Vector2Int>> approach, List<List<Vector2Int>> wander, int attackRangeRings)
        {
            EnemyId = enemyId;
            Stats = stats;
            Cell = spawn;
            _approach = approach ?? new List<List<Vector2Int>>();
            _wander = wander ?? new List<List<Vector2Int>>();
            _attackRangeRings = Mathf.Max(1, attackRangeRings);
            CurrentHp = Mathf.Max(1f, stats.hp);
        }

        public string EnemyId { get; }
        public UnitStats Stats { get; }
        public GridCell Cell { get; private set; }

        /// <summary>Thứ tự spawn (tăng dần) — dùng làm tie-break tuyệt đối khi xếp ưu tiên tấn công.</summary>
        public int SpawnOrder { get; set; }
        public EnemyState State { get; private set; } = EnemyState.Approaching;
        public bool IsAlive { get; private set; } = true;
        public float CurrentHp { get; private set; }

        // Cờ điều phối trong 1 enemy round (do ArenaEnemyRound quản lý) — cho chain movement.
        public bool ResolvedThisRound { get; set; }
        public bool ResolvingNow { get; set; }

        // ----- Passive (tính từ skill list lúc spawn) -----
        public float DmgReduce;  // giảm % damage nhận (0..1)
        public float RegenPct;   // hồi %/round máu tối đa

        // ----- Skill (chỉ giữ RUNNER — trigger/effect nằm trong skill data) -----
        public ArenaSkillRunner Skills;
        public float MaxHp => Mathf.Max(1f, Stats.hp);

        public Action<ArenaEnemyUnit> OnKilled;      // combat: bắn skill OnDeath (đã nhả ô)
        public Action<ArenaEnemyUnit> OnDamagedHook; // combat: bắn skill OnDamaged/OnLowHp

        /// <summary>Hồi máu passive mỗi round (không vượt máu tối đa).</summary>
        public void Regen()
        {
            if (!IsAlive || RegenPct <= 0f) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + MaxHp * RegenPct);
        }

        /// <summary>Cộng máu (skill heal của enemy). Không vượt máu tối đa.</summary>
        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        // Hook view (tách rời). Truyền vào khi spawn để bật anim/di chuyển model.
        public Action<GridCell> OnMoved;   // di chuyển model tới ô
        public Action OnPrepare;           // anim giơ đòn (telegraph)
        public Action OnAttack;            // anim đánh + trừ máu hero
        public Action OnIdleBlocked;       // bị chặn hết → đứng yên (idle)
        public Action OnWander;            // bắt đầu đi loanh quanh
        public Action OnHurt;              // trúng đòn (view: nháy đỏ)
        public Action OnDied;              // chết (view: anim die + dọn)

        /// <summary>Đánh dấu đã chiếm ô spawn trên lưới (gọi 1 lần khi spawn).</summary>
        public void OccupySpawn(ArenaOccupancy occ) => occ.Set(Cell, this);

        public void Kill(ArenaOccupancy occ)
        {
            if (!IsAlive) return;
            IsAlive = false;
            occ.Clear(Cell);      // nhả ô TRƯỚC → con tách có thể spawn vào ngay ô này
            OnKilled?.Invoke(this); // combat: tách con nếu có cấu hình
        }

        /// <summary>Nhận sát thương; hết máu → chết (nhả ô + fire OnDied).</summary>
        public void TakeDamage(float dmg, ArenaOccupancy occ)
        {
            if (!IsAlive) return;
            if (DmgReduce > 0f) dmg *= Mathf.Max(0f, 1f - DmgReduce); // passive giảm damage
            CurrentHp -= dmg;
            OnHurt?.Invoke();
            if (CurrentHp <= 0f)
            {
                Kill(occ);
                OnDied?.Invoke();
                return;
            }

            OnDamagedHook?.Invoke(this); // còn sống → bắn skill OnDamaged/OnLowHp
        }

        /// <summary>Ép đặt ô (dùng cho đẩy lùi) — cập nhật ô + fire OnMoved để view bám theo.</summary>
        public void SetCellForced(GridCell c)
        {
            Cell = c;
            OnMoved?.Invoke(c);
        }

        /// <summary>
        ///     Xử lý 1 enemy round cho unit này. <paramref name="ensureResolved" /> = callback nhờ resolver
        ///     xử lý TRƯỚC 1 enemy đang chặn ô đích (chain movement); null = không điều phối (test 1 enemy).
        /// </summary>
        public void TakeEnemyTurn(ArenaOccupancy occ, Action<ArenaEnemyUnit> ensureResolved = null,
            Action<ArenaEnemyUnit, GridCell> onEntered = null)
        {
            if (!IsAlive) return;

            switch (State)
            {
                case EnemyState.Preparing:
                    // Round enemy KẾ sau khi giơ đòn → phát động tấn công.
                    State = EnemyState.Attacking;
                    OnAttack?.Invoke();
                    EnterWander(); // đánh xong → đi loanh quanh (nhàn rỗi)
                    break;

                case EnemyState.Wandering:
                    MoveStep(occ, WanderBranches(), ensureResolved, onEntered);
                    if (--_wanderLeft <= 0)
                    {
                        // Hết pattern nhàn rỗi → quay lại tiến vào (lặp lại như trên).
                        State = EnemyState.Approaching;
                        _stepIndex = 0;
                    }

                    break;

                default: // Approaching
                    if (InAttackRange())
                    {
                        // Đã vào tầm → KHÔNG di chuyển nữa, giơ đòn & chờ round enemy kế.
                        State = EnemyState.Preparing;
                        OnPrepare?.Invoke();
                    }
                    else
                    {
                        MoveStep(occ, _approach, ensureResolved, onEntered);
                    }

                    break;
            }
        }

        private void EnterWander()
        {
            State = EnemyState.Wandering;
            _stepIndex = 0;
            var w = WanderBranches();
            // Chạy đúng 1 vòng pattern nhàn rỗi (độ dài nhánh chính), fallback nếu không cấu hình.
            _wanderLeft = w.Count > 0 && w[0].Count > 0 ? w[0].Count : DefaultWanderRounds;
            OnWander?.Invoke();
        }

        private List<List<Vector2Int>> WanderBranches() => _wander.Count > 0 ? _wander : _approach;

        /// <summary>Trong tầm đánh hero (ở tâm) khi ring hiện tại &lt; tầm (số vòng).</summary>
        public bool InAttackRange() => Cell.ring < _attackRangeRings;

        /// <summary>
        ///     Đi 1 bước: thử bước kế của nhánh chính; nếu ô đích bị chặn/ngoài rìa thì thử bước đầu của
        ///     các nhánh phụ (né); hết cách → đứng yên. x = ngang (+CW), y = dọc (+ra ngoài, −vào tâm).
        /// </summary>
        private void MoveStep(ArenaOccupancy occ, List<List<Vector2Int>> branches, Action<ArenaEnemyUnit> ensureResolved,
            Action<ArenaEnemyUnit, GridCell> onEntered)
        {
            if (branches == null || branches.Count == 0 || branches[0].Count == 0)
            {
                OnIdleBlocked?.Invoke();
                return;
            }

            var main = branches[0];
            var target = Apply(occ, Cell, main[_stepIndex % main.Count]);
            if (TryEnter(occ, target, ensureResolved))
            {
                occ.Move(Cell, target, this);
                Cell = target;
                _stepIndex++;
                OnMoved?.Invoke(Cell);
                onEntered?.Invoke(this, Cell); // bước vào ô → check bẫy (có thể bị đẩy lùi)
                return;
            }

            // Bị chặn → né bằng nhánh phụ (không tăng _stepIndex, coi như đường vòng).
            for (int b = 1; b < branches.Count; b++)
            {
                if (branches[b].Count == 0) continue;
                var alt = Apply(occ, Cell, branches[b][0]);
                if (!TryEnter(occ, alt, ensureResolved)) continue;
                occ.Move(Cell, alt, this);
                Cell = alt;
                OnMoved?.Invoke(Cell);
                onEntered?.Invoke(this, Cell); // bước vào ô → check bẫy
                return;
            }

            OnIdleBlocked?.Invoke(); // đứng yên round này
        }

        /// <summary>
        ///     Ô đích có vào được không:
        ///     - trống → được.
        ///     - ngoài rìa/quá tâm → không (đã xử lý ở tầng khác).
        ///     - bị enemy khác chiếm: nếu enemy đó CHƯA đi round này → nhờ resolver xử lý nó trước
        ///       (nó có thể nhường ô) rồi kiểm tra lại; nếu nó ĐÃ đi rồi (hoặc đang trong chuỗi) → chặn.
        /// </summary>
        private bool TryEnter(ArenaOccupancy occ, GridCell target, Action<ArenaEnemyUnit> ensureResolved)
        {
            if (occ.IsFree(target)) return true;
            if (!occ.InBounds(target)) return false;
            if (occ.IsHeroCell(target)) return false; // ô hero (trung tâm) → không dẫm, đệ quy lui lại

            var occupant = occ.Occupant(target) as ArenaEnemyUnit;
            if (occupant != null && occupant.IsAlive && !occupant.ResolvedThisRound && !occupant.ResolvingNow)
            {
                ensureResolved?.Invoke(occupant);
                return occ.IsFree(target);
            }

            return false;
        }

        private static GridCell Apply(ArenaOccupancy occ, GridCell c, Vector2Int move) =>
            new GridCell(c.ring + move.y, occ.Wrap(c.sector + move.x));
    }
}
