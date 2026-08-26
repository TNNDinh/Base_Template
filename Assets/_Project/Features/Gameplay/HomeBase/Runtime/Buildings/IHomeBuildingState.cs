namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Nơi đọc/ghi cấp của từng nhà. Nhà chỉ biết tới giao diện này, không biết dữ liệu nằm ở
    /// PlayerPrefs, server hay chỉ trong bộ nhớ lúc test.
    /// </summary>
    public interface IHomeBuildingState
    {
        /// <summary>Cấp hiện tại. Trả về 0 nghĩa là nhà chưa được mở khoá.</summary>
        int GetBuildingLevel(HomeBuildingType type);

        /// <summary>Ghi cấp mới rồi lưu lại.</summary>
        void SetBuildingLevel(HomeBuildingType type, int level);
    }

    /// <summary>
    /// Nơi đọc/ghi cấp của từng loại lính. Tách khỏi <see cref="IHomeBuildingState"/> vì
    /// chỉ nhà lính cần tới — nhà khác không phải biết lính là gì.
    /// </summary>
    public interface ITroopState
    {
        /// <summary>Cấp hiện tại của lính. 0 nghĩa là chưa mở.</summary>
        int GetTroopLevel(TroopType type);

        /// <summary>Ghi cấp mới rồi lưu lại.</summary>
        void SetTroopLevel(TroopType type, int level);
    }
}
