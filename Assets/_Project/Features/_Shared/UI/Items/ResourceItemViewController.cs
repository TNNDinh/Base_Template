using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Assets.Scripts._2.BUS.Features.Misc
{
    internal class ResourceItemViewController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private EnumBase.MoneyTypes _currencyType;

        [SerializeField] [TabGroup("Cấu hình")]
        private Image _icon;

        [SerializeField] [TabGroup("Cấu hình")]
        private Text _value;

        private Button _thisButton;

        private void Awake()
        {
            EventManager.StartListening(nameof(EventName.UpdateResource), UpdateUI);
            SetupButton();
            UpdateUI();
        }

        private void OnDestroy()
        {
            EventManager.StopListening(nameof(EventName.UpdateResource), UpdateUI);
        }

        private void SetupButton()
        {
            _thisButton = GetComponent<Button>();
            _thisButton.onClick.AddListener(OnClick);
        }

        private void UpdateUI()
        {
            if (_currencyType != EnumBase.MoneyTypes.None)
            {
                _icon.sprite = PlayerResource.GetCurrencyImage(_currencyType);
                _value.text = PlayerResource.GetCurrencyValue(_currencyType).MoneyConvert();
            }
        }

        private void OnClick()
        {
            GameSystems.ShowSimpleMessage("comming_soon");
        }
    }
}