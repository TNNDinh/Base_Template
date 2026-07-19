namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Cầu nối chọn stage → scene arena. Màn chọn stage set <see cref="PendingStageId" /> rồi load scene;
    ///     <see cref="ArenaSceneController" /> đọc và tiêu thụ nó lúc Start. (Giống BattleLaunch của trận 6v6.)
    /// </summary>
    public static class ArenaLaunch
    {
        public static string PendingStageId;
    }
}
