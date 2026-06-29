using UnityEngine;

namespace Ezg.Feature.Monetization.Shop
{
    /// <summary>
    ///     Icon remove ads
    ///     Tự động ẩn nếu ads đã được mua
    /// </summary>
    internal class RemoveAdsImageView : MonoBehaviour
    {
        private void OnEnable()
        {
            transform.gameObject.SetActive(!ShopService.IsRemoveAds());
        }
    }
}