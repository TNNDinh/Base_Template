using BlackFace.Libraries.Modules.UIModule;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;

namespace Ezg.Feature.Meta.HomeScene
{
    public class ScreenCurrencyBarController : FeatureBaseController
    {
        [SerializeField] [TabGroup("View")] private Transform bgHide;
        [SerializeField] [TabGroup("View")] private UIElementAnimator[] elements;

        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.StartListening(EventName.OpenBehindScrollOrder, OpenHideScrollOrder);
            EventManager.StartListening(EventName.CloseBehindScrollOrder, CloseHideScrollOrder);
            // EventManager.StartListening(EventName.UpCanvasOrderSystem, OpenHideScrollOrder);
            // EventManager.StartListening(EventName.ResetCanvasOrderSystem, CloseHideScrollOrder);

            EventManager.StartListening(EventName.SetViewTheme, HideIcon);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.StopListening(EventName.SetViewTheme, HideIcon);
            EventManager.StopListening(EventName.OpenBehindScrollOrder, OpenHideScrollOrder);
            EventManager.StopListening(EventName.CloseBehindScrollOrder, CloseHideScrollOrder);
            // EventManager.StopListening(EventName.UpCanvasOrderSystem, OpenHideScrollOrder);
            // EventManager.StopListening(EventName.ResetCanvasOrderSystem, CloseHideScrollOrder);
        }

        private void OpenHideScrollOrder()
        {
            HideScrollOrder(true);
        }

        private void CloseHideScrollOrder()
        {
            HideScrollOrder(false);
        }

        private void HideScrollOrder(bool isHide)
        {
            bgHide.gameObject.SetActive(isHide);
        }

        private void HideIcon()
        {
            var isHide = EventManager.GetBool(EventName.SetViewTheme);
            for (var i = 0; i < elements.Length; i++)
                if (!isHide)
                    elements[i].MoveIn();
                else
                    elements[i].MoveOut();
        }
    }
}