using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Kết quả 1 lần đánh ải.</summary>
    public struct StageRunResult
    {
        public bool Win;
        public int WavesCleared;
        public int TotalWaves;
        public int TeamSize;
        public int Survivors;
        public int Stars; // 0 = thua
    }

    /// <summary>
    ///     Logic đi ải: chạy các wave tuần tự bằng <see cref="BattleService" />, HP/energy hero carry
    ///     qua các wave (không hồi đầy giữa wave), buff bị xoá giữa wave. Thua nếu cả đội gục ở 1 wave.
    /// </summary>
    public static class StageService
    {
        #region Gate

        /// <summary>Có được vào ải không (đã clear ải mở khoá trước đó).</summary>
        public static bool CanEnter(string stageId, PlayerStageData progress)
        {
            var stage = BattleDatabase.GetStage(stageId);
            if (string.IsNullOrEmpty(stage.id)) return false;
            if (string.IsNullOrEmpty(stage.unlockStageId)) return true;
            return progress != null && progress.IsCleared(stage.unlockStageId);
        }

        #endregion

        #region Run

        /// <summary>Đánh ải với đội unit đã dựng sẵn. Team bị mutate (HP carry qua wave).</summary>
        public static StageRunResult RunStage(string stageId, List<Unit> playerTeam, int seed = 12345)
        {
            var result = new StageRunResult { TeamSize = playerTeam.Count };

            var waves = GroupWaves(BattleDatabase.GetStageBots(stageId));
            result.TotalWaves = waves.Count;

            foreach (var wave in waves)
            {
                var enemies = new List<Unit>(wave.Value.Count);
                for (int i = 0; i < wave.Value.Count; i++)
                    enemies.Add(BattleService.BuildBot(wave.Value[i]));

                var ctx = BattleService.CreateBattle(playerTeam, enemies, seed + wave.Key);
                var winner = BattleService.RunAuto(ctx);

                // Xoá buff còn sót giữa các wave (HP/energy vẫn carry)
                for (int i = 0; i < playerTeam.Count; i++) playerTeam[i].Buffs.Clear();

                if (winner != BattleTeam.Player)
                {
                    result.Win = false;
                    result.Survivors = CountAlive(playerTeam);
                    result.Stars = 0;
                    BattleLog.Info($"[Stage {stageId}] THUA ở wave {wave.Key}");
                    return result;
                }

                result.WavesCleared++;
            }

            result.Win = true;
            result.Survivors = CountAlive(playerTeam);
            result.Stars = RateStars(result.TeamSize, result.Survivors);
            BattleLog.Info($"[Stage {stageId}] THẮNG {result.Stars}★ (sống {result.Survivors}/{result.TeamSize})");
            return result;
        }

        /// <summary>Dựng đội từ hero sở hữu rồi đánh ải (tiện dùng từ UI).</summary>
        public static StageRunResult RunStageForTeam(string stageId, List<OwnedHero> team, int seed = 12345)
        {
            var units = new List<Unit>(team.Count);
            for (int i = 0; i < team.Count; i++)
                units.Add(BattleService.BuildUnit(team[i], BattleTeam.Player, i));
            return RunStage(stageId, units, seed);
        }

        /// <summary>Ghi kết quả vào tiến trình (mở khoá ải sau tự suy ra từ unlockStageId).</summary>
        public static void ApplyResult(PlayerStageData progress, string stageId, StageRunResult result)
        {
            if (progress != null && result.Win) progress.SetResult(stageId, result.Stars);
        }

        #endregion

        #region Helpers

        private static SortedDictionary<int, List<StageBotModel>> GroupWaves(IReadOnlyList<StageBotModel> bots)
        {
            var map = new SortedDictionary<int, List<StageBotModel>>();
            for (int i = 0; i < bots.Count; i++)
            {
                var b = bots[i];
                if (!map.TryGetValue(b.wave, out var list))
                {
                    list = new List<StageBotModel>();
                    map[b.wave] = list;
                }

                list.Add(b);
            }

            return map;
        }

        private static int CountAlive(List<Unit> team)
        {
            int n = 0;
            for (int i = 0; i < team.Count; i++)
                if (team[i].IsAlive)
                    n++;
            return n;
        }

        /// <summary>3★ không mất ai, 2★ mất ≤1, còn thắng là 1★.</summary>
        private static int RateStars(int teamSize, int survivors)
        {
            var deaths = teamSize - survivors;
            if (deaths <= 0) return 3;
            if (deaths <= 1) return 2;
            return 1;
        }

        #endregion
    }
}
