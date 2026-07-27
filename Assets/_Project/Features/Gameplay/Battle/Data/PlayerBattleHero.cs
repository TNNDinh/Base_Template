using System.Collections.Generic;
using Ezg.Package.Factory;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Module player-data persist cho hero battle: roster tướng đã unlock + đội hình active.
    ///     Auto-discover qua reflection (DataPlayer); truy cập nhanh qua <c>PlayerDataManager.BattleHero</c>.
    ///     Lưu JSON PlayerPrefs — mỗi mutation gọi <see cref="Save" /> (không Save trong Update loop).
    /// </summary>
    public class PlayerBattleHero : DataPlayerBaseGeneric<PlayerHeroData>
    {
        /// <summary>Payload persist (roster + đội hình).</summary>
        public PlayerHeroData Data => dataBase;

        #region Load / Default

        /// <summary>Chạy 1 lần khi player mới — seed roster + đội hình mặc định từ SO config.</summary>
        protected override void SetupDefaultData()
        {
            base.SetupDefaultData();
            EnsureLists();
            BattlePlayerData.SeedDefault(dataBase);
        }

        /// <summary>Sau khi nạp save — validate + seed nếu save cũ/rỗng (migration mềm).</summary>
        protected override void AfterLoad()
        {
            base.AfterLoad();
            EnsureLists();
            if (dataBase.heroes.Count == 0)
            {
                BattlePlayerData.SeedDefault(dataBase);
                Save();
            }
            else if (BattlePlayerData.TopUpDefault(dataBase))
            {
                Save(); // save cũ thiếu tướng so với SO config → bù roster + lấp slot trống
            }
        }

        private void EnsureLists()
        {
            if (dataBase.heroes == null) dataBase.heroes = new List<OwnedHero>();
            if (dataBase.activeTeam == null) dataBase.activeTeam = new List<string>();
        }

        #endregion

        #region Unlock roster

        /// <summary>Số tướng đã unlock.</summary>
        public int UnlockedCount => dataBase.heroes.Count;

        public bool IsUnlocked(string heroId) => dataBase.IsUnlocked(heroId);

        public OwnedHero GetHero(string heroId) => dataBase.GetHero(heroId);

        public IReadOnlyList<OwnedHero> Owned => dataBase.heroes;

        /// <summary>Unlock 1 tướng (bỏ qua nếu đã có). Persist ngay.</summary>
        public OwnedHero UnlockHero(string heroId, int star = 1, int level = 1)
        {
            if (string.IsNullOrEmpty(heroId)) return null;
            var existing = dataBase.GetHero(heroId);
            if (existing != null) return existing;

            var hero = dataBase.AddHero(heroId, star, level);
            Save();
            return hero;
        }

        #endregion

        #region Team formation

        /// <summary>heroId theo slot (0..TeamSize-1); phần tử rỗng = slot trống.</summary>
        public List<string> TeamSlots => dataBase.activeTeam;

        /// <summary>Đặt lại toàn bộ đội hình theo list slot. Persist ngay.</summary>
        public void SetTeam(List<string> slots)
        {
            dataBase.activeTeam = slots ?? new List<string>();
            Save();
        }

        /// <summary>Gán 1 tướng vào 1 slot (heroId rỗng = bỏ slot). Chỉ nhận tướng đã unlock. Persist ngay.</summary>
        public void SetTeamSlot(int slot, string heroId)
        {
            if (slot < 0 || slot >= PlayerHeroData.TeamSize) return;
            if (!string.IsNullOrEmpty(heroId) && !dataBase.IsUnlocked(heroId)) return;

            var team = dataBase.activeTeam;
            while (team.Count <= slot) team.Add(string.Empty);
            team[slot] = heroId ?? string.Empty;
            Save();
        }

        #endregion
    }
}
