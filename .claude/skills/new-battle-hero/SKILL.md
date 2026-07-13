---
name: new-battle-hero
description: Tạo nhanh 1 hero/bot cho module Battle bằng cách append đúng các dòng vào CSV (Resources/BattleCsv). Dùng khi user nói "tạo hero mới", "làm hero nhanh", "new battle hero", "thêm bot". KHÔNG sửa code C# — chỉ ghi CSV rồi refresh + verify.
---

# /new-battle-hero — tạo hero/bot nhanh qua CSV

Data battle nằm hoàn toàn trong `Assets/_Project/Features/Gameplay/Battle/Resources/BattleCsv/`.
Tạo hero = **append dòng vào CSV**, không đụng C#. `BattleCsvLoader` tự nạp lúc chạy.

## INPUT
User mô tả ngắn 1 hero, ví dụ:
> "Xạ thủ hệ Wind, ATK cao máu giấy, skill bắn 1 mục tiêu, passive né đòn theo sao"

Nếu thiếu thông tin cốt lõi (hệ, role, skill làm gì, có passive không) → hỏi gọn 1 lần rồi làm.

## QUY ƯỚC ID
- Hero người chơi: `hero_<4 số>` (vd `hero_1005`). Bot/enemy: `mob_<tên>` hoặc `boss_<tên>`.
- Skill: `skill_<tên>` hoặc `skill_<số>`. Effect/buff: `<tên>_<hậu tố>` (vd `bleed_dot`).
- Passive config: `<loại>_t<tier>` (vd `evasion_t1`). Kiểm tra id chưa trùng trong CSV trước khi ghi.

## CÁC BƯỚC
1. **Đọc** các CSV liên quan để biết id đã dùng + đúng thứ tự cột (header dòng đầu).
2. **Append** dòng mới (đúng số cột, để trống bằng dấu phẩy liền nhau, KHÔNG dùng dấu phẩy trong giá trị — thay bằng `_`):
   - `Heroes.csv` — 1 dòng hero. Cột: `id,name,element,heroClass,rarity,baseHp,baseAtk,baseDef,baseSpd,critRate,critDmg,damageReduction,growthHp,growthAtk,growthDef,basicSkillId,skill2Id,ultimateId,modelKey,baseHeroId,evolvePath`
   - `Skills.csv` — mỗi skill 1 dòng: `id,name,desc,type,targetType,energyCost,cooldown,hitCount,animKey`. type=`Basic/Active/Ultimate/Passive`.
   - `SkillEffects.csv` — mỗi effect 1 dòng (skill nhiều effect = nhiều dòng, tăng `order`): `skillId,order,effectType,target,scaleStat,scaleValue,flat,chance,buffId,duration`. effectType=`Damage/Heal/Shield/ApplyBuff/ApplyDebuff/Cleanse/EnergyGain`. target=`Inherit` để theo skill cha.
   - `Effects.csv` — nếu skill gây buff/debuff mới: `id,category,stat,modType,value,tickScaleStat,tickValue,maxStack,dispellable,icon`.
3. **Passive (nếu có)** — role thụ động theo sao:
   - `Passives.csv`: `id,type,value,value2,flag,refId` (type hiện có: `Block`. Loại mới cần thêm class trong `Passive/Passives.cs` + case trong `PassiveFactory` — báo user nếu cần).
   - `HeroStarPassive.csv`: `heroId,star,passiveId` (mỗi sao 1 dòng — "chia đều theo sao").
4. **Skill đổi theo sao (nếu có)** — `HeroStarLoadout.csv`: `heroId,star,activeSkillId` (mỗi sao 1 dòng, trỏ skill tier tương ứng).
5. **Tiến hóa (nếu có)** — thêm hero dạng tiến hóa vào `Heroes.csv` (set `baseHeroId` + `evolvePath`) và dòng vào `HeroEvolution.csv`.
6. **Refresh + verify**: `unity_execute_menu_item("Assets/Refresh")` → đợi compile → `unity_execute_code` gọi `BattleDatabase.Reload()` rồi in stat hero mới ở 1-2 bậc sao để xác nhận nạp đúng.

## GỢI Ý CÂN BẰNG (tham chiếu roster hiện có, lv60/3★)
- Carry: ATK ~800-1000, HP ~4500-5500. Bruiser: ATK ~700, HP ~7000. Tank: ATK ~470, HP ~9000, DEF cao.
- Skill đơn mục tiêu mạnh ~2.5-3.0×ATK; AoE ~1.4-1.6×ATK; ultimate cần `energyCost=100`.
- Đỡ đòn ở STAT = `damageReduction` (0..0.9, giảm sát thương liên tục). Đỡ đòn ở SKILL = passive `Block` (proc: đòn thường→đỡ hết, skill→hạ thành đòn thường).

## OUTPUT
Chỉ liệt kê CSV đã sửa + hero id mới + kết quả verify (stat nạp đúng chưa). Không giải thích dài.
