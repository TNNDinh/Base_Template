using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Ezg.Feature.EcsSpawner
{
    // ==========================================================================================
    //  ECS 101 — "S" = SYSTEM (logic)
    //  ISystem = system dạng struct, unmanaged -> Burst-compile được (nhanh). Chạy trên MỌI entity
    //  khớp query. Ở đây: tìm mọi entity có Spawner -> instantiate Count entity con -> bỏ spawner.
    //
    //  STRUCTURAL CHANGE (tạo/xoá entity, add/remove component) KHÔNG được làm trực tiếp giữa vòng
    //  lặp query -> dùng EntityCommandBuffer (ECB): ghi lệnh vào buffer rồi Playback 1 lần.
    // ==========================================================================================

    [BurstCompile]
    public partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // System chỉ update khi tồn tại ít nhất 1 Spawner (tối ưu: khỏi chạy vô ích).
            state.RequireForUpdate<Spawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (spawner, entity) in SystemAPI.Query<RefRO<Spawner>>().WithEntityAccess())
            {
                var s = spawner.ValueRO;
                if (s.Prefab == Entity.Null) { ecb.DestroyEntity(entity); continue; }

                var rnd = Random.CreateFromIndex((uint)entity.Index + 1u);

                for (int i = 0; i < s.Count; i++)
                {
                    var inst = ecb.Instantiate(s.Prefab);

                    var pos = new float3(
                        rnd.NextFloat(-0.5f, 0.5f) * s.AreaSize.x,
                        rnd.NextFloat(6f, 14f),
                        rnd.NextFloat(-0.5f, 0.5f) * s.AreaSize.z);

                    ecb.SetComponent(inst, LocalTransform.FromPositionRotationScale(
                        pos, quaternion.identity, rnd.NextFloat(0.3f, 0.8f)));

                    ecb.AddComponent(inst, new FallVelocity { Value = new float3(0f, -s.Speed, 0f) });
                    ecb.AddComponent<SpawnedTag>(inst);
                }

                // Spawn 1 lần rồi bỏ spawner. (Muốn spawn liên tục: giữ lại + đếm thời gian.)
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
