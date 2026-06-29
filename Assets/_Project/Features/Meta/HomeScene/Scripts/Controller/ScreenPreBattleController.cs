using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Package.Audio;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.Meta.HomeScene
{
    public class ScreenPreBattleController : FeatureBaseController
    {
        public Button buttonPlay;
        public Button buttonInfo;
        public Text textLevel;
        public GameObject vfxHeartRemove;

        protected override void Start()
        {
            base.Start();
            buttonPlay.onClick.AddListener(OnClickPlay);
            buttonInfo.onClick.AddListener(ShowInfo);
            textLevel.text = "";
        }

        private void ShowInfo()
        {
            UIManager.Instance.Show(GameEnums.Features.StreakInfo).Forget();
        }

        private void OnClickPlay()
        {
            if (PlayerResource.IsEnough(EnumBase.MoneyTypes.Energy, 1))
            {
                vfxHeartRemove.SetActive(true);
                this.DelayMethod(1f, () =>
                {
                    // PlayerResource.RemoveCurrency(EnumBase.MoneyTypes.Energy,
                    //     // PlayerResource.IsInfinityEnergy() ? 0 : DataManager.GeneralConfig.GetData().energyPerStage,
                    //     0,
                    //     () =>
                    //     {
                    //         //BattleManager.SetMode(EnumBase.BattleModes.Campaign);
                    //         //PlayerDataManager.BattleData.ResetDataCached();
                    //         AudioService.Default.StopMusic();
                    //         GameSystems.ChangeScene(GameEnums.Scenes.BattleScene);
                    //         PlayerDataManager.PlayerResource.Save();
                    //     }, unSuccess: () =>
                    //     {
                    //         UIManager.Instance.Show(GameEnums.Features.HeartRefill).Forget();
                    //     });
                    AudioService.Default.StopMusic();
                    GameSystems.ChangeScene(GameEnums.Scenes.BattleScene);
                    PlayerDataManager.PlayerResource.Save();
                });
            }
            else
            {
                UIManager.Instance.Show(GameEnums.Features.HeartRefill).Forget();
            }
        }
    }
}