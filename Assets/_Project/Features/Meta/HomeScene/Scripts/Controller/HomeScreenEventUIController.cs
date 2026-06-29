using Ezg.Core.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Meta.HomeScene
{
    internal class HomeScreenEventUIController : MonoBehaviour
    {
        public ScrollRect scrollRect;
        public float scaleFactor = 0.5f;
        public float scaleRange = 300f;
        public float minScale = 0.5f;
        public float maxScale = 1.5f;
        public float padingItem = 45f;
        private float bonusXValue;
        private Vector2 contentCenter;

        private RectTransform contentRectTransform;
        private bool isInit;
        private Vector2 scrollViewCenter;

        private void Start()
        {
            this.DelayMethod(.1f, () =>
            {
                contentRectTransform = scrollRect.content;

                var totalItem = 0;
                var rectItem = 0f;
                foreach (RectTransform rect in contentRectTransform)
                {
                    rectItem = rect.sizeDelta.x;
                    rect.pivot = new Vector2(.5f, .5f);
                    rect.transform.localPosition += new Vector3(rectItem / 2, 0, 0);
                    totalItem++;
                }

                bonusXValue = rectItem / 2 + (totalItem - 1) * padingItem + (totalItem - 1) * rectItem / 2;

                scrollViewCenter = new Vector2(scrollRect.GetComponent<RectTransform>().rect.width * 0.5f,
                    scrollRect.GetComponent<RectTransform>().rect.height * 0.5f) + new Vector2(bonusXValue, 0);
                isInit = true;
            });
        }

        private void Update()
        {
            if (!isInit) return;

            contentCenter = -contentRectTransform.anchoredPosition + scrollViewCenter;

            foreach (RectTransform item in contentRectTransform)
            {
                var itemCenter = item.anchoredPosition + new Vector2(item.rect.width * 0.5f, item.rect.height * 0.5f);
                var distance = Vector2.Distance(contentCenter, itemCenter);

                var scale = Mathf.Clamp(1f - distance / scaleRange * scaleFactor, minScale, maxScale);
                item.localScale = Vector3.one * scale;
            }
        }
    }
}