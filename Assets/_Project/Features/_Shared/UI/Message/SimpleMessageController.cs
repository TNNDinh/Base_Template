using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Shared.UI.Message
{
    public class SimpleMessageController : MonoBehaviour
    {
        [TabGroup("Cấu hình")] [SerializeField]
        private Text _messageText;

        [TabGroup("Cấu hình")] [SerializeField]
        private Image _background;

        private RectTransform _thisRectTransform;

        private void Awake()
        {
            _thisRectTransform = GetComponent<RectTransform>();
            _thisRectTransform.localScale = Vector3.zero;
        }

        private void OnEnable()
        {
            _thisRectTransform.localPosition = new Vector3(0, 300, 0);
            transform.DOScale(Vector3.one, .3f).SetUpdate(true).SetEase(Ease.OutBack);
            _thisRectTransform.DOAnchorPosY(600f, 2f).SetUpdate(true).OnComplete(() =>
            {
                _messageText.DOFade(0f, .2f).SetUpdate(true);
                // _background.DOFade(0f, .2f).SetUpdate(true);
            });
        }

        private void OnDisable()
        {
            //_background.DOFade(.5f, 0f).SetUpdate(true);
            _thisRectTransform.localScale = Vector3.zero;
            _messageText.DOFade(1f, 0f).SetUpdate(true);
        }

        public void InitData(string text)
        {
            _messageText.text = text;
        }

        public void InitTextLocalize(string key)
        {
            _messageText.text = GameSystems.Localize(key);
        }
    }
}