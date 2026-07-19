---
name: create-arena-enemy
description: Tạo 1 enemy mới cho arena micro-RPG. Thêm dòng vào EnemyStats.csv (stat + attackType), regenerate EnemyStatCollection.asset qua Unity MCP, đăng ký prefab trong ArenaPrefabRegistry, và (tuỳ chọn) thêm vào spawn của 1 stage. Dùng khi user nói "tạo enemy", "thêm quái arena", "create arena enemy".
---

# Create Arena Enemy — thêm enemy cho arena

Enemy arena = 1 dòng stat (EnemyStats) + prefab model (ST09) + kiểu tấn công. Link vào stage qua `enemyId` trong ArenaSpawns.

## Data model — `UnitStatModel` (dùng chung hero/enemy)
File: `Assets/_Project/Features/Gameplay/Battle/Arena/Data/ArenaStageModels.cs`

| Field | Ghi chú (enemy) |
|-------|-----------------|
| `id` | id prefab, dạng `11xxx` |
| `name` | tên |
| `hp,atk,def,spd,critRate,critDmg` | stat gốc |
| `attackRange` | ring vào tầm thì bắt đầu telegraph. Melee=1; ranged/charge/blink để lớn (3-6) để đánh từ xa |
| `attackType` | `0`=Melee (lao vào ô trong), `1`=Ranged (đứng yên bắn hero), `2`=Charge (bò húc thẳng về tâm), `3`=Blink (teleport tới hero) |
| `skills` | DANH SÁCH skill id `;`-sep. Skill TỰ khai báo trigger (OnDeath/OnLowHp/EveryNRounds/OnMove/OnDamaged/Passive) + effect. VD slime tách: `sk_split_slime`; soi nổ mỗi 3 round: `sk_enemy_nuke`; voi tự hồi + trâu: `sk_enemy_heal;sk_pas_hp_1` |
| `hpPerLevel,atkPerLevel,defPerLevel` | growth theo level (mul ở ArenaSpawns) |

> KHÔNG nhét điều kiện/effect vào enemy model — chỉ list `skills`. Enemy skill effect: Heal=hồi máu cả đội; Nuke/Shockwave=đòn vào hero; Spawn=sinh con quanh ô (slime tách). Trigger + cooldown nằm trong skill data (create-arena-skill).

- CSV: `.../ArenaCsv/EnemyStats.csv` · SO: `.../ArenaCsv/EnemyStatCollection.asset`
- Kiểu tấn công xử lý ở `ArenaSceneController` (`MeleeAttack/RangedAttack/ChargeAttack/BlinkAttack`).

## Quy trình

### 1 — Hỏi thông tin
`id (11xxx)`, `name`, stat, `attackType` (0-3) + `attackRange` phù hợp, growth, và **prefab model** (`Assets/TDGAMES/Prefabs/Enemy/11xxx`).

### 2 — Thêm dòng vào `EnemyStats.csv`
Header: `id,name,hp,atk,def,spd,critRate,critDmg,attackRange,attackType,ultimate,hpPerLevel,atkPerLevel,defPerLevel`.

### 3 — Regenerate `EnemyStatCollection.asset` (Unity MCP)
Đọc EnemyStats.csv, liệt kê LẠI toàn bộ enemy + enemy mới (vì `CreateAsset` ghi đè). Cách an toàn nhất khi asset còn bind tốt: **sửa `dataGroup` tại chỗ**:
```csharp
var path = "Assets/_Project/Features/Gameplay/Battle/Resources/ArenaCsv/EnemyStatCollection.asset";
var col = UnityEditor.AssetDatabase.LoadAssetAtPath<Ezg.Feature.Gameplay.Battle.EnemyStatCollection>(path);
if (col == null) return "NULL (binding hỏng → tạo lại bằng CreateInstance+CreateAsset)";
var list = new System.Collections.Generic.List<Ezg.Feature.Gameplay.Battle.UnitStatModel>(col.dataGroup);
list.Add(new Ezg.Feature.Gameplay.Battle.UnitStatModel{ id="11xxx", name="...", hp=.., atk=.., def=.., spd=1, critRate=.05f, critDmg=1.5f, attackRange=.., attackType=.., ultimate=0, hpPerLevel=.., atkPerLevel=.., defPerLevel=.. });
col.dataGroup = list.ToArray();
col.Convert();
UnityEditor.EditorUtility.SetDirty(col);
UnityEditor.AssetDatabase.SaveAssets();
return "enemies=" + col.dataGroup.Length;
```
> Nếu `col == null` (binding hỏng sau reload) → tạo lại sạch bằng `CreateInstance<EnemyStatCollection>()` + set full dataGroup + `CreateAsset` (như create-arena-hero bước 3).

### 4 — Đăng ký prefab trong `ArenaPrefabRegistry.asset`
Mảng `enemies` (Entry{id, prefab}). Thêm `{id="11xxx", prefab=<enemy prefab>}` qua Inspector hoặc `unity_execute_code` (append + SetDirty + SaveAssets + `Rebuild()`).

### 5 — Cho enemy xuất hiện trong stage (tuỳ chọn)
Thêm dòng vào `ArenaSpawns.csv` + append vào `ArenaSpawnCollection.asset.dataGroup` (`ArenaSpawnModel{ stageId, round, enemyId="11xxx", ring, sector, count, patternId, wanderPatternId, level, hpMul, atkMul, defMul }`). Pattern lấy từ `EnemyMovePatterns.csv`.

### 6 — Test
`unity_play_mode play` → enemy spawn ở round cấu hình. Kiểm tra di chuyển đúng `attackType`/`attackRange` + không lỗi console.
