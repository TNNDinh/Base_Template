using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     1 ô slot trong đội hình. Trống hoặc chứa 1 tướng. Bấm vào ô đang có tướng để gỡ.
    ///     Prefab kéo tham chiếu vào field; controller gọi <see cref="Bind" />.
    /// </summary>
    public class TeamSlotView : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Text _nameText;
        [SerializeField] private Text _infoText;   // ★x Lv y
        [SerializeField] private Text _powerText;
        [SerializeField] private Image _elementTag;
        [SerializeField] private GameObject _emptyRoot; // hiện khi slot trống
        [SerializeField] private GameObject _mainBadge; // hiện khi là slot main (0)
        [SerializeField] private Button _button;

        private int _slot;
        private Action<int> _onClick;

        #endregion

        /// <summary>Đổ dữ liệu 1 slot. <paramref name="owned" /> null = slot trống.</summary>
        public void Bind(int slot, OwnedHero owned, HeroModel model, bool isMain, Action<int> onClick)
        {
            _slot = slot;
            _onClick = onClick;

            var empty = owned == null;
            if (_emptyRoot != null) _emptyRoot.SetActive(empty);
            if (_mainBadge != null) _mainBadge.SetActive(isMain);

            if (_nameText != null) _nameText.text = empty ? "Trống" : model.name;
            if (_infoText != null) _infoText.text = empty ? "" : "★" + owned.star + " Lv" + owned.level;
            if (_powerText != null) _powerText.text = empty ? "" : TeamFormationService.PowerOf(owned).ToString("N0");
            if (_elementTag != null)
            {
                _elementTag.enabled = !empty;
                if (!empty) _elementTag.color = TeamFormationService.ElementColor(model.element);
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _onClick?.Invoke(_slot));
            }
        }
    }
}
