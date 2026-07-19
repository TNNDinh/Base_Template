using System;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Cách 1 skill được KÍCH HOẠT. Unit chỉ giữ list skill id; skill TỰ ĐỊNH NGHĨA trigger + effect.</summary>
    public enum SkillTrigger
    {
        Active,        // hero bấm ULT (tay). Enemy không dùng.
        Passive,       // luôn bật (buff chỉ số / regen / giảm damage)
        OnDeath,       // khi unit chết (vd slime tách)
        OnMove,        // mỗi khi unit di chuyển
        OnDamaged,     // mỗi khi unit nhận damage
        OnLowHp,       // khi máu xuống dưới param (%)
        EveryNRounds   // cứ param round 1 lần
    }

    /// <summary>Tác dụng của skill khi kích hoạt.</summary>
    public enum ArenaSkillEffect
    {
        Nuke,      // sát thương diện rộng (hero→toàn enemy; enemy→hero) = atk*power
        Shockwave, // như Nuke + đẩy lùi (hero→toàn enemy)
        Heal,      // hồi máu (self + đồng đội) = maxHp*power
        Spawn,     // sinh ra enemy (spawnId × spawnCount) vào ô trống gần đó
        StatBuff   // PASSIVE: buff chỉ số theo statType
    }

    /// <summary>Chỉ số mà PASSIVE (StatBuff) tác động.</summary>
    public enum ArenaStatType
    {
        AtkPct,       // +% atk
        RegenPct,     // hồi %/round máu tối đa
        DmgReducePct, // giảm % damage nhận
        MaxHpPct      // +% máu tối đa
    }

    /// <summary>
    ///     1 skill arena TỰ ĐỊNH NGHĨA: <see cref="trigger" /> (khi nào) + <see cref="effect" /> (làm gì).
    ///     Unit (hero/enemy) chỉ tham chiếu bằng id — KHÔNG nhét năng lực skill vào unit model.
    ///     Nâng cấp = đổi sang <see cref="nextSkillId" /> (chuỗi skill).
    /// </summary>
    [Serializable]
    public struct ArenaSkillModel
    {
        public string id;
        public string name;
        public int trigger;        // SkillTrigger
        public int effect;         // ArenaSkillEffect
        public int statType;       // StatBuff: ArenaStatType
        public float power;        // Nuke/Shockwave = hệ số atk; Heal = % máu; StatBuff = độ mạnh (0.2=20%)
        public int cooldown;       // số round CD (Active + trigger lặp lại)
        public float param;        // OnLowHp = ngưỡng % máu; EveryNRounds = N
        public string spawnId;     // Spawn: enemy id sinh ra
        public int spawnCount;     // Spawn: số con
        public string nextSkillId; // nâng cấp → id kế (rỗng = max)
        public int upgradeCost;    // gold nâng cấp
    }
}
