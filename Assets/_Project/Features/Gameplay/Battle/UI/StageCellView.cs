using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     1 ô ải trong danh sách stage-select. Prefab kéo tham chiếu vào các field;
    ///     controller gọi <see cref="Bind" /> để đổ dữ liệu 1 ải.
    /// </summary>
    public class StageCellView : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Text _nameText;
        [SerializeField] private Text _staminaText;
        [SerializeField] private Text _powerText;
        [SerializeField] private GameObject _lockRoot; // hiện khi bị khoá
        [SerializeField] private Image[] _starIcons;   // 3 sao
        [SerializeField] private Button _button;

        private string _stageId;
        private Action<string> _onClick;

        #endregion

        public void Bind(StageModel stage, bool unlocked, int stars, Action<string> onClick)
        {
            _stageId = stage.id;
            _onClick = onClick;

            if (_nameText != null) _nameText.text = stage.name;
            if (_staminaText != null) _staminaText.text = stage.staminaCost.ToString();
            if (_powerText != null) _powerText.text = stage.recommendedPower.ToString();
            if (_lockRoot != null) _lockRoot.SetActive(!unlocked);

            if (_starIcons != null)
                for (int i = 0; i < _starIcons.Length; i++)
                    if (_starIcons[i] != null)
                        _starIcons[i].enabled = i < stars;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.interactable = unlocked;
                _button.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnButtonClick() => _onClick?.Invoke(_stageId);
    }
}
