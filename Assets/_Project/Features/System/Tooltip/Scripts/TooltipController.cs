using System.Collections.Generic;
using Assets.Scripts._2.BUS.Features.Item;
using DG.Tweening;
using Ezg.Core.Extensions;
using Ezg.Core.UI;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.Tooltip
{
    public class TooltipController : TooltipBase
    {
        private void Start()
        {
            this.DelayRealTimeMethod(2.5f,
                () =>
                {
                    if (this != null) Destroy(gameObject);
                });
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.StartListening(EventName.ForceDestroyToolTip, ForceDestroy);
        }

        private void OnDisable()
        {
            EventManager.StopListening(EventName.ForceDestroyToolTip, ForceDestroy);
        }

        #region Fields

        private const float ItemTooltipHorizontalBuffer = 44f;
        private const float DelayedPlacementRefreshTime = 0.05f;

        [SerializeField] [TabGroup("Cấu hình")]
        private Text _content;

        [SerializeField] [TabGroup("Cấu hình")]
        private ItemPreviewController _itemPreview;

        #endregion

        #region Public Methods

        public void InitData(string data, TooltipAlignment align = TooltipAlignment.Mid,
            bool setPivot = true, ArrowPosition arrowPos = ArrowPosition.None,
            float customArrowOffsetX = 0f, float customArrowOffsetY = 0f, float delayDestroy = 2f)
        {
            if (setPivot)
                SetPivot(align);

            _content.text = WrapTextToScreenWidth(data, _content);
            _content.gameObject.SetActive(true);
            _itemPreview.gameObject.SetActive(false);

            FinalizeTooltip(arrowPos, customArrowOffsetX, customArrowOffsetY, delayDestroy);
        }

        public void InitData(List<Resource> data, TooltipAlignment align = TooltipAlignment.Mid,
            bool setPivot = true, ArrowPosition arrowPos = ArrowPosition.None,
            float customArrowOffsetX = 0f, float customArrowOffsetY = 0f, float delayDestroy = 2f)
        {
            if (setPivot)
                SetPivot(align);

            _content.gameObject.SetActive(false);
            _itemPreview.gameObject.SetActive(true);
            ConfigureItemPreviewGrid(data?.Count ?? 0);
            _itemPreview.InitData(data, useAnim: false);

            FinalizeTooltip(arrowPos, customArrowOffsetX, customArrowOffsetY, delayDestroy, true);
        }

        public void InitData(Resource[] data, TooltipAlignment align = TooltipAlignment.Mid,
            bool setPivot = true, ArrowPosition arrowPos = ArrowPosition.None,
            float customArrowOffsetX = 0f, float customArrowOffsetY = 0f, float delayDestroy = 2f,
            bool isDoingAnim = true)
        {
            if (setPivot)
                SetPivot(align);

            _content.gameObject.SetActive(false);
            _itemPreview.gameObject.SetActive(true);
            ConfigureItemPreviewGrid(data?.Length ?? 0);
            _itemPreview.InitData(data, useAnim: false);

            FinalizeTooltip(arrowPos, customArrowOffsetX, customArrowOffsetY, delayDestroy, true, isDoingAnim);
        }

        public void ForceDestroy()
        {
            if (this != null) Destroy(gameObject);
        }

        #endregion

        #region Private Methods

        private void ConfigureItemPreviewGrid(int itemCount)
        {
            if (_itemPreview == null || itemCount <= 0)
                return;

            var gridLayout = _itemPreview.GetComponent<UI_GridLayoutGroup>();
            var referenceRect = GetReferenceRectTransform();
            if (gridLayout == null || referenceRect == null)
                return;

            var layoutGroup = GetComponent<HorizontalOrVerticalLayoutGroup>();
            var tooltipHorizontalPadding = layoutGroup != null ? layoutGroup.padding.horizontal : 0f;
            var availableWidth = referenceRect.rect.width - tooltipHorizontalPadding -
                                 HorizontalSafePadding * 2f - ItemTooltipHorizontalBuffer;
            if (availableWidth <= 0f || gridLayout.cellSize.x <= 0f)
                return;

            var maxColumnCount = Mathf.Max(1,
                Mathf.FloorToInt((availableWidth + gridLayout.spacing.x) /
                                 (gridLayout.cellSize.x + gridLayout.spacing.x)));

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = Mathf.Clamp(maxColumnCount, 1, itemCount);
        }

        private void FinalizeTooltip(ArrowPosition arrowPos, float customArrowOffsetX, float customArrowOffsetY,
            float delayDestroy, bool refreshPlacementLater = false, bool isDoingAnim = true)
        {
            var initialAnchoredPos = _rectTransform.anchoredPosition;
            RefreshTooltipPlacement(arrowPos, customArrowOffsetX, customArrowOffsetY);
            if (isDoingAnim) PlaySpawnScaleAnimation();

            if (refreshPlacementLater)
                this.DelayRealTimeMethod(DelayedPlacementRefreshTime,
                    () =>
                    {
                        if (this == null) return;
                        _rectTransform.anchoredPosition = initialAnchoredPos;
                        RefreshTooltipPlacement(arrowPos, customArrowOffsetX, customArrowOffsetY);
                    });

            this.DelayRealTimeMethod(delayDestroy,
                () =>
                {
                    if (isDoingAnim)
                        transform.DOScaleX(0f, .3f).SetEase(Ease.InBack).OnComplete(ForceDestroy).SetUpdate(true);
                    else
                        ForceDestroy();
                });
        }

        private void RefreshTooltipPlacement(ArrowPosition arrowPos, float customArrowOffsetX, float customArrowOffsetY)
        {
            var itemPreviewRect = _itemPreview != null ? _itemPreview.transform as RectTransform : null;
            if (itemPreviewRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(itemPreviewRect);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

            var shift = ClampTooltipToScreen();
            SetArrowPosition(arrowPos, customArrowOffsetX - shift.x, customArrowOffsetY - shift.y);
        }

        #endregion
    }
}