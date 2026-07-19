using System;
using System.Collections.Generic;
using Ezg.Feature.Shared.GameData;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Mở khóa hero data-driven (config ở <c>HeroUnlocks.csv</c>). Trạng thái đã-mở lưu ở roster
    ///     <see cref="PlayerBattleHero" />; điều kiện (level/stage) + tiền (gold) ở <see cref="PlayerArenaProgress" />.
    ///     Mọi truy cập player-data phòng thủ (data chưa init → coi như khóa/không đủ, không crash).
    /// </summary>
    public static class ArenaHeroUnlockService
    {
        public const int GachaCost = 300; // gold cho 1 lượt quay

        public static event Action OnChanged;

        private static HeroUnlockCollection _cfg;
        private static HeroUnlockCollection Cfg
        {
            get { if (_cfg == null) _cfg = Resources.Load<HeroUnlockCollection>("ArenaCsv/HeroUnlockCollection"); return _cfg; }
        }

        private static PlayerBattleHero Roster { get { try { return PlayerDataManager.BattleHero; } catch { return null; } } }
        private static PlayerArenaProgress Progress { get { try { return PlayerDataManager.ArenaProgress; } catch { return null; } } }

        public static HeroUnlockMethod Method(string heroId) => (HeroUnlockMethod)(Cfg != null ? Cfg.GetById(heroId).method : 0);

        /// <summary>Hero đã mở khóa chưa (Default = luôn mở; còn lại xét roster).</summary>
        public static bool IsUnlocked(string heroId)
        {
            if (Method(heroId) == HeroUnlockMethod.Default) return true;
            try { return Roster != null && Roster.IsUnlocked(heroId); }
            catch { return false; }
        }

        /// <summary>Đủ điều kiện mở khóa NGAY (bấm là mở được) chưa.</summary>
        public static bool CanUnlock(string heroId)
        {
            if (IsUnlocked(heroId)) return false;
            var u = Cfg != null ? Cfg.GetById(heroId) : default;
            switch ((HeroUnlockMethod)u.method)
            {
                case HeroUnlockMethod.Level: return PlayerLevel() >= u.reqLevel;
                case HeroUnlockMethod.Currency: return Gold() >= u.cost;
                case HeroUnlockMethod.Stage: return StageCleared(u.reqStage);
                case HeroUnlockMethod.IAP: return true; // bấm = thử mua
                default: return false;                  // Gacha chỉ ra qua quay; Default đã mở
            }
        }

        /// <summary>Chuỗi mô tả điều kiện mở (cho UI).</summary>
        public static string Requirement(string heroId)
        {
            var u = Cfg != null ? Cfg.GetById(heroId) : default;
            switch ((HeroUnlockMethod)u.method)
            {
                case HeroUnlockMethod.Level: return "Lv " + u.reqLevel;
                case HeroUnlockMethod.Currency: return u.cost + "g";
                case HeroUnlockMethod.Stage: return "Clear " + u.reqStage;
                case HeroUnlockMethod.IAP: return "IAP";
                case HeroUnlockMethod.Gacha: return "Gacha";
                default: return "";
            }
        }

        /// <summary>Mở khóa hero nếu đủ điều kiện (tốn gold nếu Currency). Trả về true nếu mở thành công.</summary>
        public static bool TryUnlock(string heroId)
        {
            if (IsUnlocked(heroId) || !CanUnlock(heroId)) return false;
            var u = Cfg != null ? Cfg.GetById(heroId) : default;
            try
            {
                switch ((HeroUnlockMethod)u.method)
                {
                    case HeroUnlockMethod.Currency:
                        if (Progress == null || !Progress.SpendGold(u.cost)) return false;
                        break;
                    case HeroUnlockMethod.IAP:
                        // TODO: nối IAP thật (Purchase*). Tạm coi như mua thành công.
                        break;
                }

                return Unlock(heroId);
            }
            catch { return false; }
        }

        /// <summary>Quay gacha: tốn <see cref="GachaCost" /> gold, mở 1 hero khóa theo trọng số. Trả về heroId hoặc null.</summary>
        public static string GachaPull()
        {
            var p = Progress;
            if (p == null || Cfg == null) return null;
            try
            {
                var pool = new List<HeroUnlockModel>();
                int total = 0;
                foreach (var u in Cfg.All)
                {
                    if ((HeroUnlockMethod)u.method != HeroUnlockMethod.Gacha) continue;
                    if (u.gachaWeight <= 0 || IsUnlocked(u.heroId)) continue;
                    pool.Add(u);
                    total += u.gachaWeight;
                }

                if (pool.Count == 0 || total <= 0) return null;
                if (!p.SpendGold(GachaCost)) return null;

                int roll = UnityEngine.Random.Range(0, total);
                foreach (var u in pool)
                {
                    roll -= u.gachaWeight;
                    if (roll < 0) { Unlock(u.heroId); return u.heroId; }
                }

                Unlock(pool[0].heroId); // fallback
                return pool[0].heroId;
            }
            catch { return null; }
        }

        /// <summary>Đánh dấu stage đã clear (mở khóa hero method=Stage).</summary>
        public static void MarkStageCleared(string stageId)
        {
            try { Progress?.MarkStageCleared(stageId); OnChanged?.Invoke(); } catch { /* data chưa init */ }
        }

        /// <summary>Stage đã clear chưa (phòng thủ — data chưa init → false). Dùng cho UI chọn stage.</summary>
        public static bool IsStageCleared(string stageId) => StageCleared(stageId);

        private static bool Unlock(string heroId)
        {
            var r = Roster;
            if (r == null) return false;
            r.UnlockHero(heroId);
            OnChanged?.Invoke();
            return true;
        }

        private static int PlayerLevel() { try { return Progress != null ? Progress.PlayerLevel : 1; } catch { return 1; } }
        private static long Gold() { try { return Progress != null ? Progress.Gold : 0; } catch { return 0; } }
        private static bool StageCleared(string stageId) { try { return Progress != null && Progress.IsStageCleared(stageId); } catch { return false; } }
    }
}
