using System.Collections;
using BlackFace.Libraries.Modules.UIModule;
using UnityEngine;
#if UNITY_ANDROID || PLATFORM_ANDROID
using Google.Play.Review;
#endif

namespace Ezg.Feature.System.Rating
{
    public static class ReviewClient
    {
        public static void StartRequestReview()
        {
            UIManager.Instance.StartCoroutine(RequestReview());
        }

        private static IEnumerator RequestReview()
        {
#if UNITY_ANDROID
            _reviewManager = new ReviewManager();
            var requestFlowOperation = _reviewManager.RequestReviewFlow();
            yield return requestFlowOperation;
            if (requestFlowOperation.Error != ReviewErrorCode.NoError)
                // Log error. For example, using requestFlowOperation.Error.ToString().
                yield break;

            Debug.Log("No Error Review");

            _playReviewInfo = requestFlowOperation.GetResult();
            var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
            yield return launchFlowOperation;
            _playReviewInfo = null; // Reset the object
            if (launchFlowOperation.Error != ReviewErrorCode.NoError)
                // Log error. For example, using requestFlowOperation.Error.ToString().
                yield break;
            // The flow has finished. The API does not indicate whether the user
            // reviewed or not, or even whether the review dialog was shown. Thus, no
            // matter the result, we continue our app flow.
#else
            yield return null;
#endif
        }
#if UNITY_ANDROID
        private static ReviewManager _reviewManager;
        private static PlayReviewInfo _playReviewInfo;
#endif
    }
}