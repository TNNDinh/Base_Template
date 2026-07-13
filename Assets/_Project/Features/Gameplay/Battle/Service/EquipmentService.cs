using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Áp trang bị của hero vào <see cref="UnitRuntimeData" /> lúc build unit:
    ///     cộng stat (flat trước, percent sau), thêm passive (effect), và gộp skill riêng của món đồ.
    /// </summary>
    public static class EquipmentService
    {
        public static void Apply(UnitRuntimeData data, OwnedHero owned)
        {
            if (owned?.equipIds == null || owned.equipIds.Count == 0) return;

            // Pass 1: cộng phẳng (Flat)
            ApplyStats(data, owned, ModType.Flat);
            // Pass 2: nhân phần trăm theo base (Percent) — sau flat để công thức ổn định
            ApplyStats(data, owned, ModType.Percent);

            // Passive (effect) + skill riêng
            for (int i = 0; i < owned.equipIds.Count; i++)
            {
                var equipId = owned.equipIds[i];
                var eq = BattleDatabase.GetEquipment(equipId);
                if (string.IsNullOrEmpty(eq.id)) continue;

                var passives = BattleDatabase.GetEquipPassives(equipId);
                for (int p = 0; p < passives.Count; p++)
                {
                    var cfg = BattleDatabase.GetPassiveConfig(passives[p]);
                    if (string.IsNullOrEmpty(cfg.id)) continue;
                    var passive = PassiveFactory.Get(cfg);
                    if (passive != null) data.passives.Add(passive);
                }

                if (!string.IsNullOrEmpty(eq.grantedSkillId)) data.extraSkillIds.Add(eq.grantedSkillId);
            }
        }

        private static void ApplyStats(UnitRuntimeData data, OwnedHero owned, ModType pass)
        {
            for (int i = 0; i < owned.equipIds.Count; i++)
            {
                var stats = BattleDatabase.GetEquipStats(owned.equipIds[i]);
                for (int s = 0; s < stats.Count; s++)
                {
                    var st = stats[s];
                    if (st.modType != pass) continue;

                    var cur = data.baseStats.TryGetValue(st.stat, out var v) ? v : 0f;
                    data.baseStats[st.stat] = st.modType == ModType.Flat
                        ? cur + st.value
                        : cur * (1f + st.value);
                }
            }
        }
    }
}
