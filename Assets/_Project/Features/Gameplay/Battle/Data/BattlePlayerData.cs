using System.Collections.Generic;
using Ezg.Core.Adapter;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Facade truy cập player-data của battle. Roster tướng unlock + đội hình đọc từ module persist
    ///     <see cref="PlayerBattleHero" /> (qua <c>PlayerDataManager.BattleHero</c>, lưu JSON PlayerPrefs).
    ///     Đội hình mặc định seed 1 lần từ SO <see cref="DefaultTeamConfig" /> (Resources/BattleDefaultTeam),
    ///     load qua <see cref="ResLoader" /> — loader chuẩn (bundle-aware + fallback Resources).
    ///     Tiến trình ải (<see cref="Stage" />) tạm giữ in-memory (MVP).
    /// </summary>
    public static class BattlePlayerData
    {
        private const string DefaultTeamResPath = "BattleDefaultTeam"; // asset SO trong 1 Resources/

        private static PlayerStageData _stage;
        private static DefaultTeamConfig _cfg;

        private static DefaultTeamConfig Cfg => _cfg != null ? _cfg : _cfg = ResLoader.Load<DefaultTeamConfig>(DefaultTeamResPath);

        /// <summary>Module hero persist (roster unlock + đội hình).</summary>
        public static PlayerBattleHero Hero => PlayerDataManager.BattleHero;

        /// <summary>Payload roster + đội hình (persist).</summary>
        public static PlayerHeroData Heroes => Hero.Data;

        public static PlayerStageData Stage => _stage ?? (_stage = new PlayerStageData());

        /// <summary>Tướng MAIN cố định (từ SO config) — luôn đứng slot đầu (front).</summary>
        public static string MainHeroId => Cfg != null && !string.IsNullOrEmpty(Cfg.mainHeroId) ? Cfg.mainHeroId : "hero_1001";

        /// <summary>OwnedHero của tướng main (slot đầu đội hình).</summary>
        public static OwnedHero GetMain() => Heroes.GetHero(MainHeroId);

        /// <summary>Đội hình active dạng list OwnedHero (theo slot). Fallback = toàn bộ hero sở hữu.</summary>
        public static List<OwnedHero> GetActiveTeam()
        {
            var list = new List<OwnedHero>();
            var team = Heroes.activeTeam;
            for (int i = 0; i < team.Count; i++)
            {
                if (string.IsNullOrEmpty(team[i])) continue;
                var h = Heroes.GetHero(team[i]);
                if (h != null) list.Add(h);
            }

            if (list.Count == 0) list.AddRange(Heroes.heroes);
            return list;
        }

        /// <summary>
        ///     Seed roster + đội hình mặc định vào 1 payload (gọi bởi <see cref="PlayerBattleHero" /> khi new player
        ///     hoặc save rỗng). Ưu tiên SO config; thiếu thì fallback đội hình cứng.
        /// </summary>
        public static void SeedDefault(PlayerHeroData data)
        {
            if (data == null) return;
            if (data.heroes == null) data.heroes = new List<OwnedHero>();
            if (data.activeTeam == null) data.activeTeam = new List<string>();

            // Ưu tiên đọc từ ScriptableObject config.
            if (Cfg != null && Cfg.team != null && Cfg.team.Count > 0)
            {
                var order = new List<string>();
                for (int i = 0; i < Cfg.team.Count; i++)
                {
                    var e = Cfg.team[i];
                    if (string.IsNullOrEmpty(e.heroId)) continue;
                    data.AddHero(e.heroId, e.star <= 0 ? 1 : e.star, e.level <= 0 ? 1 : e.level);
                    order.Add(e.heroId);
                }

                data.activeTeam = order;
                return;
            }

            // Fallback (SO thiếu): đội hình cứng.
            data.AddHero("hero_1001", 3, 60);
            data.AddHero("hero_1001_tk", 3, 60);
            data.AddHero("hero_gladiator", 3, 60);
            data.activeTeam = new List<string> { "hero_1001", "hero_1001_tk", "hero_gladiator" };
        }
    }
}
