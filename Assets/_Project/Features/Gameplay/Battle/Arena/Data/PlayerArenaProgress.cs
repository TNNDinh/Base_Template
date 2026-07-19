using System;
using System.Collections.Generic;
using Ezg.Package.Factory;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>1 dòng cấp độ vũ khí đã lưu (JsonUtility-safe: list entry thay cho Dictionary).</summary>
    [Serializable]
    public class WeaponLevelEntry
    {
        public string id;
        public int level = 1;
    }

    /// <summary>Skill ultimate hiện tại của 1 hero (id trong chuỗi skill — nâng cấp = đổi id).</summary>
    [Serializable]
    public class HeroSkillEntry
    {
        public string heroId;
        public string skillId;
    }

    /// <summary>Loadout 3 vũ khí đã CHỌN của 1 hero (weapons = id ngăn bằng ';').</summary>
    [Serializable]
    public class HeroLoadoutEntry
    {
        public string heroId;
        public string weapons;
    }

    /// <summary>
    ///     Payload persist tiến trình NÂNG CẤP arena: cấp độ từng vũ khí + tiền nâng cấp (gold arena, tự chứa
    ///     — không đụng economy game chính). Hero level dùng lại roster <see cref="PlayerBattleHero" />
    ///     (<see cref="OwnedHero.level" />), nên KHÔNG lưu ở đây.
    /// </summary>
    [Serializable]
    public class ArenaProgressData : DataBase
    {
        public long gold = 500;
        public int playerLevel = 1;
        public List<WeaponLevelEntry> weapons = new List<WeaponLevelEntry>();
        public List<HeroSkillEntry> heroSkills = new List<HeroSkillEntry>();
        public List<HeroLoadoutEntry> heroLoadouts = new List<HeroLoadoutEntry>();
        public List<string> clearedStages = new List<string>();
    }

    /// <summary>
    ///     Module player-data cho nâng cấp arena (auto-discover qua DataPlayer; truy cập qua
    ///     <c>PlayerDataManager.ArenaProgress</c>). Lưu JSON PlayerPrefs — mỗi mutation gọi <see cref="Save" />.
    /// </summary>
    public class PlayerArenaProgress : DataPlayerBaseGeneric<ArenaProgressData>
    {
        public ArenaProgressData Data => dataBase;

        #region Load / Default

        protected override void SetupDefaultData()
        {
            base.SetupDefaultData();
            EnsureList();
        }

        protected override void AfterLoad()
        {
            base.AfterLoad();
            EnsureList();
        }

        private void EnsureList()
        {
            if (dataBase.weapons == null) dataBase.weapons = new List<WeaponLevelEntry>();
            if (dataBase.heroSkills == null) dataBase.heroSkills = new List<HeroSkillEntry>();
            if (dataBase.heroLoadouts == null) dataBase.heroLoadouts = new List<HeroLoadoutEntry>();
            if (dataBase.clearedStages == null) dataBase.clearedStages = new List<string>();
            if (dataBase.playerLevel < 1) dataBase.playerLevel = 1;
        }

        #region Player level / cleared stages (cho mở khóa hero)

        public int PlayerLevel => dataBase.playerLevel;

        public void SetPlayerLevel(int level)
        {
            dataBase.playerLevel = Mathf.Max(1, level);
            Save();
        }

        public bool IsStageCleared(string stageId) => !string.IsNullOrEmpty(stageId) && dataBase.clearedStages.Contains(stageId);

        public void MarkStageCleared(string stageId)
        {
            if (string.IsNullOrEmpty(stageId) || dataBase.clearedStages.Contains(stageId)) return;
            dataBase.clearedStages.Add(stageId);
            Save();
        }

        #endregion

        #endregion

        #region Gold (tiền nâng cấp)

        public long Gold => dataBase.gold;

        public void AddGold(long value)
        {
            dataBase.gold += value;
            Save();
        }

        /// <summary>Trừ <paramref name="value" /> gold nếu đủ; trả về false (không trừ) nếu thiếu.</summary>
        public bool SpendGold(long value)
        {
            if (dataBase.gold < value) return false;
            dataBase.gold -= value;
            Save();
            return true;
        }

        #endregion

        #region Weapon level

        public int GetWeaponLevel(string weaponId)
        {
            var e = Find(weaponId);
            return e != null ? e.level : 1;
        }

        public void SetWeaponLevel(string weaponId, int level)
        {
            var e = Find(weaponId);
            if (e == null)
            {
                e = new WeaponLevelEntry { id = weaponId, level = level };
                dataBase.weapons.Add(e);
            }
            else
            {
                e.level = level;
            }

            Save();
        }

        private WeaponLevelEntry Find(string weaponId) => dataBase.weapons.Find(w => w.id == weaponId);

        #endregion

        #region Hero loadout (3 vũ khí đã chọn)

        public string GetHeroLoadout(string heroId)
        {
            var e = dataBase.heroLoadouts.Find(h => h.heroId == heroId);
            return e != null ? e.weapons : null;
        }

        public void SetHeroLoadout(string heroId, string weaponsCsv)
        {
            var e = dataBase.heroLoadouts.Find(h => h.heroId == heroId);
            if (e == null)
            {
                e = new HeroLoadoutEntry { heroId = heroId, weapons = weaponsCsv };
                dataBase.heroLoadouts.Add(e);
            }
            else
            {
                e.weapons = weaponsCsv;
            }

            Save();
        }

        #endregion

        #region Hero skill (id chuỗi skill hiện tại)

        /// <summary>Skill id hiện tại của hero (rỗng = chưa lưu → dùng skill mặc định của hero).</summary>
        public string GetHeroSkill(string heroId)
        {
            var e = dataBase.heroSkills.Find(h => h.heroId == heroId);
            return e != null ? e.skillId : null;
        }

        public void SetHeroSkill(string heroId, string skillId)
        {
            var e = dataBase.heroSkills.Find(h => h.heroId == heroId);
            if (e == null)
            {
                e = new HeroSkillEntry { heroId = heroId, skillId = skillId };
                dataBase.heroSkills.Add(e);
            }
            else
            {
                e.skillId = skillId;
            }

            Save();
        }

        #endregion
    }
}
