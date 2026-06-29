using System.Collections;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

namespace _Project.Visual.ArtAsset.UI.Prefab.Money_Bar_Template
{
    public class RestoreCurrencyController : MoneyBarSlider
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private GameObject _popup;

        [SerializeField] [TabGroup("Cấu hình")]
        private Text _energyCooldown;

        [SerializeField] [TabGroup("Cấu hình")]
        private GameObject _unLimitObjects;

        private IEnumerator _coroutine;

        private void Start()
        {
            GetComponent<Button>()?.onClick.AddListener(() =>
            {
                // removed: BuyCurrencyProperty (gameplay removed)
            });
            EventManager.StartListening(nameof(EventName.ResourceUiChanged), CheckShowPopupOnResourceChange);
            SetMaxValue(PlayerResource.GetLimitEnergy());
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.StartListening(EventName.OnUnlimitedEnergy, OnUnlimitedEnergy);
            UpdateUI();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.StopListening(EventName.OnUnlimitedEnergy, OnUnlimitedEnergy);
        }

        private void CheckShowPopupOnResourceChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            var data = EventManager.GetData<OnCurrencyChangeEventData>(nameof(EventName.ResourceUiChanged));
            if (data.MoneyType != moneyType) return;

            if (_coroutine != null)
                StopCoroutine(_coroutine);

            var shouldShowPopup = !PlayerResource.IsMaxEnergy() || PlayerResource.IsInfinityEnergy();
            _popup?.gameObject.SetActive(shouldShowPopup);
            if (shouldShowPopup)
            {
                _coroutine = ShowEnergyText();
                StartCoroutine(_coroutine);
            }

            _unLimitObjects.SetActive(PlayerResource.IsInfinityEnergy());
        }

        private void UpdateUI()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (_coroutine != null)
                StopCoroutine(_coroutine);

            var shouldShowPopup = !PlayerResource.IsMaxEnergy() || PlayerResource.IsInfinityEnergy();
            _popup?.gameObject.SetActive(shouldShowPopup);
            if (shouldShowPopup)
            {
                _coroutine = ShowEnergyText(); // Tạo mới mỗi lần, không dùng lại instance cũ
                StartCoroutine(_coroutine);
            }

            OnUnlimitedEnergy();
        }

        private void OnUnlimitedEnergy()
        {
            _unLimitObjects.SetActive(PlayerResource.IsInfinityEnergy());
            value.gameObject.SetActive(!_unLimitObjects.activeInHierarchy);
        }

        private IEnumerator ShowEnergyText()
        {
            var delayTime = new WaitForSecondsRealtime(1f);

            while (true)
            {
                //OnUnlimitedEnergy();
                // var secondTime = PlayerResource.IsInfinityEnergy() ? (PlayerResource.PlayerData.EndTimeInfinityEnergy - TimeManager.GetNow()) : (PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy +
                //              PlayerResource.DelaySecondToRestoreEnergy) -
                //              TimeManager.GetNow();
                var secondTime = PlayerResource.IsInfinityEnergy()
                    ? PlayerResource.PlayerData.EndTimeInfinityEnergy - TimeManager.GetNow()
                    : PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy +
                      /*(long)DataManager.GeneralConfig.GetData().restoreEnergyTime*/
                      (long)PlayerResource.GetTimeRestoreEnergy() -
                      TimeManager.GetNow();
                if (secondTime >= 0) _energyCooldown.text = TimeManager.GetRemainingTimeToString(secondTime);

                yield return delayTime;
            }
        }
    }
}