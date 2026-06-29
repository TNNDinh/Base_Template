using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ezg.Feature.System.Rating
{
    public class RatingStarController : MonoBehaviour
    {
        public GameObject[] starImg;

        private int _index;

        private UnityAction _onClickAction;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        public void Init(int index, UnityAction onAction)
        {
            _index = index;
            _onClickAction = onAction;
        }

        public void SetState(bool isOn)
        {
            starImg[0].SetActive(!isOn);
            starImg[1].SetActive(isOn);
        }

        private void OnClick()
        {
            _onClickAction?.Invoke();
        }
    }
}