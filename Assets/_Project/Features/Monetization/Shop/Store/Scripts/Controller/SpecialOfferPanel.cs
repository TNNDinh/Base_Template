using UnityEngine;

namespace Ezg.Feature.Monetization.Shop
{
    public class SpecialOfferPanel : MonoBehaviour
    {
        public Transform content;

        private void Start()
        {
            var isActive = false;
            foreach (Transform child in content)
                if (child.gameObject.activeSelf)
                    isActive = true;

            gameObject.SetActive(isActive);
        }
    }
}