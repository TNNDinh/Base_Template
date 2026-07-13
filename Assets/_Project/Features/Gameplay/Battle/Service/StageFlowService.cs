using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Luồng vào ải từ UI: kiểm tra mở khoá + đội hình, chạy ải, ghi tiến trình.
    ///     <see cref="EnterBattleScene" /> chuyển sang BattleScene đánh có hình;
    ///     <see cref="TryEnter" /> chạy headless (giữ cho test/auto).
    /// </summary>
    public static class StageFlowService
    {
        /// <summary>
        ///     Kiểm tra điều kiện rồi chuyển sang BattleScene để đánh có hình.
        ///     Trả false + hiện thông báo nếu không đủ điều kiện.
        /// </summary>
        public static bool EnterBattleScene(string stageId)
        {
            var stage = BattleDatabase.GetStage(stageId);
            if (string.IsNullOrEmpty(stage.id))
            {
                GameSystems.ShowSimpleMessage("Ải không tồn tại");
                return false;
            }

            if (!StageService.CanEnter(stageId, BattlePlayerData.Stage))
            {
                GameSystems.ShowSimpleMessage("Chưa mở khoá ải này");
                return false;
            }

            if (BattlePlayerData.GetActiveTeam().Count == 0)
            {
                GameSystems.ShowSimpleMessage("Chưa có đội hình");
                return false;
            }

            BattleLaunch.PendingStageId = stageId;
            GameSystems.ChangeScene(GameEnums.Scenes.BattleScene);
            return true;
        }

        /// <summary>Thử vào ải. Trả false + hiện thông báo nếu không đủ điều kiện.</summary>
        public static bool TryEnter(string stageId, out StageRunResult result)
        {
            result = default;

            var stage = BattleDatabase.GetStage(stageId);
            if (string.IsNullOrEmpty(stage.id))
            {
                GameSystems.ShowSimpleMessage("Ải không tồn tại");
                return false;
            }

            if (!StageService.CanEnter(stageId, BattlePlayerData.Stage))
            {
                GameSystems.ShowSimpleMessage("Chưa mở khoá ải này");
                return false;
            }

            var team = BattlePlayerData.GetActiveTeam();
            if (team.Count == 0)
            {
                GameSystems.ShowSimpleMessage("Chưa có đội hình");
                return false;
            }

            // TODO: [Battle] - thay bằng mở scene/screen battle 3D; hiện chạy headless để lấy kết quả.
            result = StageService.RunStageForTeam(stageId, team);
            StageService.ApplyResult(BattlePlayerData.Stage, stageId, result);
            return true;
        }
    }
}
