using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;

public class ItemLockUnlockFeature : MonoBehaviour
{
    [SerializeField] private GameEnums.Features feature;

    [SerializeField] private Button buttonClick;
    [SerializeField] private GameObject panelLock;
    [SerializeField] private GameObject originalButton;

    private void Awake()
    {
        EventManager.StartListening(nameof(EventName.UnlockFeature), Show);
    }

    private void Start()
    {
        buttonClick.onClick.AddListener(Toast);
    }

    private void OnEnable()
    {
        Show();
    }

    private void Show()
    {
        panelLock.SetActive(!UnlockFeatureService.IsUnlockFeature(feature));
        if (originalButton)
            originalButton?.SetActive(!panelLock.activeSelf);
    }

    private void Toast()
    {
        var message = string.Format(GameSystems.Localize("require_reach_stage"),
            UnlockFeatureService.GetUnlockValue(feature));
        GameSystems.ShowSimpleMessage(message);
    }
}