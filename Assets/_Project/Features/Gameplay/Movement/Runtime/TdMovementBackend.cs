namespace Ezg.Feature.Movement
{
    /// <summary>Cách lính di chuyển trên tuyến.</summary>
    public enum TdMovementBackend
    {
        /// <summary>NavMesh khi đang Play, waypoint khi ngoài Play Mode.</summary>
        Auto = 0,

        /// <summary>Tự tính vị trí theo waypoint. Chạy được cả ngoài Play Mode, không cần bake.</summary>
        Waypoint = 1,

        /// <summary>NavMeshAgent lo lái và né nhau. Chỉ chạy trong Play Mode, cần NavMesh đã bake.</summary>
        NavMesh = 2,
    }
}
