using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Màn xếp đội hình. Trên: 6 slot đội hình (slot 0 = main). Dưới: lưới toàn bộ tướng đã unlock.
    ///     Bấm card để bật/tắt tướng vào đội; bấm slot đang có tướng để gỡ. Mọi thay đổi persist ngay
    ///     qua <see cref="TeamFormationService" /> (PlayerBattleHero).
    ///     Kế thừa <see cref="FeatureBaseController" />; prefab đặt tên <c>screen_team_formation</c>,
    ///     set FeatureType = TeamFormation trên inspector.
    /// </summary>
    public class TeamFormationController : FeatureBaseController
    {
        #region Fields

        [Header("Slots (đội hình)")]
        [SerializeField] private Transform _slotRoot;
        [SerializeField] private TeamSlotView _slotPrefab;

        [Header("Roster (tướng sở hữu)")]
        [SerializeField] private Transform _rosterContent;
        [SerializeField] private HeroCardView _cardPrefab;

        [Header("Tổng quan")]
        [SerializeField] private Text _teamPowerText;
        [SerializeField] private Text _teamCountText;

        private readonly List<TeamSlotView> _slots = new List<TeamSlotView>();
        private readonly List<HeroCardView> _cards = new List<HeroCardView>();

        #endregion

        #region Open

        /// <summary>Mở màn xếp đội hình (layer Main).</summary>
        public static void Open()
        {
            UIManager.Instance.Show(GameEnums.Features.TeamFormation, UIManager.UIGroupName.Main_Container).Forget();
        }

        #endregion

        #region Lifecycle

        protected override void Start()
        {
            base.Start();
            BattleDatabase.LoadSample();
            Refresh();
        }

        public override void LoadData(object data)
        {
            BattleDatabase.LoadSample();
            Refresh();
        }

        #endregion

        #region Build

        private void Refresh()
        {
            BuildSlots();
            BuildRoster();
            RefreshSummary();
        }

        private void BuildSlots()
        {
            if (_slotRoot == null || _slotPrefab == null) return;
            ClearSlots();

            var mainId = BattlePlayerData.MainHeroId;
            var slots = TeamFormationService.GetSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                var view = Instantiate(_slotPrefab, _slotRoot);
                OwnedHero owned = null;
                var model = default(HeroModel);
                if (!string.IsNullOrEmpty(slots[i]))
                {
                    owned = BattlePlayerData.Hero.GetHero(slots[i]);
                    model = BattleDatabase.GetHero(slots[i]);
                }

                var isMain = i == 0 || slots[i] == mainId;
                view.Bind(i, owned, model, isMain, OnSlotClicked);
                _slots.Add(view);
            }
        }

        private void BuildRoster()
        {
            if (_rosterContent == null || _cardPrefab == null) return;
            ClearCards();

            var roster = TeamFormationService.GetRoster();
            for (int i = 0; i < roster.Count; i++)
            {
                var owned = roster[i];
                var model = BattleDatabase.GetHero(owned.heroId);
                if (string.IsNullOrEmpty(model.id)) continue; // config đã bị xóa

                var card = Instantiate(_cardPrefab, _rosterContent);
                card.Bind(owned, model, TeamFormationService.IsInTeam(owned.heroId), OnCardClicked);
                _cards.Add(card);
            }
        }

        private void RefreshSummary()
        {
            if (_teamPowerText != null) _teamPowerText.text = TeamFormationService.TeamPower().ToString("N0");
            if (_teamCountText != null)
                _teamCountText.text = TeamFormationService.FilledCount() + "/" + TeamFormationService.TeamSize;
        }

        #endregion

        #region Interaction

        private void OnCardClicked(string heroId)
        {
            TeamFormationService.Toggle(heroId);
            Refresh();
        }

        private void OnSlotClicked(int slot)
        {
            TeamFormationService.RemoveAt(slot);
            Refresh();
        }

        #endregion

        #region Cleanup

        private void ClearSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i] != null)
                    Destroy(_slots[i].gameObject);
            _slots.Clear();
        }

        private void ClearCards()
        {
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i] != null)
                    Destroy(_cards[i].gameObject);
            _cards.Clear();
        }

        #endregion
    }
}
