using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Logic thuần cho màn xếp đội hình: đọc roster tướng đã unlock + 6 slot đội hình, gán/gỡ tướng,
    ///     persist qua <see cref="PlayerBattleHero" /> (BattlePlayerData.Hero). KHÔNG dính UI.
    /// </summary>
    public static class TeamFormationService
    {
        public const int TeamSize = PlayerHeroData.TeamSize; // 6

        private static PlayerBattleHero Hero => BattlePlayerData.Hero;

        #region Read

        /// <summary>Danh sách slot đội hình, chuẩn hóa đúng <see cref="TeamSize" /> phần tử (rỗng = trống).</summary>
        public static List<string> GetSlots()
        {
            var slots = new List<string>(TeamSize);
            var src = Hero.TeamSlots;
            for (int i = 0; i < TeamSize; i++)
                slots.Add(src != null && i < src.Count ? src[i] : string.Empty);
            return slots;
        }

        /// <summary>Toàn bộ tướng người chơi đã unlock (để hiển thị lưới chọn).</summary>
        public static IReadOnlyList<OwnedHero> GetRoster() => Hero.Owned;

        public static int IndexInTeam(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return -1;
            var slots = Hero.TeamSlots;
            if (slots == null) return -1;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] == heroId) return i;
            return -1;
        }

        public static bool IsInTeam(string heroId) => IndexInTeam(heroId) >= 0;

        public static int FilledCount()
        {
            var slots = Hero.TeamSlots;
            if (slots == null) return 0;
            var n = 0;
            for (int i = 0; i < slots.Count && i < TeamSize; i++)
                if (!string.IsNullOrEmpty(slots[i])) n++;
            return n;
        }

        #endregion

        #region Mutate (persist ngay)

        /// <summary>Gán tướng vào slot chỉ định (heroId rỗng = gỡ). Chỉ nhận tướng đã unlock.</summary>
        public static void SetSlot(int slot, string heroId)
        {
            if (slot < 0 || slot >= TeamSize) return;
            if (!string.IsNullOrEmpty(heroId) && !Hero.IsUnlocked(heroId)) return;

            var slots = GetSlots();
            // đảm bảo 1 tướng chỉ đứng 1 slot: gỡ vị trí cũ nếu đã có
            if (!string.IsNullOrEmpty(heroId))
                for (int i = 0; i < slots.Count; i++)
                    if (slots[i] == heroId) slots[i] = string.Empty;

            slots[slot] = heroId ?? string.Empty;
            Hero.SetTeam(slots);
        }

        /// <summary>Gỡ tướng khỏi 1 slot.</summary>
        public static void RemoveAt(int slot) => SetSlot(slot, string.Empty);

        /// <summary>Bật/tắt tướng trong đội: đang có → gỡ; chưa có → nhét vào slot trống đầu tiên.</summary>
        public static bool Toggle(string heroId)
        {
            if (string.IsNullOrEmpty(heroId) || !Hero.IsUnlocked(heroId)) return false;

            var idx = IndexInTeam(heroId);
            if (idx >= 0)
            {
                RemoveAt(idx);
                return false; // giờ không còn trong đội
            }

            var slots = GetSlots();
            for (int i = 0; i < slots.Count; i++)
                if (string.IsNullOrEmpty(slots[i]))
                {
                    slots[i] = heroId;
                    Hero.SetTeam(slots);
                    return true; // đã thêm
                }

            return false; // đội đã đầy
        }

        #endregion

        #region Display helpers

        /// <summary>Lực chiến 1 tướng — tổng có trọng số trên chỉ số thiết kế (khớp Hero Designer).</summary>
        public static int PowerOf(OwnedHero owned)
        {
            if (owned == null) return 0;
            var h = BattleDatabase.GetHero(owned.heroId);
            if (string.IsNullOrEmpty(h.id)) return 0;

            var lv = owned.level <= 0 ? 1 : owned.level;
            var star = owned.star <= 0 ? 1 : owned.star;
            var hp = HeroStatService.ComputeStat(h, lv, star, StatType.Hp);
            var atk = HeroStatService.ComputeStat(h, lv, star, StatType.Atk);
            var def = HeroStatService.ComputeStat(h, lv, star, StatType.Def);
            var spd = HeroStatService.ComputeStat(h, lv, star, StatType.Spd);
            var critR = HeroStatService.ComputeStat(h, lv, star, StatType.CritRate);
            var critD = HeroStatService.ComputeStat(h, lv, star, StatType.CritDmg);

            var critMult = 1f + Mathf.Clamp01(critR) * Mathf.Max(0f, critD - 1f);
            var offense = atk * critMult * (1f + spd / 400f);
            var effHp = hp * (1f + def / 500f) * (1f + Mathf.Clamp(h.damageReduction, 0f, 0.9f));
            return Mathf.RoundToInt(offense * 1.8f + effHp * 0.12f);
        }

        /// <summary>Tổng lực chiến đội hình hiện tại.</summary>
        public static int TeamPower()
        {
            var total = 0;
            foreach (var slot in GetSlots())
            {
                if (string.IsNullOrEmpty(slot)) continue;
                var owned = Hero.GetHero(slot);
                if (owned != null) total += PowerOf(owned);
            }

            return total;
        }

        /// <summary>Màu đại diện theo hệ (dùng cho tag nhỏ trên card/slot).</summary>
        public static Color ElementColor(ElementType e)
        {
            switch (e)
            {
                case ElementType.Fire: return new Color(0.90f, 0.30f, 0.20f);
                case ElementType.Water: return new Color(0.25f, 0.55f, 0.95f);
                case ElementType.Wind: return new Color(0.35f, 0.80f, 0.45f);
                case ElementType.Light: return new Color(0.95f, 0.85f, 0.35f);
                case ElementType.Dark: return new Color(0.55f, 0.35f, 0.80f);
                default: return Color.gray;
            }
        }

        #endregion
    }
}
