using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.System.GameCheat
{
    /// <summary>
    ///     A UI component that works as a toggle button controlling a sliding menu.
    ///     Attach this to a Unity UI Button.
    /// </summary>
    /// <example>
    ///     Usage:
    ///     1. Create a Button in the UI.
    ///     2. Create a child RectTransform for the "Menu Container".
    ///     3. Add buttons/content inside the Menu Container.
    ///     4. Attach this script to the Button.
    ///     5. Assign the Menu Container to the 'Target Menu' field.
    ///     6. Configure Axis, Direction, and Duration in the Inspector.
    /// </example>
    [RequireComponent(typeof(Button))]
    public class CheatMenuController : MonoBehaviour
    {
        #region Public Methods

        /// <summary>
        ///     Toggles the menu expansion state.
        /// </summary>
        public void ToggleMenu()
        {
            if (_targetMenu == null) return;

            _isExpanded = !_isExpanded;
            var targetValue = _isExpanded ? 1f : 0f;

            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);

            _animationCoroutine = StartCoroutine(AnimateScale(targetValue));
        }

        #endregion

        #region Enums

        public enum AnimationAxis
        {
            X,
            Y
        }

        public enum ExpandDirection
        {
            LeftToRight,
            RightToLeft,
            TopToBottom,
            BottomToTop
        }

        #endregion

        #region Serialized Fields

        [Header("References")] [Tooltip("The menu container that will be scaled.")] [SerializeField]
        private RectTransform _targetMenu;

        [Header("Animation Settings")] [SerializeField]
        private AnimationAxis _scaleAxis = AnimationAxis.X;

        [SerializeField] private ExpandDirection _expandDirection = ExpandDirection.LeftToRight;
        [SerializeField] private float _animationDuration = 0.25f;

        #endregion

        #region Private Fields

        private Button _button;
        private bool _isExpanded;
        private Coroutine _animationCoroutine;
        private Vector3 _initialScale;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (_targetMenu == null)
            {
                Debug.LogWarning($"[CheatMenuController] Target Menu is not assigned on {gameObject.name}");
                return;
            }

            // Initialization
            _button.onClick.AddListener(ToggleMenu);
            UpdatePivot();

            // Start collapsed
            _targetMenu.localScale = GetTargetScale(0);
            _isExpanded = false;
        }

        private void OnValidate()
        {
            if (_targetMenu != null) UpdatePivot();
        }

        #endregion

        #region Internal Logic

        /// <summary>
        ///     Adjusts the pivot of the RectTransform based on the expansion direction.
        /// </summary>
        private void UpdatePivot()
        {
            if (_targetMenu == null) return;

            var pivot = _targetMenu.pivot;

            switch (_expandDirection)
            {
                case ExpandDirection.LeftToRight:
                    pivot.x = 0f;
                    break;
                case ExpandDirection.RightToLeft:
                    pivot.x = 1f;
                    break;
                case ExpandDirection.TopToBottom:
                    pivot.y = 1f;
                    break;
                case ExpandDirection.BottomToTop:
                    pivot.y = 0f;
                    break;
            }

            _targetMenu.pivot = pivot;
        }

        /// <summary>
        ///     Coroutine for smooth scale animation using Mathf.SmoothStep.
        /// </summary>
        private IEnumerator AnimateScale(float targetValue)
        {
            var startScale = _targetMenu.localScale;
            var targetScale = GetTargetScale(targetValue);
            var elapsedTime = 0f;

            // If duration is 0, snap to target
            if (_animationDuration <= 0)
            {
                _targetMenu.localScale = targetScale;
                yield break;
            }

            while (elapsedTime < _animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsedTime / _animationDuration);

                // Use SmoothStep for a nice ease-in-out effect
                var smoothT = Mathf.SmoothStep(0f, 1f, t);

                _targetMenu.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                yield return null;
            }

            _targetMenu.localScale = targetScale;
            _animationCoroutine = null;
        }

        /// <summary>
        ///     Calculates the scale vector based on the animation axis and input value.
        /// </summary>
        private Vector3 GetTargetScale(float value)
        {
            var scale = Vector3.one;
            if (_scaleAxis == AnimationAxis.X)
                scale.x = value;
            else
                scale.y = value;
            return scale;
        }

        #endregion
    }
}