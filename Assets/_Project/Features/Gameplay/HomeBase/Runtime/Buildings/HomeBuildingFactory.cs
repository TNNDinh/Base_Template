using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>Dựng đối tượng nhà lúc chạy từ asset danh tính.</summary>
    public interface IHomeBuildingFactory
    {
        /// <summary>Tạo nhà tương ứng với cấu hình. Trả <c>null</c> nếu chưa hỗ trợ loại đó.</summary>
        IHomeBuilding Create(HomeBuildingDefinition definition);
    }

    /// <summary>
    /// Bản cài đặt mặc định.
    /// <para>
    /// ĐÂY LÀ CHỖ DUY NHẤT biết tới các class nhà cụ thể. Thêm loại nhà mới thì viết class nhà,
    /// tạo asset danh tính, thêm file CSV, rồi thêm đúng một nhánh ở đây — phần còn lại của game
    /// vẫn chỉ thấy <see cref="IHomeBuilding"/>.
    /// </para>
    /// </summary>
    public class HomeBuildingFactory : IHomeBuildingFactory
    {
        #region Fields

        private readonly IHomeBuildingState _state;
        private readonly ITroopState _troopState;
        private readonly IUpgradePayment _payment;

        #endregion

        #region Initialize

        public HomeBuildingFactory(IHomeBuildingState state, ITroopState troopState, IUpgradePayment payment)
        {
            _state = state;
            _troopState = troopState;
            _payment = payment ?? new FreeUpgradePayment();
        }

        #endregion

        #region Public

        public IHomeBuilding Create(HomeBuildingDefinition definition)
        {
            switch (definition)
            {
                case TownHallDefinition townHall:
                    return new TownHallBuilding(townHall, _state, _payment);

                case BarracksDefinition barracks:
                    return new BarracksBuilding(barracks, _state, _troopState, _payment);

                case WallDefinition wall:
                    return new WallBuilding(wall, _state, _payment);

                default:
                    Debug.LogError($"[{nameof(HomeBuildingFactory)}] Chưa biết dựng nhà từ "
                                 + $"{definition?.GetType().Name ?? "null"}. Thêm nhánh cho nó trong factory.");
                    return null;
            }
        }

        #endregion
    }
}
