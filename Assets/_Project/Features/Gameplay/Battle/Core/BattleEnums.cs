namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Hệ nguyên tố của hero/enemy. Dùng cho khắc chế sát thương.</summary>
    public enum ElementType
    {
        None = 0,
        Fire = 1,
        Water = 2,
        Wind = 3,
        Light = 4,
        Dark = 5
    }

    /// <summary>Lớp nhân vật (ảnh hưởng vai trò đội hình, sau này có thể buff theo class).</summary>
    public enum HeroClass
    {
        None = 0,
        Warrior = 1,
        Mage = 2,
        Ranger = 3,
        Support = 4,
        Tank = 5
    }

    /// <summary>
    ///     Nhánh tiến hóa của hero. <see cref="None" /> = dạng gốc (chưa tiến hóa).
    ///     Mỗi nhánh trỏ tới 1 hero id đích riêng (stat + skill riêng).
    /// </summary>
    public enum EvolutionPath
    {
        None = 0,
        Assassin = 1, // thuần sát thủ: ATK/CRIT/SPD cao, máu/giáp thấp
        Fighter = 2,  // thuần đấu sĩ: cân bằng
        Tank = 3      // thuần tank: HP/DEF cao, ATK thấp
    }

    /// <summary>Các chỉ số chiến đấu của 1 unit.</summary>
    public enum StatType
    {
        None = 0,
        Hp = 1,
        Atk = 2,
        Def = 3,
        Spd = 4,
        CritRate = 5,
        CritDmg = 6,
        DamageReduction = 7 // đỡ đòn ở STAT: % giảm sát thương nhận vào (0..0.9)
    }

    /// <summary>Loại skill.</summary>
    public enum SkillType
    {
        Basic = 0,
        Active = 1,
        Ultimate = 2,
        Passive = 3
    }

    /// <summary>
    ///     Kiểu chọn mục tiêu. <see cref="Inherit" /> = dùng targetType của skill cha
    ///     (cho từng effect override khi cần, vd skill đánh địch nhưng effect buff bản thân).
    /// </summary>
    public enum TargetType
    {
        Inherit = 0,
        Self = 1,
        EnemySingle = 2,
        EnemyAll = 3,
        EnemyRow = 4,
        EnemyLowestHp = 5,
        EnemyRandom = 6,
        AllyAll = 7,
        AllyLowestHp = 8,
        AllySingle = 9
    }

    /// <summary>Loại hiệu ứng nguyên tử mà 1 dòng SkillEffect thực thi.</summary>
    public enum EffectType
    {
        Damage = 0,
        Heal = 1,
        Shield = 2,
        ApplyBuff = 3,
        ApplyDebuff = 4,
        Cleanse = 5,
        EnergyGain = 6,
        Taunt = 7,
        Revive = 8,
        SpeedSteal = 9,     // cướp % SPD target → cộng caster
        Lifesteal = 10,     // hồi máu caster theo % tổng damage skill này gây ra
        DamageByLostHp = 11 // damage tăng theo % máu đã mất của caster (máu ít → dmg to)
    }

    /// <summary>Phân loại buff/debuff lưu trên unit.</summary>
    public enum EffectCategory
    {
        Buff = 0,
        Debuff = 1,
        Dot = 2,
        Hot = 3,
        Control = 4,
        Shield = 5
    }

    /// <summary>Kiểu cộng chỉ số của 1 modifier.</summary>
    public enum ModType
    {
        Flat = 0,
        Percent = 1
    }

    /// <summary>Nguồn chi phí nâng sao.</summary>
    public enum CostType
    {
        Dupe = 0,
        Material = 1
    }

    /// <summary>Loại passive (role thụ động của tướng) — mỗi loại map 1 class qua PassiveFactory.</summary>
    public enum PassiveType
    {
        None = 0,
        Block = 1,     // đỡ đòn (+ tuỳ chọn phản công)
        SpeedAura = 2, // +% SPD vĩnh viễn
        Shadow = 3     // bóng đi theo: đánh cùng, cướp speed, hút máu (+ thay hero chết)
        // TODO: [Battle] - Evasion, Regen, DamageReduction, Thorns...
    }

    /// <summary>Ô trang bị của hero. Mỗi hero mang tối đa 1 món mỗi slot.</summary>
    public enum EquipSlot
    {
        None = 0,
        Weapon = 1, // kiếm
        Armor = 2,  // giáp
        Boots = 3,  // giày
        Helmet = 4  // mũ
    }

    /// <summary>Phe trong trận.</summary>
    public enum BattleTeam
    {
        Player = 0,
        Enemy = 1
    }
}
