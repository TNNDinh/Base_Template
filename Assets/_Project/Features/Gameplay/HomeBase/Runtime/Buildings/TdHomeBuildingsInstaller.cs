using System.Collections.Generic;
using Ezg.Feature.Shared.GameData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Nối hệ thống nhà vào scene: đọc danh mục, cắm tầng lưu của người chơi, dựng ra các nhà.
    /// <para>
    /// Đây là chỗ duy nhất trong scene biết tới <c>PlayerDataManager</c>. Bản thân các nhà chỉ
    /// thấy hai cổng <see cref="IHomeBuildingState"/> và <see cref="ITroopState"/>, nên test
    /// chúng bằng dữ liệu giả không cần đụng tới hệ save.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class TdHomeBuildingsInstaller : MonoBehaviour
    {
        #region Fields

        [Title("Cấu hình")]
        [SerializeField, Required] private HomeBuildingCatalog _catalog;

        [Tooltip("Trừ tài nguyên thật của người chơi khi nâng cấp. Tắt thì nâng cấp miễn phí, tiện lúc dựng UI.")]
        [SerializeField] private bool _spendPlayerResource = true;

        [Tooltip("Dựng ngay khi vào scene. Tắt nếu muốn tự gọi Install() theo luồng khởi động riêng.")]
        [SerializeField] private bool _installOnAwake = true;

        private HomeBuildingService _service;

        #endregion

        #region Public - Properties

        /// <summary>Sổ tra cứu các nhà. <c>null</c> nếu chưa dựng.</summary>
        public HomeBuildingService Service => _service;

        #endregion

        #region Initialize

        private void Awake()
        {
            if (_installOnAwake) Install();
        }

        #endregion

        #region Public

        /// <summary>
        /// Dựng lại toàn bộ hệ thống nhà. Gọi lại được sau khi đồng bộ save từ server.
        /// </summary>
        /// <param name="payment">
        /// Ví trừ tài nguyên. Để trống thì dùng ví thật của người chơi khi bật
        /// <c>Spend Player Resource</c>, ngược lại nâng cấp miễn phí.
        /// </param>
        public HomeBuildingService Install(IUpgradePayment payment = null)
        {
            if (_catalog == null)
            {
                Debug.LogError($"[{nameof(TdHomeBuildingsInstaller)}] Chưa gán danh mục nhà.", this);
                return null;
            }

            PlayerHomeBase saved = PlayerDataManager.HomeBase;
            IUpgradePayment wallet = payment ?? CreateDefaultPayment();

            var factory = new HomeBuildingFactory(saved, saved, wallet);
            _service = new HomeBuildingService(_catalog, factory);
            return _service;
        }

        #endregion

        #region Private

        private IUpgradePayment CreateDefaultPayment()
        {
            return _spendPlayerResource ? new PlayerResourceUpgradePayment() : new FreeUpgradePayment();
        }

        #endregion

        #region Public

        /// <summary>In nhanh trạng thái mọi nhà ra Console, để soi khi chỉnh bảng số.</summary>
        [Button]
        public void LogState()
        {
            if (_service == null)
            {
                Debug.Log($"[{nameof(TdHomeBuildingsInstaller)}] Chưa dựng.", this);
                return;
            }

            IReadOnlyList<IHomeBuilding> buildings = _service.All;
            for (int i = 0; i < buildings.Count; i++)
            {
                IHomeBuilding building = buildings[i];
                string state = building.IsUnlocked
                    ? $"cấp {building.Level}/{building.MaxLevel}"
                    : "chưa mở khoá";
                string blocker = building.CanUpgrade(out string reason) ? "nâng được" : reason;
                string price = building is HomeBuildingBase concrete ? concrete.NextUpgradeCost.ToString() : "?";
                Debug.Log($"{building.DisplayName}: {state} — giá {price} — {blocker}", this);
            }
        }

        #endregion
    }
}
