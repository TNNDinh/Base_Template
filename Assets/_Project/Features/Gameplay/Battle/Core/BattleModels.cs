using System;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Config 1 hero (CSV <c>Heroes.csv</c>). Chỉ chứa định danh + chỉ số gốc,
    ///     KHÔNG chứa logic skill — chỉ trỏ id sang <see cref="SkillModel" />.
    /// </summary>
    [Serializable]
    public struct HeroModel
    {
        public string id;
        public string name;
        public ElementType element;
        public HeroClass heroClass;
        public int rarity;

        public int baseHp;
        public int baseAtk;
        public int baseDef;
        public int baseSpd;
        public float critRate;
        public float critDmg;
        public float damageReduction; // đỡ đòn ở stat: % giảm sát thương gốc (0..0.9)

        public float growthHp;
        public float growthAtk;
        public float growthDef;

        // --- Mana (tài nguyên dùng Active skill; Ultimate dùng Energy riêng) ---
        public float maxMana;       // mana tối đa
        public float startMana;     // mana khởi điểm đầu trận
        public float manaPerAttack; // hồi mỗi lần unit hành động
        public float manaOnHit;     // hồi khi bị tấn công

        /// <summary>Tầm đánh: nhỏ (vd 2.5) = cận chiến → lao lại gần target; lớn (vd 999) = tầm xa → đứng yên đánh.</summary>
        public float attackRange;

        public string basicSkillId;
        public string skill2Id;
        public string ultimateId;

        /// <summary>Tên prefab model 3D trong <c>Resources/Heroes/</c> (rỗng = dùng placeholder).</summary>
        public string modelKey;

        /// <summary>Con trỏ ngược: hero gốc mà dạng này tiến hóa ra (rỗng nếu là dạng gốc).</summary>
        public string baseHeroId;

        /// <summary>Nhánh tiến hóa của dạng này (None nếu là dạng gốc).</summary>
        public EvolutionPath evolvePath;

        /// <summary>Lấy chỉ số gốc (lv1) theo loại — SPD/CritRate/CritDmg không có growth.</summary>
        public float BaseOf(StatType stat)
        {
            switch (stat)
            {
                case StatType.Hp: return baseHp;
                case StatType.Atk: return baseAtk;
                case StatType.Def: return baseDef;
                case StatType.Spd: return baseSpd;
                case StatType.CritRate: return critRate;
                case StatType.CritDmg: return critDmg;
                case StatType.DamageReduction: return damageReduction;
                default: return 0f;
            }
        }

        /// <summary>Lấy hệ số tăng mỗi level theo loại.</summary>
        public float GrowthOf(StatType stat)
        {
            switch (stat)
            {
                case StatType.Hp: return growthHp;
                case StatType.Atk: return growthAtk;
                case StatType.Def: return growthDef;
                default: return 0f;
            }
        }
    }

    /// <summary>Config "viên đạn" skill (CSV <c>Skills.csv</c>) — metadata thi triển, KHÔNG chứa con số effect.</summary>
    [Serializable]
    public struct SkillModel
    {
        public string id;
        public string name;
        public string desc;
        public SkillType type;
        public TargetType targetType;
        public int energyCost; // Ultimate: energy cần/đủ để tung (thường 100)
        public int manaCost;   // Active: mana tiêu hao khi dùng
        public int cooldown;   // (không còn dùng — skill gate bằng energy/mana)
        public int hitCount;
        public string animKey;
    }

    /// <summary>
    ///     1 dòng <c>SkillEffects.csv</c> — ghép 1 effect nguyên tử vào skill.
    ///     Một skill gồm nhiều dòng, chạy theo <see cref="order" />.
    /// </summary>
    [Serializable]
    public struct SkillEffectModel
    {
        public string skillId;
        public int order;
        public EffectType effectType;
        public TargetType target;   // Inherit = theo skill cha
        public StatType scaleStat;  // chỉ số làm gốc scale (vd ATK)
        public float scaleValue;    // hệ số, 1.2 = 120%
        public int flat;            // cộng phẳng
        public float chance;        // xác suất proc, 1.0 = chắc chắn
        public string buffId;       // trỏ Effects.csv (ApplyBuff/Debuff)
        public int duration;        // số turn
    }

    /// <summary>Config buff/debuff (CSV <c>Effects.csv</c>) — cái gì xảy ra mỗi turn.</summary>
    [Serializable]
    public struct EffectModel
    {
        public string id;
        public EffectCategory category;
        public StatType stat;        // stat bị mod (Buff/Debuff)
        public ModType modType;
        public float value;          // -0.3 = giảm 30%
        public StatType tickScaleStat; // DoT/HoT scale theo stat nào của caster
        public float tickValue;      // 0.30 = 30% stat/turn
        public int maxStack;
        public bool dispellable;
        public string icon;
    }

    /// <summary>Config theo bậc sao (CSV <c>HeroStarConfig.csv</c>).</summary>
    [Serializable]
    public struct HeroStarModel
    {
        public int star;
        public float statMultiplier;
        public int levelCap;
        public string unlockSkillId;
    }

    /// <summary>Chi phí lên sao từ bậc <see cref="fromStar" /> (CSV <c>HeroStarCost.csv</c>).</summary>
    [Serializable]
    public struct HeroStarCostModel
    {
        public int fromStar;
        public CostType costType;
        public int dupeCount;
        public string materialId;
        public int materialAmount;
        public int goldCost;
    }

    /// <summary>
    ///     1 nhánh tiến hóa (CSV <c>HeroEvolution.csv</c>). 1 hero gốc → nhiều dòng.
    ///     Đây là bản ghi "cây tiến hóa": nối <see cref="baseHeroId" /> với <see cref="targetHeroId" />.
    /// </summary>
    [Serializable]
    public struct HeroEvolutionModel
    {
        public string baseHeroId;   // hero gốc
        public EvolutionPath path;  // nhánh
        public string targetHeroId; // hero sau tiến hóa
        public string pathName;     // localize key hiển thị
        public string icon;
        public int reqStar;         // điều kiện sao tối thiểu
        public int reqLevel;        // điều kiện level tối thiểu
        public string materialId;
        public int materialAmount;
        public int goldCost;
    }

    /// <summary>
    ///     Loadout theo bậc sao của 1 hero (CSV <c>HeroStarLoadout.csv</c>): mỗi sao đổi skill chính.
    ///     Passive theo sao tách sang join <c>HeroStarPassive</c> (1 hero-sao có thể nhiều passive).
    /// </summary>
    [Serializable]
    public struct HeroStarLoadoutModel
    {
        public string heroId;
        public int star;
        public string activeSkillId; // skill chính ở bậc sao này
    }

    /// <summary>
    ///     Định nghĩa 1 passive tái dùng (CSV <c>Passives.csv</c>). Tham số generic để 1 loại
    ///     phục vụ nhiều tier (vd Block: value=tỷ lệ, value2=hệ số phản, flag=có phản công).
    /// </summary>
    [Serializable]
    public struct PassiveConfigModel
    {
        public string id;
        public PassiveType type;
        public float value;   // tham số chính (vd block chance / dmgScale bóng / % speed aura)
        public float value2;  // tham số phụ (vd counter scale / % cướp speed bóng)
        public float value3;  // tham số phụ 2 (vd % lifesteal bóng)
        public bool flag;     // cờ (vd counterOnBlock)
        public string refId;  // tham chiếu thêm (buffId/skillId) nếu passive cần
    }

    /// <summary>1 dòng join <c>HeroStarPassive.csv</c>: hero tại bậc sao có passive nào.</summary>
    [Serializable]
    public struct HeroStarPassiveModel
    {
        public string heroId;
        public int star;
        public string passiveId;
    }

    /// <summary>
    ///     1 món trang bị (CSV <c>Equipments.csv</c>). Stat cộng thêm ở join <c>EquipStats</c>,
    ///     effect (passive) ở join <c>EquipPassive</c>, skill riêng ở <see cref="grantedSkillId" />.
    /// </summary>
    [Serializable]
    public struct EquipmentModel
    {
        public string id;
        public string name;
        public EquipSlot slot;
        public int rarity;
        public string grantedSkillId; // skill món đồ cấp cho hero (rỗng nếu không có)
        public string icon;
    }

    /// <summary>1 dòng cộng chỉ số của trang bị (CSV <c>EquipStats.csv</c>). 1 món → nhiều dòng.</summary>
    [Serializable]
    public struct EquipStatModel
    {
        public string equipId;
        public StatType stat;
        public ModType modType; // Flat = cộng phẳng, Percent = nhân theo base
        public float value;
    }

    /// <summary>1 dòng join <c>EquipPassive.csv</c>: trang bị cấp passive (effect) nào (trỏ Passives.csv).</summary>
    [Serializable]
    public struct EquipPassiveModel
    {
        public string equipId;
        public string passiveId;
    }
}
