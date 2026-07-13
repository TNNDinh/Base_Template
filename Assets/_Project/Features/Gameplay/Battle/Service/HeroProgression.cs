using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Tính chỉ số hero theo level + star. KHÔNG persist stat đã tính — luôn tính lại từ config.
    ///     Công thức: finalStat = (base + growth × level) × starMultiplier. SPD KHÔNG nhân theo star.
    /// </summary>
    public static class HeroStatService
    {
        /// <summary>Các stat không nhân theo bậc sao (tránh speed-creep & crit-creep).</summary>
        private static bool ScalesWithStar(StatType stat) =>
            stat == StatType.Hp || stat == StatType.Atk || stat == StatType.Def;

        public static float ComputeStat(HeroModel hero, int level, int star, StatType stat)
        {
            var baseV = hero.BaseOf(stat);
            var growth = hero.GrowthOf(stat);
            var raw = baseV + growth * level;

            if (!ScalesWithStar(stat)) return raw;

            var starCfg = BattleDatabase.GetStar(star);
            var mult = starCfg.statMultiplier > 0f ? starCfg.statMultiplier : 1f;
            return raw * mult;
        }

        /// <summary>Build dict chỉ số gốc (theo level + star) để nhồi vào <see cref="UnitRuntimeData" />.</summary>
        public static Dictionary<StatType, float> BuildStats(HeroModel hero, int level, int star)
        {
            return new Dictionary<StatType, float>
            {
                { StatType.Hp, ComputeStat(hero, level, star, StatType.Hp) },
                { StatType.Atk, ComputeStat(hero, level, star, StatType.Atk) },
                { StatType.Def, ComputeStat(hero, level, star, StatType.Def) },
                { StatType.Spd, ComputeStat(hero, level, star, StatType.Spd) },
                { StatType.CritRate, ComputeStat(hero, level, star, StatType.CritRate) },
                { StatType.CritDmg, ComputeStat(hero, level, star, StatType.CritDmg) },
                { StatType.DamageReduction, ComputeStat(hero, level, star, StatType.DamageReduction) }
            };
        }
    }

    /// <summary>Nâng sao cho hero sở hữu. Điều kiện: max level của bậc hiện tại + đủ chi phí.</summary>
    public static class HeroStarUpService
    {
        public const int MaxStar = 6;

        public static bool CanStarUp(OwnedHero owned, out HeroStarCostModel cost, out string reason)
        {
            cost = BattleDatabase.GetStarCost(owned.star);
            reason = null;

            if (owned.star >= MaxStar)
            {
                reason = "Đã đạt sao tối đa";
                return false;
            }

            var curStar = BattleDatabase.GetStar(owned.star);
            if (owned.level < curStar.levelCap)
            {
                reason = $"Cần đạt level tối đa {curStar.levelCap} của {owned.star}★";
                return false;
            }

            // TODO: [Battle] - check đủ dupe/material + gold qua Inventory/Currency của base.
            return true;
        }

        /// <summary>Thực thi lên sao. Trả về true nếu thành công.</summary>
        public static bool StarUp(OwnedHero owned)
        {
            if (!CanStarUp(owned, out _, out var reason))
            {
                Debug.LogWarning($"[Battle] Không thể lên sao: {reason}");
                return false;
            }

            // TODO: [Battle] - trừ chi phí thật ở đây (ConsumeCost).
            owned.star++;

            var newStar = BattleDatabase.GetStar(owned.star);
            if (!string.IsNullOrEmpty(newStar.unlockSkillId) && !owned.unlockedSkillIds.Contains(newStar.unlockSkillId))
                owned.unlockedSkillIds.Add(newStar.unlockSkillId);

            // TODO: [Battle] - PlayerDataManager.HeroData.Save() + EmitEvent(HeroStarUp) khi đã wire persistence.
            Debug.Log($"[Battle] {owned.heroId} lên {owned.star}★");
            return true;
        }
    }
}
