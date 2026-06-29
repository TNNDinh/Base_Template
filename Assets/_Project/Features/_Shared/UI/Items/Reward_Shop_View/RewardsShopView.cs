using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    public class RewardsShopView : MonoBehaviour
    {
        [SerializeField] private bool isInitWithScaleZero;
        [SerializeField] private Transform rewardAnchor;

        public IconView prefab;
        private List<IconView> iconViews;


        public void InitOrUpdateView(Resource[] rewards, bool isShowProgress = false)
        {
            if (prefab == null) return;
            // prefab = LoadResourceController.GetIconView();

            if (iconViews == null)
                iconViews = new List<IconView>();

            var i = 0;
            for (; i < rewards.Length; i++)
                //print(rewards[i]);
                if (i < iconViews.Count)
                {
                    iconViews[i].SetData(rewards[i]);
                    if (isInitWithScaleZero) iconViews[i].transform.localScale = Vector3.zero;

                    //iconViews[i].SetProgress(isShowProgress);
                    iconViews[i].gameObject.SetActive(rewards[i].resNumber > 0);
                }
                else
                {
                    var view = Instantiate(prefab, rewardAnchor);
                    view.SetData(rewards[i]);
                    view.gameObject.SetActive(rewards[i].resNumber > 0);
                    if (isInitWithScaleZero) view.transform.localScale = Vector3.zero;

                    iconViews.Add(view);
                }

            for (; i < iconViews.Count; i++) iconViews[i].gameObject.SetActive(false);
        }
    }
}