using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.UnlockFeature
{
    public class ItemUnlockByLevelUnlockFeature : MonoBehaviour
    {
        public GameEnums.Features feature;
        public Text textUnlock;

        private void OnEnable()
        {
            EventManager.StartListening(EventName.UnlockFeature, Show);
            UpdateView();
            Show();
        }

        private void OnDisable()
        {
            EventManager.StopListening(EventName.UnlockFeature, Show);
        }

        private void UpdateView()
        {
            var message = string.Format(GameSystems.Localize("require_reach_stage"),
                UnlockFeatureService.GetUnlockValue(feature));
            textUnlock.text = message;
        }

        private void Show()
        {
            Debug.Log("Show unlock feature");
            gameObject.SetActive(!UnlockFeatureService.IsUnlocked(feature));
        }
    }
}