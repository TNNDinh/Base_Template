using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Gói dữ liệu khởi tạo 1 <see cref="Unit" />. Mọi thứ đặc thù (chỉ số, skill, role/passive)
    ///     được build từ config rồi nhồi vào đây — <see cref="Unit" /> chỉ nhận qua <see cref="Unit.SetData" />.
    /// </summary>
    public class UnitRuntimeData
    {
        public string heroId;
        public string modelKey;
        public string displayName;
        public ElementType element;
        public BattleTeam team;
        public int slot;

        public string basicSkillId;
        public string activeSkillId;
        public string ultimateId;

        // Mana config (Active tiêu mana; Ultimate dùng Energy riêng)
        public float maxMana;
        public float startMana;
        public float manaPerAttack;
        public float manaOnHit;
        public float attackRange; // nhỏ = cận chiến (lao vào), lớn = tầm xa (đứng yên)

        public Dictionary<StatType, float> baseStats = new Dictionary<StatType, float>();
        public List<PassiveBase> passives = new List<PassiveBase>();
        public List<string> extraSkillIds = new List<string>(); // skill do trang bị cấp
    }
}
