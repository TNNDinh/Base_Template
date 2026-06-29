using System;
using System.Collections;
using Ezg.Core.Utils;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

public class UITimeCountdown : MonoBehaviour
{
    [SerializeField] private Text textTime;
    private string _localizeComplete;
    private string _localizeShow;
    private long _time = -1;
    private Coroutine countTimeRoutine;

    private bool isSetTime;

    public Action OnComplete;
    private float secondRemain;

    private void OnEnable()
    {
        StartCountdown();
    }

    private void OnDisable()
    {
        _time = -1;
        isSetTime = false;
        Dispose();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) StartCountdown();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) StartCountdown();
    }

    public void SetInfo(long timeSave, string localizeComplete, Action callback = null)
    {
        _time = timeSave;
        _localizeComplete = localizeComplete;
        OnComplete = callback;


        if (gameObject.activeSelf) CheckRemainTimeDelayClaim();
    }

    public void SetInfoShow(long timeSave, string localizeShow, Action callback = null)
    {
        _time = timeSave;
        _localizeShow = localizeShow;
        OnComplete = callback;

        if (gameObject.activeSelf) CheckRemainTimeDelayClaim();
    }

    public void Dispose()
    {
        if (countTimeRoutine != null) StopCoroutine(countTimeRoutine);
    }

    private void StartCountdown()
    {
        if (_time != -1) CheckRemainTimeDelayClaim();
    }

    private void CheckRemainTimeDelayClaim()
    {
        if (isSetTime) return;
        isSetTime = true;

        secondRemain = TimeManager.GetTotalSecondRemain(_time);
        if (secondRemain > 0)
        {
            countTimeRoutine = StartCoroutine(SetStateDelayAds());
            SetTimeDelayWatchAds();
        }
        else
        {
            OnComplete?.Invoke();
            isSetTime = false;
            textTime.text = _localizeComplete;
        }
    }

    private IEnumerator SetStateDelayAds()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);
            DelayWatchAds();
        }
    }

    private void DelayWatchAds()
    {
        --secondRemain;
        SetTimeDelayWatchAds();
        if (secondRemain < 0)
        {
            Dispose();

            textTime.text = _localizeComplete;
            OnComplete?.Invoke();
            isSetTime = false;
        }
    }

    private void SetTimeDelayWatchAds()
    {
        var time = TimeSpan.FromSeconds(secondRemain);
        var varTime = secondRemain > GameConstant.SECOND_ONE_DAY
            ? $"{time.Days}d {time.Hours}h"
            : time.ToString(@"hh\:mm\:ss");

        if (string.IsNullOrEmpty(_localizeShow))
            textTime.text = $"<color=#9bff00>{varTime}</color>";
        else
            textTime.text = string.Format(_localizeShow, $"<color=#9bff00>{varTime}</color>");
    }
}