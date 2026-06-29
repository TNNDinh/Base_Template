using UnityEngine;

namespace UnityScreenNavigator.Runtime.Core.Shared
{
    [CreateAssetMenu(menuName = "Screen Navigator/Simple Transition Animation")]
    public sealed class SimpleTransitionAnimationObject : TransitionAnimationObject
    {
        [Header("General Settings")]
        [SerializeField, Tooltip("Thời gian chờ trước khi chạy hiệu ứng")] private float _delay;
        [SerializeField, Tooltip("Thời gian kéo dài của hiệu ứng")] private float _duration = 0.3f;
        [SerializeField, Tooltip("Kiểu làm mượt hiệu ứng (Easing)")] private EaseType _easeType = EaseType.QuarticEaseOut;
        [Header("Before Animation")]
        [SerializeField, Tooltip("Vị trí bắt đầu")] private SheetAlignment _beforeAlignment = SheetAlignment.Center;
        [SerializeField, Tooltip("Tỷ lệ (Scale) bắt đầu")] private Vector3 _beforeScale = Vector3.one;
        [SerializeField, Tooltip("Độ mờ (Alpha) bắt đầu")] private float _beforeAlpha = 1.0f;
        [Header("After Animation")]
        [SerializeField, Tooltip("Vị trí kết thúc")] private SheetAlignment _afterAlignment = SheetAlignment.Center;
        [SerializeField, Tooltip("Tỷ lệ (Scale) kết thúc")] private Vector3 _afterScale = Vector3.one;
        [SerializeField, Tooltip("Độ mờ (Alpha) kết thúc")] private float _afterAlpha = 1.0f;

        private Vector3 _afterPosition;
        private Vector3 _beforePosition;
        private CanvasGroup _canvasGroup;

        public override float Duration => _duration;

        public static SimpleTransitionAnimationObject CreateInstance(float? duration = null, EaseType? easeType = null,
            SheetAlignment? beforeAlignment = null, Vector3? beforeScale = null, float? beforeAlpha = null,
            SheetAlignment? afterAlignment = null, Vector3? afterScale = null, float? afterAlpha = null)
        {
            var anim = CreateInstance<SimpleTransitionAnimationObject>();
            anim.SetParams(duration, easeType, beforeAlignment, beforeScale, beforeAlpha, afterAlignment, afterScale,
                afterAlpha);
            return anim;
        }

        public override void Setup()
        {
            _beforePosition = _beforeAlignment.ToPosition(RectTransform);
            _afterPosition = _afterAlignment.ToPosition(RectTransform);
            if (!RectTransform.gameObject.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup = RectTransform.gameObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup = canvasGroup;
        }

        public override void SetTime(float time)
        {
            time = Mathf.Max(0, time - _delay);
            var progress = _duration <= 0.0f ? 1.0f : Mathf.Clamp01(time / _duration);
            progress = Easings.Interpolate(progress, _easeType);
            var position = Vector3.Lerp(_beforePosition, _afterPosition, progress);
            var scale = Vector3.Lerp(_beforeScale, _afterScale, progress);
            var alpha = Mathf.Lerp(_beforeAlpha, _afterAlpha, progress);
            RectTransform.anchoredPosition = position;
            RectTransform.localScale = scale;
            _canvasGroup.alpha = alpha;
        }

        public void SetParams(float? duration = null, EaseType? easeType = null, SheetAlignment? beforeAlignment = null,
            Vector3? beforeScale = null, float? beforeAlpha = null, SheetAlignment? afterAlignment = null,
            Vector3? afterScale = null, float? afterAlpha = null)
        {
            if (duration.HasValue)
            {
                _duration = duration.Value;
            }

            if (easeType.HasValue)
            {
                _easeType = easeType.Value;
            }

            if (beforeAlignment.HasValue)
            {
                _beforeAlignment = beforeAlignment.Value;
            }

            if (beforeScale.HasValue)
            {
                _beforeScale = beforeScale.Value;
            }

            if (beforeAlpha.HasValue)
            {
                _beforeAlpha = beforeAlpha.Value;
            }

            if (afterAlignment.HasValue)
            {
                _afterAlignment = afterAlignment.Value;
            }

            if (afterScale.HasValue)
            {
                _afterScale = afterScale.Value;
            }

            if (afterAlpha.HasValue)
            {
                _afterAlpha = afterAlpha.Value;
            }
        }
    }
}