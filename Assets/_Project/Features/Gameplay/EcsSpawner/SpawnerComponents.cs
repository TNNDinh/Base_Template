using Unity.Entities;
using Unity.Mathematics;

namespace Ezg.Feature.EcsSpawner
{
    // ==========================================================================================
    //  ECS 101 — "C" = COMPONENT
    //  Component chỉ là DỮ LIỆU thuần (struct), KHÔNG có hàm/logic. Entity = 1 cái id rỗng,
    //  bạn "gắn" các component vào nó. System (logic) sẽ query theo tổ hợp component.
    //  IComponentData = component unmanaged (blittable) -> chạy được trong Burst + Job.
    // ==========================================================================================

    /// <summary>Cấu hình 1 spawner: prefab con + số lượng + vùng spawn + tốc độ rơi.</summary>
    public struct Spawner : IComponentData
    {
        public Entity Prefab;   // entity-prefab để nhân bản (đã bake từ GameObject prefab)
        public int Count;       // số entity spawn ra
        public float3 AreaSize; // bề rộng vùng spawn (x,z dùng để rải ngẫu nhiên)
        public float Speed;     // tốc độ rơi gán cho entity con
    }

    /// <summary>Vận tốc rơi của mỗi entity con.</summary>
    public struct FallVelocity : IComponentData
    {
        public float3 Value;
    }

    /// <summary>Tag rỗng (0 byte) — chỉ để đánh dấu/nhận diện entity do spawner tạo ra.</summary>
    public struct SpawnedTag : IComponentData
    {
    }
}
