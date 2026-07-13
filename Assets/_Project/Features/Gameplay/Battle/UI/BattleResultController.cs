using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Màn kết thúc trận: thắng/thua + số sao + chi tiết + nút về Home.
    ///     Mở bằng <c>UIManager.Instance.Show(GameEnums.Features.BattleResult, Modal_Container, data: StageRunResult)</c>.
    /// </summary>
    public class BattleResultController : FeatureBaseController
    {
        #region Fields

        [SerializeField] private Text _titleText;   // CHIẾN THẮNG / THẤT BẠI
        [SerializeField] private Text _detailText;  // sống x/y · wave a/b
        [SerializeField] private Image[] _starIcons; // 3 sao
        [SerializeField] private Button _homeButton; // về Home

        #endregion

        #region Public

        /// <summary>Mở màn kết quả với dữ liệu 1 trận.</summary>
        public static void Open(StageRunResult result)
        {
            UIManager.Instance.Show(GameEnums.Features.BattleResult, UIManager.UIGroupName.Modal_Container,
                data: result).Forget();
        }

        public override void LoadData(object data)
        {
            base.LoadData(data);
            if (data is StageRunResult r) Bind(r);
        }

        #endregion

        #region Private

        private void Bind(StageRunResult r)
        {
            if (_titleText != null) _titleText.text = r.Win ? "CHIẾN THẮNG" : "THẤT BẠI";

            if (_detailText != null)
                _detailText.text = r.Win
                    ? $"Sống {r.Survivors}/{r.TeamSize}   •   Wave {r.WavesCleared}/{r.TotalWaves}"
                    : $"Dừng ở wave {r.WavesCleared + 1}/{r.TotalWaves}";

            if (_starIcons != null)
                for (int i = 0; i < _starIcons.Length; i++)
                    if (_starIcons[i] != null)
                        _starIcons[i].enabled = i < r.Stars;

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveAllListeners();
                _homeButton.onClick.AddListener(GoHome);
            }
        }

        private void GoHome()
        {
            CloseMe();
            GameSystems.ChangeScene(GameEnums.Scenes.HomeScene);
        }

        #endregion
    }
}
