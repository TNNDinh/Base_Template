using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Bảng khắc chế nguyên tố.</summary>
    public static class ElementChart
    {
        public const float Advantage = 1.30f;
        public const float Disadvantage = 0.70f;
        public const float Neutral = 1.00f;

        /// <summary>Hệ số sát thương khi attacker hệ X đánh defender hệ Y.</summary>
        public static float Multiplier(ElementType attacker, ElementType defender)
        {
            // Tam giác: Fire > Wind > Water > Fire. Light <-> Dark khắc chế lẫn nhau.
            if (IsStrong(attacker, defender)) return Advantage;
            if (IsStrong(defender, attacker)) return Disadvantage;
            return Neutral;
        }

        private static bool IsStrong(ElementType a, ElementType b)
        {
            switch (a)
            {
                case ElementType.Fire: return b == ElementType.Wind;
                case ElementType.Wind: return b == ElementType.Water;
                case ElementType.Water: return b == ElementType.Fire;
                case ElementType.Light: return b == ElementType.Dark;
                case ElementType.Dark: return b == ElementType.Light;
                default: return false;
            }
        }
    }

    /// <summary>Công thức sát thương dùng chung.</summary>
    public static class DamageFormula
    {
        /// <summary>Hằng số giảm trừ theo giáp: mitigation = DEF / (DEF + K).</summary>
        public const float DefConstant = 300f;

        /// <summary>
        ///     Tính sát thương cuối: raw → trừ giáp → khắc chế hệ → crit.
        /// </summary>
        public static float Compute(float raw, Unit caster, Unit target, BattleContext ctx, out bool isCrit)
        {
            var def = target.GetStat(StatType.Def);
            var mitigation = def / (def + DefConstant);
            var afterDef = raw * (1f - mitigation);

            var elem = ElementChart.Multiplier(caster.Element, target.Element);
            var dmg = afterDef * elem;

            isCrit = ctx.Roll(caster.GetStat(StatType.CritRate));
            if (isCrit) dmg *= caster.GetStat(StatType.CritDmg);

            // Đỡ đòn ở STAT: giảm sát thương % (cap 90% tránh miễn nhiễm tuyệt đối)
            var dr = target.GetStat(StatType.DamageReduction);
            if (dr > 0f)
            {
                if (dr > 0.9f) dr = 0.9f;
                dmg *= 1f - dr;
            }

            dmg *= BattleTuning.DamageMultiplier; // TEST: giảm damage cho trận dài dễ quan sát
            return dmg < 0f ? 0f : dmg;
        }

        /// <summary>Sát thương của 1 đòn thường (basic skill) từ attacker — dùng khi block hạ skill thành đòn thường.</summary>
        public static float NormalAttackDamage(Unit attacker, Unit target, BattleContext ctx)
        {
            var basic = BattleDatabase.GetSkill(attacker.BasicSkillId);
            float scale = 1f;
            if (!string.IsNullOrEmpty(basic.id))
            {
                var effs = BattleDatabase.GetSkillEffects(basic.id);
                for (int i = 0; i < effs.Count; i++)
                    if (effs[i].effectType == EffectType.Damage)
                    {
                        scale = effs[i].scaleValue;
                        break;
                    }
            }

            var raw = attacker.GetStat(StatType.Atk) * scale;
            return Compute(raw, attacker, target, ctx, out _);
        }
    }

    /// <summary>Phân giải danh sách mục tiêu theo <see cref="TargetType" />.</summary>
    public static class TargetResolver
    {
        public static List<Unit> Resolve(TargetType type, Unit caster, Unit primaryTarget, BattleContext ctx)
        {
            var result = new List<Unit>();
            var enemies = AliveOf(ctx.EnemiesOf(caster));
            var allies = AliveOf(ctx.AlliesOf(caster));

            switch (type)
            {
                case TargetType.Self:
                    result.Add(caster);
                    break;
                case TargetType.EnemySingle:
                case TargetType.AllySingle:
                    if (primaryTarget != null && primaryTarget.IsAlive) result.Add(primaryTarget);
                    else if (type == TargetType.EnemySingle && enemies.Count > 0) result.Add(enemies[0]);
                    else if (type == TargetType.AllySingle && allies.Count > 0) result.Add(allies[0]);
                    break;
                case TargetType.EnemyAll:
                    result.AddRange(enemies);
                    break;
                case TargetType.AllyAll:
                    result.AddRange(allies);
                    break;
                case TargetType.EnemyLowestHp:
                    AddLowestHp(enemies, result);
                    break;
                case TargetType.AllyLowestHp:
                    AddLowestHp(allies, result);
                    break;
                case TargetType.EnemyRandom:
                    if (enemies.Count > 0) result.Add(enemies[ctx.Rng.Next(enemies.Count)]);
                    break;
                case TargetType.EnemyRow:
                    AddFrontRow(enemies, result);
                    break;
            }

            return result;
        }

        private static List<Unit> AliveOf(List<Unit> team)
        {
            var list = new List<Unit>();
            for (int i = 0; i < team.Count; i++)
                if (team[i].IsAlive)
                    list.Add(team[i]);
            return list;
        }

        /// <summary>Hàng đầu = slot thuộc row 0 (slot / Cols == 0). Chết hết → fallback toàn bộ.</summary>
        private static void AddFrontRow(List<Unit> pool, List<Unit> result)
        {
            for (int i = 0; i < pool.Count; i++)
                if (pool[i].Slot / BattleFormation.Cols == 0)
                    result.Add(pool[i]);
            if (result.Count == 0) result.AddRange(pool);
        }

        private static void AddLowestHp(List<Unit> pool, List<Unit> result)
        {
            Unit lowest = null;
            for (int i = 0; i < pool.Count; i++)
                if (lowest == null || pool[i].CurrentHp < lowest.CurrentHp)
                    lowest = pool[i];
            if (lowest != null) result.Add(lowest);
        }
    }
}
