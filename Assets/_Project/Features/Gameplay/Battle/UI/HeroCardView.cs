using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     1 card tướng trong lưới roster (màn xếp đội). Bấm để bật/tắt tướng khỏi đội.
    ///     Prefab kéo tham chiếu vào field; controller gọi <see cref="Bind" />.
    /// </summary>
    public class HeroCardView : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Text _nameText;
        [SerializeField] private Text _infoText;   // ★x Lv y
        [SerializeField] private Text _powerText;
        [SerializeField] private Image _elementTag;
        [SerializeField] private GameObject _inTeamBadge; // hiện khi tướng đang trong đội
        [SerializeField] private Button _button;

        private string _heroId;
        private Action<string> _onClick;

        #endregion

        public void Bind(OwnedHero owned, HeroModel model, bool inTeam, Action<string> onClick)
        {
            _heroId = owned.heroId;
            _onClick = onClick;

            if (_nameText != null) _nameText.text = model.name;
            if (_infoText != null) _infoText.text = "★" + owned.star + " Lv" + owned.level;
            if (_powerText != null) _powerText.text = TeamFormationService.PowerOf(owned).ToString("N0");
            if (_elementTag != null) _elementTag.color = TeamFormationService.ElementColor(model.element);
            if (_inTeamBadge != null) _inTeamBadge.SetActive(inTeam);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _onClick?.Invoke(_heroId));
            }
        }
    }
}
