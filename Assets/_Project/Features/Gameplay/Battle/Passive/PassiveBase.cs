namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Base cho mọi passive (role/hành vi thụ động của tướng). Combat pipeline gọi các hook;
    ///     mỗi loại passive = 1 subclass, dựng từ config qua <see cref="PassiveFactory" />.
    ///     Mirror pattern <see cref="SkillEffectBase" /> — thêm passive mới KHÔNG đụng <see cref="Unit" />.
    /// </summary>
    public abstract class PassiveBase
    {
        /// <summary>Đầu lượt của unit sở hữu.</summary>
        public virtual void OnTurnStart(BattleContext ctx, Unit self) { }

        /// <summary>
        ///     Trước khi unit nhận sát thương. Được sửa <paramref name="dmg" /> và đặt
        ///     <paramref name="blocked" /> = true nếu chặn đòn.
        /// </summary>
        public virtual void OnIncomingDamage(BattleContext ctx, Unit attacker, Unit self,
            ref float dmg, ref bool blocked) { }

        /// <summary>Sau khi đỡ đòn thành công (blocked = true).</summary>
        public virtual void OnAfterBlocked(BattleContext ctx, Unit attacker, Unit self) { }

        /// <summary>Sau khi CHÍNH unit này đánh trúng target (dealt = sát thương thực). Dùng cho bóng đánh cùng...</summary>
        public virtual void OnAfterDealDamage(BattleContext ctx, Unit self, Unit target, float dealt) { }

        /// <summary>Sau khi 1 unit trong trận chết. Dùng cho passive phản ứng khi đồng đội chết (vd Bóng thay hero).</summary>
        public virtual void OnUnitDied(BattleContext ctx, Unit self, Unit dead) { }
    }
}
