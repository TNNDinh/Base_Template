using Ezg.Feature.LocalNotification;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.Meta.HomeScene
{
    public class ButtonTestNoti : MonoBehaviour
    {
        [SerializeField] [Title("Notification Info")]
        private string _title = "Test Title";

        [SerializeField] [TextArea] private string _message = "This is a test notification message from mynoti-lib!";

        [SerializeField] [Title("Icons")] private string _smallIconName = "app_icon";

        [SerializeField] private Texture2D _largeIcon;

        /// <summary>
        ///     Gắn hàm này vào sự kiện OnClick của Button trong Inspector
        /// </summary>
        [Button("Test Notification")]
        public void OnClickTestNoti()
        {
            NotificationNativeBridge.ShowNotification(_title, _message, _smallIconName, _largeIcon);
            Debug.Log("[ButtonTestNoti] Triggered notification test.");
        }
    }
}