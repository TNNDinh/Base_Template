using System;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.System.SaveFound
{
    public class SaveFoundElement : MonoBehaviour
    {
        #region Initialize

        /// <summary>
        ///     Initializes the component and sets up event listeners.
        /// </summary>
        private void Start()
        {
            buttonClick.onClick.AddListener(OnClick);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Sets the data for the element based on current player progress.
        /// </summary>
        public void SetData()
        {
            // removed: LevelDataManager, DataManager.LevelInfor (gameplay removed)
            textLevel.text = string.Empty;
            imageLevel.fillAmount = 0f;
            textDate.text = string.Empty;
            textTime.text = string.Empty;
            coin.SetData(new Resource());
            gem.SetData(new Resource());
        }

        #endregion

        #region Event Handlers

        /// <summary>
        ///     Handles the click event for the element button.
        /// </summary>
        private void OnClick()
        {
            _onClick?.Invoke();
        }

        #endregion

        #region Fields

        public Text textLevel;
        public Image imageLevel;
        public Text textDate;
        public Text textTime;
        public IconView coin;
        public IconView gem;
        public Button buttonClick;

        private Action _onClick;

        #endregion
    }
}