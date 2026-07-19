using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    #region Enums

    /// <summary>Nhóm nhiệm vụ: Star = điều kiện đạt sao; Bonus = thưởng thêm.</summary>
    public enum ObjectiveGroup
    {
        Star,
        Bonus
    }

    /// <summary>Loại điều kiện của 1 nhiệm vụ.</summary>
    public enum ArenaObjectiveType
    {
        HpRemaining,  // máu hero còn ≥ amount (%)
        BeforeRound,  // hoàn thành TRƯỚC round = amount
        KillSet       // giết đủ mảng quái mô tả trong killSpec
    }

    /// <summary>Nguồn phát thưởng của 1 dòng reward.</summary>
    public enum RewardSource
    {
        Clear,  // thưởng khi clear stage
        Bonus   // thưởng khi đạt nhiệm vụ bonus
    }

    /// <summary>Kiểu tấn công của enemy (cách ra đòn khi tới lượt Attacking).</summary>
    public enum EnemyAttackType
    {
        Melee,  // gần: lao tới ô đích rồi lùi về (đánh occupant ô 1 vòng phía tâm)
        Ranged, // xa: đứng yên tại chỗ bắn thẳng vào hero
        Charge, // lao vào: bò húc thẳng về tâm, trúng enemy đầu tiên trên đường thì enemy đó ăn damage rồi lui về
        Blink   // dịch chuyển: teleport tới sát hero đánh rồi teleport về ô cũ
    }

    #endregion

    /// <summary>Config 1 stage arena (CSV <c>ArenaStages.csv</c>).</summary>
    [Serializable]
    public struct ArenaStageModel
    {
        public string id;
        public string name;
        public int maxRounds;   // số round giới hạn
        public string weather;  // id thời tiết (biome) của map — trỏ tới WeatherCollection. Rỗng = không có.
    }

    /// <summary>
    ///     1 nhóm enemy spawn ở 1 round của stage (CSV <c>ArenaSpawns.csv</c>).
    ///     1 stage → nhiều dòng (nhiều round × nhiều nhóm). <c>patternId</c> trỏ tới pattern di chuyển,
    ///     <c>wanderPatternId</c> là pattern đi loanh quanh sau khi tấn công (rỗng = không có).
    /// </summary>
    [Serializable]
    public struct ArenaSpawnModel
    {
        public string stageId;
        public int round;          // spawn ở round nào
        public string enemyId;     // id prefab enemy (vd "11001")
        public int ring;           // vòng spawn (thường vòng ngoài cùng)
        public int sector;         // ô góc spawn; -1 = rải/ random
        public int count;          // số con
        public string patternId;   // pattern di chuyển chính (tiến vào tấn công)
        public string wanderPatternId; // pattern đi loanh quanh sau khi đánh (rỗng = không)
        public int level;

        // Stat enemy nằm ở collection RIÊNG (EnemyStats.csv), tra theo enemyId.
        // mul = tinh chỉnh độ khó theo từng spawn (nhân lên stat base).
        public float hpMul;
        public float atkMul;
        public float defMul;
    }

    /// <summary>
    ///     1 nhiệm vụ (sao hoặc bonus) của stage (CSV <c>ArenaObjectives.csv</c>).
    ///     <c>amount</c> nghĩa tuỳ <c>type</c> (HpRemaining=%, BeforeRound=round). <c>killSpec</c> dùng cho
    ///     KillSet, dạng <c>"enemyId:count|enemyId:count"</c> (vd "11001:2|11002:3").
    /// </summary>
    [Serializable]
    public struct ArenaObjectiveModel
    {
        public string stageId;
        public ObjectiveGroup group;
        public ArenaObjectiveType type;
        public int amount;
        public string killSpec;
    }

    /// <summary>1 dòng phần thưởng resource của stage (CSV <c>ArenaRewards.csv</c>).</summary>
    [Serializable]
    public struct ArenaRewardModel
    {
        public string stageId;
        public RewardSource source;
        public string resourceId;
        public int amount;
    }

    /// <summary>
    ///     1 pattern di chuyển của enemy (CSV <c>EnemyMovePatterns.csv</c>).
    ///     <c>steps</c> mã hoá TRỰC TIẾP chuỗi bước theo (x, y):
    ///     <list type="bullet">
    ///         <item>1 bước = "x y" (vd "1 0", "0 -1"). x = ngang (+phải/CW, −trái/CCW); y = dọc (+ra ngoài, −vào tâm).</item>
    ///         <item>Các bước trong 1 nhánh ngăn bằng ';' — vd "1 0;1 0;0 -1" = đi phải 2 ô rồi vào trong 1 ô.</item>
    ///         <item>Nhiều NHÁNH (mảng-của-mảng) ngăn bằng '|' — vd "1 0;1 0;0 -1|1 0;0 -1". Runtime thử
    ///               nhánh đầu; nếu ô đích bị chặn/ngoài vùng thì đệ quy sang nhánh sau tìm ô trống, hết thì đứng yên.</item>
    ///     </list>
    ///     Tương ứng ký hiệu bạn dùng: <c>[1 0, 1 0, 0 -1],[1 0, 0 -1]</c>.
    /// </summary>
    [Serializable]
    public struct EnemyMovePatternModel
    {
        public string id;
        public string name;
        public bool loop;   // tới cuối chuỗi thì lặp lại
        public string steps; // xem mô tả trên
    }

    /// <summary>Parse chuỗi <see cref="EnemyMovePatternModel.steps" /> → danh sách NHÁNH, mỗi nhánh là chuỗi bước (x,y).</summary>
    public static class MovePattern
    {
        public static List<List<Vector2Int>> Parse(string raw)
        {
            var branches = new List<List<Vector2Int>>();
            if (string.IsNullOrWhiteSpace(raw)) return branches;

            var branchStrs = raw.Split('|');
            for (int b = 0; b < branchStrs.Length; b++)
            {
                var steps = new List<Vector2Int>();
                var stepStrs = branchStrs[b].Split(';');
                for (int s = 0; s < stepStrs.Length; s++)
                {
                    var pair = stepStrs[s].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (pair.Length != 2) continue;
                    if (!int.TryParse(pair[0], out var x) || !int.TryParse(pair[1], out var y)) continue;
                    // Enemy đi TỪNG Ô KỀ: tách bước đa ô/chéo thành các bước đơn vị 1 ô (dọc trước, ngang sau).
                    AddUnitSteps(steps, x, y);
                }

                if (steps.Count > 0) branches.Add(steps);
            }

            return branches;
        }

        /// <summary>Tách 1 vector (x,y) thành các bước đơn vị 1 ô: đi dọc (ring) trước, rồi ngang (sector).</summary>
        private static void AddUnitSteps(List<Vector2Int> outSteps, int x, int y)
        {
            int sy = Math.Sign(y);
            for (int i = 0; i < Math.Abs(y); i++) outSteps.Add(new Vector2Int(0, sy));
            int sx = Math.Sign(x);
            for (int i = 0; i < Math.Abs(x); i++) outSteps.Add(new Vector2Int(sx, 0));
        }
    }

    /// <summary>
    ///     Stat gốc + growth theo level của 1 UNIT (hero hoặc enemy). <c>id</c> = id prefab
    ///     (vd "44000", "11001"). Dùng chung cho HeroStats.csv &amp; EnemyStats.csv (2 collection riêng,
    ///     link vào stage/spawn bằng id). Stat tại level N = base + perLevel * (N - 1) — xem <see cref="UnitStats.Of" />.
    /// </summary>
    [Serializable]
    public struct UnitStatModel
    {
        public string id;
        public string name;
        public float hp;
        public float atk;
        public float def;
        public float spd;          // tốc độ (số ô/round hoặc thứ tự hành động)
        public float critRate;     // 0..1
        public float critDmg;      // hệ số sát thương chí mạng (vd 1.5)
        public float attackRange;  // tầm đánh (số ô). Enemy vào tầm (ring < attackRange) thì chuẩn bị đánh.
        public int attackType;     // kiểu đánh (EnemyAttackType): 0=Melee,1=Ranged,2=Charge,3=Blink. Hero để 0.
        public string skills;      // DANH SÁCH skill id (';'-sep). Skill TỰ ĐỊNH NGHĨA trigger+effect (SkillCollection).
        public float hpPerLevel;
        public float atkPerLevel;
        public float defPerLevel;
    }

    /// <summary>Stat đã tính ở 1 level cụ thể (runtime, không lưu).</summary>
    public struct UnitStats
    {
        public float hp;
        public float atk;
        public float def;
        public float spd;
        public float critRate;
        public float critDmg;
        public float attackRange;
        public EnemyAttackType attackType;
        public string skills;

        /// <summary>Tính stat của unit tại <paramref name="level" /> (level 1 = base).</summary>
        public static UnitStats Of(UnitStatModel m, int level)
        {
            int steps = Mathf.Max(0, level - 1);
            return new UnitStats
            {
                hp = m.hp + m.hpPerLevel * steps,
                atk = m.atk + m.atkPerLevel * steps,
                def = m.def + m.defPerLevel * steps,
                spd = m.spd,
                critRate = m.critRate,
                critDmg = m.critDmg,
                attackRange = m.attackRange,
                attackType = (EnemyAttackType)m.attackType,
                skills = m.skills
            };
        }
    }

    /// <summary>Cặp (enemyId, count) parse từ <see cref="ArenaObjectiveModel.killSpec" />.</summary>
    public struct KillRequirement
    {
        public string enemyId;
        public int count;
    }

    /// <summary>Helper parse killSpec "id:count|id:count" → danh sách <see cref="KillRequirement" />.</summary>
    public static class KillSpec
    {
        public static List<KillRequirement> Parse(string spec)
        {
            var list = new List<KillRequirement>();
            if (string.IsNullOrWhiteSpace(spec)) return list;

            var groups = spec.Split('|');
            for (int i = 0; i < groups.Length; i++)
            {
                var g = groups[i].Trim();
                if (g.Length == 0) continue;

                var kv = g.Split(':');
                if (kv.Length != 2) continue;
                if (!int.TryParse(kv[1].Trim(), out var count)) continue;
                list.Add(new KillRequirement { enemyId = kv[0].Trim(), count = count });
            }

            return list;
        }
    }
}
