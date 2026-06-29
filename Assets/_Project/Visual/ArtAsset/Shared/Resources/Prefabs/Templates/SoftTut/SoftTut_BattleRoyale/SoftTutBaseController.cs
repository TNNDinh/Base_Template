using BlackFace.Libraries.Modules.UIModule;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Core.UI
{
    public class SoftTutBaseController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private Button _closeButton;

        [SerializeField] [TabGroup("Cấu hình")]
        private float _backgroundAlpha;

        private Image _background;
        private CanvasGroup _thisCanvasGrp;

        private UITransition _transition;

        private void Awake()
        {
            _thisCanvasGrp = GetComponent<CanvasGroup>();
            _transition = GetComponent<UITransition>();

            if (transform.childCount > 0)
                _background = transform.GetChild(0).GetComponent<Image>();

            _closeButton.onClick.AddListener(OnClose);
        }

        private void OnEnable()
        {
            _transition?.PlayOpen(transform, _background, _thisCanvasGrp, _backgroundAlpha, null);
        }

        private void OnClose()
        {
            _transition?.PlayClose(transform, _background, _thisCanvasGrp, () => gameObject.SetActive(false));
        }
    }
}