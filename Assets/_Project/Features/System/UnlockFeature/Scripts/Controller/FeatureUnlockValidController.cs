using System;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.System.UnlockFeature
{
    public class FeatureUnlockValidController : MonoBehaviour
    {
        [SerializeField] [Required] [TabGroup("Cấu hình tính năng")]
        private GameEnums.Features _feature;

        [SerializeField] [Required] [TabGroup("Cấu hình tính năng")]
        private bool _isRevese;


        private void OnEnable()
        {
            Unlock();
            //EventManager.StartListening(EventName.UpdateResource, OnEnable);
            EventManager.StartListening(EventName.UnlockFeature, OnEnable);
        }

        private void Unlock()
        {
            try
            {
                gameObject.SetActive(_isRevese
                    ? !UnlockFeatureService.IsUnlocked(_feature)
                    : UnlockFeatureService.IsUnlocked(_feature));
            }
            catch (Exception e)
            {
                gameObject.SetActive(false);
            }
        }
    }
}