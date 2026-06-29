using System;
using System.Collections.Generic;
using DG.Tweening;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

public class MoneyBarSlider : MonoBehaviour
{
    public Slider slider;
    public Image icon;
    public Text value;
    public EnumBase.MoneyTypes moneyType;
    public float maxValue;

    [SerializeField] private bool isShowMaxValue = true;
    [SerializeField] private float countUpDuration = 0.5f;

    private readonly Queue<long> _pendingDeltas = new();
    protected Tween _countTween;

    protected bool _isInitialized;
    private long _targetAnimValue;

    public long DisplayValue { get; private set; }

    public bool HasPendingPresentation => _pendingDeltas.Count > 0 || IsTweenActive();

    protected virtual void OnEnable()
    {
        EventManager.StartListening(nameof(EventName.ResourceUiChanged), OnResourceUiChanged);

        if (!_isInitialized)
        {
            SyncFromWalletImmediate();
            return;
        }

        var wallet = PlayerResource.GetCurrencyValue(moneyType);
        if (wallet < DisplayValue)
            SyncFromWalletImmediate();
    }

    protected virtual void OnDisable()
    {
        EventManager.StopListening(nameof(EventName.ResourceUiChanged), OnResourceUiChanged);
        _pendingDeltas.Clear();
        KillActiveTween();
    }

    public void SetMaxValue(float max)
    {
        maxValue = max;
        SetData(DisplayValue);
    }

    public void EnqueueCatchUpToWallet()
    {
        if (moneyType == EnumBase.MoneyTypes.None)
            return;

        var delta = PlayerResource.GetCurrencyValue(moneyType) - DisplayValue;
        if (delta != 0)
            EnqueueDelta(delta);
        else
            TryDrainDeltaQueue();
    }

    protected void SyncFromWalletImmediate()
    {
        _pendingDeltas.Clear();
        KillActiveTween();

        if (moneyType == EnumBase.MoneyTypes.None)
            return;

        DisplayValue = PlayerResource.GetCurrencyValue(moneyType);
        _targetAnimValue = DisplayValue;
        _isInitialized = true;
        SetData(DisplayValue);
    }

    private void OnResourceUiChanged()
    {
        if (moneyType == EnumBase.MoneyTypes.None)
            return;

        var data = EventManager.GetData<OnCurrencyChangeEventData>(nameof(EventName.ResourceUiChanged));
        if (data.MoneyType != moneyType)
            return;

        if (data.Delta == 0)
            return;

        if (data.ApplyImmediate)
            ApplyDeltaImmediate(data.Delta);
        else
            EnqueueDelta(data.Delta);
    }

    private void EnqueueDelta(long delta)
    {
        if (delta == 0)
            return;

        if (!_isInitialized)
        {
            SyncFromWalletImmediate();
            return;
        }

        delta = ClampDeltaToWallet(delta);
        if (delta == 0)
        {
            TryDrainDeltaQueue();
            return;
        }

        var target = DisplayValue + delta;
        if (target == DisplayValue)
        {
            TryDrainDeltaQueue();
            return;
        }

        _pendingDeltas.Enqueue(target);
        TryDrainDeltaQueue();
    }

    private long ClampDeltaToWallet(long delta)
    {
        if (moneyType == EnumBase.MoneyTypes.None || !_isInitialized)
            return delta;

        var wallet = PlayerResource.GetCurrencyValue(moneyType);
        var maxDelta = wallet - DisplayValue;
        if (delta > 0)
            return Math.Min(delta, maxDelta);
        if (delta < 0)
            return Math.Max(delta, maxDelta);
        return 0;
    }

    private void ApplyDeltaImmediate(long delta)
    {
        if (!_isInitialized)
        {
            SyncFromWalletImmediate();
            return;
        }

        delta = ClampDeltaToWallet(delta);
        if (delta == 0)
            return;

        DisplayValue += delta;
        SetData(DisplayValue);

        if (IsTweenActive())
        {
            KillActiveTween();
            if (DisplayValue < _targetAnimValue)
                AnimateToValue(_targetAnimValue, TryDrainDeltaQueue);
            else
                TryDrainDeltaQueue();
        }
    }

    private void TryDrainDeltaQueue()
    {
        if (!isActiveAndEnabled || moneyType == EnumBase.MoneyTypes.None)
            return;

        if (IsTweenActive())
            return;

        if (_pendingDeltas.Count == 0)
        {
            ReconcileWithWalletIfNeeded();
            return;
        }

        if (!_isInitialized)
        {
            SyncFromWalletImmediate();
            return;
        }

        var target = _pendingDeltas.Dequeue();
        var delta = ClampDeltaToWallet(target - DisplayValue);
        if (delta == 0)
        {
            TryDrainDeltaQueue();
            return;
        }

        target = DisplayValue + delta;
        if (target == DisplayValue)
        {
            TryDrainDeltaQueue();
            return;
        }

        AnimateToValue(target, TryDrainDeltaQueue);
    }

    private void ReconcileWithWalletIfNeeded()
    {
        if (moneyType == EnumBase.MoneyTypes.None || !_isInitialized)
            return;

        var actual = PlayerResource.GetCurrencyValue(moneyType);
        if (DisplayValue == actual)
            return;

        AnimateToValue(actual, TryDrainDeltaQueue);
    }

    protected void KillActiveTween()
    {
        if (_countTween == null)
            return;

        var tween = _countTween;
        _countTween = null;

        if (tween.IsActive())
            tween.Kill();
    }

    private bool IsTweenActive()
    {
        return _countTween != null && _countTween.IsActive();
    }

    protected void AnimateToValue(long targetValue, Action onComplete = null)
    {
        if (!_isInitialized)
        {
            DisplayValue = targetValue;
            _targetAnimValue = targetValue;
            _isInitialized = true;
            SetData(DisplayValue);
            onComplete?.Invoke();
            return;
        }

        if (DisplayValue == targetValue)
        {
            SetData(DisplayValue);
            onComplete?.Invoke();
            return;
        }

        if (IsTweenActive() && _targetAnimValue == targetValue)
            return;

        KillActiveTween();
        _targetAnimValue = targetValue;

        _countTween = DOTween.To(
                () => DisplayValue,
                x =>
                {
                    DisplayValue = x;
                    SetData(DisplayValue);
                },
                targetValue,
                countUpDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                DisplayValue = targetValue;
                _countTween = null;
                onComplete?.Invoke();
            })
            .OnKill(() => { _countTween = null; });
    }

    private void SetData(long number)
    {
        slider.maxValue = maxValue;

        if (isShowMaxValue)
            value.text = $"{number.MoneyConvert()}/{maxValue}";
        else
            value.text = $"{number.MoneyConvert()}";

        if (icon != null)
            icon.sprite = PlayerResource.GetCurrencyImage(moneyType);

        slider.value = Mathf.Clamp(number, slider.minValue, slider.maxValue);
    }
}