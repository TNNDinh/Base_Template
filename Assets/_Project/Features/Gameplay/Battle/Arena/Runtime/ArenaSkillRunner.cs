using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Bộ chạy skill cho 1 unit (hero/enemy). Unit chỉ có DANH SÁCH skill id; runner đọc định nghĩa
    ///     (trigger + effect + cooldown) từ <see cref="ArenaUpgradeService.GetSkill" />. Gom passive (StatBuff)
    ///     và bắn skill theo trigger (OnDeath/OnMove/OnDamaged/OnLowHp/EveryNRounds), có cooldown riêng từng skill.
    /// </summary>
    public class ArenaSkillRunner
    {
        private readonly List<string> _ids = new List<string>();
        private readonly Dictionary<string, int> _cd = new Dictionary<string, int>();

        public ArenaSkillRunner(string skillsCsv)
        {
            if (string.IsNullOrEmpty(skillsCsv)) return;
            var parts = skillsCsv.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                var id = parts[i].Trim();
                if (id.Length > 0) _ids.Add(id);
            }
        }

        public bool HasAny => _ids.Count > 0;

        /// <summary>Tổng buff PASSIVE (StatBuff) cộng dồn.</summary>
        public ArenaUpgradeService.PassiveBonuses Passives()
        {
            var b = new ArenaUpgradeService.PassiveBonuses();
            for (int i = 0; i < _ids.Count; i++)
            {
                var s = ArenaUpgradeService.GetSkill(_ids[i]);
                if (string.IsNullOrEmpty(s.id)) continue;
                if (s.trigger != (int)SkillTrigger.Passive || s.effect != (int)ArenaSkillEffect.StatBuff) continue;
                switch ((ArenaStatType)s.statType)
                {
                    case ArenaStatType.AtkPct: b.atkPct += s.power; break;
                    case ArenaStatType.RegenPct: b.regenPct += s.power; break;
                    case ArenaStatType.DmgReducePct: b.dmgReducePct += s.power; break;
                    case ArenaStatType.MaxHpPct: b.maxHpPct += s.power; break;
                }
            }

            return b;
        }

        /// <summary>Skill id ACTIVE đầu tiên (ult của hero) — rỗng nếu không có.</summary>
        public string ActiveSkillId()
        {
            for (int i = 0; i < _ids.Count; i++)
                if (ArenaUpgradeService.GetSkill(_ids[i]).trigger == (int)SkillTrigger.Active)
                    return _ids[i];
            return null;
        }

        public void TickCooldowns()
        {
            if (_cd.Count == 0) return;
            var keys = new List<string>(_cd.Keys);
            for (int i = 0; i < keys.Count; i++)
                if (_cd[keys[i]] > 0) _cd[keys[i]]--;
        }

        /// <summary>
        ///     Trả về các skill NÊN kích hoạt cho <paramref name="trigger" /> lúc này (đã lọc cooldown + điều kiện
        ///     OnLowHp/EveryNRounds) và đặt cooldown cho chúng.
        /// </summary>
        public List<ArenaSkillModel> Fire(SkillTrigger trigger, float hpRatio, int roundNo)
        {
            List<ArenaSkillModel> res = null;
            for (int i = 0; i < _ids.Count; i++)
            {
                var id = _ids[i];
                var s = ArenaUpgradeService.GetSkill(id);
                if (string.IsNullOrEmpty(s.id) || s.trigger != (int)trigger) continue;
                if (_cd.TryGetValue(id, out var left) && left > 0) continue;
                if (trigger == SkillTrigger.OnLowHp && hpRatio > (s.param <= 0f ? 0.5f : s.param)) continue;
                if (trigger == SkillTrigger.EveryNRounds && roundNo % Mathf.Max(1, Mathf.RoundToInt(s.param)) != 0) continue;

                (res ??= new List<ArenaSkillModel>()).Add(s);
                if (s.cooldown > 0) _cd[id] = s.cooldown;
            }

            return res ?? EmptyList;
        }

        private static readonly List<ArenaSkillModel> EmptyList = new List<ArenaSkillModel>();
    }
}
