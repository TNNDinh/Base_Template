using TigerForge;
using UnityEngine;

namespace Ezg.Feature.Monetization.Shop
{
    public class RemoveAdsLinkButton : MonoBehaviour
    {
        private void Start()
        {
            EventManager.StartListening(nameof(EventName.PurchasedIapSuccess), ValidData);
            ValidData();
        }

        private void ValidData()
        {
            gameObject.SetActive(!ShopService.IsRemoveAds());
        }
    }
}