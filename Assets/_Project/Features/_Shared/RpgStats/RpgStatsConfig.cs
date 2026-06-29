using UnityEngine;

namespace Ezg.Package.RpgStats
{
    /// <summary>
    ///     Asset cấu hình stats của project (đóng TKey = <see cref="RPGStatType" />).
    ///     Asset được tạo qua menu: Create > Ezg > Rpg Stats > Project Setup.
    ///     Cấu hình generic (vital list, percent-only, limits) nằm ở <see cref="RpgStatsConfigBase{TKey}" />;
    ///     lớp này chỉ bổ sung tuỳ biến presentation riêng của project (đường dẫn icon).
    /// </summary>
    public class RpgStatsConfig : RpgStatsConfigBase<RPGStatType>
    {
        #region Fields

        [Tooltip("Thư mục Resources chứa icon stat.")] [SerializeField]
        private string _statIconFolder = "Images/StatsIcon/";

        [Tooltip("Tiền tố tên file icon stat. Tên file = <prefix><(int)statType>.")] [SerializeField]
        private string _statIconPrefix = "stat_";

        #endregion

        #region Properties

        public string StatIconFolder => _statIconFolder;
        public string StatIconPrefix => _statIconPrefix;

        #endregion
    }
}