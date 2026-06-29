using BlackFace.Libraries.Modules.UIModule;
using TigerForge;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.Shared.UI
{
public static class UIChangeContainer
{
    public static FeatureBaseController shop;

    public static void Init()
    {
        RegisEvent();
    }

    private static void RegisEvent()
    {
        EventManager.StartListening(EventName.ChangeUIOverlays, ChangeContainerOpenFromOverlays);
        EventManager.StartListening(EventName.ChangeUIDefault, ChangeContainerDefault);
    }


    private static void ChangeContainerDefault()
    {
        UIManager.Instance.MoveToGroup(GameEnums.Features.Shop, UIManager.UIGroupName.Modal_Container);
        UIManager.Instance.MoveToGroup(GameEnums.Features.CurrencyBar, UIManager.UIGroupName.CurrencyBar_Container);
    }

    private static void ChangeContainerOpenFromOverlays()
    {
        //var lastOpenFeature = UIManager.Instance.GetLastFeatureController();
        UIManager.Instance.MoveToGroup(GameEnums.Features.Shop, UIManager.UIGroupName.Overlay_Container,
            UIManager.Instance.CurrentLayerOrder);
        UIManager.Instance.MoveToGroup(GameEnums.Features.CurrencyBar, UIManager.UIGroupName.Overlay_Container,
            UIManager.Instance.CurrentLayerOrder + 1);
    }
}
}