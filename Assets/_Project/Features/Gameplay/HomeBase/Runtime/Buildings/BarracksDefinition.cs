using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>Nhà lính: bảng cấp của chính nó trong <c>HomeBarracks.csv</c>, cộng danh sách lính nó nuôi.</summary>
    [CreateAssetMenu(fileName = "BarracksDefinition", menuName = "Tower Defense/Buildings/Barracks", order = 11)]
    public class BarracksDefinition : HomeBuildingDefinition
    {
        #region Fields

        [Title("Bảng cấp")]
        [Tooltip("Asset sinh ra từ HomeBarracks.csv.")]
        [SerializeField, Required] private HomeBarracksCollection _table;

        [Title("Lính nuôi ở đây")]
        [SerializeField] private TroopDefinition[] _troops = new TroopDefinition[0];

        #endregion

        #region Public - Properties

        public override HomeLevelTableCollection Table => _table;

        /// <summary>Bảng đã ép sẵn kiểu, để đọc sức chứa và trần cấp lính.</summary>
        public HomeBarracksCollection BarracksTable => _table;

        public TroopDefinition[] Troops => _troops;

        #endregion

        #region Public

        /// <summary>Tìm cấu hình của một loại lính. Trả <c>null</c> nếu nhà này không nuôi loại đó.</summary>
        public TroopDefinition FindTroop(TroopType type)
        {
            for (int i = 0; i < _troops.Length; i++)
            {
                if (_troops[i] != null && _troops[i].Type == type) return _troops[i];
            }

            return null;
        }

        #endregion
    }
}
