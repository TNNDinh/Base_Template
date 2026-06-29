using Ezg.Feature.Shared;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.Social.AvatarSelect
{
    public class AvatarFrameController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Custom")] private Image _frame;

        [SerializeField] [TabGroup("Custom")] private Button _selectButton;

        [SerializeField] [TabGroup("Custom")] private GameObject _selectedObject;

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
            _frame.sprite = DataManager.UserFrame[id];
            SetSelectedState();
        }

        public void OnSelect()
        {
            PlayerDataManager.Settings.dataBase.FrameId = _thisId;
            PlayerDataManager.Settings.Save();
            _callBack?.Invoke();
            EventManager.EmitEvent(EventName.AvatarSelectAvatarEvent);
        }

        private void SetSelectedState()
        {
            _selectedObject.SetActive(PlayerDataManager.Settings.dataBase.FrameId == _thisId);
        }
    }
}