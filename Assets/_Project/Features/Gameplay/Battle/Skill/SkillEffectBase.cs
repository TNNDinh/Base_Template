namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Base cho mọi hiệu ứng nguyên tử của skill (mirror pattern <c>ItemRoleBase</c> của base).
    ///     Mỗi <see cref="EffectType" /> = 1 subclass, lấy qua <see cref="SkillEffectFactory" />.
    /// </summary>
    public abstract class SkillEffectBase
    {
        /// <summary>Áp 1 effect lên 1 target. data là dòng SkillEffects đang xử lý.</summary>
        public abstract void Apply(BattleContext ctx, SkillEffectModel data, Unit caster, Unit target);

        /// <summary>Sát thương/giá trị gốc trước giảm trừ = stat × scale + flat.</summary>
        protected static float RawValue(SkillEffectModel data, Unit caster)
        {
            var stat = data.scaleStat == StatType.None ? 0f : caster.GetStat(data.scaleStat);
            return stat * data.scaleValue + data.flat;
        }
    }
}
