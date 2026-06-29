using AppsFlyerSDK;
using DG.Tweening;
using Ezg.Core.Adapter;
using Ezg.Core.Utils;
using Ezg.Feature.IAP;
using Ezg.Feature.Shared;
using Ezg.Package.Factory;
using UnityEngine;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.Shared.Systems
{
public static class GameInitialize
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitBeforeSceneLoad()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        DOTween.SetTweensCapacity(1500, 50);
        Application.runInBackground = true;
    }

    public static void InitAppsFlyer()
    {
#if UNITY_ANDROID
        AppsFlyer.initSDK("Ge7ssp4Hs6ZKXqQ2UU6yeD", null);
#elif UNITY_EDITOR
        AppsFlyer.initSDK("Ge7ssp4Hs6ZKXqQ2UU6yeD", null);
#else
        AppsFlyer.initSDK("Ge7ssp4Hs6ZKXqQ2UU6yeD", "6444881859");
#endif
        AppsFlyer.getConversionData(InAppManager.Instance.Listener.gameObject.name);
        AppsFlyer.startSDK();
        AppFlyerEvent.af_first_open.Send();
    }

    public static void ClearStatic()
    {
        // removed: GridService (gameplay removed)
        DataManager.Clear();
        // removed: CookingRecipes (gameplay removed)
        GameSystems.ResetRuntimeState();
        TimeManager.ResetRuntimeState();
        DataPlayer.DisposeAllModuleSubscriptions();
        PlayerDataManager.ClearCachedModules();
        PlayerResource.ResetRuntimeState();
        // removed: TutorialContainer, EventMergeService, PetalPlatePartyService, PizzaTowerRaceService, SpeedFeastRaceService (gameplay removed)

        var assetBundleManager = Object.FindObjectOfType<AssetBundleManager>();
        if (assetBundleManager != null)
        {
            assetBundleManager.CancelAllTasks();
            assetBundleManager.ResetData();
        }
#if UNITY_EDITOR
#endif
    }
}
}