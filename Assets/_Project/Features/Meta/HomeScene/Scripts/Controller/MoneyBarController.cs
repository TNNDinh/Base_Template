using DG.Tweening;
using TigerForge;
using UnityEngine;

public class MoneyBarController : MonoBehaviour
{
    [SerializeField] private Transform diamondBar;
    [SerializeField] private Transform skipTimeBar;

    [SerializeField] private Vector3 _vector3StartPos;
    [SerializeField] private float timeMove = 0.3f;

    private Tween _tweenDiamondBar;
    private int moneyType;

    private void Start()
    {
        moneyType = 2;
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventName.ChangeMoneyBar, ChangeMoneyBar);
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventName.ChangeMoneyBar, ChangeMoneyBar);
    }

    private void ChangeMoneyBar()
    {
        var money = EventManager.GetInt(EventName.ChangeMoneyBar);
        if (moneyType == money) return;
        moneyType = money;
        if (_tweenDiamondBar != null) _tweenDiamondBar.Kill();
        switch (money)
        {
            case 2:
                skipTimeBar.gameObject.SetActive(false);
                diamondBar.gameObject.SetActive(true);
                var rect = diamondBar.GetComponent<RectTransform>();
                rect.anchoredPosition = _vector3StartPos;
                _tweenDiamondBar = rect.DOAnchorPos(Vector3.zero, timeMove);
                break;
            case 6:
                diamondBar.gameObject.SetActive(false);
                skipTimeBar.gameObject.SetActive(true);
                var rect1 = skipTimeBar.GetComponent<RectTransform>();
                rect1.anchoredPosition = _vector3StartPos;
                _tweenDiamondBar = rect1.DOAnchorPos(Vector3.zero, timeMove);
                break;
        }
    }
}