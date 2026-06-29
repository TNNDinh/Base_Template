using Ezg.Core.UI;
using TigerForge;
using UnityEngine;
using UnityEngine.Serialization;

public class PackDurationIcon : MonoBehaviour
{
    [SerializeField] private PackDurationType _packDurationType;

    [FormerlySerializedAs("_textDuration")] [SerializeField]
    private UI_CooldownTimeView _UiCooldown;

    private void OnEnable()
    {
        CheckActive();
        EventManager.StartListening(EventName.ActivePackDuration + _packDurationType, ActivePackDuration);
        EventManager.StartListening(EventName.DeActivePackDuration + _packDurationType, DeActivePackDuration);
    }

    private void CheckActive()
    {
        gameObject.SetActive(PackDurationService.IsPackActive(_packDurationType));
    }

    private void ActivePackDuration()
    {
        gameObject.SetActive(true);
        TimeDuration();
    }

    private void DeActivePackDuration()
    {
        gameObject.SetActive(false);
    }

    private void TimeDuration()
    {
        _UiCooldown.InitCustomCooldown(PackDurationService.GetDurationPack(_packDurationType));
    }
}