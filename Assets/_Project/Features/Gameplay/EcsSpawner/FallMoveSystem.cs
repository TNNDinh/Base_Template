using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Ezg.Feature.EcsSpawner
{
    // ==========================================================================================
    //  ECS 101 — IJobEntity + BURST + PARALLEL
    //  Đây là chỗ ECS "ăn tiền": di chuyển HÀNG NGHÌN/CHỤC NGHÌN entity mỗi frame, chia trên nhiều
    //  core CPU (ScheduleParallel) + Burst compile ra mã máy tối ưu. So với MonoBehaviour.Update
    //  (mỗi object 1 lời gọi managed) thì nhanh hơn nhiều bậc khi số lượng lớn.
    // ==========================================================================================

    [BurstCompile]
    public partial struct FallMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new FallJob { Dt = SystemAPI.Time.DeltaTime };
            // ScheduleParallel: chia entity thành nhiều batch chạy song song trên các worker thread.
            job.ScheduleParallel();
        }
    }

    /// <summary>
    ///     IJobEntity: Unity tự sinh vòng lặp gọi <see cref="Execute" /> cho MỌI entity có đủ
    ///     (LocalTransform + FallVelocity). Rơi xuống; chạm đáy thì đẩy lên lại -> loop vô hạn để xem.
    /// </summary>
    [BurstCompile]
    public partial struct FallJob : IJobEntity
    {
        public float Dt;

        private void Execute(ref LocalTransform xform, in FallVelocity vel)
        {
            xform.Position += vel.Value * Dt;
            if (xform.Position.y < -6f)
                xform.Position.y = 12f;
        }
    }
}
