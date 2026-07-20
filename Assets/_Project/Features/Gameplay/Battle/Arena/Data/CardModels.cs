using System;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Loại hiệu ứng của 1 thẻ bài.</summary>
    public enum CardType
    {
        Damage = 0,     // gây damage lên enemy trong <see cref="CardTargetShape" /> (kèm knockback nếu có)
        Heal = 1,       // hồi máu hero (power = lượng máu)
        BuffDamage = 2, // buff HỆ SỐ % damage hero trong <see cref="CardModel.dur" /> round (power = mul cộng, 0.5 = +50%)
        Trap = 3,       // đặt bẫy lên ô nhắm (power = damage, dur = round tồn tại, hits = số lần trúng)
        DrawCards = 4,  // rút thêm bài lên tay (power = số lá)
        GainEnergy = 5, // +energy lượt này (power = số energy)
        StatBuff = 6    // CỘNG CHỈ SỐ hero (<see cref="CardModel.stat" /> = atk/def/maxHp/critRate/critDmg; power = lượng cộng; dur = số round, 0 = cả trận)
    }

    /// <summary>
    ///     Tập ô mục tiêu trên lưới cực (tâm = hero). Xoay theo hướng NHẮM của người chơi.
    ///     <para><b>Ràng buộc:</b> HERO ĐỨNG YÊN Ở TÂM — không có shape/loại card nào dịch chuyển hero.
    ///     Mọi targeting đều quay quanh tâm cố định theo hướng nhắm.</para>
    /// </summary>
    public enum CardTargetShape
    {
        None = 0,       // không nhắm ô (self: heal/buff/draw/energy)
        RadialLine = 1, // 1 tia xuyên tâm: mọi ring của sector nhắm
        Arc = 2,        // hình quạt: sector nhắm ± <see cref="CardModel.shapeSize" />, mọi ring
        Ring = 3,       // 1 vòng ring: toàn bộ sector của ring <see cref="CardModel.ring" />
        Board = 4       // toàn sàn (mọi enemy)
    }

    /// <summary>
    ///     Config 1 thẻ bài (CSV <c>ArenaCards.csv</c>). Thẻ là lớp chiến thuật CỘNG THÊM lên vũ khí/ult —
    ///     mỗi player round người chơi bốc bài lên tay, tiêu energy để đánh.
    ///     <para><b>knockback</b>: chuỗi bước đẩy lùi tuyệt đối "x y" (x = lệch sector ±, y = +ra ngoài/−vào tâm),
    ///     ngăn bằng ';' — dùng chung <see cref="CellSet" /> với vũ khí. Vd "0 1;0 1" = đẩy ra ngoài 2 ring.</para>
    /// </summary>
    [Serializable]
    public struct CardModel
    {
        public string id;
        public string name;
        public string heroClass; // "tank"/"mage"/... | "neutral" (mọi hero) | HOẶC heroId (vd "44001") = card RIÊNG hero đó
        public string stat;      // StatBuff: chỉ số cộng — "atk"/"def"/"maxHp"/"critRate"/"critDmg"
        public int cost;         // energy cần để đánh lá này
        public int type;         // CardType
        public int shape;        // CardTargetShape
        public float power;      // Damage/Heal = lượng; BuffDamage = mul cộng; Draw/Energy = số lượng
        public string knockback; // các bước đẩy lùi (Damage/Trap) — xem mô tả
        public int shapeSize;    // Arc: số sector mỗi bên tính từ sector nhắm
        public int ring;         // Ring: ring mục tiêu; Trap: ring đặt bẫy; <0 = mặc định (ring 1)
        public int dur;          // BuffDamage: số round hiệu lực; Trap: số round bẫy tồn tại
        public int hits;         // Trap: số lần trúng enemy trước khi bẫy mất
        public int rarity;       // 1 common .. cao dần
        public string art;       // key sprite icon (Resources)
        public string desc;      // mô tả hiển thị UI
    }

    /// <summary>Quy tắc energy MỖI player round (tăng dần theo round, có trần).</summary>
    public static class CardEnergy
    {
        public const int Base = 1; // energy round player đầu tiên
        public const int Ramp = 1; // +energy mỗi round player kế
        public const int Cap = 5;  // trần energy

        /// <summary>Energy đầy của player round thứ <paramref name="playerRound" /> (1-based).</summary>
        public static int ForRound(int playerRound)
        {
            if (playerRound < 1) playerRound = 1;
            int e = Base + Ramp * (playerRound - 1);
            return e < Cap ? e : Cap;
        }
    }
}
