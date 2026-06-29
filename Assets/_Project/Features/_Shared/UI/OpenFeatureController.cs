using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.Shared.UI
{
    internal class OpenFeatureController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Cấu hình")] [Title("Tính năng cần mở")] [Required]
        private GameEnums.Features _featuer;

        [SerializeField] [TabGroup("Cấu hình")] [Title("Group")] [Required]
        private UIManager.UIGroupName _groupName = UIManager.UIGroupName.Overlay_Container;

        //[SerializeField]
        //[TabGroup("Cấu hình")]
        //[Required]
        //private bool _requiredInternet;

        //[SerializeField]
        //[TabGroup("Cấu hình")]
        //[Required]
        //private bool _requiredLogin;

        //[SerializeField]
        //[TabGroup("Cấu hình")]
        //[Required]
        //private bool _checkAccountBlock;

        //[SerializeField]
        //[TabGroup("Cấu hình")]
        //[Required]
        //private bool _checkUnlockFeature;

        [SerializeField] [TabGroup("Cấu hình")] [Title("Là toggle")] [Required]
        private bool _isToggle;

        [SerializeField] [TabGroup("Cấu hình")] [Title("Set normal scale time")] [Required]
        private bool _setNormalScaleTime;

        private void Start()
        {
            if (_isToggle)
                GetComponent<Toggle>().onValueChanged.AddListener(OnClick);
            else
                GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
//            if (_checkUnlockFeature)
//            {
//                if (!UnlockFeatureService.IsUnlockFeature(_featuer))
//                {
//                    _2.BUS.Systems.GameSystems.ShowSimpleMessage(string.Format(_2.BUS.Systems.GameSystems.Localize("require_reach_stage"),
//                        UnlockFeatureService.GetUnlockValue(_featuer)));
//                    return;
//                }
//            }

//#if UNITY_EDITOR
//            if (_requiredLogin && string.IsNullOrEmpty(PlayerDataManager.Account.AccountId))
//            {
//                _2.BUS.Systems.GameSystems.ShowMessage("login_required", acceptAction: () => UIManager.Instance.Show(GameEnums.Features.AdminGetData).Forget());
//                return;
//            }
//#endif

//#if !UNITY_EDITOR
//            if (_requiredInternet && Application.internetReachability == NetworkReachability.NotReachable)
//            {
//                _2.BUS.Systems.GameSystems.ShowSimpleMessage("internet_required");
//                return;
//            }

//            if (_checkAccountBlock && (PlayerDataManager.Account.dataBase.IsLocked ||
//                                       (ProfileManager.ProfileData != null && ProfileManager.ProfileData.IsBlock)))
//            {
//                _2.BUS.Systems.GameSystems.ShowMessage("account_blocked_feature", acceptAction: SettingManager.OpenDiscord);
//                return;
//            }

//            if (_requiredLogin && !ProfileManager.IsLogon())
//            {
//                _2.BUS.Systems.GameSystems.ShowMessage("login_required", acceptAction: () => ProfileManager.Login(null, false).Forget());
//                return;
//            }
//#endif

            if (_setNormalScaleTime) Time.timeScale = 1;
            EventManager.EmitEvent("Open" + _featuer);
            UIManager.Instance.Show(_featuer, _groupName, true).Forget();
        }

        private void OnClick(bool isOn)
        {
//#if !UNITY_EDITOR
//            if (_requiredInternet && Application.internetReachability == NetworkReachability.NotReachable)
//            {
//                _2.BUS.Systems.GameSystems.ShowSimpleMessage("internet_required");
//                return;
//            }

//            if (_requiredLogin && !ProfileManager.IsLogon())
//            {
//                _2.BUS.Systems.GameSystems.ShowMessage("login_required", acceptAction: () => ProfileManager.Login(null, false).Forget());
//                return;
//            }

//            if (_checkAccountBlock && (PlayerDataManager.Account.dataBase.IsLocked ||
//                                       (ProfileManager.ProfileData != null && ProfileManager.ProfileData.IsBlock)))
//            {
//                _2.BUS.Systems.GameSystems.ShowMessage("account_blocked_feature", acceptAction: SettingManager.OpenDiscord);
//                return;
//            }
//#endif

            if (_setNormalScaleTime) Time.timeScale = 1;

            EventManager.EmitEvent("Open" + _featuer);
            UIManager.Instance.Show(_featuer, _groupName, true).Forget();
        }
    }
}