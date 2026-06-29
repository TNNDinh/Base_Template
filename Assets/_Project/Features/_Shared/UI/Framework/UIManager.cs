using Cysharp.Threading.Tasks;
using Ezg.Core.Adapter;
using Ezg.Feature.Meta.HomeScene;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;
using Ezg.Package.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace BlackFace.Libraries.Modules.UIModule
{
    public class UIManager : Singleton<UIManager>
    {
        #region Fields

        private readonly string FEATURE_PATH = "{0}";
        private readonly string _thisName = "screen_init";

        public static bool IsEnableTouch;
        public static bool IsShowMessageBox;
        public static bool IsShowLoading => GameSystems.IsShowWaitingScreen();
        public static UnityAction CloseMessageBoxAction;
        public static UnityAction QuitGameAction;
        public static UnityAction CloseAnyFeatureAction;

        private static GameEnums.Features _featuresLatestClosed;

        public FeatureBaseController currencyBar;

        // Cache feature paths to avoid string allocation every Show call
        private static readonly Dictionary<GameEnums.Features, (string path, string bundleName)> _featurePathCache =
            new();

        public enum UIGroupName
        {
            Main_Container,
            Modal_Container,
            CurrencyBar_Container, // Currency bar dùng chung, hiển thị trên Modal
            Overlay_Container, // Sales, event, mini game — đè lên currency bar
            Tutorial_Container,
            Toast_Container // System alerts (mất mạng...) — đè lên tất cả
        }

        private readonly List<UIGroupName> _clearWhenChangeScene = new()
        {
            UIGroupName.Main_Container,
            UIGroupName.Modal_Container,
            UIGroupName.Overlay_Container,
            UIGroupName.CurrencyBar_Container
        };

        /// <summary>
        ///     Các tính năng đang được kích hoạt và hiển thị, sẽ bị loại bỏ sau khi chuyển scene
        /// </summary>
        private readonly Dictionary<GameEnums.Features, GameObject> _featuresActiving = new(50);

        private readonly Dictionary<GameEnums.Features, UnityAction> _actionCloseFeature = new();

        /// <summary>
        ///     Các tính năng đang được kích hoạt và hiển thị, ko bị loại bỏ sau khi chuyển scene
        /// </summary>
        private readonly Dictionary<GameEnums.Features, GameObject> _outFeaturesActiving = new(50);

        private GameEnums.Scenes _currentScene;
        private Dictionary<UIGroupName, Transform> _groupTransform;

        public Dictionary<GameEnums.Features, ScreenDontDestroyController> _cacheFeatureDontDestroy = new();

        /// <summary>
        ///     Layer bắt đầu
        /// </summary>
        private readonly int _startLayerOrder = 10;

        /// <summary>
        ///     Layer order hiện tại của UI mới nhất
        /// </summary>
        public int CurrentLayerOrder { get; private set; }

        /// <summary>
        ///     Hàng chờ các tính năng cần bật lên — lưu kèm group để giữ đúng layer khi dequeue
        /// </summary>
        private Dictionary<GameEnums.Scenes, Queue<(GameEnums.Features feature, UIGroupName grp)>> _featureSequense;

        private static EventSystem _eventSystem;

        public static EventSystem EventSystem
        {
            get
            {
                if (_eventSystem == null) _eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

                return _eventSystem;
            }
        }

        // Cache prefabs to avoid loading from Resources/AssetBundle every time
        private static readonly Dictionary<GameEnums.Features, GameObject> _prefabCache = new();

        #endregion

        #region Initialize

        /// <summary>
        ///     Initializes the UI manager group transforms and queues.
        /// </summary>
        public void InitUI()
        {
            if (_groupTransform is { Count: > 0 }) return;

            _groupTransform = new Dictionary<UIGroupName, Transform>();
            gameObject.name = _thisName;
            foreach (var grp in (UIGroupName[])Enum.GetValues(typeof(UIGroupName)))
            {
                var obj = new GameObject(grp.ToString());
                obj.transform.SetParent(transform);
                _groupTransform.Add(grp, obj.transform);
            }

            _featureSequense = new Dictionary<GameEnums.Scenes, Queue<(GameEnums.Features feature, UIGroupName grp)>>();
            foreach (var scene in (GameEnums.Scenes[])Enum.GetValues(typeof(GameEnums.Scenes)))
                _featureSequense.Add(scene, new Queue<(GameEnums.Features, UIGroupName)>());
        }

        /// <summary>
        ///     Initializes screens that should not be destroyed on scene load.
        /// </summary>
        /// <returns>A UniTask representation of the asynchronous operation.</returns>
        public async UniTask InitScreenDontDestroy()
        {
            foreach (var uiDontDestroyModel in DataManager.UIDontDestroy.dataGroups)
            {
                var model = await Show(uiDontDestroyModel.feature, uiDontDestroyModel.group);
                var uiDontDestroyModelController = model.GetComponent<ScreenDontDestroyController>();
                uiDontDestroyModelController.Hide(true);
                if (!_cacheFeatureDontDestroy.TryGetValue(uiDontDestroyModel.feature, out var existingController))
                    _cacheFeatureDontDestroy.Add(uiDontDestroyModel.feature, uiDontDestroyModelController);
            }
        }

        /// <summary>
        ///     Resets active UI and layers when transitioning to a new scene.
        /// </summary>
        public void ResetDataWhenChangeScene()
        {
            SetLayerOrder(_startLayerOrder);
            foreach (var item in _clearWhenChangeScene)
            foreach (Transform trans in _groupTransform[item])
                Destroy(trans.gameObject);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Registers a callback action to be executed when any feature is closed.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void RegisterCloseAnyFeature(UnityAction action)
        {
            CloseAnyFeatureAction -= action;
            CloseAnyFeatureAction += action;
        }

        /// <summary>
        ///     Gets the container transform for a specific UI group.
        /// </summary>
        /// <param name="grp">The UI group name.</param>
        /// <returns>The transform container.</returns>
        public Transform GetContainerByGrpName(UIGroupName grp)
        {
            return _groupTransform[grp];
        }

        /// <summary>
        ///     Gets the UI group that the specified controller belongs to.
        /// </summary>
        /// <param name="controller">The feature base controller.</param>
        /// <returns>The UI group name, or null if not found.</returns>
        public UIGroupName? GetGroupOf(FeatureBaseController controller)
        {
            if (controller == null) return null;
            var parent = controller.transform.parent;
            foreach (var kvp in _groupTransform)
                if (kvp.Value == parent)
                    return kvp.Key;

            return null;
        }

        /// <summary>
        ///     Gets the current layer order index.
        /// </summary>
        /// <returns>The current layer order value.</returns>
        public int GetCurrentLayer()
        {
            return CurrentLayerOrder;
        }

        /// <summary>
        ///     Sets the current active scene value.
        /// </summary>
        /// <param name="scene">The target scene.</param>
        public void SetCurrentScene(GameEnums.Scenes scene)
        {
            _currentScene = scene;
        }

        /// <summary>
        ///     Gets the current active scene value.
        /// </summary>
        /// <returns>The current active scene.</returns>
        public GameEnums.Scenes GetCurrentScene()
        {
            return _currentScene;
        }

        /// <summary>
        ///     Gets the identifier of the latest feature that was closed.
        /// </summary>
        /// <returns>The latest closed feature.</returns>
        public GameEnums.Features GetFeatureLatestClosed()
        {
            return _featuresLatestClosed;
        }

        /// <summary>
        ///     Increments the current layer order index.
        /// </summary>
        public void BonusLayerOrder()
        {
            CurrentLayerOrder++;
        }

        /// <summary>
        ///     Sets the layer order to a specific value.
        /// </summary>
        /// <param name="value">The layer order value.</param>
        public void SetLayerOrder(int value)
        {
            CurrentLayerOrder = value;
        }


        /// <summary>
        ///     Shows the specified feature UI.
        /// </summary>
        /// <param name="featureType">The type of feature to show.</param>
        /// <param name="grp">The UI group container name.</param>
        /// <param name="isAsync">Whether to load the feature prefab asynchronously.</param>
        /// <param name="data">Optional initialization data to load into the feature controller.</param>
        /// <param name="bundleName">Optional specific AssetBundle name to load from.</param>
        /// <returns>A UniTask returning the instantiated feature GameObject.</returns>
        public async UniTask<GameObject> Show(GameEnums.Features featureType,
            UIGroupName grp = UIGroupName.Overlay_Container, bool isAsync = true, object data = null,
            string bundleName = "")
        {
            if (_outFeaturesActiving.TryGetValue(featureType, out var featureFilter)) return featureFilter;

            if (featureType != GameEnums.Features.Tutorial)
                //Debug.Log(featureType + " aaaaaaaaaaaaaaabbbbbbbbbbbb" + _currentLayerOrder);
                BonusLayerOrder();

            //Debug.Log(featureType + " aaaaaaaaaaaaaaa" + _currentLayerOrder);

            GameObject resultObject = null;

            // 1. Check local cache first
            if (_prefabCache.TryGetValue(featureType, out var cachedPrefab))
            {
                resultObject = cachedPrefab;
            }
            else
            {
                // 2. Load from Resources/AssetBundle if not cached

                // Sử dụng cache path để tránh string allocation mỗi lần Show
                if (!_featurePathCache.TryGetValue(featureType, out var pathInfo))
                {
                    var finalName = $"screen_{featureType.ToString().ToSnakeCase()}";
                    var resPath = string.Format(FEATURE_PATH, finalName);
                    var name = "";
                    if (!string.IsNullOrEmpty(bundleName))
                        name = bundleName;
                    else
                        name = featureType.ToString().ToLower();

                    pathInfo = (resPath, name);
                    _featurePathCache[featureType] = pathInfo;
                }

                // Load động - mặc định async để tránh block main thread
                if (isAsync)
                    resultObject = await ResLoader.LoadAsync<GameObject>(pathInfo.path, pathInfo.bundleName);
                else
                    resultObject = ResLoader.Load<GameObject>(pathInfo.path, pathInfo.bundleName);

                // Cache result for next time
                if (resultObject != null) _prefabCache[featureType] = resultObject;
            }

            if (resultObject == null)
            {
                GameSystems.ShowSimpleMessage(GameSystems.Localize("coming_soon"));
                return null;
            }

            // Clean up null references in _featuresActiving
            for (var i = _featuresActiving.Count - 1; i >= 0; i--)
                if (_featuresActiving.ElementAt(i).Value == null)
                    _featuresActiving.Remove(_featuresActiving.ElementAt(i).Key);

            // Yield removed to prevent initialization race condition
            // await UniTask.Yield();

            if (!_featuresActiving.ContainsKey(featureType))
            {
                var uiInstantiate = Instantiate(resultObject, Vector3.zero, Quaternion.identity);

                var controller = uiInstantiate.GetComponent<FeatureBaseController>();

                if (featureType == GameEnums.Features.CurrencyBar) currencyBar = controller;

                if (_clearWhenChangeScene.Contains(grp))
                {
                    _featuresActiving.TryAdd(featureType, uiInstantiate);
                    _featuresActiving[featureType].transform.SetParent(_groupTransform[grp]);
                    controller?.ApplySortingLayer(grp);
                }
                else
                {
                    _outFeaturesActiving.TryAdd(featureType, uiInstantiate);
                    uiInstantiate.transform.SetParent(_groupTransform[grp]);
                    controller?.ApplySortingLayer(grp);

                    if (data != null) controller?.LoadData(data);

                    return uiInstantiate;
                }
            }

            if (data != null) _featuresActiving[featureType].GetComponent<FeatureBaseController>().LoadData(data);

            return _featuresActiving[featureType];
        }

        /// <summary>
        ///     Closes the specified feature UI and releases its resources.
        /// </summary>
        /// <param name="feature">The feature type to close.</param>
        /// <param name="activeFeatureSequense">Whether to activate the next queued feature sequence.</param>
        public void CloseFeature(GameEnums.Features feature, bool activeFeatureSequense = true)
        {
            if (_featuresActiving.TryGetValue(feature, out var featureResult))
            {
                Destroy(featureResult);
                _featuresActiving.Remove(feature);
                SetFeatureLatestClosed(feature);
                EventManager.EmitEvent(nameof(EventName.OnCloseFeature));
                if (activeFeatureSequense) ActiveFeatureSequense();
                ActiveActionCloseFeature(feature);
            }
            else
            {
                if (_outFeaturesActiving.TryGetValue(feature, out var featureResult2))
                {
                    Destroy(featureResult2);
                    _outFeaturesActiving.Remove(feature);
                    SetFeatureLatestClosed(feature);
                    EventManager.EmitEvent(nameof(EventName.OnCloseFeature));
                    if (activeFeatureSequense) ActiveFeatureSequense();
                    ActiveActionCloseFeature(feature);
                }
            }
        }

        /// <summary>
        ///     Moves an active feature to a different UI group at runtime.
        ///     Updates the tracking dictionary if the new group changes the clear-on-scene behavior.
        /// </summary>
        /// <param name="feature">The feature type to move.</param>
        /// <param name="newGroup">The target UI group container.</param>
        public void MoveToGroup(GameEnums.Features feature, UIGroupName newGroup)
        {
            GameObject featureObj = null;
            var wasInActiving = false;

            if (_featuresActiving.TryGetValue(feature, out var obj1))
            {
                featureObj = obj1;
                wasInActiving = true;
            }
            else if (_outFeaturesActiving.TryGetValue(feature, out var obj2))
            {
                featureObj = obj2;
            }

            if (featureObj == null) return;

            featureObj.transform.SetParent(_groupTransform[newGroup]);
            featureObj.GetComponent<FeatureBaseController>()?.ApplySortingLayer(newGroup);

            var nowInActiving = _clearWhenChangeScene.Contains(newGroup);

            if (wasInActiving && !nowInActiving)
            {
                _featuresActiving.Remove(feature);
                _outFeaturesActiving.TryAdd(feature, featureObj);
            }
            else if (!wasInActiving && nowInActiving)
            {
                _outFeaturesActiving.Remove(feature);
                _featuresActiving.TryAdd(feature, featureObj);
            }
        }

        /// <summary>
        ///     Moves an active feature to a different UI group at runtime with a specific sorting layer order.
        ///     Updates the tracking dictionary if the new group changes the clear-on-scene behavior.
        /// </summary>
        /// <param name="feature">The feature type to move.</param>
        /// <param name="newGroup">The target UI group container.</param>
        /// <param name="sortingLayerOrder">The sorting layer order value to apply.</param>
        public void MoveToGroup(GameEnums.Features feature, UIGroupName newGroup, int sortingLayerOrder)
        {
            GameObject featureObj = null;
            var wasInActiving = false;

            if (_featuresActiving.TryGetValue(feature, out var obj1))
            {
                featureObj = obj1;
                wasInActiving = true;
            }
            else if (_outFeaturesActiving.TryGetValue(feature, out var obj2))
            {
                featureObj = obj2;
            }

            if (featureObj == null) return;

            featureObj.transform.SetParent(_groupTransform[newGroup]);
            var controller = featureObj.GetComponent<FeatureBaseController>();

            if (controller != null)
                controller.ApplySortingLayer(newGroup, sortingLayerOrder);
            else
                Debug.Log("CONTROLLER NULL " + feature);

            var nowInActiving = _clearWhenChangeScene.Contains(newGroup);

            if (wasInActiving && !nowInActiving)
            {
                _featuresActiving.Remove(feature);
                _outFeaturesActiving.TryAdd(feature, featureObj);
            }
            else if (!wasInActiving && nowInActiving)
            {
                _outFeaturesActiving.Remove(feature);
                _featuresActiving.TryAdd(feature, featureObj);
            }
        }

        /// <summary>
        ///     Gets the type of the last active feature in the hierarchy.
        /// </summary>
        /// <returns>The active feature type.</returns>
        public GameEnums.Features GetLastFeature()
        {
            if (_featuresActiving.Count <= 0) return _outFeaturesActiving.ElementAt(_outFeaturesActiving.Count - 1).Key;

            return _featuresActiving.ElementAt(_featuresActiving.Count - 1).Key;
        }

        /// <summary>
        ///     Gets the controller instance of the last active feature in the hierarchy.
        /// </summary>
        /// <returns>The feature base controller instance.</returns>
        public FeatureBaseController GetLastFeatureController()
        {
            if (_featuresActiving.Count <= 0)
            {
                var key = _outFeaturesActiving.ElementAt(_outFeaturesActiving.Count - 1).Key;
                _outFeaturesActiving.TryGetValue(key, out var featureResult);
                return featureResult.GetComponent<FeatureBaseController>();
            }

            var key2 = _featuresActiving.ElementAt(_featuresActiving.Count - 1).Key;
            _featuresActiving.TryGetValue(key2, out var featureResult2);
            return featureResult2.GetComponent<FeatureBaseController>();
        }

        /// <summary>
        ///     Gets the controller instance of the last active feature that is not closed.
        /// </summary>
        /// <returns>The feature base controller instance, or null if none found.</returns>
        public FeatureBaseController GetLastFeatureNotClose()
        {
            var listFeature = _featuresActiving.ToList();
            for (var i = listFeature.Count - 1; i >= 0; i--)
                if (listFeature[i].Value != null)
                {
                    if (listFeature[i].Value.GetComponent<FeatureBaseController>() is ScreenCurrencyBarController)
                        continue;
                    if (!listFeature[i].Value.GetComponent<FeatureBaseController>().isClose)
                        return listFeature[i].Value.GetComponent<FeatureBaseController>();
                }

            return null;
        }

        /// <summary>
        ///     Checks whether the specified feature is currently active.
        /// </summary>
        /// <param name="feature">The feature type to check.</param>
        /// <returns>True if the feature is active; otherwise, false.</returns>
        public bool IsFeatureActiving(GameEnums.Features feature)
        {
            if (_featuresActiving.TryGetValue(feature, out var featureResult)) return featureResult != null;

            if (_outFeaturesActiving.TryGetValue(feature, out var featureResult2)) return featureResult2 != null;

            return false;
        }

        /// <summary>
        ///     Gets the sorting layer name of the specified feature controller canvas.
        /// </summary>
        /// <param name="controller">The feature controller instance.</param>
        /// <returns>The sorting layer name, or null if canvas is missing.</returns>
        public string GetSortingLayerFeature(FeatureBaseController controller)
        {
            var canvas = controller.GetComponent<Canvas>();
            if (canvas != null) return canvas.sortingLayerName;

            return null;
        }

        /// <summary>
        ///     Gets a dictionary of all active features.
        /// </summary>
        /// <returns>A dictionary containing active features and their GameObjects.</returns>
        public Dictionary<GameEnums.Features, GameObject> GetAllFeatureActivating()
        {
            return _featuresActiving;
        }

        /// <summary>
        ///     Closes the latest opened UI feature.
        /// </summary>
        /// <param name="closeWithBackKey">Whether this action was triggered by the hardware Back key.</param>
        public void CloseLastestUI(bool closeWithBackKey = false)
        {
            if (_featuresActiving.Count > 0)
            {
                var key = _featuresActiving.ElementAt(_featuresActiving.Count - 1).Key;
                _featuresActiving.TryGetValue(key, out var featureResult);
                if (featureResult != null)
                {
                    if (closeWithBackKey)
                    {
                        var result = featureResult.GetComponent<FeatureBaseController>().CloseWithBackKey();
                        if (result)
                        {
                            SetFeatureLatestClosed(key);
                            ActiveActionCloseFeature(key);

                            EventManager.EmitEvent(nameof(EventName.OnCloseFeature));
                            ActiveFeatureSequense();
                        }
                    }
                    else
                    {
                        featureResult.GetComponent<FeatureBaseController>().CloseMe();

                        SetFeatureLatestClosed(key);
                        ActiveActionCloseFeature(key);

                        EventManager.EmitEvent(nameof(EventName.OnCloseFeature));
                        ActiveFeatureSequense();
                    }
                }
            }
        }

        /// <summary>
        ///     Enqueues a feature into the sequence queue for a specific scene.
        /// </summary>
        /// <param name="scene">The scene context.</param>
        /// <param name="feature">The feature type to queue.</param>
        /// <param name="grp">The target UI group container.</param>
        public void AddFeatureSequense(GameEnums.Scenes scene, GameEnums.Features feature,
            UIGroupName grp = UIGroupName.Overlay_Container)
        {
            _featureSequense[scene].Enqueue((feature, grp));
        }

        /// <summary>
        ///     Checks whether a specific feature is currently queued in the scene sequence.
        /// </summary>
        /// <param name="scene">The scene context.</param>
        /// <param name="feature">The feature type to check.</param>
        /// <returns>True if the feature is queued; otherwise, false.</returns>
        public bool IsFeatureQueued(GameEnums.Scenes scene, GameEnums.Features feature)
        {
            return _featureSequense.TryGetValue(scene, out var queue) && queue.Any(x => x.feature == feature);
        }

        /// <summary>
        ///     Checks whether a specific feature is currently open.
        /// </summary>
        /// <param name="feature">The feature type to check.</param>
        /// <returns>True if the feature is open and not closed; otherwise, false.</returns>
        public bool IsFeatureOpen(GameEnums.Features feature)
        {
            if (_featuresActiving.TryGetValue(feature, out var go) && go != null)
            {
                var ctrl = go.GetComponent<FeatureBaseController>();
                return ctrl != null && !ctrl.isClose;
            }

            if (_outFeaturesActiving.TryGetValue(feature, out var go2) && go2 != null)
            {
                var ctrl = go2.GetComponent<FeatureBaseController>();
                return ctrl != null && !ctrl.isClose;
            }

            return false;
        }

        /// <summary>
        ///     Tries to start the queued feature sequence if no popup overlay/modal is currently open.
        ///     Ignores pre-loaded screen which are hidden (isClose = true).
        /// </summary>
        public void TryStartSequence()
        {
            foreach (var grp in new[] { UIGroupName.Overlay_Container, UIGroupName.Modal_Container })
            foreach (Transform child in _groupTransform[grp])
            {
                var ctrl = child.GetComponent<FeatureBaseController>();
                if (ctrl != null && !ctrl.isClose)
                    return;
            }

            ActiveFeatureSequense();
        }

        /// <summary>
        ///     Registers a callback action to execute when a specific feature is closed.
        /// </summary>
        /// <param name="feature">The feature type to observe.</param>
        /// <param name="action">The callback action to invoke.</param>
        public void AddActionCloseFeature(GameEnums.Features feature, UnityAction action)
        {
            if (action == null) return;

            if (!_actionCloseFeature.TryAdd(feature, action))
            {
                _actionCloseFeature[feature] -= action;
                _actionCloseFeature[feature] += action;
            }
        }

        /// <summary>
        ///     Enables or disables user interaction and Touch / EventSystem in the game.
        /// </summary>
        /// <param name="isEnable">True to enable touch; false to disable.</param>
        public static void EnableTouch(bool isEnable)
        {
            //Debug.Log("aaaaaaaaaaaaaaaaa" + isEnable);
            EventSystem.enabled = isEnable;
            IsEnableTouch = isEnable;
            EventManager.EmitEvent(isEnable ? nameof(EventName.OnEnableTouch) : nameof(EventName.OnDisableTouch));
        }

        /// <summary>
        ///     Gets the child count of the specified UI group container transform.
        /// </summary>
        /// <param name="uiGroupName">The UI group name.</param>
        /// <returns>The number of active children under the group transform.</returns>
        public int GetChildCountGroup(UIGroupName uiGroupName)
        {
            if (_groupTransform.ContainsKey(uiGroupName)) return _groupTransform[uiGroupName].childCount;

            return 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Sets the identifier of the latest feature that was closed.
        /// </summary>
        /// <param name="feature">The feature that was closed.</param>
        private void SetFeatureLatestClosed(GameEnums.Features feature)
        {
            _featuresLatestClosed = feature;
        }

        /// <summary>
        ///     Activates the next feature in the queue if the sequence is active.
        /// </summary>
        private void ActiveFeatureSequense()
        {
            // removed: BuildUpGoalManager.isBuilding guard + BuildUpGoalDone listener (gameplay removed)

            if (_featureSequense[_currentScene].Count <= 0)
                return;

            var (feature, grp) = _featureSequense[_currentScene].Dequeue();
            if (feature != GameEnums.Features.none) Show(feature, grp).Forget();
        }

        /// <summary>
        ///     Triggers registered callback actions for closing a feature and emits global events.
        /// </summary>
        /// <param name="feature">The feature type that closed.</param>
        private void ActiveActionCloseFeature(GameEnums.Features feature)
        {
            if (_actionCloseFeature.ContainsKey(feature))
            {
                _actionCloseFeature[feature]?.Invoke();
                _actionCloseFeature.Remove(feature);
            }

            CloseAnyFeatureAction?.Invoke();
        }

        /// <summary>
        ///     Unity Update method to handle Android Back/Escape key functionality.
        /// </summary>
        private void Update()
        {
            if (Application.platform == RuntimePlatform.Android || Application.isEditor)
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (IsShowMessageBox || IsShowLoading) return;

                    if (_featuresActiving.Count > 0) CloseLastestUI(true);
                }
        }

        #endregion
    }
}