using Ezg.Feature.Shared;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.Social.AvatarSelect
{
    internal class AvatarHomeIconController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Custom")] private Image _avatar;

        [SerializeField] [TabGroup("Custom")] private Image _frame;

        private void Start()
        {
            LoadMainAvt();
            EventManager.StartListening(nameof(AvatarSelectController), LoadMainAvt);
        }

        private void LoadMainAvt()
        {
            if (DataManager.UserAvatars.TryGetValue(PlayerDataManager.Settings.dataBase.AvatarId, out var avatar))
                _avatar.sprite = avatar;
            if (DataManager.UserFrame.TryGetValue(PlayerDataManager.Settings.dataBase.FrameId, out var frame))
                _frame.sprite = frame;
        }
    }
}