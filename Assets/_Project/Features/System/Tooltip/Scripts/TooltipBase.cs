using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.System.Tooltip
{
    public abstract class TooltipBase : MonoBehaviour
    {
        #region Fields

        [SerializeField] [TabGroup("Arrows")] protected RectTransform _arrowBot;

        [SerializeField] [TabGroup("Arrows")] protected RectTransform _arrowTop;

        [SerializeField] [TabGroup("Arrows")] protected RectTransform _arrowLeft;

        [SerializeField] [TabGroup("Arrows")] protected RectTransform _arrowRight;

        protected RectTransform _rectTransform;

        protected const float ArrowHorizontalOffset = 40f;
        protected const float ArrowVerticalOffset = 40f;
        protected const float HorizontalSafePadding = 30f;
        protected const float VerticalSafePadding = 16f;

        #endregion

        #region Enums

        public enum TooltipAlignment
        {
            TopLeft,
            TopMid,
            TopRight,
            Left,
            Mid,
            Right,
            BotLeft,
            BotMid,
            BotRight
        }

        public enum ArrowPosition
        {
            None,
            BotLeft,
            Bot,
            BotRight,
            TopLeft,
            Top,
            TopRight,
            Left,
            Right
        }

        #endregion

        #region Initialize

        protected virtual void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            foreach (var dotweenAnim in GetComponents<DOTweenAnimation>())
                dotweenAnim.DOKill();
        }

        protected virtual void OnEnable()
        {
        }

        #endregion

        #region Protected Methods

        protected void SetPivot(TooltipAlignment align)
        {
            _rectTransform.pivot = align switch
            {
                TooltipAlignment.TopLeft => new Vector2(0, 0),
                TooltipAlignment.TopMid => new Vector2(.5f, 0),
                TooltipAlignment.TopRight => new Vector2(1, 0),
                TooltipAlignment.Left => new Vector2(1, .5f),
                TooltipAlignment.Mid => new Vector2(.5f, .5f),
                TooltipAlignment.Right => new Vector2(0, .5f),
                TooltipAlignment.BotLeft => new Vector2(1, 1),
                TooltipAlignment.BotMid => new Vector2(.5f, 1),
                TooltipAlignment.BotRight => new Vector2(0, 1),
                _ => _rectTransform.pivot
            };
        }

        protected void SetArrowPosition(ArrowPosition arrowPos, float customOffsetX = 0f, float customOffsetY = 0f)
        {
            _arrowBot?.gameObject.SetActive(false);
            _arrowTop?.gameObject.SetActive(false);
            _arrowLeft?.gameObject.SetActive(false);
            _arrowRight?.gameObject.SetActive(false);

            if (arrowPos == ArrowPosition.None)
                return;

            var (arrow, anchor, offsetX, offsetY) = arrowPos switch
            {
                ArrowPosition.BotLeft => (_arrowBot, new Vector2(0, 0), ArrowHorizontalOffset, 0f),
                ArrowPosition.Bot => (_arrowBot, new Vector2(0.5f, 0), 0f, 0f),
                ArrowPosition.BotRight => (_arrowBot, new Vector2(1, 0), -ArrowHorizontalOffset, 0f),
                ArrowPosition.TopLeft => (_arrowTop, new Vector2(0, 1), ArrowHorizontalOffset, 0f),
                ArrowPosition.Top => (_arrowTop, new Vector2(0.5f, 1), 0f, 0f),
                ArrowPosition.TopRight => (_arrowTop, new Vector2(1, 1), -ArrowHorizontalOffset, 0f),
                ArrowPosition.Left => (_arrowLeft, new Vector2(0, 0.5f), 0f, ArrowVerticalOffset),
                ArrowPosition.Right => (_arrowRight, new Vector2(1, 0.5f), 0f, -ArrowVerticalOffset),
                _ => (null, Vector2.zero, 0f, 0f)
            };

            if (arrow == null)
                return;

            arrow.gameObject.SetActive(true);

            arrow.anchorMin = anchor;
            arrow.anchorMax = anchor;
            arrow.anchoredPosition = new Vector2(offsetX + customOffsetX, offsetY + customOffsetY);
        }

        protected void PlaySpawnScaleAnimation()
        {
            transform.DOKill();
            transform.localScale = new Vector3(0f, 1f, 1f);
            transform.DOScaleX(1f, .3f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        protected RectTransform GetReferenceRectTransform()
        {
            var referenceCanvas = _rectTransform != null && _rectTransform.parent != null
                ? _rectTransform.parent.GetComponentInParent<Canvas>()
                : null;
            return referenceCanvas != null
                ? referenceCanvas.transform as RectTransform
                : _rectTransform != null
                    ? _rectTransform.parent as RectTransform
                    : null;
        }

        protected Vector2 ClampTooltipToScreen()
        {
            var referenceRect = GetReferenceRectTransform();
            if (_rectTransform == null || referenceRect == null)
                return Vector2.zero;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            Canvas.ForceUpdateCanvases();

            var tooltipBounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(referenceRect, _rectTransform);
            var referenceRectBounds = referenceRect.rect;
            var minX = referenceRectBounds.xMin + HorizontalSafePadding;
            var maxX = referenceRectBounds.xMax - HorizontalSafePadding;
            var minY = referenceRectBounds.yMin + VerticalSafePadding;
            var maxY = referenceRectBounds.yMax - VerticalSafePadding;

            var shift = Vector2.zero;
            if (tooltipBounds.min.x < minX)
                shift.x = minX - tooltipBounds.min.x;
            else if (tooltipBounds.max.x > maxX)
                shift.x = maxX - tooltipBounds.max.x;

            if (tooltipBounds.min.y < minY)
                shift.y = minY - tooltipBounds.min.y;
            else if (tooltipBounds.max.y > maxY)
                shift.y = maxY - tooltipBounds.max.y;

            if (!Mathf.Approximately(shift.x, 0f) || !Mathf.Approximately(shift.y, 0f))
            {
                var anchoredPosition = _rectTransform.anchoredPosition;
                anchoredPosition += shift;
                _rectTransform.anchoredPosition = anchoredPosition;
            }

            return shift;
        }

        protected float ClampTooltipHorizontal(bool keepArrowsDuringClamp = true, float padding = HorizontalSafePadding)
        {
            if (keepArrowsDuringClamp)
                return ClampTooltipToScreen().x;

            var referenceRect = GetReferenceRectTransform();
            if (_rectTransform == null || referenceRect == null)
                return 0f;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            Canvas.ForceUpdateCanvases();

            var corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);
            var worldToLocal = referenceRect.worldToLocalMatrix;
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            foreach (var c in corners)
            {
                var lx = worldToLocal.MultiplyPoint3x4(c).x;
                if (lx < minX) minX = lx;
                if (lx > maxX) maxX = lx;
            }

            var allowedMin = referenceRect.rect.xMin + padding;
            var allowedMax = referenceRect.rect.xMax - padding;

            var shiftX = 0f;
            if (minX < allowedMin) shiftX = allowedMin - minX;
            else if (maxX > allowedMax) shiftX = allowedMax - maxX;

            if (!Mathf.Approximately(shiftX, 0f))
                _rectTransform.anchoredPosition += new Vector2(shiftX, 0f);

            return shiftX;
        }

        protected string WrapTextToScreenWidth(string data, Text textComponent)
        {
            if (string.IsNullOrEmpty(data) || textComponent == null)
                return data;

            var maxWidth = Screen.width * 0.9f;
            var textGen = new TextGenerator();
            var settings = textComponent.GetGenerationSettings(new Vector2(float.MaxValue, 0));

            var words = data.Split(' ');
            var result = "";
            var currentLine = "";

            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var width = textGen.GetPreferredWidth(testLine, settings);

                if (width > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    result += currentLine + "\n";
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
                result += currentLine;

            return result;
        }

        #endregion
    }
}