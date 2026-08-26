using System;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Đòi một cái nhà khác phải đạt tới cấp nào đó.
    /// <para>
    /// Đây chính là cách nhà chính khoá các nhà còn lại: đặt <c>Building = TownHall</c>.
    /// Viết tổng quát theo loại nhà chứ không đóng cứng vào nhà chính, để sau muốn bắt
    /// "nhà lính cấp 3 mới mở được X" thì không phải thêm class mới.
    /// </para>
    /// </summary>
    [Serializable]
    public class BuildingLevelRequirement : IBuildingRequirement
    {
        #region Fields

        [SerializeField] private HomeBuildingType _building = HomeBuildingType.TownHall;

        [SerializeField, Min(1)] private int _minLevel = 1;

        #endregion

        #region Public - Properties

        public HomeBuildingType Building => _building;

        public int MinLevel => _minLevel;

        #endregion

        #region Initialize

        public BuildingLevelRequirement() { }

        public BuildingLevelRequirement(HomeBuildingType building, int minLevel)
        {
            _building = building;
            _minLevel = Mathf.Max(1, minLevel);
        }

        #endregion

        #region Public

        public bool IsMet(IHomeBuildingRegistry buildings)
        {
            return buildings != null && buildings.GetLevel(_building) >= _minLevel;
        }

        // TODO: [HomeBase] - đổi sang khoá localize khi có bảng text cho phần nhà cửa.
        public string Describe(IHomeBuildingRegistry buildings)
        {
            int current = buildings?.GetLevel(_building) ?? 0;
            return $"Cần {ResolveName(buildings)} cấp {_minLevel} (đang cấp {current})";
        }

        #endregion

        #region Private

        /// <summary>Lấy tên hiển thị của nhà; chưa tra được thì đành dùng tên enum.</summary>
        private string ResolveName(IHomeBuildingRegistry buildings)
        {
            if (buildings != null && buildings.TryGet(_building, out IHomeBuilding building)) return building.DisplayName;
            return _building.ToString();
        }

        #endregion
    }
}
