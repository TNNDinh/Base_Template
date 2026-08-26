using System;
using System.Collections.Generic;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Dựng và giữ toàn bộ nhà của căn cứ, đồng thời là sổ tra cứu cho các điều kiện mở khoá.
    /// <para>
    /// Là class thuần, không phải MonoBehaviour — nhờ vậy test được luật lên cấp bằng dữ liệu
    /// giả mà không cần mở scene.
    /// </para>
    /// </summary>
    public class HomeBuildingService : IHomeBuildingRegistry
    {
        #region Fields

        private readonly List<IHomeBuilding> _buildings = new List<IHomeBuilding>();
        private readonly Dictionary<HomeBuildingType, IHomeBuilding> _byType =
            new Dictionary<HomeBuildingType, IHomeBuilding>();

        #endregion

        #region Initialize

        public HomeBuildingService(HomeBuildingCatalog catalog, IHomeBuildingFactory factory)
        {
            if (catalog == null || factory == null) return;

            Build(catalog, factory);
            UnlockStarterBuildings(catalog);
        }

        #endregion

        #region Public - Properties

        public IReadOnlyList<IHomeBuilding> All => _buildings;

        #endregion

        #region Public - Events

        /// <summary>Bắn khi bất kỳ nhà nào đổi cấp. Tiện cho UI chỉ cần nghe một chỗ.</summary>
        public event Action<IHomeBuilding> BuildingChanged;

        #endregion

        #region Public

        public bool TryGet(HomeBuildingType type, out IHomeBuilding building)
        {
            return _byType.TryGetValue(type, out building);
        }

        public int GetLevel(HomeBuildingType type)
        {
            return _byType.TryGetValue(type, out IHomeBuilding building) ? building.Level : 0;
        }

        /// <summary>Lấy nhà đã ép sẵn về kiểu mong muốn, ví dụ <see cref="ITroopTrainer"/>.</summary>
        /// <returns><c>false</c> nếu không có nhà đó hoặc nhà đó không phải kiểu này.</returns>
        public bool TryGet<T>(HomeBuildingType type, out T building) where T : class
        {
            building = null;
            if (!_byType.TryGetValue(type, out IHomeBuilding found)) return false;

            building = found as T;
            return building != null;
        }

        #endregion

        #region Private

        private void Build(HomeBuildingCatalog catalog, IHomeBuildingFactory factory)
        {
            HomeBuildingDefinition[] definitions = catalog.Definitions;
            for (int i = 0; i < definitions.Length; i++)
            {
                HomeBuildingDefinition definition = definitions[i];
                if (definition == null || _byType.ContainsKey(definition.Type)) continue;

                IHomeBuilding building = factory.Create(definition);
                if (building == null) continue;

                _buildings.Add(building);
                _byType.Add(definition.Type, building);
            }

            // Cắm sổ sau khi đã dựng đủ: điều kiện của nhà này hỏi cấp của nhà kia,
            // nên chưa có mặt đủ mà cắm sớm thì tra ra số sai.
            for (int i = 0; i < _buildings.Count; i++)
            {
                if (_buildings[i] is HomeBuildingBase concrete) concrete.BindRegistry(this);
                _buildings[i].Changed += OnBuildingChanged;
            }
        }

        /// <summary>Nhà có sẵn từ đầu — nhà chính — được đưa lên cấp 1 ngay lần chơi đầu tiên.</summary>
        private void UnlockStarterBuildings(HomeBuildingCatalog catalog)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                IHomeBuilding building = _buildings[i];
                if (building.IsUnlocked) continue;

                HomeBuildingDefinition definition = catalog.Find(building.Type);
                if (definition == null || !definition.UnlockedFromStart) continue;

                building.TryUnlock();
            }
        }

        private void OnBuildingChanged(IHomeBuilding building) => BuildingChanged?.Invoke(building);

        #endregion
    }
}
