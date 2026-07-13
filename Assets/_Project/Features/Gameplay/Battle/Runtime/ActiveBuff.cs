namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Một buff/debuff đang chạy trên 1 unit. Snapshot lại giá trị tick lúc apply
    ///     (DoT/HoT không đổi dù caster sau đó bị buff/giảm chỉ số).
    /// </summary>
    public class ActiveBuff
    {
        #region Fields

        public EffectModel Effect;
        public int RemainingTurns;
        public int Stacks;

        /// <summary>Sát thương/hồi mỗi turn đã snapshot (DoT/HoT). 0 nếu là buff stat thuần.</summary>
        public float TickAmount;

        /// <summary>Unit gây ra buff (để quy công sát thương DoT nếu cần).</summary>
        public Unit Source;

        #endregion

        public ActiveBuff(EffectModel effect, int duration, Unit source, float tickAmount)
        {
            Effect = effect;
            RemainingTurns = duration;
            Source = source;
            TickAmount = tickAmount;
            Stacks = 1;
        }

        public bool IsExpired => RemainingTurns <= 0;
    }
}
