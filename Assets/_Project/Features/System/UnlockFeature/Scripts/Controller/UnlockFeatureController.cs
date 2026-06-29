using System.Linq;
using BlackFace.Libraries.Modules.UIModule;
using Ezg.Core.Adapter;
using Ezg.Core.Extensions;
using Ezg.Feature.Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.UnlockFeature
{
    internal class UnlockFeatureController : FeatureBaseController
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private Text _unlockText;

        [SerializeField] [TabGroup("Cấu hình")]
        private Text _featureName;

        [SerializeField] [TabGroup("Cấu hình")]
        private Image _featureImage;

        protected override void LoadData()
        {
            base.LoadData();

            var featureInThisLevel =
                DataManager.UnlockFeature.dataGroups.FirstOrDefault(x =>
                    x.unlockValue == PlayerDataManager.Campaign.Level);

            Debug.Log("Unlock " + featureInThisLevel.feature);

            if (featureInThisLevel != null)
            {
                _featureImage.sprite =
                    ResLoader.Load<Sprite>("Images/IconFeature/icon_feature_" + (int)featureInThisLevel.feature);
                _unlockText.text = GameSystems.Localize(featureInThisLevel.type == FeatureTypes.Feature
                    ? "unlock_feature"
                    : "unlock_booster");
                _featureName.text = GameSystems.Localize(featureInThisLevel.feature.ToString().ToSnakeCase().ToLower());
            }
        }
    }
}