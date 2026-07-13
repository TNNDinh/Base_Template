namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Dữ liệu bàn giao khi chuyển sang BattleScene. Stage-select set <see cref="PendingStageId" />
    ///     rồi đổi scene; <see cref="BattleSceneController" /> đọc lúc Start để auto-đánh.
    /// </summary>
    public static class BattleLaunch
    {
        /// <summary>Ải sẽ đánh khi BattleScene load xong. Null = vào scene trống (không auto-đánh).</summary>
        public static string PendingStageId;
    }
}
