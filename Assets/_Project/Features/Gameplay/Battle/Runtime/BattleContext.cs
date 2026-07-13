using System;
using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Trạng thái 1 trận đấu: 2 đội, RNG, đếm turn. Truyền vào mọi effect.</summary>
    public class BattleContext
    {
        #region Fields

        public readonly List<Unit> PlayerTeam;
        public readonly List<Unit> EnemyTeam;
        public readonly Random Rng;
        public int TurnCount;

        /// <summary>Loại đòn đang được thi triển (đặt bởi CastSkill) — để passive phân biệt đòn thường vs skill.</summary>
        public SkillType CurrentSkillType = SkillType.Basic;

        /// <summary>Tổng sát thương skill hiện tại đã gây (reset ở CastSkill) — cho Lifesteal effect.</summary>
        public float AccumulatedDamage;

        /// <summary>Đang trong đòn của Bóng (chặn đệ quy passive Shadow).</summary>
        public bool InShadowAttack;

        #endregion

        public BattleContext(List<Unit> playerTeam, List<Unit> enemyTeam, int seed)
        {
            PlayerTeam = playerTeam;
            EnemyTeam = enemyTeam;
            Rng = new Random(seed);
        }

        #region Queries

        public List<Unit> AlliesOf(Unit u) => u.Team == BattleTeam.Player ? PlayerTeam : EnemyTeam;
        public List<Unit> EnemiesOf(Unit u) => u.Team == BattleTeam.Player ? EnemyTeam : PlayerTeam;

        public IEnumerable<Unit> AllUnits()
        {
            foreach (var u in PlayerTeam) yield return u;
            foreach (var u in EnemyTeam) yield return u;
        }

        public bool PlayerAlive => HasAlive(PlayerTeam);
        public bool EnemyAlive => HasAlive(EnemyTeam);
        public bool IsOver => !PlayerAlive || !EnemyAlive;

        private static bool HasAlive(List<Unit> team)
        {
            for (int i = 0; i < team.Count; i++)
                if (team[i].IsAlive)
                    return true;
            return false;
        }

        /// <summary>Roll xác suất [0,1) &lt; chance.</summary>
        public bool Roll(float chance) => Rng.NextDouble() < chance;

        #endregion
    }
}
