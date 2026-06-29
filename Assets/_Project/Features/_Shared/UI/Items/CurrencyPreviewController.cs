using System;
using System.Collections.Generic;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.Monetization.Shop;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Assets.Scripts._2.BUS.Features.Item
{
    public class CurrencyPreviewController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private Image _icon;

        [SerializeField] [TabGroup("Cấu hình")]
        private Image _backgroundImage;

        [SerializeField] [TabGroup("Cấu hình")]
        private Text _value;

        [SerializeField] [TabGroup("Cấu hình")]
        private CurrencyPreviewController _currencyTemplate;

        public void InitData(EnumBase.MoneyTypes type, long value, bool useMoneyConvert = false,
            UnityAction action = null, bool isValidResource = false, bool showRemaining = false)
        {
            if (type == EnumBase.MoneyTypes.None)
            {
                _icon.gameObject.SetActive(false);
                _value.text = GameSystems.Localize("free");
            }
            else if (type == EnumBase.MoneyTypes.Ads)
            {
                _icon.sprite = PlayerResource.GetCurrencyImage(type);
                if (!_icon.TryGetComponent<RemoveAdsImageView>(out _))
                    _icon.gameObject.AddComponent<RemoveAdsImageView>();
                _value.text = GameSystems.Localize("free");
            }
            else
            {
                _icon.gameObject.SetActive(true);
                _icon.sprite = PlayerResource.GetCurrencyImage(type);
                _value.text =
                    (showRemaining
                        ? (useMoneyConvert
                            ? PlayerResource.GetMoneyQuantity(type).MoneyConvert()
                            : PlayerResource.GetMoneyQuantity(type).ToString("n0")) + "/"
                        : "") + (isValidResource ? value.ValidResource(type, useMoneyConvert) :
                        useMoneyConvert ? value.MoneyConvert() : value.ToString("n0"));

                SetRarity(type, value);
            }

            if (action != null) GetComponent<Button>().onClick.AddListener(action);
        }

        public void InitData(List<Resource> res, bool useMoneyConvert = false, bool isValidResource = false)
        {
            if (_currencyTemplate == null) return;

            foreach (Transform trans in transform) Destroy(trans.gameObject);

            res.ForEach(x =>
            {
                Instantiate(_currencyTemplate, transform)
                    .InitData((EnumBase.MoneyTypes)x.resId, x.resNumber, useMoneyConvert,
                        isValidResource: isValidResource);
            });
        }

        public void InitData(Resource res, bool useMoneyConvert = false, bool isValidResource = false)
        {
            if (_currencyTemplate == null) return;

            foreach (Transform trans in transform) Destroy(trans.gameObject);

            Instantiate(_currencyTemplate, transform)
                .InitData((EnumBase.MoneyTypes)res.resId, res.resNumber, useMoneyConvert,
                    isValidResource: isValidResource);
        }

        public void InitData(EnumBase.MoneyTypes type, string value)
        {
            if (type == EnumBase.MoneyTypes.None)
            {
                _icon.gameObject.SetActive(false);
                _value.text = string.IsNullOrEmpty(value) ? GameSystems.Localize("free") : value;
            }
            else
            {
                _icon.gameObject.SetActive(type != EnumBase.MoneyTypes.Cash);
                _icon.sprite = PlayerResource.GetCurrencyImage(type);
                _value.text = value;
                if (type != EnumBase.MoneyTypes.Ads && type != EnumBase.MoneyTypes.Cash)
                    SetRarity(type, Convert.ToInt64(value));
            }
        }

        public void InitData(string value, UnityAction action = null)
        {
            _icon.gameObject.SetActive(false);
            _value.text = value;
            if (action != null) GetComponent<Button>().onClick.AddListener(action);
        }

        private void SetRarity(EnumBase.MoneyTypes type, long value)
        {
            if (_backgroundImage == null) return;

            _backgroundImage.gameObject.SetActive(true);
            //_backgroundImage.sprite = PlayerResource.GetCurrencyBackground(DataManager.RarityBackground.GetRarity(type, value));
        }
    }
}