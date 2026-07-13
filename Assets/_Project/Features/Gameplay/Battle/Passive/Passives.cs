namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Passive đỡ đòn (SKILL-based): proc theo tỷ lệ. Khi đỡ thành công:
    ///     - đòn THƯỜNG  → đỡ hoàn toàn (0 sát thương).
    ///     - đòn SKILL   → hạ xuống chỉ còn sát thương của 1 đòn thường.
    ///     Tuỳ chọn phản công khi đỡ. (Khác với đỡ đòn ở STAT = giảm sát thương % liên tục.)
    /// </summary>
    public class BlockPassive : PassiveBase
    {
        private readonly float _chance;
        private readonly bool _counter;
        private readonly float _counterScale;

        public BlockPassive(float chance, bool counter, float counterScale)
        {
            _chance = chance;
            _counter = counter;
            _counterScale = counterScale;
        }

        public override void OnIncomingDamage(BattleContext ctx, Unit attacker, Unit self,
            ref float dmg, ref bool blocked)
        {
            if (_chance <= 0f || !ctx.Roll(_chance)) return;
            blocked = true;

            if (ctx.CurrentSkillType == SkillType.Basic)
            {
                dmg = 0f;
                BattleLog.Info($"{self.DisplayName} ĐỠ HOÀN TOÀN đòn thường của {attacker.DisplayName}!");
            }
            else
            {
                dmg = DamageFormula.NormalAttackDamage(attacker, self, ctx);
                BattleLog.Info($"{self.DisplayName} ĐỠ ĐÒN: skill của {attacker.DisplayName} bị hạ thành đòn thường ({dmg:0})");
            }
        }

        public override void OnAfterBlocked(BattleContext ctx, Unit attacker, Unit self)
        {
            if (!_counter || attacker == null || !attacker.IsAlive || !self.IsAlive) return;

            var raw = self.GetStat(StatType.Atk) * _counterScale;
            var dmg = DamageFormula.Compute(raw, self, attacker, ctx, out var isCrit);
            var dealt = attacker.TakeDamage(dmg);
            BattleLog.Info($"{self.DisplayName} PHẢN CÔNG {attacker.DisplayName}: {dealt:0}{(isCrit ? " (CRIT)" : "")}");
        }
    }

    /// <summary>+% SPD vĩnh viễn (áp buff 1 lần đầu trận).</summary>
    public class SpeedAuraPassive : PassiveBase
    {
        private readonly float _pct;
        private bool _applied;

        public SpeedAuraPassive(float pct) => _pct = pct;

        public override void OnTurnStart(BattleContext ctx, Unit self)
        {
            if (_applied || _pct <= 0f || self == null) return;
            _applied = true;
            self.AddBuff(new ActiveBuff(SpeedStealEffect.SpdMod("spd_aura", EffectCategory.Buff, _pct, 1), 9999, self, 0f));
        }
    }

    /// <summary>
    ///     Bóng đi theo hero: mỗi khi hero đánh trúng, bóng cũng đánh target (dmgScale × ATK),
    ///     cướp % SPD target cho hero, hút máu cho hero theo % sát thương bóng gây.
    ///     (Phần "bóng thay hero chết / hồi sinh" xử lý ở <see cref="OnUnitDied" /> — phase sau.)
    /// </summary>
    public class ShadowPassive : PassiveBase
    {
        private readonly float _dmgScale;
        private readonly float _speedSteal;
        private readonly float _lifesteal;

        private const float ShadowHeroStatMul = 0.6f; // bóng-hero mạnh 60% chủ

        private Unit _shadowHero;  // bóng đang hoá hero (null = chưa)
        private Unit _replacedAlly; // đồng minh chết mà bóng thế chỗ (để trả về khi hồi sinh)

        public ShadowPassive(float dmgScale, float speedSteal, float lifesteal)
        {
            _dmgScale = dmgScale;
            _speedSteal = speedSteal;
            _lifesteal = lifesteal;
        }

        public override void OnAfterDealDamage(BattleContext ctx, Unit self, Unit target, float dealt)
        {
            if (ctx.InShadowAttack || target == null || !target.IsAlive || self == null || !self.IsAlive) return;

            ctx.InShadowAttack = true; // chặn đệ quy (đòn bóng không proc lại bóng)
            var raw = self.GetStat(StatType.Atk) * _dmgScale;
            var shadowDealt = DamagePipeline.Deal(ctx, self, target, raw, false);

            if (_speedSteal > 0f && target.IsAlive)
            {
                target.AddBuff(new ActiveBuff(SpeedStealEffect.SpdMod("shadow_spd_dn", EffectCategory.Debuff, -_speedSteal, 6), 9999, self, 0f));
                self.AddBuff(new ActiveBuff(SpeedStealEffect.SpdMod("shadow_spd_up", EffectCategory.Buff, +_speedSteal, 6), 9999, self, 0f));
            }

            if (_lifesteal > 0f) self.Heal(shadowDealt * _lifesteal);
            BattleLog.Info($"Bóng của {self.DisplayName} đánh {target.DisplayName}: {shadowDealt:0} (cướp SPD + hút máu)");
            ctx.InShadowAttack = false;
        }

        /// <summary>Đồng minh chết → triệu hồi bóng-hero ở ô đó. Bóng-hero chết → bóng về (thu hồi).</summary>
        public override void OnUnitDied(BattleContext ctx, Unit self, Unit dead)
        {
            if (dead == null || self == null) return;

            // Bóng-hero của MÌNH vừa chết → thu hồi (view huỷ).
            if (_shadowHero != null && dead == _shadowHero)
            {
                BattleLog.DespawnUnit(_shadowHero);
                BattleLog.Info($"Bóng của {self.DisplayName} tan biến (về lại chủ)");
                _shadowHero = null;
                _replacedAlly = null;
                return;
            }

            // Đồng minh (không phải mình, cùng phe) chết + chưa có bóng-hero → triệu hồi bóng ở ô đó.
            if (_shadowHero == null && dead != self && dead.Team == self.Team && self.IsAlive)
            {
                var data = self.MakeShadowData(dead.Slot, ShadowHeroStatMul);
                _shadowHero = new Unit();
                _shadowHero.SetData(data);
                ctx.AlliesOf(self).Add(_shadowHero); // tham chiến từ round sau
                _replacedAlly = dead;
                BattleLog.SpawnUnit(_shadowHero);
                BattleLog.Info($"{dead.DisplayName} gục → Bóng của {self.DisplayName} hiện thân thế chỗ!");
            }
        }

        /// <summary>Nếu đồng minh bị thế chỗ hồi sinh → bóng-hero rút về.</summary>
        public override void OnTurnStart(BattleContext ctx, Unit self)
        {
            if (_shadowHero != null && _replacedAlly != null && _replacedAlly.IsAlive)
            {
                if (_shadowHero.IsAlive) _shadowHero.CurrentHp = 0f; // "rút" = kết thúc bóng-hero
                BattleLog.DespawnUnit(_shadowHero);
                BattleLog.Info($"{_replacedAlly.DisplayName} hồi sinh → Bóng về lại {self.DisplayName}");
                _shadowHero = null;
                _replacedAlly = null;
            }
        }
    }

    // TODO: [Battle] - EvasionPassive, RegenPassive, DamageReductionPassive...
}
