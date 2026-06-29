using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Shared;
using Ezg.Feature.Shared.GameData;

namespace BlackFace.Libraries.Modules.UIModule
{
    public class UITransition : MonoBehaviour
    {
        #region Fields

        [Title("Transition Animation")]
        [SerializeField]
        [Tooltip("ScriptableObject animation khi mở. Nếu null sẽ dùng DOTween mặc định.")]
        private TransitionAnimationObject _animIn;

        [SerializeField] [Tooltip("ScriptableObject animation khi đóng. Nếu null sẽ dùng DOTween mặc định.")]
        private TransitionAnimationObject _animOut;

        private readonly List<Coroutine> _animCoroutines = new();
        private Sequence _openSequence;

        // removed: DataManager.GameFeelConfig (gameplay removed) — default transition values
        private const float SCALE_START = 0.8f;
        private const float SCALE_END = 1f;
        private const float SCALE_OPEN_TIME = 0.3f;
        private const float SCALE_CLOSE_TIME = 0.2f;
        private const float FADE_START = 0f;
        private const float FADE_END = 1f;
        private const float FADE_OPEN_DURATION = 0.3f;
        private const float FADE_CLOSE_DURATION = 0.2f;
        private const Ease OPEN_TWEEN = Ease.OutBack;
        private const Ease CLOSE_TWEEN = Ease.InBack;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Plays the opening transition animation for the UI.
        /// </summary>
        /// <param name="mainUI">The main UI transform.</param>
        /// <param name="background">The background image.</param>
        /// <param name="canvasGroup">The canvas group for fading.</param>
        /// <param name="backgroundAlpha">The target alpha value for the background.</param>
        /// <param name="onComplete">Callback invoked when the animation completes.</param>
        public void PlayOpen(Transform mainUI, Image background, CanvasGroup canvasGroup, float backgroundAlpha,
            Action onComplete)
        {
            if (_animIn != null)
            {
                StopAllAnimCoroutines();
                background?.DOKill();

                //AnhNT fix: dùng startedCount thay childCount tránh onComplete ko gọi khi một số child ko có RectTransform
                var startedCount = 0;
                var completedCount = 0;
                var childCount = transform.childCount;
                for (var i = 0; i < childCount; i++)
                {
                    var child = transform.GetChild(i);
                    var rect = child as RectTransform ?? child.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        startedCount++;
                        var alphaOnly = i == 0;
                        _animCoroutines.Add(StartCoroutine(RunAnimation(_animIn, rect, alphaOnly, () =>
                        {
                            completedCount++;
                            if (completedCount >= startedCount)
                                onComplete?.Invoke();
                        })));
                    }
                }

                if (startedCount == 0) onComplete?.Invoke();
            }
            else
            {
                _openSequence?.Kill();
                mainUI.DOKill();
                _openSequence = DOTween.Sequence();

                mainUI.localScale = new Vector3(
                    SCALE_START,
                    SCALE_START,
                    SCALE_START);

                if (background != null)
                {
                    var bgColor = background.color;
                    bgColor.a = 0;
                    background.color = bgColor;
                }

                if (canvasGroup != null)
                    canvasGroup.alpha = FADE_START;

                _openSequence.Append(mainUI.DOScale(
                        SCALE_END,
                        SCALE_OPEN_TIME)
                    .SetEase(OPEN_TWEEN));

                if (background != null)
                    _openSequence.Join(background.DOFade(backgroundAlpha, 0.3f));

                if (canvasGroup != null)
                    _openSequence.Join(canvasGroup.DOFade(
                        FADE_END,
                        FADE_OPEN_DURATION));

                _openSequence.OnComplete(() => onComplete?.Invoke());
                _openSequence.SetUpdate(true);
                _openSequence.Play();
            }
        }

        /// <summary>
        ///     Plays the closing transition animation for the UI.
        /// </summary>
        /// <param name="mainUI">The main UI transform.</param>
        /// <param name="background">The background image.</param>
        /// <param name="canvasGroup">The canvas group for fading.</param>
        /// <param name="onComplete">Callback invoked when the animation completes.</param>
        public void PlayClose(Transform mainUI, Image background, CanvasGroup canvasGroup, Action onComplete)
        {
            if (_animOut != null)
            {
                StopAllAnimCoroutines();
                background?.DOKill();

                //AnhNT fix: cũng như trên
                var startedCount = 0;
                var completedCount = 0;
                var childCount = transform.childCount;
                for (var i = 0; i < childCount; i++)
                {
                    var child = transform.GetChild(i);
                    var rect = child as RectTransform ?? child.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        startedCount++;
                        var alphaOnly = i == 0;
                        _animCoroutines.Add(StartCoroutine(RunAnimation(_animOut, rect, alphaOnly, () =>
                        {
                            completedCount++;
                            if (completedCount >= startedCount)
                                onComplete?.Invoke();
                        })));
                    }
                }

                if (startedCount == 0) onComplete?.Invoke();
            }
            else
            {
                //AnhNT fix: stop open coroutines trước khi chạy close để tránh race condition khi gọi CloseMe lúc open animation (_animIn != null) vẫn đang chạy
                StopAllAnimCoroutines();
                _openSequence?.Kill();
                mainUI.DOKill();

                //AnhNT fix: chuyển OnComplete sang mainUI.DOScale thay vì background?.DOFade tránh onComplete ko gọi khi background == null
                canvasGroup?.DOFade(FADE_START,
                    FADE_CLOSE_DURATION).SetUpdate(true);
                background?.DOFade(FADE_START, FADE_CLOSE_DURATION)
                    .SetUpdate(true);
                mainUI.DOScale(SCALE_START, SCALE_CLOSE_TIME)
                    .SetEase(CLOSE_TWEEN)
                    .SetUpdate(true)
                    .OnComplete(() => onComplete?.Invoke());
            }
        }

        /// <summary>
        ///     Kills any active transition animations and coroutines.
        /// </summary>
        public void Kill()
        {
            _openSequence?.Kill();
            StopAllAnimCoroutines();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Stops all running transition coroutines.
        /// </summary>
        private void StopAllAnimCoroutines()
        {
            foreach (var coroutine in _animCoroutines)
                if (coroutine != null)
                    StopCoroutine(coroutine);
            _animCoroutines.Clear();
        }

        /// <summary>
        ///     Coroutine that runs a transition animation object on a UI element.
        /// </summary>
        /// <param name="anim">The transition animation object.</param>
        /// <param name="mainUI">The UI element's transform.</param>
        /// <param name="alphaOnly">If true, position and scale changes are ignored, and only alpha is updated.</param>
        /// <param name="onComplete">Callback invoked when the animation completes.</param>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator RunAnimation(TransitionAnimationObject anim, Transform mainUI, bool alphaOnly,
            Action onComplete)
        {
            var animInstance = Instantiate(anim);
            var rectTransform = mainUI as RectTransform ?? mainUI.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var originalPos = rectTransform.anchoredPosition;
            var originalScale = rectTransform.localScale;

            ((ITransitionAnimation)animInstance).Setup(rectTransform);

            var duration = animInstance.Duration;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                //AnhNT fix: rectTransform có thể bị destroy trong lúc anim chạy, thoát sớm và vẫn gọi onComplete để chain destroy không bị kẹt
                if (rectTransform == null)
                {
                    Destroy(animInstance);
                    onComplete?.Invoke();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                animInstance.SetTime(elapsed);
                if (alphaOnly)
                {
                    rectTransform.anchoredPosition = originalPos;
                    rectTransform.localScale = originalScale;
                }

                yield return null;
            }

            if (rectTransform != null)
            {
                animInstance.SetTime(duration);
                if (alphaOnly)
                {
                    rectTransform.anchoredPosition = originalPos;
                    rectTransform.localScale = originalScale;
                }
            }

            Destroy(animInstance);
            onComplete?.Invoke();
        }

        #endregion
    }
}