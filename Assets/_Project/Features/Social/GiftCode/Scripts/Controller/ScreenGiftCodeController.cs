using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Social.GiftCode
{
    public class ScreenGiftCodeController : FeatureBaseController
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private InputField _giftCodeField;

        [SerializeField] [TabGroup("Cấu hình")]
        private Button _giftCodeClaimButton;


        protected override void Start()
        {
            base.Start();
            _giftCodeClaimButton.onClick.AddListener(() => OnClaimGiftCodeAsync().Forget());
        }

        private async UniTask OnClaimGiftCodeAsync()
        {
            if (string.IsNullOrEmpty(_giftCodeField.text))
            {
                GameSystems.ShowSimpleMessage("gift_code_input_please");
                return;
            }

            if (_giftCodeField.text.Length < 6)
            {
                GameSystems.ShowSimpleMessage("gift_code_wrong");
                return;
            }

            GameSystems.ShowWaitingScreen(true);
            await GiftCodeManager.ClaimGiftCode(_giftCodeField.text.ToUpper());
            GameSystems.ShowWaitingScreen(false);
        }
    }
}