using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Logic chiến đấu thuần (KHÔNG UI) — build unit, turn order theo SPD, pipeline cast skill,
    ///     AI/auto đơn giản. Có thể chạy headless để test & auto-battle.
    /// </summary>
    public static class BattleService
    {
        #region Constants

        private const float EnergyPerAction = 10f;
        private const int SafetyMaxRounds = 100;

        /// <summary>Speed tiêu hao mỗi đòn. Đầu round pool = SPD; còn &gt;0 thì unit được đánh tiếp.</summary>
        private const float ActionCost = 100f;

        /// <summary>Trần số hành động/round (an toàn tránh loop vô hạn).</summary>
        private const int MaxActionsPerRound = 64;

        #endregion

        #region Build

        /// <summary>Tạo 1 <see cref="Unit" /> runtime từ hero người chơi sở hữu (áp cả trang bị).</summary>
        public static Unit BuildUnit(OwnedHero owned, BattleTeam team, int slot)
        {
            var hero = BattleDatabase.GetHero(owned.heroId);
            var data = BuildData(hero, owned.level, owned.star, team, slot);
            EquipmentService.Apply(data, owned); // stat + effect + skill từ trang bị
            var unit = new Unit();
            unit.SetData(data);
            return unit;
        }

        /// <summary>Tạo unit địch trực tiếp từ config (không trang bị).</summary>
        public static Unit BuildEnemy(string heroId, int level, int star, int slot)
        {
            var hero = BattleDatabase.GetHero(heroId);
            var data = BuildData(hero, level, star, BattleTeam.Enemy, slot);
            var unit = new Unit();
            unit.SetData(data);
            return unit;
        }

        /// <summary>Tạo bot cho ải: dựng enemy rồi áp hệ số mul để tinh chỉnh độ khó.</summary>
        public static Unit BuildBot(StageBotModel bot)
        {
            var lvl = bot.level <= 0 ? 1 : bot.level;
            var star = bot.star <= 0 ? 1 : bot.star;
            var unit = BuildEnemy(bot.enemyId, lvl, star, bot.slot);

            unit.MultiplyBaseStat(StatType.Hp, Norm(bot.hpMul));
            unit.MultiplyBaseStat(StatType.Atk, Norm(bot.atkMul));
            unit.MultiplyBaseStat(StatType.Def, Norm(bot.defMul));
            unit.InitVitals(); // reset HP theo max mới sau khi nhân
            return unit;
        }

        private static float Norm(float mul) => mul <= 0f ? 1f : mul;

        /// <summary>Dựng dữ liệu khởi tạo unit (stat/skill/passive theo sao). Trang bị áp riêng ở BuildUnit.</summary>
        private static UnitRuntimeData BuildData(HeroModel hero, int level, int star, BattleTeam team, int slot)
        {
            // Loadout theo sao: đổi skill chính (nếu hero có cấu hình)
            var activeSkillId = hero.skill2Id;
            if (BattleDatabase.HasLoadout(hero.id, star))
            {
                var lo = BattleDatabase.GetLoadout(hero.id, star);
                if (!string.IsNullOrEmpty(lo.activeSkillId)) activeSkillId = lo.activeSkillId;
            }

            var stats = HeroStatService.BuildStats(hero, level, star);
            if (stats.ContainsKey(StatType.Hp)) stats[StatType.Hp] *= BattleTuning.HpMultiplier; // TEST: tăng máu

            return new UnitRuntimeData
            {
                heroId = hero.id,
                modelKey = hero.modelKey,
                displayName = hero.name,
                element = hero.element,
                team = team,
                slot = slot,
                basicSkillId = hero.basicSkillId,
                activeSkillId = activeSkillId,
                ultimateId = hero.ultimateId,
                maxMana = hero.maxMana,
                startMana = hero.startMana,
                manaPerAttack = hero.manaPerAttack,
                manaOnHit = hero.manaOnHit,
                attackRange = hero.attackRange,
                baseStats = stats,
                passives = PassiveFactory.BuildPassives(hero.id, star) // role/hành vi từ config
            };
        }

        public static BattleContext CreateBattle(List<Unit> playerTeam, List<Unit> enemyTeam, int seed = 12345)
        {
            return new BattleContext(playerTeam, enemyTeam, seed);
        }

        #endregion

        #region Turn loop

        /// <summary>Chạy auto toàn trận tới khi 1 phe gục. Trả về phe thắng.</summary>
        public static BattleTeam RunAuto(BattleContext ctx)
        {
            int round = 0;
            while (!ctx.IsOver && round < SafetyMaxRounds)
            {
                ctx.TurnCount = round + 1;
                BattleLog.Round(ctx.TurnCount);

                RefillSpeedPools(ctx);
                int acts = 0;
                while (!ctx.IsOver && acts++ < MaxActionsPerRound)
                {
                    var unit = PickNextActor(ctx);
                    if (unit == null) break;      // không ai còn speed → hết round
                    unit.SpeedPool -= ActionCost; // đánh 1 đòn tốn ActionCost
                    TakeTurn(unit, ctx);
                }

                round++;
            }

            var winner = ctx.PlayerAlive ? BattleTeam.Player : BattleTeam.Enemy;
            BattleLog.Info($"=== KẾT THÚC: {winner} thắng (sau {ctx.TurnCount} round) ===");
            return winner;
        }

        /// <summary>
        ///     Như <see cref="RunAuto" /> nhưng chờ <paramref name="turnDelay" /> giây sau mỗi lượt
        ///     để view kịp phát anim (đánh có hình). Dùng từ BattleSceneController.
        /// </summary>
        public static async UniTask<BattleTeam> RunAutoVisual(BattleContext ctx, float turnDelay, CancellationToken ct)
        {
            int round = 0;
            while (!ctx.IsOver && round < SafetyMaxRounds)
            {
                ctx.TurnCount = round + 1;
                BattleLog.Round(ctx.TurnCount);

                RefillSpeedPools(ctx);
                int acts = 0;
                while (!ctx.IsOver && acts++ < MaxActionsPerRound)
                {
                    var unit = PickNextActor(ctx);
                    if (unit == null) break;
                    unit.SpeedPool -= ActionCost;
                    TakeTurn(unit, ctx);
                    if (turnDelay > 0f)
                        await UniTask.Delay(TimeSpan.FromSeconds(turnDelay), cancellationToken: ct);
                }

                round++;
            }

            var winner = ctx.PlayerAlive ? BattleTeam.Player : BattleTeam.Enemy;
            BattleLog.Info($"=== KẾT THÚC: {winner} thắng (sau {ctx.TurnCount} round) ===");
            return winner;
        }

        /// <summary>Đầu mỗi round: nạp lại pool = SPD cho mọi unit còn sống (reset, không carry-over).</summary>
        private static void RefillSpeedPools(BattleContext ctx)
        {
            foreach (var u in ctx.AllUnits())
                if (u.IsAlive)
                    u.SpeedPool = u.GetStat(StatType.Spd);
        }

        /// <summary>
        ///     Chọn unit hành động kế: còn sống &amp; <c>SpeedPool &gt; 0</c>, lấy pool cao nhất
        ///     (ưu tiên hero nhanh). Hòa pool: phe Player trước, rồi slot tăng. Null nếu không ai còn speed.
        /// </summary>
        private static Unit PickNextActor(BattleContext ctx)
        {
            Unit best = null;
            foreach (var u in ctx.AllUnits())
            {
                if (!u.IsAlive || u.SpeedPool <= 0f) continue;
                if (best == null) { best = u; continue; }

                var cmp = u.SpeedPool.CompareTo(best.SpeedPool); // pool cao hơn thắng
                if (cmp == 0) cmp = best.Team.CompareTo(u.Team); // hòa: Player (enum nhỏ hơn) trước
                if (cmp == 0) cmp = best.Slot.CompareTo(u.Slot); // rồi slot nhỏ hơn
                if (cmp > 0) best = u;
            }

            return best;
        }

        private static void TakeTurn(Unit unit, BattleContext ctx)
        {
            unit.TickBuffsTurnStart();
            unit.RunPassivesTurnStart(ctx);
            if (!unit.IsAlive) return; // chết do DoT

            var skill = ChooseSkill(unit);
            var primary = ChoosePrimaryTarget(unit, skill, ctx);
            CastSkill(unit, skill, primary, ctx);

            unit.GainEnergy(EnergyPerAction);       // sạc năng lượng cho Ultimate
            unit.GainMana(unit.ManaPerAttack);       // hồi mana mỗi lần hành động
        }

        #endregion

        #region Cast pipeline

        /// <summary>Thi triển 1 skill: lặp hitCount → từng effect (theo order) → resolve target → apply.</summary>
        public static void CastSkill(Unit caster, SkillModel skill, Unit primaryTarget, BattleContext ctx)
        {
            if (string.IsNullOrEmpty(skill.id)) return;

            BattleLog.Info($"{caster.DisplayName} dùng [{skill.name}]");

            ctx.CurrentSkillType = skill.type; // để passive phân biệt đòn thường vs skill
            ctx.AccumulatedDamage = 0f;        // reset để Lifesteal tính đúng damage skill này
            if (skill.energyCost > 0) caster.GainEnergy(-skill.energyCost); // Ultimate: xả Energy
            if (skill.manaCost > 0) caster.SpendMana(skill.manaCost);        // Active: tiêu Mana

            var hits = skill.hitCount <= 0 ? 1 : skill.hitCount;
            for (int h = 0; h < hits; h++)
            {
                var effects = BattleDatabase.GetSkillEffects(skill.id);
                for (int i = 0; i < effects.Count; i++)
                {
                    var se = effects[i];
                    var targetType = se.target == TargetType.Inherit ? skill.targetType : se.target;
                    var targets = TargetResolver.Resolve(targetType, caster, primaryTarget, ctx);

                    var handler = SkillEffectFactory.Get(se.effectType);
                    if (handler == null) continue;

                    for (int t = 0; t < targets.Count; t++)
                        handler.Apply(ctx, se, caster, targets[t]);
                }
            }

        }

        #endregion

        #region Simple AI / auto

        /// <summary>
        ///     Ưu tiên: Ultimate (đủ Energy) → Active/skill trang bị (đủ Mana) → Basic (miễn phí).
        ///     Không còn cooldown — skill gate hoàn toàn bằng tài nguyên.
        /// </summary>
        private static SkillModel ChooseSkill(Unit unit)
        {
            // Ultimate: cần đủ Energy (thường đầy 100)
            if (!string.IsNullOrEmpty(unit.UltimateId))
            {
                var ult = BattleDatabase.GetSkill(unit.UltimateId);
                if (!string.IsNullOrEmpty(ult.id) && unit.Energy >= ult.energyCost)
                    return ult;
            }

            // Skill từ trang bị: cần đủ Mana
            var extras = unit.ExtraSkillIds;
            for (int i = 0; i < extras.Count; i++)
            {
                if (string.IsNullOrEmpty(extras[i])) continue;
                var ex = BattleDatabase.GetSkill(extras[i]);
                if (!string.IsNullOrEmpty(ex.id) && ex.manaCost > 0 && unit.Mana >= ex.manaCost)
                    return ex;
            }

            // Active: cần đủ Mana
            if (!string.IsNullOrEmpty(unit.ActiveSkillId))
            {
                var act = BattleDatabase.GetSkill(unit.ActiveSkillId);
                if (!string.IsNullOrEmpty(act.id) && act.manaCost > 0 && unit.Mana >= act.manaCost)
                    return act;
            }

            return BattleDatabase.GetSkill(unit.BasicSkillId);
        }

        /// <summary>MVP: nhắm địch còn sống có HP thấp nhất.</summary>
        private static Unit ChoosePrimaryTarget(Unit unit, SkillModel skill, BattleContext ctx)
        {
            var enemies = ctx.EnemiesOf(unit);
            Unit lowest = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!enemies[i].IsAlive) continue;
                if (lowest == null || enemies[i].CurrentHp < lowest.CurrentHp) lowest = enemies[i];
            }

            return lowest;
        }

        #endregion
    }
}
