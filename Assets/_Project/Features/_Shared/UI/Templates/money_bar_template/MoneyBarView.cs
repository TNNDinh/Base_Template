using System;
using System.Collections;
using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.Meta.HomeScene;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

public class MoneyBarView : MonoBehaviour
{
    public Image icon;
    public Text value;
    public EnumBase.MoneyTypes moneyType;
    [SerializeField] private bool haveActionOnClick = true;
    [SerializeField] private float countUpDuration = 0.5f;
    [SerializeField] private Transform iconPlus;

    private readonly Queue<long> _pendingDeltas = new();
    private UnityAction _clickActionCustom;
    private Tween _countTween;

    private long _displayValue;
    private bool _isInitialized;
    private long _targetValue;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public virtual void OnEnable()
    {
        EventManager.StartListening(nameof(EventName.ResourceUiChanged), OnResourceUiChanged);

        if (!_isInitialized)
        {
            SyncFromWalletImmediate();
            return;
        }

        var wallet = PlayerResource.GetCurrencyValue(moneyType);
        if (wallet < _displayValue)
            SyncFromWalletImmediate();
    }

    public virtual void OnDisable()
    {
        icon.transform.localScale = Vector3.one;
        EventManager.StopListening(nameof(EventName.ResourceUiChanged), OnResourceUiChanged);
        _pendingDeltas.Clear();
        KillActiveTween();
    }

    private void OnResourceUiChanged()
    {
        if (moneyType == EnumBase.MoneyTypes.None)
            return;

        var data = EventManager.GetData<OnCurrencyChangeEventData>(nameof(EventName.ResourceUiChanged));
        if (data.MoneyType != moneyType || data.Delta == 0)
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

        var target = _displayValue + delta;
        if (target == _displayValue)
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
        var maxDelta = wallet - _displayValue;
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

        _displayValue += delta;
        if (_displayValue < 0)
            _displayValue = 0;
        SetData(_displayValue);

        if (IsTweenActive())
        {
            KillActiveTween();
            if (_displayValue < _targetValue)
                AnimateToValue(_targetValue, TryDrainDeltaQueue);
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
        var delta = ClampDeltaToWallet(target - _displayValue);
        if (delta == 0)
        {
            TryDrainDeltaQueue();
            return;
        }

        target = _displayValue + delta;
        if (target == _displayValue)
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
        if (_displayValue == actual)
            return;

        AnimateToValue(actual, TryDrainDeltaQueue);
    }

    private void SyncFromWalletImmediate()
    {
        _pendingDeltas.Clear();
        KillActiveTween();

        if (moneyType == EnumBase.MoneyTypes.None)
            return;

        _displayValue = PlayerResource.GetCurrencyValue(moneyType);
        _targetValue = _displayValue;
        _isInitialized = true;
        SetData(_displayValue);
    }

    private void KillActiveTween()
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

    private void AnimateToValue(long targetValue, Action onComplete = null)
    {
        if (!_isInitialized)
        {
            _displayValue = targetValue;
            _targetValue = targetValue;
            _isInitialized = true;
            SetData(_displayValue);
            onComplete?.Invoke();
            return;
        }

        if (_displayValue == targetValue)
        {
            SetData(_displayValue);
            onComplete?.Invoke();
            return;
        }

        if (targetValue < _displayValue)
        {
            _targetValue = targetValue;
            KillActiveTween();
            _displayValue = targetValue;
            SetData(_displayValue);
            onComplete?.Invoke();
            return;
        }

        if (IsTweenActive() && _targetValue == targetValue)
            return;

        _targetValue = targetValue;
        KillActiveTween();

        _countTween = DOTween.To(
                () => _displayValue,
                x =>
                {
                    _displayValue = x;
                    SetData(_displayValue);
                },
                targetValue,
                countUpDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                _displayValue = targetValue;
                _countTween = null;
                onComplete?.Invoke();
            })
            .OnKill(() => _countTween = null);
    }

    public void ChangeMoneyType(EnumBase.MoneyTypes type)
    {
        moneyType = type;
        _isInitialized = false;
        SyncFromWalletImmediate();
    }

    public void ScaleIcon()
    {
        icon.transform.DOScale(Vector3.one * 1.4f, 0.1f)
            .OnComplete(() => { icon.transform.DOScale(Vector3.one, 0.1f); });
    }

    private void SetData(long number)
    {
        icon.gameObject.SetActive(false);
        StartCoroutine(RefreshTextSize(value, number.MoneyConvert()));
        icon.sprite = PlayerResource.GetCurrencyImage(moneyType);
        icon.gameObject.SetActive(true);
    }

    private IEnumerator RefreshTextSize(Text value, string text)
    {
        value.resizeTextForBestFit = true;
        value.text = text;
        yield return null;
    }

    public void SetOnClick(UnityAction action)
    {
        _clickActionCustom = action;
    }

    private void OnClick()
    {
        if (!haveActionOnClick) return;
        if (_clickActionCustom != null)
        {
            _clickActionCustom.Invoke();
            return;
        }

        switch (moneyType)
        {
            case EnumBase.MoneyTypes.Diamonds:
                if (!UIManager.Instance.IsFeatureActiving(GameEnums.Features.Shop))
                    HomeSceneManager.OpenShopAndSnapTo(8);
                break;
            case EnumBase.MoneyTypes.Gold:
                break;
            case EnumBase.MoneyTypes.Energy:
                if (PlayerResource.IsMaxEnergy())
                {
                    GameSystems.ShowSimpleMessage("energy_is_max");
                    return;
                }

                UIManager.Instance.Show(GameEnums.Features.HeartRefill).Forget();
                break;
        }
    }
}