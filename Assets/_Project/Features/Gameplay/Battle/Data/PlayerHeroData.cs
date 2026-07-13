using System;
using System.Collections.Generic;
using Ezg.Package.Factory;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     1 hero người chơi sở hữu (persist). Chỉ lưu định danh + tiến trình — KHÔNG lưu stat đã tính.
    /// </summary>
    [Serializable]
    public class OwnedHero
    {
        public string heroId; // id dạng hiện tại (gốc hoặc đã tiến hóa)
        public int level = 1;
        public int star = 1;
        public int exp;
        public List<string> equipIds = new List<string>();
        public List<string> unlockedSkillIds = new List<string>();

        /// <summary>Id hero gốc trước khi tiến hóa (rỗng nếu chưa tiến hóa).</summary>
        public string originHeroId;

        /// <summary>Nhánh tiến hóa đã chọn (None nếu chưa tiến hóa).</summary>
        public EvolutionPath evolvePath = EvolutionPath.None;

        /// <summary>Id gốc để tra cây tiến hóa / gộp dupe nâng sao.</summary>
        public string BaseHeroId => string.IsNullOrEmpty(originHeroId) ? heroId : originHeroId;
    }

    /// <summary>
    ///     Payload persist của hero: danh sách tướng đã unlock + đội hình active theo slot.
    ///     Là <see cref="DataBase" /> để module <see cref="PlayerBattleHero" /> lưu/nạp qua DataPlayer (JSON PlayerPrefs).
    ///     Vẫn dùng được như plain object (<c>new PlayerHeroData()</c>) cho test headless.
    /// </summary>
    [Serializable]
    public class PlayerHeroData : DataBase
    {
        public const int TeamSize = 6;

        /// <summary>Toàn bộ tướng người chơi đã unlock (roster). Count = số tướng unlock.</summary>
        public List<OwnedHero> heroes = new List<OwnedHero>();

        /// <summary>heroId theo từng slot 0..5 của đội active (rỗng = trống).</summary>
        public List<string> activeTeam = new List<string>();

        public OwnedHero GetHero(string heroId) => heroes.Find(h => h.heroId == heroId);

        public bool IsUnlocked(string heroId) => GetHero(heroId) != null;

        /// <summary>Số tướng đã unlock.</summary>
        public int UnlockedCount => heroes.Count;

        public OwnedHero AddHero(string heroId, int star = 1, int level = 1)
        {
            var existing = GetHero(heroId);
            if (existing != null) return existing; // MVP: chưa xử lý dupe → fusion sao

            var hero = new OwnedHero { heroId = heroId, star = star, level = level };
            heroes.Add(hero);
            return hero;
        }
    }
}
