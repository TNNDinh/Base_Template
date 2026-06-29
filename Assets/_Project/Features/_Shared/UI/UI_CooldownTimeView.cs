using System;
using System.Collections;
using Ezg.Core.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Core.UI
{
    public class UI_CooldownTimeView : MonoBehaviour
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private CooldownTypes _cooldownType;

        [SerializeField] [TabGroup("Cấu hình")]
        private bool _showCustom;

        private Text _cooldownText;

        private IEnumerator _customAction;

        /// <summary>
        ///     Sự kiện khi kết thúc cooldown
        /// </summary>
        private UnityAction _customActionEndCooldown;

        private void Awake()
        {
            _cooldownText = GetComponentInChildren<Text>(true);
        }

        private void OnEnable()
        {
            if (_cooldownText != null && _cooldownType != CooldownTypes.Custom) StartCoroutine(ShowCooldownTime());
        }

        private string FormatTime(long seconds)
        {
            if (!_showCustom) return TimeManager.GetRemainingTimeToString(seconds);
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Days > 0 ? ts.ToString(@"dd\d\ hh\h") : ts.ToString(@"hh\h\ mm\m");
        }

        private IEnumerator ShowCooldownTime()
        {
            var delayTime = new WaitForSeconds(1);
            while (true)
            {
                _cooldownText.text = FormatTime(_cooldownType switch
                {
                    CooldownTypes.NextDay => TimeManager.GetRemainingTimeToNextDay(),
                    CooldownTypes.NextWeek => TimeManager.GetRemainingTimeToNextWeek(),
                    CooldownTypes.NextMonth => TimeManager.GetRemainingTimeToNextMonth(),
                    _ => 0
                });

                yield return delayTime;
            }
        }

        public void InitCustomCooldown(long endTime, UnityAction callbackEndAction = null)
        {
            _cooldownText = GetComponentInChildren<Text>(true);

            _customActionEndCooldown = callbackEndAction;
            if (_customAction != null) StopCoroutine(_customAction);

            _customAction = ShowCooldownTimeCustom(endTime);
            if (gameObject.activeInHierarchy) StartCoroutine(_customAction);
        }

        private IEnumerator ShowCooldownTimeCustom(long endTime)
        {
            var delayTime = new WaitForSeconds(1);
            //var localize = GameSystems.Localize("end_in");
            var cooldown = endTime - TimeManager.GetOnlineNow();
            while (cooldown > 0)
            {
                if (_cooldownText == null)
                    //Debug.Log("aaaaaaaaaaaaaaaaaa");
                    yield break;

                _cooldownText.text = FormatTime(cooldown);
                yield return delayTime;
                cooldown = endTime - TimeManager.GetOnlineNow();
            }

            _customActionEndCooldown?.Invoke();
        }

        private enum CooldownTypes
        {
            NextDay,
            NextWeek,
            NextMonth,
            Custom
        }
    }
}