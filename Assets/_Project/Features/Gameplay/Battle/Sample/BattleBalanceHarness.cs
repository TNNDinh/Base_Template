using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Roster mẫu (hero + enemy) + harness mô phỏng để soi cân bằng.
    ///     Chạy: chuột phải component <see cref="BattleSampleTest" /> hoặc gọi
    ///     <see cref="BuildReport" /> qua Unity execute-code. KHÔNG dùng trong build production.
    /// </summary>
    public static class BattleBalanceHarness
    {
        #region Constants

        private const int HeroLevel = 60;
        private const int HeroStar = 3;
        private const int Seed = 777;

        #endregion

        public static readonly string[] Heroes =
            { "h_ignis", "h_bruno", "h_tempest", "h_umbra", "h_marina", "h_aegis" };

        public static readonly string[] Enemies =
            { "e_slime", "e_goblin", "e_orc", "e_wolf", "e_impmage" };

        private static bool _rosterLoaded;

        #region Load roster

        public static void LoadRoster()
        {
            if (_rosterLoaded) return;
            BattleDatabase.LoadSample(); // lấy sẵn skill 2001-2003 / 3001-3003 + effect + star config

            // --- Skill bổ sung cho support/tank ---
            BattleDatabase.RegisterSkill(new SkillModel { id = "skill_h1", name = "tidal_mend", type = SkillType.Active, targetType = TargetType.AllyLowestHp, energyCost = 0, cooldown = 2, hitCount = 1, animKey = "fx_heal" });
            BattleDatabase.RegisterSkillEffect(new SkillEffectModel { skillId = "skill_h1", order = 0, effectType = EffectType.Heal, target = TargetType.Inherit, scaleStat = StatType.Atk, scaleValue = 2.0f, chance = 1f });

            BattleDatabase.RegisterSkill(new SkillModel { id = "skill_h2", name = "high_tide", type = SkillType.Ultimate, targetType = TargetType.AllyAll, energyCost = 100, cooldown = 0, hitCount = 1, animKey = "fx_hightide" });
            BattleDatabase.RegisterSkillEffect(new SkillEffectModel { skillId = "skill_h2", order = 0, effectType = EffectType.Heal, target = TargetType.AllyAll, scaleStat = StatType.Atk, scaleValue = 1.5f, chance = 1f });
            BattleDatabase.RegisterSkillEffect(new SkillEffectModel { skillId = "skill_h2", order = 1, effectType = EffectType.Cleanse, target = TargetType.AllyAll, chance = 1f });

            BattleDatabase.RegisterSkill(new SkillModel { id = "skill_t1", name = "bulwark", type = SkillType.Active, targetType = TargetType.AllyAll, energyCost = 0, cooldown = 3, hitCount = 1, animKey = "fx_bulwark" });
            BattleDatabase.RegisterSkillEffect(new SkillEffectModel { skillId = "skill_t1", order = 0, effectType = EffectType.Shield, target = TargetType.AllyAll, scaleStat = StatType.Hp, scaleValue = 0.25f, chance = 1f });

            // --- Heroes (lv1 base + growth; sẽ tính tại lv60/3★, ×1.25) ---
            H("h_ignis",   "Ignis",   ElementType.Fire,  HeroClass.Mage,    800, 120, 50, 95, 0.15f, 1.5f, 60, 9, 3, "skill_2001", "skill_2002", "skill_2003");
            H("h_bruno",   "Bruno",   ElementType.Fire,  HeroClass.Warrior, 1000, 110, 70, 90, 0.12f, 1.5f, 75, 8, 4, "skill_2001", "skill_3002", "skill_2003");
            H("h_tempest", "Tempest", ElementType.Wind,  HeroClass.Ranger,  700, 130, 45, 110, 0.25f, 1.6f, 50, 10, 2, "skill_2001", "skill_2002", "skill_3001");
            H("h_umbra",   "Umbra",   ElementType.Dark,  HeroClass.Ranger,  680, 140, 40, 108, 0.30f, 1.8f, 48, 11, 2, "skill_2001", "skill_2002", "skill_3001");
            H("h_marina",  "Marina",  ElementType.Water, HeroClass.Support, 850, 90, 55, 98, 0.10f, 1.5f, 62, 6, 3, "skill_2001", "skill_h1", "skill_h2");
            H("h_aegis",   "Aegis",   ElementType.Light, HeroClass.Tank,    1400, 75, 150, 82, 0.05f, 1.5f, 100, 5, 6, "skill_2001", "skill_t1", "skill_3003");

            // --- Enemies (final stat baked, growth 0, gọi ở star 1 => ×1.0) ---
            E("e_slime",   "Slime",    ElementType.Water, 2000, 150, 60, 70, 0.05f, 1.5f, "skill_2001", "");
            E("e_goblin",  "Goblin",   ElementType.Wind,  3000, 260, 120, 95, 0.10f, 1.5f, "skill_2001", "");
            E("e_orc",     "Orc",      ElementType.Fire,  6000, 300, 250, 75, 0.08f, 1.5f, "skill_2001", "skill_3002");
            E("e_wolf",    "DireWolf", ElementType.Wind,  2500, 380, 90, 120, 0.20f, 1.6f, "skill_2001", "");
            E("e_impmage", "ImpMage",  ElementType.Dark,  1800, 500, 80, 100, 0.15f, 1.7f, "skill_2001", "skill_2003");
            // Boss
            E("e_dragon",  "Dragon",   ElementType.Fire,  25000, 650, 350, 90, 0.15f, 1.8f, "skill_2001", "skill_2003");

            _rosterLoaded = true;
        }

        private static void H(string id, string name, ElementType el, HeroClass cls, int hp, int atk, int def, int spd,
            float cr, float cd, float gHp, float gAtk, float gDef, string basic, string active, string ult)
        {
            BattleDatabase.RegisterHero(new HeroModel
            {
                id = id, name = name, element = el, heroClass = cls, rarity = 3,
                baseHp = hp, baseAtk = atk, baseDef = def, baseSpd = spd, critRate = cr, critDmg = cd,
                growthHp = gHp, growthAtk = gAtk, growthDef = gDef,
                basicSkillId = basic, skill2Id = active, ultimateId = ult
            });
        }

        private static void E(string id, string name, ElementType el, int hp, int atk, int def, int spd,
            float cr, float cd, string basic, string ult)
        {
            BattleDatabase.RegisterHero(new HeroModel
            {
                id = id, name = name, element = el, heroClass = HeroClass.None, rarity = 1,
                baseHp = hp, baseAtk = atk, baseDef = def, baseSpd = spd, critRate = cr, critDmg = cd,
                growthHp = 0, growthAtk = 0, growthDef = 0,
                basicSkillId = basic, skill2Id = "", ultimateId = ult
            });
        }

        #endregion

        #region Report

        /// <summary>Sinh báo cáo cân bằng dạng text (stats + kết quả mô phỏng).</summary>
        public static string BuildReport()
        {
            LoadRoster();
            var prevConsole = BattleLog.EnableConsole;
            BattleLog.EnableConsole = false; // tắt spam khi mô phỏng

            var sb = new StringBuilder();

            sb.AppendLine("### HERO STATS (lv60, 3★) ###");
            sb.AppendLine(StatHeader());
            foreach (var id in Heroes) sb.AppendLine(StatRow(id, HeroLevel, HeroStar));

            sb.AppendLine();
            sb.AppendLine("### ENEMY STATS ###");
            sb.AppendLine(StatHeader());
            foreach (var id in Enemies) sb.AppendLine(StatRow(id, 1, 1));
            sb.AppendLine(StatRow("e_dragon", 1, 1));

            sb.AppendLine();
            sb.AppendLine("### 1v1: HERO vs ENEMY (W=hero thắng, số = rounds) ###");
            sb.Append("hero\\enemy".PadRight(10));
            foreach (var e in Enemies) sb.Append(Short(e).PadLeft(10));
            sb.AppendLine();
            foreach (var h in Heroes)
            {
                sb.Append(Short(h).PadRight(10));
                foreach (var e in Enemies)
                {
                    var r = Duel(h, e);
                    sb.Append($"{(r.heroWin ? "W" : "L")}/{r.rounds}".PadLeft(10));
                }

                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("### HERO vs HERO (winner mỗi cặp — soi hero nào trội) ###");
            var winCount = new Dictionary<string, int>();
            foreach (var h in Heroes) winCount[h] = 0;
            for (int i = 0; i < Heroes.Length; i++)
            for (int j = i + 1; j < Heroes.Length; j++)
            {
                var a = Heroes[i];
                var b = Heroes[j];
                var r = DuelHeroes(a, b);
                winCount[r.winnerId] = winCount[r.winnerId] + 1;
                sb.AppendLine($"{Short(a)} vs {Short(b)} -> {Short(r.winnerId)} ({r.rounds} rounds)");
            }

            sb.AppendLine("-- Win count (round-robin) --");
            foreach (var h in Heroes) sb.AppendLine($"{Short(h)}: {winCount[h]}/{Heroes.Length - 1}");

            sb.AppendLine();
            sb.AppendLine("### TEAM 6 HERO vs BOSS Dragon ###");
            var boss = DuelTeamVsBoss();
            sb.AppendLine($"Kết quả: {(boss.playerWin ? "ĐỘI HERO THẮNG" : "BOSS THẮNG")} sau {boss.rounds} rounds, hero sống sót: {boss.survivors}/6");

            BattleLog.EnableConsole = prevConsole;
            var report = sb.ToString();
            Debug.Log(report);
            return report;
        }

        #endregion

        #region Sim helpers

        private static (bool heroWin, int rounds) Duel(string heroId, string enemyId)
        {
            var p = new List<Unit> { BattleService.BuildUnit(Owned(heroId, HeroLevel, HeroStar), BattleTeam.Player, 0) };
            var e = new List<Unit> { BattleService.BuildEnemy(enemyId, 1, 1, 0) };
            var ctx = BattleService.CreateBattle(p, e, Seed);
            var w = BattleService.RunAuto(ctx);
            return (w == BattleTeam.Player, ctx.TurnCount);
        }

        private static (string winnerId, int rounds) DuelHeroes(string aId, string bId)
        {
            var p = new List<Unit> { BattleService.BuildUnit(Owned(aId, HeroLevel, HeroStar), BattleTeam.Player, 0) };
            var e = new List<Unit> { BattleService.BuildUnit(Owned(bId, HeroLevel, HeroStar), BattleTeam.Enemy, 0) };
            var ctx = BattleService.CreateBattle(p, e, Seed);
            var w = BattleService.RunAuto(ctx);
            return (w == BattleTeam.Player ? aId : bId, ctx.TurnCount);
        }

        private static (bool playerWin, int rounds, int survivors) DuelTeamVsBoss()
        {
            var p = new List<Unit>();
            for (int i = 0; i < Heroes.Length; i++)
                p.Add(BattleService.BuildUnit(Owned(Heroes[i], HeroLevel, HeroStar), BattleTeam.Player, i));
            var e = new List<Unit> { BattleService.BuildEnemy("e_dragon", 1, 1, 0) };
            var ctx = BattleService.CreateBattle(p, e, Seed);
            var w = BattleService.RunAuto(ctx);
            int alive = 0;
            for (int i = 0; i < p.Count; i++)
                if (p[i].IsAlive)
                    alive++;
            return (w == BattleTeam.Player, ctx.TurnCount, alive);
        }

        private static OwnedHero Owned(string id, int lv, int star) => new OwnedHero { heroId = id, level = lv, star = star };

        #endregion

        #region Format

        private static string StatHeader() =>
            "name".PadRight(10) + "HP".PadLeft(8) + "ATK".PadLeft(7) + "DEF".PadLeft(6) +
            "SPD".PadLeft(6) + "EHP".PadLeft(9) + "avgHit".PadLeft(8) + "power".PadLeft(9);

        private static string StatRow(string id, int lv, int star)
        {
            var m = BattleDatabase.GetHero(id);
            var hp = HeroStatService.ComputeStat(m, lv, star, StatType.Hp);
            var atk = HeroStatService.ComputeStat(m, lv, star, StatType.Atk);
            var def = HeroStatService.ComputeStat(m, lv, star, StatType.Def);
            var spd = HeroStatService.ComputeStat(m, lv, star, StatType.Spd);
            var cr = HeroStatService.ComputeStat(m, lv, star, StatType.CritRate);
            var cd = HeroStatService.ComputeStat(m, lv, star, StatType.CritDmg);

            var ehp = hp * (def + DamageFormula.DefConstant) / DamageFormula.DefConstant;
            var avgHit = atk * (1f + cr * (cd - 1f));
            var power = ehp * avgHit / 100000f;

            return m.name.PadRight(10) + hp.ToString("0").PadLeft(8) + atk.ToString("0").PadLeft(7) +
                   def.ToString("0").PadLeft(6) + spd.ToString("0").PadLeft(6) +
                   ehp.ToString("0").PadLeft(9) + avgHit.ToString("0").PadLeft(8) + power.ToString("0.0").PadLeft(9);
        }

        private static string Short(string id)
        {
            var m = BattleDatabase.GetHero(id);
            return string.IsNullOrEmpty(m.name) ? id : m.name;
        }

        #endregion
    }
}
