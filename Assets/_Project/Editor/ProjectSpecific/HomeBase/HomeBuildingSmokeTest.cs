using System.Collections.Generic;
using System.Text;
using Ezg.Feature.HomeBase;
using UnityEditor;
using UnityEngine;

namespace Ezg.Feature.HomeBase.EditorTools
{
    /// <summary>
    /// Tầng lưu chạy trong bộ nhớ, dùng cho test. Nhờ nhà chỉ phụ thuộc vào hai cổng
    /// <see cref="IHomeBuildingState"/> và <see cref="ITroopState"/> nên thay tầng lưu thật
    /// bằng cái này là test được luật lên cấp mà không đụng tới save của người chơi.
    /// </summary>
    public class InMemoryHomeBaseState : IHomeBuildingState, ITroopState
    {
        #region Fields

        private readonly Dictionary<HomeBuildingType, int> _buildings = new Dictionary<HomeBuildingType, int>();
        private readonly Dictionary<TroopType, int> _troops = new Dictionary<TroopType, int>();

        #endregion

        #region Public

        public int GetBuildingLevel(HomeBuildingType type) => _buildings.TryGetValue(type, out int v) ? v : 0;

        public void SetBuildingLevel(HomeBuildingType type, int level) => _buildings[type] = level;

        public int GetTroopLevel(TroopType type) => _troops.TryGetValue(type, out int v) ? v : 0;

        public void SetTroopLevel(TroopType type, int level) => _troops[type] = level;

        #endregion
    }

    /// <summary>
    /// Chạy thử toàn bộ luật mở khoá / lên cấp nhà và lên cấp lính trên dữ liệu thật trong project,
    /// nhưng với tầng lưu trong bộ nhớ. In kết quả ra Console.
    /// </summary>
    public static class HomeBuildingSmokeTest
    {
        #region Constants

        private const string MENU_PATH = "Tools/Tower Defense/Test hệ thống nhà";
        private const string CATALOG_FILTER = "t:HomeBuildingCatalog";

        #endregion

        #region Public

        [MenuItem(MENU_PATH)]
        public static void Run()
        {
            HomeBuildingCatalog catalog = LoadCatalog();
            if (catalog == null)
            {
                Debug.LogError($"[{nameof(HomeBuildingSmokeTest)}] Không tìm thấy asset HomeBuildingCatalog.");
                return;
            }

            var state = new InMemoryHomeBaseState();

            // Ví miễn phí: test này soi luật mở khoá, không soi kinh tế — nếu dùng ví thật thì
            // kết quả phụ thuộc số vàng đang có trong save, chạy hai lần ra hai kiểu.
            var service = new HomeBuildingService(catalog, new HomeBuildingFactory(state, state, null));

            var log = new StringBuilder();
            log.AppendLine("=== TRẠNG THÁI ĐẦU ===");
            Dump(service, log);

            log.AppendLine();
            log.AppendLine("=== THỬ MỞ NHÀ LÍNH VÀ TƯỜNG KHI NHÀ CHÍNH MỚI CẤP 1 ===");
            TryUnlock(service, HomeBuildingType.Barracks, log);
            TryUnlock(service, HomeBuildingType.Wall, log);

            log.AppendLine();
            log.AppendLine("=== NÂNG NHÀ CHÍNH LÊN 3 RỒI THỬ LẠI ===");
            RaiseTo(service, HomeBuildingType.TownHall, 3, log);
            TryUnlock(service, HomeBuildingType.Barracks, log);
            TryUnlock(service, HomeBuildingType.Wall, log);

            log.AppendLine();
            log.AppendLine("=== TRẦN CẤP LÍNH THEO CẤP NHÀ LÍNH ===");
            TestTroops(service, log);

            log.AppendLine();
            log.AppendLine("=== TƯỜNG THÀNH ===");
            TestWall(service, log);

            Debug.Log(log.ToString());
        }

        #endregion

        #region Private

        private static void Dump(HomeBuildingService service, StringBuilder log)
        {
            IReadOnlyList<IHomeBuilding> buildings = service.All;
            for (int i = 0; i < buildings.Count; i++)
            {
                IHomeBuilding building = buildings[i];
                string level = building.IsUnlocked ? $"cấp {building.Level}/{building.MaxLevel}" : "KHOÁ";
                string price = building is HomeBuildingBase concrete ? concrete.NextUpgradeCost.ToString() : "?";
                log.AppendLine($"  {building.DisplayName,-12} {level,-14} giá {price,-14} "
                             + $"{string.Join(" | ", building.DescribeNextRequirements())}");
            }
        }

        private static void TryUnlock(HomeBuildingService service, HomeBuildingType type, StringBuilder log)
        {
            if (!service.TryGet(type, out IHomeBuilding building)) return;

            bool allowed = building.CanUnlock(out string reason);
            bool done = building.TryUnlock();
            log.AppendLine($"  mở {building.DisplayName}: cho phép={allowed} thực thi={done} "
                         + $"cấp={building.Level} {(allowed ? string.Empty : "-> " + reason)}");
        }

        private static void RaiseTo(HomeBuildingService service, HomeBuildingType type, int level, StringBuilder log)
        {
            if (!service.TryGet(type, out IHomeBuilding building)) return;

            while (building.Level < level && building.TryUpgrade()) { }
            log.AppendLine($"  {building.DisplayName} -> cấp {building.Level}");
        }

        private static void TestTroops(HomeBuildingService service, StringBuilder log)
        {
            if (!service.TryGet(HomeBuildingType.Barracks, out ITroopTrainer trainer))
            {
                log.AppendLine("  không lấy được ITroopTrainer");
                return;
            }

            log.AppendLine($"  sức chứa={trainer.TroopCapacity} trần cấp lính={trainer.MaxTroopLevel}");

            IReadOnlyList<TroopDefinition> troops = trainer.Troops;
            for (int i = 0; i < troops.Count; i++)
            {
                TroopType type = troops[i].Type;
                TroopStats before = trainer.GetTroopStats(type);
                log.AppendLine($"  {troops[i].DisplayName,-10} cấp {trainer.GetTroopLevel(type)} "
                             + $"HP={before.Health} DMG={before.Damage} DPS={before.DamagePerSecond:F1} "
                             + $"tầm={before.AttackRange} — nâng tốn {trainer.GetTroopUpgradeCost(type)}");
            }

            // Nâng một loại lính tới khi bị nhà lính chặn lại.
            TroopType target = troops[0].Type;
            int guard = 0;
            while (trainer.TryUpgradeTroop(target) && guard++ < 50) { }

            bool can = trainer.CanUpgradeTroop(target, out string reason);
            TroopStats after = trainer.GetTroopStats(target);
            log.AppendLine($"  nâng {troops[0].DisplayName} hết cỡ -> cấp {trainer.GetTroopLevel(target)} "
                         + $"HP={after.Health} DMG={after.Damage}");
            log.AppendLine($"  nâng tiếp được? {can} -> {reason}");
        }

        private static void TestWall(HomeBuildingService service, StringBuilder log)
        {
            if (!service.TryGet(HomeBuildingType.Wall, out IWallDefense wall))
            {
                log.AppendLine("  không lấy được IWallDefense");
                return;
            }

            service.TryGet(HomeBuildingType.Wall, out IHomeBuilding building);
            log.AppendLine($"  cấp {building.Level} máu tường={wall.MaxHealth}");

            while (building.TryUpgrade()) { }
            log.AppendLine($"  nâng hết cỡ -> cấp {building.Level} máu tường={wall.MaxHealth}");

            building.CanUpgrade(out string reason);
            log.AppendLine($"  chặn tại: {reason}");
        }

        private static HomeBuildingCatalog LoadCatalog()
        {
            string[] guids = AssetDatabase.FindAssets(CATALOG_FILTER);
            if (guids.Length == 0) return null;

            return AssetDatabase.LoadAssetAtPath<HomeBuildingCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        #endregion
    }
}
