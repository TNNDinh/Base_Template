using System.Collections;
using Ezg.Core.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Meta.HomeScene
{
    /// <summary>
    ///     Điều khiển logic 2 thanh scroll 2 bên ở home
    /// </summary>
    internal class HomeScreenFeatureScrollController : MonoBehaviour
    {
        #region Fields

        [SerializeField] [TabGroup("Setings")] [Required]
        private LayoutElement _scrollLayout;

        [SerializeField] [TabGroup("Setings")] [Required]
        private RectTransform _scrollContent;

        [SerializeField] [TabGroup("Setings")] [Required]
        private float _itemHeight;

        [SerializeField] [TabGroup("Setings")] [Required]
        private int _maxItem;

        [SerializeField] [TabGroup("Setings")] [Required]
        private GameObject _topArrow;

        [SerializeField] [TabGroup("Setings")] [Required]
        private GameObject _botArrow;

        /// <summary>
        ///     Số lượng item trong scroll enable
        /// </summary>
        private int _itemActivated;

        #endregion

        #region Functions

        private void Start()
        {
            this.DelayMethod(.2f, Setup);
            StartCoroutine(OnOffArrow());
        }

        private void Setup()
        {
            _scrollLayout.preferredHeight = _itemHeight * _maxItem;

            foreach (Transform a in _scrollContent.transform) _itemActivated += a.gameObject.activeSelf ? 1 : 0;

            if (_itemActivated <= _maxItem) _scrollLayout.preferredHeight = _itemHeight * _itemActivated;
        }

        private IEnumerator OnOffArrow()
        {
            var delayTime = new WaitForSeconds(1f);
            while (true)
            {
                _topArrow.SetActive(_scrollContent.localPosition.y > _itemHeight / 5 && _itemActivated > _maxItem);
                _botArrow.SetActive(
                    _scrollContent.localPosition.y < _itemHeight * _maxItem - _itemHeight * _maxItem / (5 * _maxItem) &&
                    _itemActivated > _maxItem);
                yield return delayTime;
            }
        }

        #endregion
    }
}