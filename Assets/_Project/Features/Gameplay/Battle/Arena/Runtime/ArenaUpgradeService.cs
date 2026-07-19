using System;
using Ezg.Feature.Shared.GameData;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Cầu nối NÂNG CẤP hero &amp; vũ khí cho arena. Hero level dùng lại roster
    ///     <see cref="PlayerBattleHero" /> (<see cref="OwnedHero.level" />); cấp vũ khí + gold nâng cấp lưu ở
    ///     <see cref="PlayerArenaProgress" />. Mọi truy cập player-data đều PHÒNG THỦ (try/catch) — nếu tầng
    ///     data chưa init thì trả mặc định (level 1, gold 0) để arena vẫn chạy, không crash.
    /// </summary>
    public static class ArenaUpgradeService
    {
        public const int HeroBaseCost = 120;   // gold nâng hero (nhân theo cấp hiện tại)
        public const int HeroMaxLevel = 30;

        /// <summary>Bắn khi gold/level/skill đổi (UI nghe để refresh).</summary>
        public static event Action OnChanged;

        private static PlayerArenaProgress Progress
        {
            get { try { return PlayerDataManager.ArenaProgress; } catch { return null; } }
        }

        private static PlayerBattleHero Roster
        {
            get { try { return PlayerDataManager.BattleHero; } catch { return null; } }
        }

        private static SkillCollection _skills;
        private static SkillCollection Skills
        {
            get { if (_skills == null) _skills = Resources.Load<SkillCollection>("ArenaCsv/SkillCollection"); return _skills; }
        }

        private static HeroStatCollection _heroStats;
        private static HeroStatCollection HeroStatsCol
        {
            get { if (_heroStats == null) _heroStats = Resources.Load<HeroStatCollection>("ArenaCsv/HeroStatCollection"); return _heroStats; }
        }

        #region Gold

        public static long Gold
        {
            get { try { var p = Progress; return p != null ? p.Gold : 0; } catch { return 0; } }
        }

        public static void AddGold(long value)
        {
            try { Progress?.AddGold(value); } catch { /* data chưa init */ }
            OnChanged?.Invoke();
        }

        #endregion

        #region Hero

        public static int HeroLevel(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return 1;
            try
            {
                var h = Roster?.GetHero(heroId);
                return h != null ? Mathf.Max(1, h.level) : 1;
            }
            catch { return 1; } // data chưa init → mặc định level 1
        }

        public static bool HeroMaxed(string heroId) => HeroLevel(heroId) >= HeroMaxLevel;

        public static int HeroUpgradeCost(string heroId) => HeroBaseCost * HeroLevel(heroId);

        #endregion

        #region Skill ultimate (id chuỗi — nâng cấp = đổi id)

        /// <summary>Skill ACTIVE mặc định của hero = skill trigger=Active đầu tiên trong list <c>skills</c> của hero.</summary>
        private static string DefaultSkillId(string heroId)
        {
            var col = HeroStatsCol;
            if (col == null) return null;
            return new ArenaSkillRunner(col.GetById(heroId).skills).ActiveSkillId();
        }

        /// <summary>Skill id hiện tại của hero: đã lưu (đã nâng cấp) → dùng; chưa có → skill mặc định.</summary>
        public static string CurrentSkillId(string heroId)
        {
            try { var s = Progress?.GetHeroSkill(heroId); if (!string.IsNullOrEmpty(s)) return s; }
            catch { /* data chưa init */ }
            return DefaultSkillId(heroId);
        }

        public static ArenaSkillModel CurrentSkill(string heroId)
        {
            var col = Skills;
            return col != null ? col.GetById(CurrentSkillId(heroId)) : default;
        }

        public static string SkillName(string heroId)
        {
            var s = CurrentSkill(heroId);
            return string.IsNullOrEmpty(s.name) ? CurrentSkillId(heroId) : s.name;
        }

        /// <summary>Cooldown (round) của active skill hiện tại.</summary>
        public static int SkillCooldown(string heroId) => Mathf.Max(0, CurrentSkill(heroId).cooldown);

        public static bool CanUpgradeSkill(string heroId) => !string.IsNullOrEmpty(CurrentSkill(heroId).nextSkillId);

        public static int SkillUpgradeCost(string heroId) => CurrentSkill(heroId).upgradeCost;

        /// <summary>Nâng cấp skill = đổi sang <see cref="SkillModel.nextSkillId" /> nếu đủ gold. Trả về true nếu thành công.</summary>
        public static bool TryUpgradeSkill(string heroId)
        {
            var cur = CurrentSkill(heroId);
            if (string.IsNullOrEmpty(cur.nextSkillId)) return false;
            try
            {
                var p = Progress;
                if (p == null) return false;
                if (!p.SpendGold(cur.upgradeCost)) return false;
                p.SetHeroSkill(heroId, cur.nextSkillId);
                OnChanged?.Invoke();
                return true;
            }
            catch { return false; }
        }

        /// <summary>Nâng cấp hero 1 level nếu đủ gold &amp; chưa max. Trả về true nếu thành công.</summary>
        public static bool TryUpgradeHero(string heroId)
        {
            if (string.IsNullOrEmpty(heroId) || HeroMaxed(heroId)) return false;
            try
            {
                var p = Progress;
                var r = Roster;
                if (p == null || r == null) return false;

                int cost = HeroUpgradeCost(heroId);
                if (!p.SpendGold(cost)) return false;

                var h = r.GetHero(heroId) ?? r.UnlockHero(heroId);
                if (h == null) return false;
                h.level += 1;
                r.Save();
                OnChanged?.Invoke();
                return true;
            }
            catch { return false; }
        }

        #endregion

        #region Passive (tổng hợp buff từ danh sách passive skill id)

        /// <summary>Tổng các buff passive (đã cộng dồn) từ danh sách skill id ngăn bằng ';'.</summary>
        public struct PassiveBonuses
        {
            public float atkPct;       // +% atk (dùng cho ult)
            public float regenPct;     // hồi %/round
            public float dmgReducePct; // giảm % damage nhận
            public float maxHpPct;     // +% máu tối đa
        }

        public static PassiveBonuses AggregatePassives(string skillsCsv) => new ArenaSkillRunner(skillsCsv).Passives();

        /// <summary>Tra skill theo id (cho enemy/combat dùng trực tiếp).</summary>
        public static ArenaSkillModel GetSkill(string skillId)
        {
            var col = Skills;
            return col != null ? col.GetById(skillId) : default;
        }

        /// <summary>Danh sách skill id của hero (từ HeroStats).</summary>
        public static string HeroSkills(string heroId)
        {
            var col = HeroStatsCol;
            return col != null ? col.GetById(heroId).skills : null;
        }

        /// <summary>Loadout 3 vũ khí đã LƯU của hero (';'-sep), null nếu chưa chọn.</summary>
        public static string GetSavedLoadout(string heroId)
        {
            try { return Progress?.GetHeroLoadout(heroId); } catch { return null; }
        }

        public static void SaveLoadout(string heroId, string weaponsCsv)
        {
            try { Progress?.SetHeroLoadout(heroId, weaponsCsv); } catch { /* data chưa init */ }
        }

        #endregion

        #region Weapon

        public static int WeaponLevel(string weaponId)
        {
            try { var p = Progress; return p != null ? p.GetWeaponLevel(weaponId) : 1; }
            catch { return 1; }
        }

        public static bool WeaponMaxed(string weaponId, WeaponCollection col) =>
            col == null || WeaponLevel(weaponId) >= col.MaxLevel(weaponId);

        public static int WeaponUpgradeCost(string weaponId, WeaponCollection col) =>
            col != null ? col.UpgradeCost(weaponId, WeaponLevel(weaponId)) : 0;

        /// <summary>Nâng cấp vũ khí 1 level nếu đủ gold &amp; chưa max. Trả về true nếu thành công.</summary>
        public static bool TryUpgradeWeapon(string weaponId, WeaponCollection col)
        {
            if (col == null || string.IsNullOrEmpty(weaponId) || WeaponMaxed(weaponId, col)) return false;
            try
            {
                var p = Progress;
                if (p == null) return false;

                int cost = WeaponUpgradeCost(weaponId, col);
                if (!p.SpendGold(cost)) return false;

                p.SetWeaponLevel(weaponId, WeaponLevel(weaponId) + 1);
                OnChanged?.Invoke();
                return true;
            }
            catch { return false; }
        }

        #endregion
    }
}
