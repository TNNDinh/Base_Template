using System.Collections;
using System.Linq;
using Ezg.Core.Extensions;
using Ezg.Feature.Shared;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.UnlockFeature
{
    public class UIProgressUnlockFeature : MonoBehaviour
    {
        public Slider slider;
        public Text textPercent;
        public Text textNameFeature;
        public Image iconFeature;
        public float fillDuration = 2f;

        private void OnEnable()
        {
            slider.value = 0;
            ShowAnimation();
        }

        private void ShowAnimation()
        {
            var currentLevel = PlayerDataManager.Campaign.Level;
            var (previousUnlock, currentUnlock, nextUnlock) = GetUnlockFeatureRange(currentLevel);

            float targetPercent;
            UnlockFeatureModel featureToShow;

            if (currentUnlock != null && currentLevel == currentUnlock.unlockValue)
            {
                featureToShow = currentUnlock;
                targetPercent = 1f;

                textPercent.text = GameSystems.Localize("unlock_feature");
            }
            else if (nextUnlock != null)
            {
                featureToShow = nextUnlock;
                var range = nextUnlock.unlockValue - (previousUnlock?.unlockValue ?? 0);
                var progressInRange = currentLevel - (previousUnlock?.unlockValue ?? 0);
                targetPercent = range > 0 ? Mathf.Clamp01((float)progressInRange / range) : 1f;

                var levelValue = Mathf.Abs(nextUnlock.unlockValue - currentLevel);

                textPercent.text =
                    string.Format(
                        levelValue > 1
                            ? GameSystems.Localize("unlock_new_features")
                            : GameSystems.Localize("unlock_new_feature"), levelValue);
            }
            else
            {
                featureToShow = null;
                targetPercent = 1f;
            }

            if (featureToShow != null)
            {
                iconFeature.sprite = UnlockFeatureService.GetIconFeature(featureToShow.feature);
                textNameFeature.text = GameSystems.Localize(featureToShow.feature.ToString().ToSnakeCase());

                StartCoroutine(ShowUnlockAnimation(targetPercent));
            }
            else
            {
                textNameFeature.text = "All Features Unlocked";
                iconFeature.sprite = null;
            }
        }

        private IEnumerator ShowUnlockAnimation(float targetPercent)
        {
            slider.value = 0f;

            var elapsedTime = 0f;
            while (elapsedTime < fillDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsedTime / fillDuration);
                var progress = Mathf.Lerp(0f, targetPercent, t);
                slider.value = progress;
                //textPercent.text = $"{Mathf.RoundToInt(progress * 100)}%";
                yield return null;
            }

            slider.value = targetPercent;
            //textPercent.text = $"{Mathf.RoundToInt(targetPercent * 100)}%";
        }

        private (UnlockFeatureModel previous, UnlockFeatureModel current, UnlockFeatureModel next)
            GetUnlockFeatureRange(int currentLevel)
        {
            var unlockFeatures = DataManager.UnlockFeature.dataGroups
                .Where(x => x.unlockType == UnlockFeatureType.Level)
                .OrderBy(x => x.unlockValue)
                .ToList();

            var previousUnlock = unlockFeatures
                .LastOrDefault(x => x.unlockValue < currentLevel);

            var currentUnlock = unlockFeatures
                .FirstOrDefault(x => x.unlockValue == currentLevel);

            var nextUnlock = unlockFeatures
                .FirstOrDefault(x => x.unlockValue > currentLevel);

            return (previousUnlock, currentUnlock, nextUnlock);
        }
    }
}