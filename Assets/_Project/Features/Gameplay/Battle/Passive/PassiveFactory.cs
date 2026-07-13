using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Tạo instance passive từ config (mirror <see cref="SkillEffectFactory" />) và build danh sách
    ///     passive cho 1 hero tại 1 bậc sao (đọc join <c>HeroStarPassive</c> → <c>Passives</c> config).
    /// </summary>
    public static class PassiveFactory
    {
        public static PassiveBase Get(PassiveConfigModel cfg)
        {
            switch (cfg.type)
            {
                case PassiveType.Block:
                    return new BlockPassive(cfg.value, cfg.flag, cfg.value2); // value=tỷ lệ, flag=phản công, value2=hệ số phản
                case PassiveType.SpeedAura:
                    return new SpeedAuraPassive(cfg.value); // value=% SPD
                case PassiveType.Shadow:
                    return new ShadowPassive(cfg.value, cfg.value2, cfg.value3); // dmgScale / %speedSteal / %lifesteal
                case PassiveType.None:
                    return null;
                default:
                    Debug.LogWarning($"[Battle] PassiveType chưa hỗ trợ: {cfg.type}");
                    return null;
            }
        }

        /// <summary>Danh sách passive của hero tại bậc sao (rỗng nếu hero không có role passive nào).</summary>
        public static List<PassiveBase> BuildPassives(string heroId, int star)
        {
            var list = new List<PassiveBase>();
            var ids = BattleDatabase.GetHeroStarPassives(heroId, star);
            for (int i = 0; i < ids.Count; i++)
            {
                var cfg = BattleDatabase.GetPassiveConfig(ids[i]);
                if (string.IsNullOrEmpty(cfg.id)) continue;
                var p = Get(cfg);
                if (p != null) list.Add(p);
            }

            return list;
        }
    }
}
