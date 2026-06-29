using System;
using System.Linq;
using BlackFace.Libraries.Modules.UIModule;
using Ezg.Core.Firebase;
using Ezg.Core.Utils;
using Ezg.Feature.Firebase;
using Ezg.Feature.IAP;
using Ezg.Feature.Shared;
using Ezg.Package.AdsManager;
using Ezg.Package.Factory;
using TigerForge;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.System.GameCheat
{
    internal class GameCheatController : FeatureBaseController
    {
        public InputField CurrentLevel;
        public InputField CurrentLevelType;
        public InputField CurrentResourceAmount;
        public InputField CheatTheme;

        private Resource currenResource;

        protected override void Start()
        {
            base.Start();
            CurrentLevel.text = PlayerDataManager.Campaign.Level.ToString();
            CurrentLevelType.text = GameRemoteConfig.levelType.ToString();
        }

        public void OnClearTut()
        {
            // removed: PlayerTutorial, TutorialContainer (gameplay removed)
            GameSystems.ShowSimpleMessage("Clear Tut");
        }

        public void CheatAllResource()
        {
            //Money
            foreach (var res in PlayerDataManager.PlayerResource.dataBase.Monies.Keys.ToList())
                PlayerDataManager.PlayerResource.dataBase.Monies[res] = int.MaxValue / 5;

            PlayerDataManager.PlayerResource.Save();
            EventManager.EmitEvent(nameof(EventName.UpdateResource));
        }

        public void CheatClearAllResource()
        {
            //Money
            foreach (var res in PlayerDataManager.PlayerResource.dataBase.Monies.Keys.ToList())
                PlayerDataManager.PlayerResource.dataBase.Monies[res] = 0;

            PlayerDataManager.PlayerResource.Save();
            EventManager.EmitEvent(nameof(EventName.UpdateResource));
        }

        public void ClearAllPacks()
        {
            PlayerDataManager.PlayerShop.ClearAllPacks();
            PlayerDataManager.PlayerShop.Save();
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
        }

        public void ClearBooster()
        {
            // PlayerResource.RemoveCurrency(EnumBase.MoneyTypes.BoosterFreeze, PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.BoosterFreeze));
            // PlayerResource.RemoveCurrency(EnumBase.MoneyTypes.BoosterCancelOrder, PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.BoosterCancelOrder));
            // PlayerResource.RemoveCurrency(EnumBase.MoneyTypes.BoosterMagnet, PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.BoosterMagnet));
            // PlayerResource.RemoveCurrency(EnumBase.MoneyTypes.BoosterVipOrder, PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.BoosterVipOrder));
        }

        public void SetLevel()
        {
            PlayerDataManager.Campaign.Level = Convert.ToInt32(CurrentLevel.text);
            //EventManager.EmitEvent(nameof(LevelSelectController));
        }

        public void SetLevelType()
        {
            GameRemoteConfig.levelType = Convert.ToInt32(CurrentLevelType.text);
            EventManager.EmitEvent(nameof(EventName.CheatLevelType));
        }

        public void ShowBanner()
        {
            AdsManager.Instance.LoadBanner();
        }

        public void CheatLevel()
        {
            // removed: OrderManager (gameplay removed)
        }

        public void CheatInfinityGenerator()
        {
            GameCheatManager.isInfinityGenerator = !GameCheatManager.isInfinityGenerator;
        }

        public void ChooseCoin()
        {
            ChooseCoin(new Resource
            {
                resType = EnumBase.ResourceTypes.Money,
                resId = 1
            });
        }

        public void ChooseGem()
        {
            ChooseCoin(new Resource
            {
                resType = EnumBase.ResourceTypes.Money,
                resId = 2
            });
        }

        public void ChooseSkipTime()
        {
            ChooseCoin(new Resource
            {
                resType = EnumBase.ResourceTypes.Money,
                resId = 3
            });
        }

        public void ChooseCoin(Resource resource)
        {
            currenResource = resource;
        }

        public void AddResource()
        {
            var amount = Convert.ToInt32(CurrentResourceAmount.text);
            var moneyType = (EnumBase.MoneyTypes)currenResource.resId;

            if (amount >= 0)
            {
                currenResource.resNumber = amount;
                RewardsService.ReceiveReward(currenResource, isShowPopup: false, source: SourceTracking.Cheat,
                    sourceDetail: SourceTracking.Cheat);
            }
            else
            {
                var current = PlayerDataManager.PlayerResource.dataBase.Monies[moneyType];
                PlayerDataManager.PlayerResource.dataBase.Monies[moneyType] = Math.Max(0, current + amount);
                PlayerDataManager.PlayerResource.Save();
            }

            EventManager.EmitEvent(nameof(EventName.UpdateResource));
        }

        public void CheatNextDay()
        {
            PlayerDataManager.Settings.dataBase.BonusTimeCheat += 86400;
            PlayerDataManager.Settings.Save();
            TimeManager.BonusTimeNow = PlayerDataManager.Settings.dataBase.BonusTimeCheat;
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
            //EventManager.EmitEvent(EventName.NextDayEvent);
        }

        public void CheatCurrentBuild()
        {
            // removed: PlayerBuildUpGoalDataManager (gameplay removed)
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
        }

        public void CheatResetBuildUpGoal()
        {
            // removed: PlayerBuildUpGoalDataManager (gameplay removed)
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
        }

        public void CheatClearBoard()
        {
            // removed: GameplayService (gameplay removed)
        }

        public void CheatTutorial()
        {
            // removed: TutorialContainer (gameplay removed)
        }

        public void GetItemGenerator()
        {
            var screenCheatItems = FindObjectOfType<ScreenCheatItems>();
            if (screenCheatItems != null) screenCheatItems.GetItemGenerator();
        }

        public void AddAllItemInGen()
        {
            var screenCheatItems = FindObjectOfType<ScreenCheatItems>();
            if (screenCheatItems != null) screenCheatItems.AddAllItemInGen();
        }

        public void EmitStarterPack()
        {
            var properties = new PackDurationProperties
            {
                type = PackDurationType.StarterPack,
                isShowPopup = true
            };
            EventManager.EmitEventData(EventName.ShowPackDuration, properties);
        }

        public void EmitOpenningPack()
        {
            var properties = new PackDurationProperties
            {
                type = PackDurationType.OpenningPack,
                isShowPopup = true
            };
            EventManager.EmitEventData(EventName.ShowPackDuration, properties);
        }

        public void CheatScene()
        {
            _ = int.Parse(CheatTheme.text);
            // removed: PlayerBuildUpGoalDataManager (gameplay removed)
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
        }

        public void CheatNoAds()
        {
            AdsManager.Instance.SetTestAds(true);
        }

        public void CheatIAP()
        {
            InAppManager.Instance.SetIsTestIAP(true);
        }

        public void ForeStopEvent()
        {
            // removed: EventMergeService (gameplay removed)
            GameSystems.ChangeScene(GameEnums.Scenes.SplashScene);
        }

        public void LogoutAndClearAllData()
        {
            DataPlayer.ClearAllData();
            GameInitialize.ClearStatic();
            FirebaseLoginManager.OnGoogleLogout(() => { GameSystems.ChangeScene(GameEnums.Scenes.SplashScene); });
        }
    }
}