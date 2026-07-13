using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Bóng cosmetic bám theo hero (chỉ hình, không đụng combat). Lerp về sau lưng hero với độ trễ.
    /// </summary>
    public class ShadowFollower : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(-0.6f, 0f, 0.5f); // hơi lệch sau + sâu hơn
        public float lag = 7f;

        private void LateUpdate()
        {
            if (target == null) { Destroy(gameObject); return; }
            var goal = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, goal, Time.deltaTime * lag);
        }
    }
}
