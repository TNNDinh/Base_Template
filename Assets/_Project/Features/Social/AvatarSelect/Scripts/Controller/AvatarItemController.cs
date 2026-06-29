using Ezg.Feature.Shared;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.Social.AvatarSelect
{
    public class AvatarItemController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Custom")] private Image _avatar;

        [SerializeField] [TabGroup("Custom")] private Button _selectButton;

        [SerializeField] [TabGroup("Custom")] private GameObject _selectedObject;

        [SerializeField] [TabGroup("Custom")] private GameObject iconLock;

        private UnityAction _callBack;
        private int _thisId;

        private void Start()
        {
            _selectButton.onClick.AddListener(OnSelect);
            EventManager.StartListening(nameof(AvatarSelectController), SetSelectedState);
        }

        public void InitData(int id, UnityAction callBack)
        {
            _callBack = callBack;
            _thisId = id;
            _avatar.sprite = DataManager.UserAvatars[id];
            SetSelectedState();
        }

        public void OnSelect()
        {
            PlayerDataManager.Settings.dataBase.AvatarId = _thisId;
            PlayerDataManager.Settings.Save();
            _callBack?.Invoke();
            EventManager.EmitEvent(EventName.AvatarSelectAvatarEvent);
        }

        private void SetSelectedState()
        {
            _selectedObject.SetActive(PlayerDataManager.Settings.dataBase.AvatarId == _thisId);
        }
    }
}