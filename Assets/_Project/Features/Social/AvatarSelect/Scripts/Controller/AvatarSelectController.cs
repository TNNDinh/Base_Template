using BlackFace.Libraries.Modules.UIModule;
using Ezg.Feature.Shared;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.Social.AvatarSelect
{
    internal class AvatarSelectController : FeatureBaseController
    {
        [SerializeField] [TabGroup("Custom")] private Image _mainAvt;

        [SerializeField] [TabGroup("Custom")] private Image _mainFrame;

        [SerializeField] [TabGroup("Custom")] private Transform _parentList;

        [SerializeField] [TabGroup("Custom")] private Transform _frameParent;

        [SerializeField] [TabGroup("Custom")] private AvatarItemController _itemTemplate;

        [SerializeField] [TabGroup("Custom")] private AvatarFrameController _frameItemTemplate;

        protected override void LoadData()
        {
            base.LoadData();
            LoadMainAvt();
            LoadListAvt();
        }

        private void LoadListAvt()
        {
            foreach (var x in DataManager.UserAvatars)
                Instantiate(_itemTemplate, _parentList).InitData(x.Key, LoadMainAvt);

            foreach (var x in DataManager.UserFrame)
                Instantiate(_frameItemTemplate, _frameParent).InitData(x.Key, LoadMainAvt);
        }

        private void LoadMainAvt()
        {
            _mainAvt.sprite = DataManager.UserAvatars[PlayerDataManager.Settings.dataBase.AvatarId];
            _mainFrame.sprite = DataManager.UserFrame[PlayerDataManager.Settings.dataBase.FrameId];
            EventManager.EmitEvent(nameof(AvatarSelectController));
        }
    }
}