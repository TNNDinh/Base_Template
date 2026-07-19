---
name: create-arena-skill
description: Tạo 1 skill arena (trigger-based, tự định nghĩa). Thêm dòng vào Skills.csv rồi regenerate SkillCollection.asset qua Unity MCP. Skill tự khai báo trigger (Active/Passive/OnDeath/OnMove/OnDamaged/OnLowHp/EveryNRounds) + effect (Nuke/Shockwave/Heal/Spawn/StatBuff). Dùng khi user nói "tạo skill", "thêm skill", "create arena skill".
---

# Create Arena Skill — skill TỰ ĐỊNH NGHĨA (trigger + effect)

Unit (hero/enemy) chỉ giữ **danh sách skill id** (`skills` trong HeroStats/EnemyStats). Mọi năng lực nằm trong SkillCollection: skill tự khai báo **trigger** (khi nào kích hoạt) + **effect** (làm gì).

## Data model — `ArenaSkillModel`
File: `Assets/_Project/Features/Gameplay/Battle/Arena/Data/SkillModels.cs`

| Field | Ý nghĩa |
|-------|---------|
| `id`, `name` | khoá + tên |
| `trigger` | `0`=Active(ult tay), `1`=Passive, `2`=OnDeath, `3`=OnMove, `4`=OnDamaged, `5`=OnLowHp, `6`=EveryNRounds |
| `effect` | `0`=Nuke, `1`=Shockwave, `2`=Heal, `3`=Spawn, `4`=StatBuff(passive) |
| `statType` | StatBuff: `0`=AtkPct, `1`=RegenPct, `2`=DmgReducePct, `3`=MaxHpPct |
| `power` | Nuke/Shockwave = hệ số atk; Heal = % máu; StatBuff = độ mạnh (0.2=20%) |
| `cooldown` | số round CD (Active + trigger lặp) |
| `param` | OnLowHp = ngưỡng % máu; EveryNRounds = N |
| `spawnId`, `spawnCount` | effect Spawn (vd slime tách: spawnId="11003", spawnCount=3) |
| `nextSkillId`, `upgradeCost` | nâng cấp = đổi sang id kế (rỗng = max) |

- CSV: `.../ArenaCsv/Skills.csv` · SO: `.../ArenaCsv/SkillCollection.asset` (`SkillCollection` file riêng → bind ổn định)
- Runtime: `ArenaSkillRunner` (mỗi unit) gom Passive (StatBuff) + bắn skill theo trigger (có cooldown). Hero active = skill trigger=Active đầu tiên trong list; enemy Spawn xử lý ở `ArenaCombat`, Heal/Nuke ở `ArenaSceneController`.

## Quy trình
1. **Hỏi**: trigger, effect, power, cooldown, (Spawn → spawnId/count; OnLowHp/EveryNRounds → param), có chuỗi nâng cấp không.
2. **Thêm dòng** `Skills.csv`. Header: `id,name,trigger,effect,statType,power,cooldown,param,spawnId,spawnCount,nextSkillId,upgradeCost`.
3. **Regenerate** `SkillCollection.asset` (Unity MCP, fully-qualified, no `using`): tạo lại sạch bằng `CreateInstance<SkillCollection>()` + set full `dataGroup` (liệt kê LẠI mọi skill vì `CreateAsset` ghi đè) + `CreateAsset` + `Refresh`.
   ```csharp
   S(id,name,trigger,effect,statType,power,cooldown,param,spawnId,spawnCount,next,cost)
   ```
4. **Gán skill vào unit**: thêm id vào `skills` (';'-sep) của hero/enemy trong HeroStats/EnemyStats (xem create-arena-hero / create-arena-enemy). VD slime tách: enemy `skills="sk_split_slime"`.

> Chú ý slime tách: skill Spawn spawnId nên trỏ tới enemy KHÁC (không có skill Spawn) để tránh tách vô hạn.
