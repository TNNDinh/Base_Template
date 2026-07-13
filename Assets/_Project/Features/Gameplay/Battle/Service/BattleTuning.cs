namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Hệ số cân bằng toàn cục để TEST (kéo dài trận cho dễ quan sát skill/mana/anim).
    ///     Về bản chính thức: đặt cả 2 = 1f.
    /// </summary>
    public static class BattleTuning
    {
        /// <summary>Nhân HP tối đa của mọi unit (test = máu trâu).</summary>
        public static float HpMultiplier = 5f;

        /// <summary>Nhân sát thương cuối của mọi đòn (test = damage thấp, trận dài).</summary>
        public static float DamageMultiplier = 0.35f;
    }
}
