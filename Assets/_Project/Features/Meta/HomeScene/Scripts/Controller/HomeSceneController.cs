using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Adapter;
using Ezg.Feature.Shared;
using Ezg.Feature.System.Admin;
using Ezg.Package.Pooling;
using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.Meta.HomeScene
{
    internal class HomeSceneController : MonoBehaviour
    {
        private static GameObject _impactMergeVfxPrefab;
        private bool isAnim;
        private bool isClicked;
        private readonly float timeDelay = 1f;
        private float timmer;

        private static GameObject ImpactMergeVfxPrefab
        {
            get
            {
                if (_impactMergeVfxPrefab == null)
                    _impactMergeVfxPrefab = ResLoader.Load<GameObject>(PathUtils.VfxImpactMerge);
                return _impactMergeVfxPrefab;
            }
        }

        private void Awake()
        {
            // removed: TutorialContainer / TutorialFactory (gameplay removed)
        }

        private async void Start()
        {
            UIManager.Instance.Show(GameEnums.Features.HomeScreen, UIManager.UIGroupName.Main_Container).Forget();
            UIManager.Instance.Show(GameEnums.Features.CurrencyBar, UIManager.UIGroupName.CurrencyBar_Container)
                .Forget();

            AdminManager.InitAdmin().Forget();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                isAnim = false;
                timmer = 0;
                SpawnImpactVfx(Input.mousePosition);
            }

            //if (Input.GetKeyDown(KeyCode.G))
            //{
            //    PackDurationProperties properties = new PackDurationProperties()
            //    {
            //        type = PackDurationType.StarterPack,
            //        isShowPopup = true,
            //    };
            //    EventManager.EmitEventData(EventName.ShowPackDuration, data: properties);
            //}

            //if (Input.GetKeyDown(KeyCode.K))
            //{
            //    PackDurationProperties properties = new PackDurationProperties()
            //    {
            //        type = PackDurationType.OpenningPack,
            //        isShowPopup = true,
            //    };
            //    EventManager.EmitEventData(EventName.ShowPackDuration, data: properties);
            //}

            //if (Input.GetKeyDown(KeyCode.L))
            //{
            //    EventManager.EmitEvent(EventName.OutOfEnergy);
            //}

            // removed: TutorialContainer / TutorialStep / TutorialStatus / Finger tutorial-finger block (gameplay removed)
        }

        private void OnEnable()
        {
            EventManager.StartListening(EventName.ResetTutButtonPlay, ResetTutButtonPlay);
        }

        private void OnDisable()
        {
            EventManager.StopListening(EventName.ResetTutButtonPlay, ResetTutButtonPlay);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) HomeSceneManager.ReCheckFeature();
        }

        private void ResetTutButtonPlay()
        {
            isClicked = false;
            isAnim = false;
            // if (!TutorialContainer.isRunning)
            // {
            //     TutorialContainer.HideFinger();
            // }
        }

        private static void SpawnImpactVfx(Vector3 position)
        {
            if (ImpactMergeVfxPrefab == null) return;

            var vfxObj = PoolingManager.Instance.Show(
                ImpactMergeVfxPrefab,
                position,
                Quaternion.identity,
                GameSystems.OverviewCanvasController.transform);

            vfxObj.transform.localScale = Vector3.one;
            vfxObj.transform.SetAsLastSibling();
        }


        //private void Update()
        //{
        //    if (Input.GetKeyUp(KeyCode.B))
        //    {
        //        ProfileManager.DeleteAllLocalPlayerData();
        //        DataPlayer.ClearAllData();
        //        DataPlayer.ClearData();
        //        TutorialContainer.ClearData();
        //        DataPlayer.IsNewPlayer = true;
        //        GameSystems.ShowSimpleMessage("ClearAllData");
        //       // GameSystems.ChangeScene(GameEnums.Scenes.SplashScene);
        //    }
        //    if (Input.GetKeyUp(KeyCode.N))
        //    {
        //        GameSystems.ChangeScene(GameEnums.Scenes.SplashScene);
        //    }
        //}
    }
}