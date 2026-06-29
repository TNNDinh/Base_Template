using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Shared.UI.Message
{
public class SimpleMessageSetPosController : MonoBehaviour
{
    [TabGroup("Cấu hình")] [SerializeField]
    private Text _messageText;

    [TabGroup("Cấu hình")] [SerializeField]
    private Image _background;

    private RectTransform _thisRectTransform;

    private void Awake()
    {
        _thisRectTransform = GetComponent<RectTransform>();
    }

    // private void OnEnable()
    // {
    //     _thisRectTransform.localPosition = new Vector3(0, 300, 0);
    //     transform.DOScale(Vector3.one, .3f).SetUpdate(true).SetEase(Ease.OutBack);
    //     _thisRectTransform.DOAnchorPosY(600f, 2f).SetUpdate(true).OnComplete(() =>
    //     {
    //         _messageText.DOFade(0f, .2f).SetUpdate(true);
    //         _background.DOFade(0f, .2f).SetUpdate(true);
    //     });
    // }

    private void OnDisable()
    {
        //_background.DOFade(.5f, 0f).SetUpdate(true);
        _thisRectTransform.localScale = Vector3.zero;
        _messageText.DOFade(1f, 0f).SetUpdate(true);
    }

    public void SetPos(Vector3 pos)
    {
        _thisRectTransform.localScale = Vector3.zero;
        //_thisRectTransform.localPosition = pos - new Vector3(0,100,0);
        transform.DOScale(Vector3.one * 0.7f, .7f).SetUpdate(true).SetEase(Ease.OutBack);
        var newPos = _thisRectTransform.localPosition + new Vector3(0, 50, 0);

        _thisRectTransform.DOAnchorPos(newPos, 2f).SetUpdate(true).OnComplete(() =>
        {
            _messageText.DOFade(0f, .2f).SetUpdate(true);
            //_background.DOFade(0f, .2f).SetUpdate(true);
        });


        // _thisRectTransform.DOAnchorPosY(200f, 2f).SetUpdate(true).OnComplete(() =>
        // {
        //     _messageText.DOFade(0f, .2f).SetUpdate(true);
        //     _background.DOFade(0f, .2f).SetUpdate(true);
        //     
        // });
    }

    public void InitData(string text, Vector3 pos)
    {
        _messageText.text = text;
        SetPos(pos);
    }

    public void InitTextLocalize(string key)
    {
        _messageText.text = GameSystems.Localize(key);
    }
}
}