using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Ezg.Core.Adapter;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.Firebase;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.Account;
using Ezg.Feature.System.OverviewCanvas;
using Ezg.Feature.System.RewardPopup;
using Ezg.Feature.System.Tooltip;
using Ezg.Package.Audio;
using Ezg.Package.Factory;
using Ezg.Package.Localize;
using Ezg.Package.Localize.Localization;
using Ezg.Package.Pooling;
using Newtonsoft.Json;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using UIManager = BlackFace.Libraries.Modules.UIModule.UIManager;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.UI.Message;

namespace Ezg.Feature.Shared.Systems
{
    public static class GameSystems
    {
        private const float TextTooltipGap = 20f;
        private const float ResourceTooltipGap = 50f;

        //public static GameCheatModel Cheat = new();

        public static bool isCheat = true;

        public static bool isNewOpen;

        private static TooltipController _tooltipObject;
        private static readonly Dictionary<int, TooltipController> _tooltipCache = new();

        private static bool _isCheckingInternet;

        public static OverviewCanvas OverviewCanvasController;
        public static GameObject SimpleMessageTemplate;
        public static GameObject SimpleMessageSetPosTemplate;

        static GameSystems()
        {
            //UIManager.QuitGameAction = QuitGameAction;
            IsShowingSimpleNotif = false;
            isNewOpen = false;
            SimpleNotifWaitLine.Clear();
        }

        public static bool IsPremium => GameConstant.PackNameAndroidPremium.Equals(Application.identifier);

        public static TooltipImageButtonController TooltipImageButtonController { get; private set; }

        public static bool CanEnableCheatFromRemoteConfig()
        {
#if UNITY_EDITOR
            return true;
#endif
            if (!GameRemoteConfig.enableCheatDefault) return false;

            if (string.IsNullOrWhiteSpace(GameRemoteConfig.enableCheatDefaultDevice)) return false;

            CheatDeviceConfig cheatDeviceConfig;
            try
            {
                cheatDeviceConfig = JsonConvert.DeserializeObject<CheatDeviceConfig>(
                    GameRemoteConfig.enableCheatDefaultDevice);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Invalid cheat device config: {exception.Message}");
                return false;
            }

            if (cheatDeviceConfig?.Devices == null || cheatDeviceConfig.Devices.Count == 0) return false;

            return cheatDeviceConfig.Devices
                .Where(deviceId => !string.IsNullOrWhiteSpace(deviceId))
                .Select(deviceId => deviceId.Trim())
                .Any(deviceId =>
                    string.Equals(deviceId, GameConstant.DeviceId, StringComparison.OrdinalIgnoreCase));
        }

        public static void SyncCheatStateFromRemoteConfig()
        {
            isCheat = CanEnableCheatFromRemoteConfig();
            EventManager.EmitEvent(nameof(EventName.CheatChanged));
        }

        public static bool TryEnableCheat()
        {
            if (!CanEnableCheatFromRemoteConfig())
            {
                isCheat = false;
                EventManager.EmitEvent(nameof(EventName.CheatChanged));
                return false;
            }

            isCheat = true;
            EventManager.EmitEvent(nameof(EventName.CheatChanged));
            return true;
        }

        public static void DisableCheat(bool emitEvent = true)
        {
            isCheat = false;
            if (emitEvent) EventManager.EmitEvent(nameof(EventName.CheatChanged));
        }

        public static async void CheckInternet()
        {
            if (_isCheckingInternet) return;

            _isCheckingInternet = true;
            while (_isCheckingInternet)
            {
                await UniTask.Delay(5.ToMiliseconds(), true);
                if (!_isCheckingInternet) break;

                // Khi stop Play Mode, singleton UIManager bị destroy nhưng vòng lặp static vẫn chạy
                // → Instance == null gây NullReferenceException. Thoát vòng lặp khi UIManager không còn.
                if (UIManager.Instance == null)
                {
                    _isCheckingInternet = false;
                    break;
                }

                if (!IsInternetConnection())
                    UIManager.Instance.Show(GameEnums.Features.RequireInternet).Forget();
                else
                    UIManager.Instance.CloseFeature(GameEnums.Features.RequireInternet);
            }
        }

        private static Vector2 GetTooltipOffset(RectTransform sourceRect, TooltipBase.TooltipAlignment align,
            float edgeGap)
        {
            if (sourceRect == null)
                return Vector2.zero;

            var rect = sourceRect.rect;
            var center = rect.center;

            return align switch
            {
                TooltipBase.TooltipAlignment.Left => new Vector2(rect.xMin - edgeGap, center.y),
                TooltipBase.TooltipAlignment.Mid => center,
                TooltipBase.TooltipAlignment.Right => new Vector2(rect.xMax + edgeGap, center.y),
                TooltipBase.TooltipAlignment.TopLeft => new Vector2(rect.xMin, rect.yMax + edgeGap),
                TooltipBase.TooltipAlignment.TopMid => new Vector2(center.x, rect.yMax + edgeGap),
                TooltipBase.TooltipAlignment.TopRight => new Vector2(rect.xMax, rect.yMax + edgeGap),
                TooltipBase.TooltipAlignment.BotLeft => new Vector2(rect.xMin, rect.yMin - edgeGap),
                TooltipBase.TooltipAlignment.BotMid => new Vector2(center.x, rect.yMin - edgeGap),
                TooltipBase.TooltipAlignment.BotRight => new Vector2(rect.xMax, rect.yMin - edgeGap),
                _ => Vector2.zero
            };
        }

        private static RectTransform ResolveTooltipParent(GameObject source, Transform parentCustom)
        {
            if (parentCustom is RectTransform customRect)
                return customRect;

            return source != null ? source.transform.parent as RectTransform : null;
        }

        private static Vector2 GetTooltipLocalPosition(RectTransform sourceRect, RectTransform parentRect,
            TooltipBase.TooltipAlignment align, float edgeGap, float bonusX, float bonusY)
        {
            if (sourceRect == null || parentRect == null)
                return new Vector2(bonusX, bonusY);

            var worldPosition = sourceRect.TransformPoint(GetTooltipOffset(sourceRect, align, edgeGap) +
                                                          new Vector2(bonusX, bonusY));
            var canvas = parentRect.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, camera,
                out var localPoint);

            return localPoint;
        }

        public static void ShowTooltip(this GameObject source, string content,
            TooltipBase.TooltipAlignment align = TooltipBase.TooltipAlignment.TopMid, float bonusX = 0,
            float bonusY = 0, Transform parentCustom = null, bool setPivot = true,
            TooltipBase.ArrowPosition arrowPos = TooltipBase.ArrowPosition.Bot,
            float customArrowOffsetX = 0f, float customArrowOffsetY = 0f, float delayDestroy = 2f)
        {
            ClearToolTipObject();

            var sourceRect = source.GetComponent<RectTransform>();
            var tooltipParent = ResolveTooltipParent(source, parentCustom);
            var localPos = GetTooltipLocalPosition(sourceRect, tooltipParent, align, TextTooltipGap, bonusX, bonusY);

            _tooltipObject = PoolingManager.Instantiate<TooltipController>(DataManager.GeneralAssets.TooltipElement,
                localPos, tooltipParent, isUI: true);

            _tooltipObject.InitData(content, align, setPivot, arrowPos, customArrowOffsetX, customArrowOffsetY,
                delayDestroy);
        }

        public static void ClearToolTipObject()
        {
            if (_tooltipObject != null)
            {
                GameObject.Destroy(_tooltipObject.gameObject);
                _tooltipObject = null;
            }
        }

        public static void ShowTooltip(this GameObject source, Resource[] data,
            TooltipBase.TooltipAlignment align = TooltipBase.TooltipAlignment.TopMid, float bonusX = 0,
            float bonusY = 0, Transform parentCustom = null, bool setPivot = true,
            TooltipBase.ArrowPosition arrowPos = TooltipBase.ArrowPosition.Bot,
            float customArrowOffsetX = 0f, float customArrowOffsetY = 0f, float delayDestroy = 2f)
        {
            ClearToolTipObject();

            var sourceRect = source.GetComponent<RectTransform>();
            var tooltipParent = ResolveTooltipParent(source, parentCustom);
            var localPos =
                GetTooltipLocalPosition(sourceRect, tooltipParent, align, ResourceTooltipGap, bonusX, bonusY);

            _tooltipObject = PoolingManager.Instantiate<TooltipController>(DataManager.GeneralAssets.TooltipElement,
                localPos, tooltipParent, isUI: true);

            _tooltipObject.InitData(data, align, setPivot, arrowPos, customArrowOffsetX, customArrowOffsetY,
                delayDestroy);
        }

        public static void ShowTooltips(this GameObject source, Resource[] data,
            TooltipBase.TooltipAlignment align = TooltipBase.TooltipAlignment.TopMid, float bonusX = 0,
            float bonusY = 0, Transform parentCustom = null, bool setPivot = true,
            TooltipBase.ArrowPosition arrowPos = TooltipBase.ArrowPosition.Bot,
            float customArrowOffsetX = 0f, float customArrowOffsetY = 0f, float delayDestroy = 2f)
        {
            if (source == null) return;

            var sourceID = source.GetInstanceID();

            if (_tooltipCache.TryGetValue(sourceID, out var oldTooltip))
            {
                if (oldTooltip != null) GameObject.Destroy(oldTooltip.gameObject);
                _tooltipCache.Remove(sourceID);
            }

            var sourceRect = source.GetComponent<RectTransform>();
            var tooltipParent = ResolveTooltipParent(source, parentCustom);
            var localPos =
                GetTooltipLocalPosition(sourceRect, tooltipParent, align, ResourceTooltipGap, bonusX, bonusY);

            var newTooltip = PoolingManager.Instantiate<TooltipController>(
                DataManager.GeneralAssets.TooltipIngredientElement,
                localPos,
                tooltipParent,
                isUI: true
            );

            newTooltip.InitData(data, align, setPivot, arrowPos, customArrowOffsetX, customArrowOffsetY, delayDestroy,
                false);

            _tooltipCache[sourceID] = newTooltip;

            CleanUpTooltipCache();
        }

        public static void HideTooltipFromDic(this GameObject source)
        {
            var sourceID = source.GetInstanceID();

            if (_tooltipCache.TryGetValue(sourceID, out var oldTooltip))
            {
                if (oldTooltip != null) GameObject.Destroy(oldTooltip.gameObject);
                _tooltipCache.Remove(sourceID);
            }
        }

        private static void CleanUpTooltipCache()
        {
            var keysToRemove = new List<int>();
            foreach (var kvp in _tooltipCache)
                if (kvp.Value == null)
                    keysToRemove.Add(kvp.Key);

            foreach (var key in keysToRemove) _tooltipCache.Remove(key);
        }

        public static void ShowTooltipImageButton(this GameObject source, Sprite sprite, UnityAction onButtonClick,
            TooltipBase.TooltipAlignment align = TooltipBase.TooltipAlignment.TopMid, float bonusX = 0,
            float bonusY = 0, Transform parentCustom = null, bool setPivot = true,
            TooltipBase.ArrowPosition arrowPos = TooltipBase.ArrowPosition.Bot,
            float customArrowOffsetX = 0f, float customArrowOffsetY = 0f, float delayDestroy = 2f)
        {
            ClearTooltipImageButton();
            ClearToolTipObject();

            var sourceRect = source.GetComponent<RectTransform>();
            var tooltipParent = ResolveTooltipParent(source, parentCustom);
            var localPos = GetTooltipLocalPosition(sourceRect, tooltipParent, align, TextTooltipGap, bonusX, bonusY);

            TooltipImageButtonController = PoolingManager.Instantiate<TooltipImageButtonController>(
                DataManager.GeneralAssets.TooltipImageButtonElement,
                localPos, tooltipParent, isUI: true);

            TooltipImageButtonController.InitData(sprite, onButtonClick, align, setPivot, arrowPos,
                customArrowOffsetX, customArrowOffsetY, delayDestroy, false);
        }

        public static void ClearTooltipImageButton()
        {
            if (TooltipImageButtonController != null)
            {
                GameObject.Destroy(TooltipImageButtonController.gameObject);
                TooltipImageButtonController = null;
            }
        }

        public static void ShowTooltipIngredient(this GameObject source, Resource[] data,
            TooltipBase.TooltipAlignment align = TooltipBase.TooltipAlignment.TopMid, float bonusX = 0,
            float bonusY = 0, Transform parentCustom = null, bool setPivot = true,
            TooltipBase.ArrowPosition arrowPos = TooltipBase.ArrowPosition.Bot,
            float customArrowOffsetX = 0f, float customArrowOffsetY = 0f, float delayDestroy = 2f)
        {
            ClearToolTipObject();

            var sourceRect = source.GetComponent<RectTransform>();
            var tooltipParent = ResolveTooltipParent(source, parentCustom);
            var localPos =
                GetTooltipLocalPosition(sourceRect, tooltipParent, align, ResourceTooltipGap, bonusX, bonusY);

            _tooltipObject = PoolingManager.Instantiate<TooltipController>(
                DataManager.GeneralAssets.TooltipIngredientElement,
                localPos, tooltipParent, isUI: true);

            _tooltipObject.InitData(data, align, setPivot, arrowPos, customArrowOffsetX, customArrowOffsetY,
                delayDestroy);
        }

        private static void QuitGameAction()
        {
            ShowMessage("confirm_quit_game", acceptAction: Application.Quit, focusYes: false);
        }

        public static void ChangeScene(GameEnums.Scenes scene)
        {
            InitCancelToken(scene);
            OverviewCanvasController.ChangeScene(scene).Forget();
        }

        public static void OpenLoading()
        {
            OverviewCanvasController.OpenLoading().Forget();
        }

        /// <summary>
        ///     Tự động ẩn object sau 1 khoảng time
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="delayTime"></param>
        /// <returns></returns>
        public static IEnumerator AutoHideObject(GameObject obj, float delayTime, bool isRealtime = false)
        {
            if (delayTime != 0)
                if (!isRealtime)
                    yield return new WaitForSeconds(delayTime);
                else
                    yield return new WaitForSecondsRealtime(delayTime);

            if (obj != null && obj.activeSelf)
                obj.SetActive(false);
        }

        /// <summary>
        ///     Bật/tắt cho phép thao tác trong game
        /// </summary>
        /// <param name="isEnable"></param>
        public static void EnableTouch(bool isEnable)
        {
            var eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
            eventSystem.enabled = isEnable;
        }

        /// <summary>
        ///     Get localize
        /// </summary>
        /// <param name="key"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string Localize(string key, LocalizeCategory category = LocalizeCategory.Common)
        {
            var localize = Localization.Current.Get(category.ToString().ToSnakeCase(), key);
            return string.IsNullOrEmpty(localize) ? category + ": " + key : localize;
        }

        public static string FormatString(string input, float[] param)
        {
            if (param == null) return input;

            try
            {
                switch (param.Length)
                {
                    case 1:
                        return string.Format(input, param[0]);
                    case 2:
                        return string.Format(input, param[0], param[1]);
                    case 3:
                        return string.Format(input, param[0], param[1], param[2]);
                    case 4:
                        return string.Format(input, param[0], param[1], param[2], param[3]);
                    case 5:
                        return string.Format(input, param[0], param[1], param[2], param[3], param[4]);
                    default:
                        return input;
                }
            }
            catch (Exception e)
            {
                ShowSimpleMessage(input);
                return input;
            }
        }

        public static string FormatString(string input, object[] param)
        {
            if (param == null) return input;

            try
            {
                switch (param.Length)
                {
                    case 1:
                        return string.Format(input, param[0]);
                    case 2:
                        return string.Format(input, param[0], param[1]);
                    case 3:
                        return string.Format(input, param[0], param[1], param[2]);
                    case 4:
                        return string.Format(input, param[0], param[1], param[2], param[3]);
                    case 5:
                        return string.Format(input, param[0], param[1], param[2], param[3], param[4]);
                    default:
                        return input;
                }
            }
            catch (Exception e)
            {
                ShowSimpleMessage(input);
                return input;
            }
        }

        public static void ShowWaitingScreen(bool isWaiting)
        {
            OverviewCanvasController.WaitingPurchase(isWaiting);
        }

        public static bool IsShowWaitingScreen()
        {
            return OverviewCanvasController.IsShowWaitingScreen();
        }

        //public static Sprite GetImageByMoneyType(EnumBase.MoneyTypes moneyType)
        //{
        //    return ResourcesManager.Load<Sprite>(ResourcesManager.MoneyTypeImgPath, ((int)moneyType).ToString());
        //}

        public static void ShowRewardPopup(List<Resource> rewards, PurchaseType type, Action onClose = null)
        {
            UIManager.Instance.Show(GameEnums.Features.RewardPopup, UIManager.UIGroupName.Toast_Container,
                data: new RewardPopupProperty(rewards, onClose, type)).Forget();
            //var rewardPopup = InitFeature(GameEnums.Features.RewardPopup);
            //rewardPopup.GetComponent<RewardPopupController>().InitData(rewards);
            //AudioService.Default.PlaySound(EnumBase.Sounds.sfx_ui_buy);
        }

        public static void ShowRewardPopup(Resource reward, Action onClose = null)
        {
            UIManager.Instance.Show(GameEnums.Features.RewardPopup, data: new RewardPopupProperty(reward, onClose))
                .Forget();
            //var rewardPopup = InitFeature(GameEnums.Features.RewardPopup);
            //rewardPopup.GetComponent<RewardPopupController>().InitData(reward);
            //AudioService.Default.PlaySound(EnumBase.Sounds.sfx_ui_buy);
        }

        /// <summary>
        ///     Khởi chạy lại ứng dụng trên android
        /// </summary>
        public static void RestartApplication()
        {
            AudioService.Default.StopMusic();
            AudioService.Default.StopSound();
            if (Application.isEditor) ResetIOS();
#if UNITY_IOS && !UNITY_EDITOR
            //ResetScene();
            ResetIOS();
            EventManager.EmitEvent(EventName.CloseWaiting);
#endif


#if UNITY_ANDROID && !UNITY_EDITOR
             using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
             {
                 const int kIntent_FLAG_ACTIVITY_CLEAR_TASK = 0x00008000;
                 const int kIntent_FLAG_ACTIVITY_NEW_TASK = 0x10000000;
            
                 var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                 var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                 var intent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", Application.identifier);
            
                 intent.Call<AndroidJavaObject>("setFlags",
                     kIntent_FLAG_ACTIVITY_NEW_TASK | kIntent_FLAG_ACTIVITY_CLEAR_TASK);
                 currentActivity.Call("startActivity", intent);
                 currentActivity.Call("finish");
                 var process = new AndroidJavaClass("android.os.Process");
                 int pid = process.CallStatic<int>("myPid");
                 process.CallStatic("killProcess", pid);
             }
            //ResetIOS();
            GameSystems.ShowWaitingScreen(false);
            //EventManager.EmitEvent(EventName.CloseWaiting);
#endif
        }

        private static void ResetIOS()
        {
            GameInitialize.ClearStatic();

            // Tear down the persistent sync singleton so its Awake/OnEnable re-fire
            // on the next access — without this, OnEnable's StartListening calls
            // never run again on iOS soft restart and the singleton keeps any
            // stale references it accumulated during the previous session.
            // Object.Destroy (not DestroyImmediate) lets in-flight UniTasks finish
            // before the GameObject is removed at end of frame.
            var syncManager = Object.FindObjectOfType<PlayerDataSyncManager>();
            if (syncManager != null) Object.Destroy(syncManager.gameObject);

            ChangeScene(GameEnums.Scenes.SplashScene);
        }

        public static void ResetRuntimeState()
        {
            _isCheckingInternet = false;
            IsShowingSimpleNotif = false;
            SimpleNotifWaitLine.Clear();
            DisableCheat(false);
            isNewOpen = false;

            if (_battleCancelToken is { IsCancellationRequested: false }) _battleCancelToken.Cancel();

            _battleCancelToken?.Dispose();
            _battleCancelToken = null;

            if (HomeCancelToken is { IsCancellationRequested: false }) HomeCancelToken.Cancel();

            HomeCancelToken?.Dispose();
            HomeCancelToken = null;
        }


        public static bool IsInstallFromStore()
        {
            if (Application.isEditor) return true;

#if PLATFORM_ANDROID
            return Application.installerName == "com.android.vending";
#else
            return Application.installMode == ApplicationInstallMode.Store;
#endif
        }

        public static IEnumerator AnimMove(Transform trans, Vector3 targetPos, float duration, AnimationCurve animCurve,
            Action actionComplete = null, bool isLocalMove = false)
        {
            var startPos = isLocalMove ? trans.localPosition : trans.position;
            float time = 0;
            var rate = 1 / duration;

            while (time < 1)
            {
                time += rate * Time.deltaTime;
                if (trans.gameObject == null)
                    yield break;

                if (isLocalMove)
                    trans.localPosition = Vector3.Lerp(startPos, targetPos, animCurve.Evaluate(time));
                else
                    trans.position = Vector3.Lerp(startPos, targetPos, animCurve.Evaluate(time));

                yield return null;
            }

            //Gán lại tọa độ sau khi move xong
            if (isLocalMove)
                trans.localPosition = targetPos;
            else
                trans.position = targetPos;

            //Thực thi actionComplete nếu có truyền vào
            if (actionComplete != null)
                actionComplete();
        }

        /// <summary>
        ///     Tạo các thứ mặc định nếu là tài khoản mới
        /// </summary>
        public static async void SetupNewAccount()
        {
            if (DataPlayer.IsNewPlayer)
            {
                RewardsService.ReceiveRewards(DataManager.DefaultResource.GetAll().GenerateReward(), PurchaseType.Free,
                    SourceTracking.NewAccount, isShowPopup: false);
                DataPlayer.IsNewPlayer = false;
                DataPlayer.SaveAllData();

                var systemLang = Application.systemLanguage;

                if (SelectLanguageService.systemLanguages.Select(x => x.systemLanguage).ToList().Contains(systemLang))
                {
                    PlayerDataManager.Settings.SetLanguage(systemLang);
                    EventManager.EmitEvent(EventName.SettingLanguageChanged);
                }

                // removed: DataManager.BoardDefaultConfig, DataManager.SlotStateConfig, StatusItemTypes, PlayerDataManager.Gameplay (gameplay removed)
            }
        }

        public static bool IsInternetConnection()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        public static IEnumerator LerpFloat(this float value, float targetValue, float duration)
        {
            float time = 0;
            var rate = 1 / duration;
            while (time < 1)
            {
                time += rate * Time.deltaTime;
                value = Mathf.Lerp(value, targetValue, time);
                yield return null;
            }
        }

        public static async Task<GameObject> SpawnObjStar(Transform parent)
        {
            var prefab = await ResLoader.LoadAsync<GameObject>(PathUtils.RoleItemImageStar);

            return Object.Instantiate(prefab, parent);
        }

        private sealed class CheatDeviceConfig
        {
            [JsonProperty("devices")] public List<string> Devices { get; set; }
        }

        #region SimpleMessage

        private static bool IsShowingSimpleNotif;
        public static Queue<string> SimpleNotifWaitLine { get; set; } = new();

        /// <summary>
        ///     Show thông báo nhỏ trong game
        /// </summary>
        /// <returns></returns>
        private static async UniTask StartShowSimpleMessage()
        {
            IsShowingSimpleNotif = true;
            Begin:

            var msgTemp = MonoBehaviour.Instantiate(SimpleMessageTemplate, Vector3.zero, Quaternion.identity.normalized,
                OverviewCanvasController.ParentSimpleMessage).GetComponent<SimpleMessageController>();
            msgTemp.gameObject.SetActive(false);

            //var msgTemp = PoolingManager.Instance.ShowLst<SimpleMessageController>(SimpleMessageTemplate,
            //    new Vector3(0, 0, 0), isShowObject: false);
            //if (msgTemp.IsNewCreate)
            //    msgTemp.GObject.transform.SetParent(OverviewCanvasController.ParentSimpleMessage, false);
            msgTemp.InitData(SimpleNotifWaitLine.Dequeue());
            msgTemp.gameObject.SetActive(true);

            await UniTask.Delay(130, true);
            if (SimpleNotifWaitLine.Count <= 0)
                IsShowingSimpleNotif = false;
            else
                goto Begin;
        }

        private static async UniTask StartShowSimpleMessageSetPos(RectTransform posRect, Transform parent = null)
        {
            IsShowingSimpleNotif = true;

            Begin:
            var parentTransform = parent != null
                ? parent
                : OverviewCanvasController.ParentSimpleMessage;

            var msgTemp = MonoBehaviour.Instantiate(SimpleMessageSetPosTemplate, parentTransform)
                .GetComponent<SimpleMessageSetPosController>();

            msgTemp.gameObject.SetActive(false);

            var msgRect = msgTemp.GetComponent<RectTransform>();
            var parentRect = parentTransform as RectTransform;

            var localPos = Vector2.zero;
            Camera cam = null;

            if (parentRect != null && posRect != null)
            {
                // ✅ Lấy screen position chuẩn từ RectTransform
                var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, posRect.position);

                // ✅ Xác định camera chính xác
                var canvas = parentRect.GetComponent<Canvas>();
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    cam = canvas.worldCamera;

                // ✅ Convert tọa độ màn hình sang tọa độ local trong canvas
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out localPos);

                msgRect.anchoredPosition = localPos;
            }

            // ✅ Reset transform state
            msgRect.localScale = Vector3.one;
            msgRect.localRotation = Quaternion.identity;

            //Debug.Log($"✅ Final localPos={localPos}, parentRect={parentRect.name}, cam={cam?.name ?? "none"}");

            msgTemp.InitData(SimpleNotifWaitLine.Dequeue(), posRect.position);
            msgTemp.gameObject.SetActive(true);

            // ✅ Force update layout sau khi bật object
            LayoutRebuilder.ForceRebuildLayoutImmediate(msgRect);

            await UniTask.Delay(130, true);

            if (SimpleNotifWaitLine.Count <= 0)
                IsShowingSimpleNotif = false;
            else
                goto Begin;
        }


        /// <summary>
        ///     Hiển thị thông báo nổi trong game
        /// </summary>
        /// <param name="text"></param>
        /// <param name="category"></param>
        public static async void ShowSimpleMessage(string text, LocalizeCategory category = LocalizeCategory.Common)
        {
            if (string.IsNullOrEmpty(text)) return;

            text = text.Contains("_") ? Localize(text, category) : text;

            SimpleNotifWaitLine.Enqueue(text);
            if (!IsShowingSimpleNotif)
                await StartShowSimpleMessage();
        }

        public static async void ShowSimpleMessageWithPos(string text, RectTransform pos, Transform parent,
            LocalizeCategory category = LocalizeCategory.Common)
        {
            if (string.IsNullOrEmpty(text)) return;

            text = text.Contains("_") ? Localize(text, category) : text;

            SimpleNotifWaitLine.Enqueue(text);
            if (!IsShowingSimpleNotif)
                await StartShowSimpleMessageSetPos(pos, parent);
        }

        public static async void ShowMessageNotEnoughResource(Resource resource)
        {
            var text = Localize("not_enough_resource");
            var final = string.Format(text, PlayerResource.GetItemName(resource));
            SimpleNotifWaitLine.Enqueue(final);
            if (!IsShowingSimpleNotif)
                await StartShowSimpleMessage();
        }

        public static void ShowMessage(string text, string title = null)
        {
            OverviewCanvasController.ShowMessage(text, title, true);
        }

        public static void ShowMessage(string text, string title = null, UnityAction acceptAction = null,
            UnityAction cancelAction = null, EnumBase.MoneyTypes moneyType = EnumBase.MoneyTypes.None, long value = -1,
            bool focusYes = true, bool showCancelButton = true)
        {
            OverviewCanvasController.ShowMessage(text, title, acceptAction, cancelAction, moneyType, value, focusYes,
                showCancelButton);
        }

        public static async UniTask SetupMessage()
        {
            //await UniTask.Delay(Random.Range(120, 300).ToMiliseconds(), DelayType.Realtime);
            //if (ProfileManager.IsLogon() && (PlayerDataManager.Account.Email.Contains("deviloper.vn") || PlayerDataManager.Account.Email.Contains("deviloper.")))
            //{

            //}
            //else
            //{
            //    Application.Quit();
            //}
        }

        #endregion

        #region CancelToken for UniTask

        //Các Task sử dụng canceltoken cần được gọi bằng UniTask.Create() hoặc Task.Run() để tạo luồng mới, gọi từ await sẽ bị lỗi

        /// <summary>
        ///     CancelToken for battle scene
        /// </summary>
        private static CancellationTokenSource _battleCancelToken;

        public static CancellationTokenSource BattleCancelToken
        {
            get
            {
                if (_battleCancelToken == null)
                    BattleCancelToken = new CancellationTokenSource();
                return _battleCancelToken;
            }
            set => _battleCancelToken = value;
        }

        /// <summary>
        ///     CancelToken for battle scene
        /// </summary>
        public static CancellationTokenSource HomeCancelToken { get; set; }

        /// <summary>
        ///     Khởi tạo và đặt lại canceltoken cho các scene
        /// </summary>
        public static void InitCancelToken(GameEnums.Scenes sceneType)
        {
            switch (sceneType)
            {
                case GameEnums.Scenes.BattleScene:
                    if (BattleCancelToken is { IsCancellationRequested: false })
                    {
                        BattleCancelToken?.Cancel();
                        BattleCancelToken?.Dispose();
                    }

                    if (HomeCancelToken is { IsCancellationRequested: false })
                    {
                        HomeCancelToken?.Cancel();
                        HomeCancelToken?.Dispose();
                    }

                    BattleCancelToken = new CancellationTokenSource();
                    break;
                case GameEnums.Scenes.HomeScene:
                    if (BattleCancelToken is { IsCancellationRequested: false })
                    {
                        BattleCancelToken?.Cancel();
                        BattleCancelToken?.Dispose();
                    }

                    if (HomeCancelToken is { IsCancellationRequested: false })
                    {
                        HomeCancelToken?.Cancel();
                        HomeCancelToken?.Dispose();
                    }

                    HomeCancelToken = new CancellationTokenSource();
                    break;
            }
        }

        public static void OnQuitGame()
        {
            if (BattleCancelToken != null && !BattleCancelToken.IsCancellationRequested)
                BattleCancelToken?.Cancel();
            if (HomeCancelToken != null && !HomeCancelToken.IsCancellationRequested)
                HomeCancelToken?.Cancel();

            FirebaseEvent.quit_game.Send(new FirebaseEventConfig
            {
                energy_current = PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.Energy),
                // removed: ItemGeneratorManager, GridType, OrderManager, ItemToolManager (gameplay removed)
                type_generator_cooldown = 0,
                id_order_remain = Array.Empty<int>(),
                type_tool_cooldown = 0
            });
        }

        #endregion
    }
}