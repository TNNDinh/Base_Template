using TigerForge;
using UnityEngine;

public class ObjCheatHideUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventName.CheatHideUIUA, HideUI);
        EventManager.StartListening(EventName.CheatShowUIUA, ShowUI);
        CheckCheat();
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventName.CheatShowUIUA, ShowUI);
        EventManager.StopListening(EventName.CheatHideUIUA, HideUI);
    }

    private void CheckCheat()
    {
        if (GameCheatManager.isHideCheatUI)
            HideUI();
        else
            ShowUI();
    }

    private void HideUI()
    {
        _canvasGroup.alpha = 0f;
    }

    private void ShowUI()
    {
        _canvasGroup.alpha = 1f;
    }
}