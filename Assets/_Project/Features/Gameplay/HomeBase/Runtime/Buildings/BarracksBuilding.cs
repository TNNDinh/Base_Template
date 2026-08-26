using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Phần huấn luyện lính của nhà lính: xem chỉ số và nâng cấp từng loại lính.
    /// <para>
    /// Tách khỏi <see cref="IHomeBuilding"/> để màn hình nào chỉ cần bảng chỉ số lính thì
    /// nhận đúng giao diện này, không phải kéo theo cả chuyện mở khoá nhà.
    /// </para>
    /// </summary>
    public interface ITroopTrainer
    {
        #region Properties

        /// <summary>Các loại lính nuôi ở đây.</summary>
        IReadOnlyList<TroopDefinition> Troops { get; }

        /// <summary>Số lính mang ra trận được ở cấp nhà hiện tại.</summary>
        int TroopCapacity { get; }

        /// <summary>Cấp lính cao nhất mà nhà lính hiện tại cho phép.</summary>
        int MaxTroopLevel { get; }

        #endregion

        #region Methods

        /// <summary>Cấp hiện tại của một loại lính. 0 nghĩa là chưa mở.</summary>
        int GetTroopLevel(TroopType type);

        /// <summary>Chỉ số của lính ở cấp hiện tại.</summary>
        TroopStats GetTroopStats(TroopType type);

        /// <summary>Chỉ số của lính ở một cấp bất kỳ, để so trước sau khi nâng.</summary>
        TroopStats GetTroopStatsAt(TroopType type, int level);

        /// <summary>Giá để nâng một loại lính lên cấp kế tiếp.</summary>
        UpgradeCost GetTroopUpgradeCost(TroopType type);

        /// <summary>Điều kiện còn thiếu để nâng lính lên cấp kế tiếp.</summary>
        string[] DescribeTroopRequirements(TroopType type);

        /// <summary>Nâng được lính này lên cấp kế tiếp chưa.</summary>
        /// <param name="reason">Điều kiện còn thiếu, rỗng nếu nâng được.</param>
        bool CanUpgradeTroop(TroopType type, out string reason);

        /// <summary>Nâng lính lên cấp kế tiếp.</summary>
        bool TryUpgradeTroop(TroopType type);

        #endregion

        #region Events

        /// <summary>Bắn khi một loại lính đổi cấp.</summary>
        event Action<TroopType> TroopChanged;

        #endregion
    }

    /// <summary>
    /// Nhà lính. Ngoài việc tự lên cấp như mọi nhà khác, nó còn nuôi lính: mỗi loại lính có
    /// file CSV riêng, và cấp lính bị chặn trần bởi cột <c>max_troop_level</c> của nhà lính.
    /// </summary>
    public class BarracksBuilding : HomeBuildingBase, ITroopTrainer
    {
        #region Constants

        /// <summary>Cấp của lính chưa mở.</summary>
        private const int TROOP_LOCKED_LEVEL = 0;

        #endregion

        #region Fields

        private readonly BarracksDefinition _definition;
        private readonly ITroopState _troopState;

        #endregion

        #region Initialize

        public BarracksBuilding(BarracksDefinition definition, IHomeBuildingState state, ITroopState troopState,
                                IUpgradePayment payment)
            : base(definition, state, payment)
        {
            _definition = definition;
            _troopState = troopState;
        }

        #endregion

        #region Public - Properties

        public IReadOnlyList<TroopDefinition> Troops => _definition.Troops;

        public int TroopCapacity => IsUnlocked ? CurrentRow()?.troopCapacity ?? 0 : 0;

        public int MaxTroopLevel => IsUnlocked ? CurrentRow()?.maxTroopLevel ?? 0 : 0;

        #endregion

        #region Public - Events

        public event Action<TroopType> TroopChanged;

        #endregion

        #region Public

        public int GetTroopLevel(TroopType type) => _troopState.GetTroopLevel(type);

        public TroopStats GetTroopStats(TroopType type) => GetTroopStatsAt(type, Mathf.Max(1, GetTroopLevel(type)));

        public TroopStats GetTroopStatsAt(TroopType type, int level)
        {
            TroopDefinition troop = _definition.FindTroop(type);
            return troop != null ? troop.StatsAt(level) : default;
        }

        public UpgradeCost GetTroopUpgradeCost(TroopType type)
        {
            TroopDefinition troop = _definition.FindTroop(type);
            return troop != null ? troop.CostAt(GetTroopLevel(type) + 1) : default;
        }

        public string[] DescribeTroopRequirements(TroopType type)
        {
            IReadOnlyList<IBuildingRequirement> requirements = TroopRequirements(type, GetTroopLevel(type) + 1);
            if (requirements.Count == 0) return Array.Empty<string>();

            var result = new List<string>(requirements.Count);
            for (int i = 0; i < requirements.Count; i++)
            {
                if (requirements[i] == null) continue;
                result.Add(requirements[i].Describe(Registry));
            }

            return result.ToArray();
        }

        public bool CanUpgradeTroop(TroopType type, out string reason)
        {
            reason = string.Empty;

            if (!IsUnlocked)
            {
                reason = "Nhà lính chưa mở khoá";
                return false;
            }

            TroopDefinition troop = _definition.FindTroop(type);
            if (troop == null)
            {
                reason = $"Nhà lính không nuôi {type}";
                return false;
            }

            int target = GetTroopLevel(type) + 1;
            if (target > troop.MaxLevel)
            {
                reason = "Lính đã đạt cấp cao nhất";
                return false;
            }

            // Trần cấp lính do nhà lính đặt ra: muốn lính mạnh hơn thì nâng nhà trước.
            if (target > MaxTroopLevel)
            {
                reason = $"Cần nâng {DisplayName} lên trước (đang cho tối đa cấp {MaxTroopLevel})";
                return false;
            }

            if (!MeetsAll(TroopRequirements(type, target), out reason)) return false;

            return Payment.CanAfford(troop.CostAt(target), out reason);
        }

        public bool TryUpgradeTroop(TroopType type)
        {
            if (!CanUpgradeTroop(type, out _)) return false;

            TroopDefinition troop = _definition.FindTroop(type);
            int target = GetTroopLevel(type) + 1;
            if (!Payment.TrySpend(troop.CostAt(target))) return false;

            _troopState.SetTroopLevel(type, target);
            TroopChanged?.Invoke(type);
            RaiseChanged();
            return true;
        }

        #endregion

        #region Private

        private HomeBarracksModel CurrentRow()
        {
            HomeBarracksCollection table = _definition.BarracksTable;
            return table != null ? table.Row(Level) : null;
        }

        private IReadOnlyList<IBuildingRequirement> TroopRequirements(TroopType type, int level)
        {
            TroopDefinition troop = _definition.FindTroop(type);
            TroopTableCollection table = troop != null ? troop.Table : null;
            return table != null ? table.GetRequirements(level) : Array.Empty<IBuildingRequirement>();
        }

        /// <summary>
        /// Mở khoá nhà lính là mở luôn cấp 1 cho loại lính nào đã đủ điều kiện — nhà lính rỗng
        /// không có lính thì chẳng dùng được việc gì. Lính nào còn vướng điều kiện thì để nguyên.
        /// </summary>
        protected override void OnLevelChanged(int previousLevel, int newLevel)
        {
            if (previousLevel != LOCKED_LEVEL) return;

            TroopDefinition[] troops = _definition.Troops;
            for (int i = 0; i < troops.Length; i++)
            {
                TroopDefinition troop = troops[i];
                if (troop == null) continue;
                if (_troopState.GetTroopLevel(troop.Type) > TROOP_LOCKED_LEVEL) continue;
                if (!MeetsAll(TroopRequirements(troop.Type, 1), out _)) continue;

                _troopState.SetTroopLevel(troop.Type, 1);
                TroopChanged?.Invoke(troop.Type);
            }
        }

        #endregion
    }
}
