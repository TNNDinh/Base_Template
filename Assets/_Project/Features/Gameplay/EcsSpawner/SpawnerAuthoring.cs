using Unity.Entities;
using UnityEngine;

namespace Ezg.Feature.EcsSpawner
{
    // ==========================================================================================
    //  ECS 101 — AUTHORING + BAKING
    //  ECS runtime KHÔNG dùng GameObject. Nhưng ta vẫn thiết kế trong Editor bằng GameObject cho dễ.
    //  -> "Authoring" là 1 MonoBehaviour bình thường bạn gắn lên GameObject (đặt trong 1 SUBSCENE).
    //  -> "Baker" chạy 1 LẦN lúc bake (Editor/build), CHUYỂN GameObject đó thành entity + component.
    //     Baker KHÔNG chạy runtime. Runtime chỉ thấy entity data đã bake.
    // ==========================================================================================

    /// <summary>
    ///     Gắn component này lên 1 GameObject bên trong SubScene. Kéo 1 prefab (có MeshRenderer) vào
    ///     <see cref="Prefab" /> để Entities Graphics vẽ được các entity con.
    /// </summary>
    public class SpawnerAuthoring : MonoBehaviour
    {
        [Tooltip("Prefab con — phải có MeshFilter + MeshRenderer (vd Cube) để render bằng Entities Graphics.")]
        public GameObject Prefab;

        [Tooltip("Số lượng entity spawn (thử 1000, 10000... để thấy sức mạnh ECS).")]
        public int Count = 1000;

        public Vector3 AreaSize = new Vector3(24f, 0f, 24f);
        public float Speed = 4f;

        // Baker: nested class, generic theo đúng authoring type. Unity tự gọi khi bake SubScene.
        private class SpawnerBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring a)
            {
                // GetEntity(prefab, ...) bake luôn prefab con thành 1 entity-prefab và trả về ref.
                var prefabEntity = a.Prefab != null
                    ? GetEntity(a.Prefab, TransformUsageFlags.Dynamic)
                    : Entity.Null;

                // Entity của chính spawner GameObject này.
                var self = GetEntity(TransformUsageFlags.None);

                AddComponent(self, new Spawner
                {
                    Prefab = prefabEntity,
                    Count = a.Count,
                    AreaSize = a.AreaSize,
                    Speed = a.Speed
                });
            }
        }
    }
}
