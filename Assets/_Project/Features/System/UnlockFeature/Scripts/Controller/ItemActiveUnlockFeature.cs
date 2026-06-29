using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.Config;

public class ItemActiveUnlockFeature : MonoBehaviour
{
    public GameEnums.Features feature;

    protected virtual void Awake()
    {
        EventManager.StartListening(EventName.UnlockFeature, Show);
    }

    private void OnEnable()
    {
        Show();
    }

    private void Show()
    {
        gameObject.SetActive(UnlockFeatureService.IsUnlockFeature(feature) && CustomCondition());
    }

    protected virtual bool CustomCondition()
    {
        return true;
    }
}