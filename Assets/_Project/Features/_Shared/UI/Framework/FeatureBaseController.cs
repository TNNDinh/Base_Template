using System;
using System.Collections.Generic;
using System.Linq;
using Ezg.Core.Utils;
using Ezg.Package.Audio;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

namespace BlackFace.Libraries.Modules.UIModule
{
    public class FeatureBaseController : MonoBehaviour
    {
        #region Fields

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Feature type")]
        protected GameEnums.Features FeatureType;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Main UI")]
        protected Transform MainUI;

        [ShowIf("@FeatureType != GameEnums.Features.none")] [SerializeField] [TabGroup("Cấu hình chung")]
        protected bool PushEventWhenOpen;

        [ShowIf("@FeatureType != GameEnums.Features.none")] [SerializeField] [TabGroup("Cấu hình chung")]
        protected bool PushEventWhenClose;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Click Background To Exit")]
        protected bool ClickBackgroundToExit;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Close Buttons")]
        protected Button[] _closeButtons;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("backgroundAlpha")]
        protected float _backgroundAlpha = .75f;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Scale time = 0 khi mở tính năng")]
        private bool _stopTimeWhenOpen;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Scale time = 1 khi đóng tính năng")]
        private bool _closeRestoreTime = true;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Đóng function với backey")]
        private bool _closeWithBackey = true;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Chạy sound đã config")]
        private bool _isPlaySoundTransition = true;

        [FormerlySerializedAs("ResourcePosList")] [SerializeField] [TabGroup("Cấu hình chung")]
        public List<ResourceMoneyPos> ResourceTargetPosList = new();

        [SerializeField] [TabGroup("Cấu hình chung")]
        public List<ResourceMoneyPos> ResourceStartPosList = new();

        [SerializeField] [TabGroup("Cấu hình chung")]
        private bool isNotActiveFeatureSequenseWhenClose;

        [FormerlySerializedAs("isCheatUI")] [SerializeField] [TabGroup("Cấu hình chung")]
        private bool showWhenCheat;

        protected Image MainBackground;
        protected Canvas ThisCanvas;
        protected CanvasGroup ThisCanvasGroup;

        private UITransition _transition;

        public bool isClose;

        #endregion

        #region Initialize

        protected virtual void Awake()
        {
            if (_stopTimeWhenOpen) Time.timeScale = 0;

            ThisCanvas = GetComponent<Canvas>();
            ResetSortingOrder();

            if (MainUI == null) MainUI = transform.GetChild(1);

            MainBackground = transform.GetChild(0).GetComponent<Image>();
            _transition = GetComponent<UITransition>();

            AnimOpenUI();

            if (ClickBackgroundToExit)
                transform.GetChild(0).GetComponent<Button>()?.onClick.AddListener(() => CloseMe());

            if (_closeButtons.Length > 0)
                foreach (var btn in _closeButtons.Where(x => x != null))
                    btn.onClick.AddListener(() => { CloseMe(); });

            isClose = false;
        }

        protected virtual void AnimOpenUI()
        {
            _transition?.PlayOpen(MainUI, MainBackground, ThisCanvasGroup, _backgroundAlpha,
                () => UIManager.EnableTouch(true));
        }

        protected virtual void AnimCloseUI(Action completeAction)
        {
            if (_transition != null)
            {
                _transition?.PlayClose(MainUI, MainBackground, ThisCanvasGroup, () =>
                {
                    UIManager.EnableTouch(true);
                    completeAction?.Invoke();
                });
            }
            else
            {
                UIManager.EnableTouch(true);
                completeAction?.Invoke();
            }
        }

        protected virtual void ResetSortingOrder()
        {
            if (ThisCanvas != null)
            {
                ThisCanvas.worldCamera = Camera.main;
                //ThisCanvas.sortingLayerName = nameof(EnumBase.LayerSorting.UI);
                ThisCanvas.sortingOrder = UIManager.Instance.GetCurrentLayer();
                ThisCanvasGroup = GetComponent<CanvasGroup>();
            }
        }

        /// <summary>
        ///     Gọi từ UIManager ngay sau SetParent để áp đúng sorting layer theo group.
        /// </summary>
        public void ApplySortingLayer(UIManager.UIGroupName groupName)
        {
            if (ThisCanvas == null) return;
            ThisCanvas.sortingLayerName = GetLayerSorting(groupName).ToString();
        }

        public void ApplySortingLayer(UIManager.UIGroupName groupName, int layerSorting)
        {
            if (ThisCanvas == null) return;
            ThisCanvas.sortingLayerName = GetLayerSorting(groupName).ToString();
            ThisCanvas.sortingOrder = layerSorting;
        }

        public UIManager.UIGroupName GetCurrentGroupName()
        {
            var layerSorting =
                (EnumBase.LayerSorting)Enum.Parse(typeof(EnumBase.LayerSorting), ThisCanvas.sortingLayerName);
            return GetGroupName(layerSorting);
        }

        private EnumBase.LayerSorting GetLayerSorting(UIManager.UIGroupName groupName)
        {
            switch (groupName)
            {
                case UIManager.UIGroupName.Main_Container:
                    return EnumBase.LayerSorting.Main;
                case UIManager.UIGroupName.Modal_Container:
                    return EnumBase.LayerSorting.Modal;
                case UIManager.UIGroupName.CurrencyBar_Container:
                    return EnumBase.LayerSorting.CurrencyBar;
                case UIManager.UIGroupName.Overlay_Container:
                    return EnumBase.LayerSorting.Overlays;
                case UIManager.UIGroupName.Tutorial_Container:
                    return EnumBase.LayerSorting.Tutorial;
                case UIManager.UIGroupName.Toast_Container:
                    return EnumBase.LayerSorting.Toast;
                default:
                    throw new ArgumentOutOfRangeException(nameof(groupName), groupName, null);
            }
        }

        private UIManager.UIGroupName GetGroupName(EnumBase.LayerSorting layerSorting)
        {
            switch (layerSorting)
            {
                case EnumBase.LayerSorting.Main:
                    return UIManager.UIGroupName.Main_Container;

                case EnumBase.LayerSorting.Modal:
                    return UIManager.UIGroupName.Modal_Container;

                case EnumBase.LayerSorting.CurrencyBar:
                    return UIManager.UIGroupName.CurrencyBar_Container;

                case EnumBase.LayerSorting.Overlays:
                    return UIManager.UIGroupName.Overlay_Container;

                case EnumBase.LayerSorting.Tutorial:
                    return UIManager.UIGroupName.Tutorial_Container;

                case EnumBase.LayerSorting.Toast:
                    return UIManager.UIGroupName.Toast_Container;

                default:
                    throw new ArgumentOutOfRangeException(nameof(layerSorting), layerSorting, null);
            }
        }

        protected virtual void OnEnable()
        {
            // removed: TutorialContainer.HideFinger (gameplay removed)
            if (_isPlaySoundTransition) AudioService.Default.PlaySound(DataManager.SoundConfig.OpenPopup);

            EventManager.StartListening(EventName.CheatHideUIUA, HideUICheat);
            EventManager.StartListening(EventName.CheatShowUIUA, ShowUICheat);
            CheckUICheat();
        }

        protected virtual void OnDisable()
        {
            EventManager.StopListening(EventName.CheatHideUIUA, HideUICheat);
            EventManager.StopListening(EventName.CheatShowUIUA, ShowUICheat);
        }

        protected virtual void Start()
        {
            LoadData();
            if (PushEventWhenOpen && FeatureType != GameEnums.Features.none)
                EventManager.EmitEvent(nameof(FeatureType));


            EventManager.EmitEvent(nameof(EventName.OnShowFeature));
        }

        protected virtual void LoadData()
        {
        }

        public virtual void LoadData(object data)
        {
        }

        #endregion

        #region Functions

        /// <summary>
        ///     Đóng tính năng
        /// </summary>
        public virtual void CloseMe(Action completeAction = null)
        {
            if (_isPlaySoundTransition) AudioService.Default.PlaySound(DataManager.SoundConfig.ClosePopup);

            isClose = true;

            UIManager.EnableTouch(false);
            AnimCloseUI(() =>
            {
                if (_closeRestoreTime) Time.timeScale = 1;
                UIManager.Instance.CloseFeature(FeatureType, !isNotActiveFeatureSequenseWhenClose);
                completeAction?.Invoke();
            });
        }

        public virtual bool CloseWithBackKey()
        {
            if (_closeWithBackey && !UIManager.IsShowLoading)
            {
                CloseMe();
                return true;
            }

            return false;
        }

        #region CheatUA

        private void HideUICheat()
        {
            if (!showWhenCheat) ChangeLayerUI(GameConstant.LayerUIHideByCheat);
        }

        private void ShowUICheat()
        {
            ChangeLayerUI(GameConstant.LayerUI);
        }

        private void ChangeLayerUI(string layerName)
        {
            gameObject.layer = LayerMask.NameToLayer(layerName);
        }

        private void CheckUICheat()
        {
            if (GameCheatManager.isHideCheatUI)
                HideUICheat();
            else
                ShowUICheat();
        }

        #endregion

        #endregion
    }
}