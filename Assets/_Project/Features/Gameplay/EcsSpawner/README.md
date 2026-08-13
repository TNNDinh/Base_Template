# ECS Spawner — vertical slice học Unity DOTS/ECS

Bộ ví dụ tối giản để **học ECS** và làm nền cho game "spawn nhiều thứ". Spawn hàng nghìn/chục nghìn
entity rơi xuống, di chuyển bằng job song song + Burst. Compile sạch trên Unity 6000.2 + Entities 1.4.

## ECS trong 30 giây
- **Entity** = 1 id rỗng (không chứa gì).
- **Component** (`IComponentData`) = DỮ LIỆU thuần (struct), không logic. Gắn vào entity.
- **System** (`ISystem`) = LOGIC chạy mỗi frame trên mọi entity khớp query.
- **Baking** = chuyển GameObject (Editor) → entity data (runtime), qua `Baker`. Chỉ chạy lúc bake.
- **Burst + Jobs** = compile system/job ra mã máy tối ưu + chạy song song nhiều core → nhanh khi số lượng lớn.

## Các file (đọc theo thứ tự này)
| File | Vai trò ECS | Học được gì |
|------|-------------|-------------|
| `SpawnerComponents.cs` | **C** — Component | Cách khai báo `IComponentData`, tag component |
| `SpawnerAuthoring.cs` | Authoring + **Baker** | Cầu nối GameObject→Entity, `GetEntity`, `TransformUsageFlags` |
| `SpawnerSystem.cs` | **S** — ISystem | Query, `EntityCommandBuffer`, structural change, instantiate |
| `FallMoveSystem.cs` | **S** — IJobEntity | `[BurstCompile]`, `ScheduleParallel`, di chuyển hàng loạt |

## Setup scene để CHẠY (làm 1 lần)
> ECS render qua **Entities Graphics** cần entity có mesh+material, và authoring phải nằm trong **SubScene** (để được bake).

1. **Tạo prefab con để render**: `GameObject > 3D Object > Cube` → kéo vào `Assets/…/EcsSpawner/` thành prefab `EcsCube.prefab` → xoá cube khỏi scene. (Material URP Lit mặc định là được.)
2. **Tạo scene demo**: `File > New Scene` (Basic URP) → lưu `Assets/_Project/Scenes/EcsSpawnerDemo.unity`. Đặt Camera nhìn xuống vùng spawn (vd position `(0, 4, -18)`, xoay hơi chúc xuống).
3. **Tạo SubScene**: chuột phải trong Hierarchy → `New Sub Scene > Empty Scene…` → lưu `EcsSpawnerSub.unity`.
4. **Trong SubScene**: tạo `Create Empty` → gắn component **Spawner Authoring** → kéo `EcsCube.prefab` vào ô **Prefab**, đặt `Count = 2000`, `Speed = 4`.
5. **Play** ▶ → thấy 2000 cube rơi + loop. Tăng `Count` lên 20000/50000 để thấy ECS gánh mượt.

## Thử nghiệm (bài tập)
- Đổi `Count` 1k → 50k, mở **Window > Analysis > Profiler** xem FrameTime (so cảm giác với 50k GameObject).
- Mở **Window > Entities > Hierarchy** để xem entity runtime.
- Thêm component `AngularSpin { float3 axisSpeed }` + 1 system quay từng cube (bài tự làm).
- Đổi `FallJob` cho cube nảy lại thay vì teleport lên.

## Ghi chú quan trọng
- **2D/sprite trong ECS còn yếu**: Entities Graphics thiên về **mesh 3D**. Game 2D nên render hybrid
  (GameObject companion) hoặc custom instancing — demo này dùng cube 3D cho đơn giản.
- **Không dùng** Odin/UniTask/TigerForge bên trong `ISystem` (unmanaged). Muốn bắn event ra UI/meta
  (OOP) thì tạo lớp bridge (managed system hoặc component managed).
- Assembly riêng `Ezg.Feature.EcsSpawner` (asmdef) tham chiếu Unity.Entities/Burst/Collections/Mathematics/
  Transforms/Entities.Graphics — mẫu để bạn tách assembly cho tầng gameplay ECS.
