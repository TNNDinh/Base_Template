---
name: create-arena-hero
description: Tạo 1 hero mới cho arena micro-RPG. Thêm dòng vào HeroStats.csv, regenerate HeroStatCollection.asset qua Unity MCP, gán loadout vũ khí + skill mặc định, đăng ký prefab trong ArenaPrefabRegistry. Dùng khi user nói "tạo hero", "thêm hero arena", "create arena hero".
---

# Create Arena Hero — thêm hero cho arena

Hero arena = 1 dòng stat (HeroStats) + prefab model (ST09) + loadout 3 vũ khí ("skill" playstyle) + skill ultimate mặc định.

## Data model — `UnitStatModel` (dùng chung hero/enemy)
File: `Assets/_Project/Features/Gameplay/Battle/Arena/Data/ArenaStageModels.cs`

| Field | Ghi chú (hero) |
|-------|----------------|
| `id` | id prefab, dạng `44xxx` |
| `name` | tên hiển thị |
| `hp,atk,def,spd,critRate,critDmg,attackRange` | stat gốc |
| `attackType` | hero để `0` |
| `skills` | DANH SÁCH skill id `;`-sep. Hero cần 1 skill trigger=Active (ult) + tuỳ chọn passive. VD `sk_nuke_1;sk_pas_atk_1`. Skill tự định nghĩa (xem create-arena-skill) |
| `hpPerLevel,atkPerLevel,defPerLevel` | growth mỗi cấp (nâng cấp hero ngoài battle) |

> `UnitStatModel` chỉ có 1 field skill là `skills` (list id) — KHÔNG nhét năng lực skill vào model. Ult của hero = skill trigger=Active đầu tiên trong list; nâng cấp = đổi id trong chuỗi.

- CSV: `.../ArenaCsv/HeroStats.csv` · SO: `.../ArenaCsv/HeroStatCollection.asset`
- `HeroStatCollection` là **class file riêng** (`HeroStatCollection.cs`) để asset bind ổn định sau domain reload — QUAN TRỌNG: nếu tạo asset kiểu cũ (class chung file) sẽ null sau reload.

## Quy trình

### 1 — Hỏi thông tin
`id (44xxx)`, `name`, stat, `ultimate` (0/1/2), growth, và **prefab model** (path prefab ST09 ở `Assets/TDGAMES/Prefabs/Hero/44xxx`). Nếu chưa có prefab → hỏi/ dùng prefab hero có sẵn.

### 2 — Thêm dòng vào `HeroStats.csv`
Header: `id,name,hp,atk,def,spd,critRate,critDmg,attackRange,attackType,ultimate,hpPerLevel,atkPerLevel,defPerLevel`.

### 3 — Regenerate `HeroStatCollection.asset` (Unity MCP)
Đọc HeroStats.csv, liệt kê LẠI toàn bộ hero + hero mới. Qua `unity_execute_code` (fully-qualified, no `using`):
```csharp
var dir = "Assets/_Project/Features/Gameplay/Battle/Resources/ArenaCsv/";
var path = dir + "HeroStatCollection.asset";
System.Func<string,string,float,float,float,float,float,float,float,int,int,float,float,float,Ezg.Feature.Gameplay.Battle.UnitStatModel> H =
 (id,nm,hp,atk,def,spd,cr,cd,ar,at,ult,hpl,atl,dfl)=> new Ezg.Feature.Gameplay.Battle.UnitStatModel{ id=id,name=nm,hp=hp,atk=atk,def=def,spd=spd,critRate=cr,critDmg=cd,attackRange=ar,attackType=at,ultimate=ult,hpPerLevel=hpl,atkPerLevel=atl,defPerLevel=dfl };
var col = ScriptableObject.CreateInstance<Ezg.Feature.Gameplay.Battle.HeroStatCollection>();
col.dataGroup = new Ezg.Feature.Gameplay.Battle.UnitStatModel[]{ /* tất cả hero + mới */ };
UnityEditor.AssetDatabase.DeleteAsset(path);
UnityEditor.AssetDatabase.CreateAsset(col, path);
UnityEditor.AssetDatabase.SaveAssets(); UnityEditor.AssetDatabase.Refresh();
return Resources.Load<Ezg.Feature.Gameplay.Battle.HeroStatCollection>("ArenaCsv/HeroStatCollection") != null ? "OK" : "NULL";
```

### 4 — Đăng ký prefab trong `ArenaPrefabRegistry.asset`
`.../ArenaCsv/ArenaPrefabRegistry.asset` → mảng `heroes` (Entry{id, prefab}). Thêm entry `{id="44xxx", prefab=<hero prefab>}` qua Inspector hoặc `unity_execute_code` (load registry, append vào `heroes`, SetDirty, SaveAssets, gọi `Rebuild()`).

### 5 — Loadout + skill (tuỳ chọn)
- Loadout 3 vũ khí: `ArenaSceneController.HeroLoadouts` (dict trong code) — thêm entry `{ "44xxx", new[]{ "wp_..","wp_..","wp_.." } }`. Không có → dùng loadout mặc định.
- Skill mặc định tự suy từ `ultimate` enum (bước data). Nâng cấp skill lo phần đổi id.

### 6 — compile-check nếu sửa code
Nếu chỉ sửa CSV + asset + registry → không cần compile. Nếu sửa `HeroLoadouts` (code) → chạy `/compile-check`.

### 7 — Test
Hero mới hiện trong nút đổi hero (cycle theo HeroStats). `ArenaSceneController.SetHero("44xxx")` để test nhanh trong play mode.
