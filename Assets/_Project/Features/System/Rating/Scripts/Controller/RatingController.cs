using BlackFace.Libraries.Modules.UIModule;
using Ezg.Feature.Shared;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.Rating
{
    public class RatingController : FeatureBaseController
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private Text _ratingContent;

        [SerializeField] [TabGroup("Cấu hình")]
        private RatingStarController[] _stars;

        protected override void Start()
        {
            base.Start();
#if UNITY_ANDROID
            _ratingContent.text = GameSystems.Localize("rating_content_google");
#elif UNITY_IPHONE || UNITY_IOS
            _ratingContent.text = GameSystems.Localize("rating_content_apple");
#endif

            var index = 0;
            _stars.ForEach(x =>
            {
                var i2 = index;
                x.Init(index, () => { ActiveStar(i2); });
                x.SetState(true);
                index++;
            });
        }

        private void ActiveStar(int index)
        {
            for (var i = 0; i < _stars.Length; i++) _stars[i].SetState(i <= index);
        }

        public void Rating(int star)
        {
            if (star >= 5)
            {
#if UNITY_ANDROID
                //Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
                ReviewClient.StartRequestReview();
#elif UNITY_IPHONE || UNITY_IOS
      if (!UnityEngine.iOS.Device.RequestStoreReview())
      {
         Application.OpenURL("https://apps.apple.com/app/id6755586510");
      }
#endif
                PlayerDataManager.Settings.dataBase.IsRating = true;
                PlayerDataManager.Settings.Save();
            }

            GameSystems.ShowSimpleMessage("rating_thanks");
            CloseMe();
        }
    }
}