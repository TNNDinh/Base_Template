using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

public class RatingService
{
    private static PlayerRating _playerRating => PlayerDataManager.PlayerRating;

    public static bool CanRating()
    {
        // removed: PlayerDataManager.PlayerBuildUpGoalDataManager.dataBase.themeCurrentId >= 1 (gameplay removed)
        return !PlayerDataManager.Settings.dataBase.IsRating &&
               _playerRating.dataBase.timeNextRating < TimeManager.GetOnlineNow();
    }

    public static async UniTask ShowRating()
    {
        await UIManager.Instance.Show(GameEnums.Features.Rating);
        _playerRating.dataBase.timeNextRating =
            TimeManager.GetOnlineNow() + DataManager.RatingConfig.dataGroup.timeNextRating;
        _playerRating.Save();
    }
}