using BlackFace.Libraries.Modules.UIModule;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.RequireInternet
{
    internal class RequireInternetController : FeatureBaseController
    {
        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Feature type")]
        private Button _acceptButton;

        protected override void Start()
        {
            base.Start();
            //_acceptButton.onClick.AddListener(OnConfirm);
        }

        private void OnConfirm()
        {
            if (!GameSystems.IsInternetConnection())
            {
                GameSystems.ShowSimpleMessage("internet_please");
                return;
            }

            CloseMe(() =>
            {
                if (SceneManager.GetActiveScene().name.StartsWith("Splash"))
                {
#if UNITY_EDITOR || UNITY_ANDROID || PLATFORM_ANDROID
                    GameSystems.RestartApplication();
#else
                    GameSystems.ChangeScene(GameEnums.Scenes.SplashScene);
#endif
                }
            });
        }
    }
}