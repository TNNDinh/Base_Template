using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Pipeline sát thương dùng chung: passive đỡ đòn → trừ máu → tích damage → passive "đánh trúng" → báo chết.</summary>
    public static class DamagePipeline
    {
        /// <summary>Gây <paramref name="raw" /> sát thương gốc lên target. Trả sát thương thực.</summary>
        public static float Deal(BattleContext ctx, Unit caster, Unit target, float raw, bool triggerDealHooks = true)
        {
            if (target == null || !target.IsAlive) return 0f;
            var wasAlive = target.IsAlive;
            var dmg = DamageFormula.Compute(raw, caster, target, ctx, out var isCrit);

            var passives = target.Passives;
            var blocked = false;
            for (int i = 0; i < passives.Count; i++)
                passives[i].OnIncomingDamage(ctx, caster, target, ref dmg, ref blocked);

            var dealt = target.TakeDamage(dmg);
            ctx.AccumulatedDamage += dealt;

            if (blocked)
                for (int i = 0; i < passives.Count; i++)
                    passives[i].OnAfterBlocked(ctx, caster, target);
            else
                BattleLog.Damage(caster, target, dealt, isCrit);

            // Passive của CASTER khi đánh trúng (vd Bóng đánh cùng) — không chạy khi chính Bóng đang đánh.
            if (triggerDealHooks && caster != null)
            {
                var cp = caster.Passives;
                for (int i = 0; i < cp.Count; i++) cp[i].OnAfterDealDamage(ctx, caster, target, dealt);
            }

            if (wasAlive && !target.IsAlive) NotifyDeath(ctx, target);
            return dealt;
        }

        /// <summary>Giết 1 unit tức thì + kích hoạt hook chết (dùng test/cheat). </summary>
        public static void Kill(BattleContext ctx, Unit u)
        {
            if (u == null || !u.IsAlive) return;
            u.CurrentHp = 0f;
            NotifyDeath(ctx, u);
        }

        private static void NotifyDeath(BattleContext ctx, Unit dead)
        {
            // Snapshot: OnUnitDied có thể thêm unit vào team (Bóng thay hero) → tránh sửa list đang iterate.
            var snapshot = new global::System.Collections.Generic.List<Unit>();
            foreach (var u in ctx.AllUnits()) snapshot.Add(u);
            foreach (var u in snapshot)
            {
                if (u == dead || !u.IsAlive) continue;
                var ps = u.Passives;
                for (int i = 0; i < ps.Count; i++) ps[i].OnUnitDied(ctx, u, dead);
            }
        }
    }

    /// <summary>Gây sát thương qua <see cref="DamagePipeline" />; passive target được gọi qua hook.</summary>
    public class DamageEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive) return;
            DamagePipeline.Deal(ctx, caster, target, RawValue(data, caster));
        }
    }

    /// <summary>Damage tăng theo % máu ĐÃ MẤT của caster (máu ít → dmg to). scaleValue=hệ số gốc, flat=hệ số bonus theo máu mất.</summary>
    public class DamageByLostHpEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive || caster == null) return;
            var max = caster.MaxHp;
            var lostFrac = max > 0f ? 1f - Mathf.Clamp01(caster.CurrentHp / max) : 0f;
            var bonus = data.flat > 0 ? data.flat : 1.5f;               // full máu ×1, cạn máu ×(1+bonus)
            var raw = caster.GetStat(StatType.Atk) * data.scaleValue * (1f + lostFrac * bonus);
            DamagePipeline.Deal(ctx, caster, target, raw);
        }
    }

    /// <summary>Cướp % SPD của target → cộng cho caster (debuff SPD target + buff SPD caster, cùng %).</summary>
    public class SpeedStealEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive || caster == null) return;
            var pct = data.scaleValue; // 0.15 = cướp 15% SPD
            if (pct <= 0f) return;
            var dur = data.duration <= 0 ? 9999 : data.duration;
            target.AddBuff(new ActiveBuff(SpdMod("spd_steal_dn", EffectCategory.Debuff, -pct, 3), dur, caster, 0f));
            caster.AddBuff(new ActiveBuff(SpdMod("spd_steal_up", EffectCategory.Buff, +pct, 3), dur, caster, 0f));
        }

        internal static EffectModel SpdMod(string id, EffectCategory cat, float value, int maxStack) => new EffectModel
        {
            id = id, category = cat, stat = StatType.Spd, modType = ModType.Percent,
            value = value, tickScaleStat = StatType.None, maxStack = maxStack, dispellable = cat == EffectCategory.Debuff
        };
    }

    /// <summary>Hồi máu caster theo % TỔNG sát thương skill này đã gây (đặt SAU các effect Damage). scaleValue=% (0.3=30%).</summary>
    public class LifestealEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            if (caster == null || !caster.IsAlive) return;
            var heal = ctx.AccumulatedDamage * data.scaleValue;
            if (heal <= 0f) return;
            caster.Heal(heal);
            BattleLog.Info($"{caster.DisplayName} hút {heal:0} HP (lifesteal {data.scaleValue:P0})");
        }
    }

    /// <summary>Hồi máu theo scale stat của caster.</summary>
    public class HealEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive) return;
            var amount = RawValue(data, caster);
            target.Heal(amount);
            BattleLog.Info($"{caster.DisplayName} hồi {amount:0} HP cho {target.DisplayName}");
        }
    }

    /// <summary>Tạo khiên hấp thụ sát thương.</summary>
    public class ShieldEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive) return;
            target.AddShield(RawValue(data, caster));
        }
    }

    /// <summary>Áp buff (có lợi) lên ally/self.</summary>
    public class ApplyBuffEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            ApplyEffectShared.AddBuff(ctx, data, caster, target);
        }
    }

    /// <summary>Áp debuff (bất lợi) lên địch — có thể trượt theo chance.</summary>
    public class ApplyDebuffEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            ApplyEffectShared.AddBuff(ctx, data, caster, target);
        }
    }

    /// <summary>Gỡ debuff trên ally.</summary>
    public class CleanseEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive) return;
            target.CleanseDebuffs();
        }
    }

    /// <summary>Cộng năng lượng (cho ultimate).</summary>
    public class EnergyGainEffect : SkillEffectBase
    {
        public override void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            if (target == null) return;
            target.GainEnergy(RawValue(data, caster));
        }
    }

    /// <summary>Logic dùng chung cho ApplyBuff/ApplyDebuff.</summary>
    internal static class ApplyEffectShared
    {
        public static void AddBuff(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive) return;
            if (!ctx.Roll(data.chance)) return;
            if (string.IsNullOrEmpty(data.buffId)) return;

            var effect = BattleDatabase.GetEffect(data.buffId);
            if (string.IsNullOrEmpty(effect.id)) return;

            float tick = 0f;
            if (effect.category == EffectCategory.Dot || effect.category == EffectCategory.Hot)
            {
                var src = effect.tickScaleStat == StatType.None ? 0f : caster.GetStat(effect.tickScaleStat);
                tick = src * effect.tickValue;
            }

            target.AddBuff(new ActiveBuff(effect, data.duration, caster, tick));
            BattleLog.Info($"{target.DisplayName} dính {effect.id} ({data.duration} turn)");
        }
    }

    /// <summary>
    ///     Factory tạo instance effect theo type — mirror <c>ItemRoleFactory.Get(roleType)</c>.
    ///     Thêm loại mới = thêm 1 class + 1 case ở đây.
    /// </summary>
    public static class SkillEffectFactory
    {
        public static SkillEffectBase Get(EffectType type)
        {
            switch (type)
            {
                case EffectType.Damage: return new DamageEffect();
                case EffectType.Heal: return new HealEffect();
                case EffectType.Shield: return new ShieldEffect();
                case EffectType.ApplyBuff: return new ApplyBuffEffect();
                case EffectType.ApplyDebuff: return new ApplyDebuffEffect();
                case EffectType.Cleanse: return new CleanseEffect();
                case EffectType.EnergyGain: return new EnergyGainEffect();
                case EffectType.SpeedSteal: return new SpeedStealEffect();
                case EffectType.Lifesteal: return new LifestealEffect();
                case EffectType.DamageByLostHp: return new DamageByLostHpEffect();
                default:
                    Debug.LogWarning($"[Battle] EffectType chưa hỗ trợ: {type}");
                    return null;
            }
        }
    }
}
