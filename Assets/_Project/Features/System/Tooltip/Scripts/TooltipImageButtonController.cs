using DG.Tweening;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ezg.Feature.System.Tooltip
{
    public class TooltipImageButtonController : TooltipBase
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.StartListening(EventName.ForceDestroyToolTipButton, ForceDestroy);
            EventManager.StartListening(EventName.SelectItem, ForceDestroy);
        }

        private void OnDisable()
        {
            EventManager.StopListening(EventName.ForceDestroyToolTipButton, ForceDestroy);
            EventManager.StartListening(EventName.SelectItem, ForceDestroy);
        }

        #region Fields

        [SerializeField] [TabGroup("Cấu hình")]
        private Image _icon;

        [SerializeField] [TabGroup("Cấu hình")]
        private Button _actionButton;

        public Button ActionBtn => _actionButton;

        #endregion

        // private void Start()
        // {
        //     this.DelayRealTimeMethod(2.5f,
        //         () =>
        //         {
        //             if (this != null) Destroy(gameObject);
        //         });
        // }

        #region Public Methods

        public void InitData(Sprite sprite, UnityAction onButtonClick,
            TooltipAlignment align = TooltipAlignment.Mid,
            bool setPivot = true, ArrowPosition arrowPos = ArrowPosition.None,
            float customArrowOffsetX = 0f, float customArrowOffsetY = 0f, float delayDestroy = 2f,
            bool isDoingAnimation = true)
        {
            if (setPivot)
                SetPivot(align);

            _icon.sprite = sprite;

            _actionButton.onClick.RemoveAllListeners();
            if (onButtonClick != null)
                _actionButton.onClick.AddListener(onButtonClick);
            _actionButton.onClick.AddListener(() =>
            {
                _actionButton.interactable = false;
                transform.DOScaleX(0f, .3f).SetEase(Ease.InBack).OnComplete(ForceDestroy).SetUpdate(true);
            });

            var horizontalShift = ClampTooltipHorizontal(false, 5f);
            var arrowX = arrowPos != ArrowPosition.None ? customArrowOffsetX - horizontalShift : customArrowOffsetX;
            SetArrowPosition(arrowPos, arrowX, customArrowOffsetY);
            if (isDoingAnimation) PlaySpawnScaleAnimation();

            // this.DelayRealTimeMethod(delayDestroy,
            //     () => transform.DOScaleX(0f, .3f).SetEase(Ease.InBack).OnComplete(ForceDestroy).SetUpdate(true));
        }

        public void ForceDestroy()
        {
            if (this != null) Destroy(gameObject);
        }

        #endregion
    }
}