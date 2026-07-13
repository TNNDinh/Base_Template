using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Runner kiểm tra hệ thống bằng data mẫu (Ignis). Gắn vào 1 GameObject rồi chuột phải
    ///     component → "Run Sample Battle" (hoặc bật <see cref="_runOnStart" />). Xem kết quả ở Console.
    /// </summary>
    public class BattleSampleTest : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private int _heroLevel = 60;
        [SerializeField] private int _heroStar = 3;

        private void Start()
        {
            if (_runOnStart) RunSampleBattle();
        }

        [ContextMenu("Print Stat Check")]
        public void PrintStatCheck()
        {
            BattleDatabase.LoadSample();
            var hero = BattleDatabase.GetHero("hero_1001");

            Debug.Log($"=== Ignis @ {_heroStar}★ lv{_heroLevel} ===");
            Debug.Log($"HP  = {HeroStatService.ComputeStat(hero, _heroLevel, _heroStar, StatType.Hp):0}");
            Debug.Log($"ATK = {HeroStatService.ComputeStat(hero, _heroLevel, _heroStar, StatType.Atk):0}");
            Debug.Log($"DEF = {HeroStatService.ComputeStat(hero, _heroLevel, _heroStar, StatType.Def):0}");
            Debug.Log($"SPD = {HeroStatService.ComputeStat(hero, _heroLevel, _heroStar, StatType.Spd):0} (không nhân star)");
        }

        [ContextMenu("Run Sample Battle")]
        public void RunSampleBattle()
        {
            BattleDatabase.LoadSample();
            PrintStatCheck();

            // Đội người chơi: 1 Ignis (mở rộng lên 6 slot tuỳ ý)
            var playerData = new PlayerHeroData();
            playerData.AddHero("hero_1001", _heroStar, _heroLevel);

            var playerTeam = new List<Unit>();
            for (int i = 0; i < playerData.heroes.Count; i++)
                playerTeam.Add(BattleService.BuildUnit(playerData.heroes[i], BattleTeam.Player, i));

            // Đội địch: 1 dummy hệ Wind (test Fire > Wind)
            var enemyTeam = new List<Unit>
            {
                BattleService.BuildEnemy("enemy_9001", 60, 3, 0)
            };

            var ctx = BattleService.CreateBattle(playerTeam, enemyTeam);
            BattleService.RunAuto(ctx);
        }

        [ContextMenu("Print Evolution Paths")]
        public void PrintEvolutionPaths()
        {
            BattleDatabase.LoadSample();

            var owned = new OwnedHero { heroId = "hero_1001", star = _heroStar, level = _heroLevel };
            var paths = HeroEvolutionService.GetAvailablePaths(owned);
            Debug.Log($"=== Ignis có {paths.Count} nhánh tiến hóa ===");
            for (int i = 0; i < paths.Count; i++)
            {
                var p = paths[i];
                var can = HeroEvolutionService.CanEvolve(owned, p.path, out _, out var reason);
                Debug.Log($"- {p.path} → {p.targetHeroId} (yêu cầu {p.reqStar}★ lv{p.reqLevel}) | {(can ? "ĐỦ ĐIỀU KIỆN" : reason)}");
            }
        }

        [ContextMenu("Evolve Demo (Assassin)")]
        public void EvolveDemoAssassin() => EvolveAndCompare(EvolutionPath.Assassin);

        [ContextMenu("Evolve Demo (Tank)")]
        public void EvolveDemoTank() => EvolveAndCompare(EvolutionPath.Tank);

        [ContextMenu("Run Stage Demo (Chapter 1)")]
        public void RunStageDemo()
        {
            BattleDatabase.LoadSample();
            var progress = new PlayerStageData();

            // Đội mẫu: Ignis gốc + dạng Fighter + dạng Tank
            var team = new List<OwnedHero>
            {
                new OwnedHero { heroId = "hero_1001", star = _heroStar, level = _heroLevel },
                new OwnedHero { heroId = "hero_1001_fg", star = _heroStar, level = _heroLevel },
                new OwnedHero { heroId = "hero_1001_tk", star = _heroStar, level = _heroLevel }
            };

            foreach (var stageId in new[] { "stage_1_1", "stage_1_2", "stage_1_3" })
            {
                if (!StageService.CanEnter(stageId, progress))
                {
                    Debug.Log($"[Stage {stageId}] BỊ KHÓA (chưa clear ải trước)");
                    continue;
                }

                // build đội mới mỗi ải (full HP đầu ải)
                var units = new List<OwnedHero>(team);
                var res = StageService.RunStageForTeam(stageId, units);
                StageService.ApplyResult(progress, stageId, res);
                var s = BattleDatabase.GetStage(stageId);
                Debug.Log($"[{s.name}] {(res.Win ? "WIN" : "LOSE")} | wave {res.WavesCleared}/{res.TotalWaves} | sống {res.Survivors}/{res.TeamSize} | {res.Stars}★");
            }
        }

        private void EvolveAndCompare(EvolutionPath path)
        {
            BattleDatabase.LoadSample();

            var owned = new OwnedHero { heroId = "hero_1001", star = _heroStar, level = _heroLevel };
            var before = BattleDatabase.GetHero(owned.heroId);
            Debug.Log($"[Trước] {before.name} | ATK {HeroStatService.ComputeStat(before, owned.level, owned.star, StatType.Atk):0} | ult {before.ultimateId}");

            if (!HeroEvolutionService.Evolve(owned, path)) return;

            var after = BattleDatabase.GetHero(owned.heroId);
            Debug.Log($"[Sau]   {after.name} | ATK {HeroStatService.ComputeStat(after, owned.level, owned.star, StatType.Atk):0} | ult {after.ultimateId} | gốc = {owned.BaseHeroId}");
        }
    }
}
