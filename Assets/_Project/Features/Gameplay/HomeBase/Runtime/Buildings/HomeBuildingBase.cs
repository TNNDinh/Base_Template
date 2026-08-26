using System;
using System.Collections.Generic;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Phần chung của mọi cái nhà: giữ cấp, xét điều kiện, trừ tài nguyên, lên cấp.
    /// <para>
    /// Toàn bộ số liệu — cấp cao nhất, giá, điều kiện — đọc từ bảng CSV của chính cái nhà đó,
    /// nên cân bằng lại game không phải mở Unity. Lớp con chỉ thêm phần chỉ số riêng của mình.
    /// </para>
    /// </summary>
    public abstract class HomeBuildingBase : IHomeBuilding
    {
        #region Constants

        /// <summary>Cấp của nhà chưa mở khoá.</summary>
        public const int LOCKED_LEVEL = 0;

        #endregion

        #region Fields

        private readonly HomeBuildingDefinition _definition;
        private readonly IHomeBuildingState _state;
        private readonly IUpgradePayment _payment;

        private IHomeBuildingRegistry _registry;

        #endregion

        #region Initialize

        protected HomeBuildingBase(HomeBuildingDefinition definition, IHomeBuildingState state, IUpgradePayment payment)
        {
            _definition = definition;
            _state = state;
            _payment = payment ?? new FreeUpgradePayment();
        }

        /// <summary>
        /// Cắm sổ tra cứu vào sau khi tạo. Phải làm sau vì các nhà tham chiếu vòng qua nhau:
        /// nhà lính hỏi cấp nhà chính, mà cả hai cùng nằm trong sổ.
        /// </summary>
        public void BindRegistry(IHomeBuildingRegistry registry) => _registry = registry;

        #endregion

        #region Public - Properties

        public HomeBuildingType Type => _definition.Type;

        public string DisplayName => _definition.DisplayName;

        public int Level => _state.GetBuildingLevel(Type);

        public int MaxLevel => _definition.MaxLevel;

        public bool IsUnlocked => Level > LOCKED_LEVEL;

        public bool IsMaxLevel => Level >= MaxLevel;

        /// <summary>Giá để lên cấp kế tiếp. Kịch cấp thì miễn phí vì chẳng nâng được nữa.</summary>
        public UpgradeCost NextUpgradeCost => IsMaxLevel ? default : CostFor(Level + 1);

        /// <summary>Bảng cấp CSV của nhà này.</summary>
        protected HomeLevelTableCollection Table => _definition.Table;

        /// <summary>Sổ tra cứu các nhà khác.</summary>
        protected IHomeBuildingRegistry Registry => _registry;

        /// <summary>Ví trừ tài nguyên đang dùng.</summary>
        protected IUpgradePayment Payment => _payment;

        #endregion

        #region Public - Events

        public event Action<IHomeBuilding> Changed;

        #endregion

        #region Public

        public bool CanUnlock(out string reason)
        {
            if (IsUnlocked)
            {
                reason = "Nhà đã mở khoá rồi";
                return false;
            }

            return CanReach(LOCKED_LEVEL + 1, out reason);
        }

        public bool CanUpgrade(out string reason)
        {
            if (!IsUnlocked)
            {
                reason = "Nhà chưa mở khoá";
                return false;
            }

            if (IsMaxLevel)
            {
                reason = "Đã đạt cấp cao nhất";
                return false;
            }

            return CanReach(Level + 1, out reason);
        }

        public bool TryUnlock()
        {
            if (!CanUnlock(out _)) return false;
            return ApplyLevel(LOCKED_LEVEL + 1);
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade(out _)) return false;
            return ApplyLevel(Level + 1);
        }

        public string[] DescribeNextRequirements()
        {
            if (IsMaxLevel) return Array.Empty<string>();

            IReadOnlyList<IBuildingRequirement> requirements = RequirementsFor(Level + 1);
            if (requirements.Count == 0) return Array.Empty<string>();

            var result = new List<string>(requirements.Count);
            for (int i = 0; i < requirements.Count; i++)
            {
                if (requirements[i] == null) continue;
                result.Add(requirements[i].Describe(_registry));
            }

            return result.ToArray();
        }

        #endregion

        #region Protected

        /// <summary>Báo cho ai đang nghe rằng nhà vừa đổi. Lớp con gọi khi phần riêng của nó đổi.</summary>
        protected void RaiseChanged() => Changed?.Invoke(this);

        /// <summary>Hook cho lớp con chạy thêm việc riêng sau khi lên cấp.</summary>
        protected virtual void OnLevelChanged(int previousLevel, int newLevel) { }

        /// <summary>
        /// Xét một danh sách điều kiện. Dùng chung cho nhà và cho lính, vì luật y hệt nhau.
        /// </summary>
        protected bool MeetsAll(IReadOnlyList<IBuildingRequirement> requirements, out string reason)
        {
            reason = string.Empty;
            for (int i = 0; i < requirements.Count; i++)
            {
                IBuildingRequirement requirement = requirements[i];
                if (requirement == null || requirement.IsMet(_registry)) continue;

                reason = requirement.Describe(_registry);
                return false;
            }

            return true;
        }

        #endregion

        #region Private

        private UpgradeCost CostFor(int level) => Table != null ? Table.GetCost(level) : default;

        private IReadOnlyList<IBuildingRequirement> RequirementsFor(int level)
        {
            return Table != null ? Table.GetRequirements(level) : Array.Empty<IBuildingRequirement>();
        }

        /// <summary>Xét mọi điều kiện của cấp <paramref name="targetLevel"/>, kể cả tiền.</summary>
        private bool CanReach(int targetLevel, out string reason)
        {
            if (targetLevel > MaxLevel)
            {
                reason = "Đã đạt cấp cao nhất";
                return false;
            }

            if (!MeetsAll(RequirementsFor(targetLevel), out reason)) return false;

            return _payment.CanAfford(CostFor(targetLevel), out reason);
        }

        /// <summary>Trừ tiền rồi ghi cấp mới. Không trừ được thì giữ nguyên, không lên cấp nửa vời.</summary>
        private bool ApplyLevel(int targetLevel)
        {
            if (!_payment.TrySpend(CostFor(targetLevel))) return false;

            int previous = Level;
            _state.SetBuildingLevel(Type, targetLevel);

            OnLevelChanged(previous, targetLevel);
            RaiseChanged();
            return true;
        }

        #endregion
    }
}
