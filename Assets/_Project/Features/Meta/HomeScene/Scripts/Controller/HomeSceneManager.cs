using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Feature.Social.Account;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.UI;

namespace Ezg.Feature.Meta.HomeScene
{
    public static class HomeSceneManager
    {
        public static HomeScreenController Controller;

        public static ShopController ShopHomeController;

        public static void OpenShopAndSnapTo(int index,
            UIManager.UIGroupName groupName = UIManager.UIGroupName.Main_Container)
        {
            // if (SceneManager.GetActiveScene().name.StartsWith("Home"))
            // {
            //     Controller.MainTab.SetTabIndex(0);
            //     Controller.DelayMethod(.1f, () => ShopHomeController.SnapTo(1));
            // }
            // else
            // {
            //     UIManager.Instance.Show(GameEnums.Features.Shop, data: index).Forget();
            // }
            var properties = new ShopProperties
            {
                indexScroll = index,
                groupName = groupName
            };
            UIManager.Instance.Show(GameEnums.Features.Shop, UIManager.UIGroupName.Modal_Container, data: properties)
                .Forget();
        }

        public static async void InitScene()
        {
            UIChangeContainer.Init();
            PackDurationService.Init();
            // await UIManager.Instance.InitScreenDontDestroy();

            // removed: BuildUpGoalManager / PlayerBuildUpGoalDataManager (gameplay removed)
            // removed: DailyRewardService / EventTimeService / StarChestManager (gameplay removed)
            GameCheatManager.Init();
            ShopService.ValidNextDay();
            WeeklyPassService.CheckAllWeeklyPassReset();

            ProfileManager.EnsureHomeAuthentication(CheckTut);
            // removed: DiscountGemRawService / SpeedPackageService / HappinessExpressService (gameplay removed)

            // removed: SupabaseHandleEvent.FetchAllEventsData / EventMergeService.CheckAllEvent (gameplay removed)
            UIManager.Instance.TryStartSequence();
        }

        public static void CheckTut()
        {
            // removed: TutorialContainer / Location (gameplay removed)
        }

        public static void ReCheckFeature()
        {
            // removed: PiggyBankManager.ReCheckPiggyBank (gameplay removed)
        }
    }
}