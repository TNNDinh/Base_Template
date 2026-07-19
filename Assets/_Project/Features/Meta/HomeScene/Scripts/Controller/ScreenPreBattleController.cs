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
                // Mở màn CHỌN MÀN (arena stage-select); chọn map (nút CHƠI trong đó) mới vào BattleScene.
                // (Việc dừng nhạc + đổi scene chuyển sang ArenaStageSelectController.EnterStage.)
                UIManager.Instance.Show(GameEnums.Features.ArenaStageSelect).Forget();
            }
            else
            {
                UIManager.Instance.Show(GameEnums.Features.HeartRefill).Forget();
            }
        }
    }
}