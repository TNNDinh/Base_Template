namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Các loại nhà trong căn cứ. Thêm nhà mới thì thêm một mục ở đây, đừng chèn giữa —
    /// giá trị số được ghi vào file save.
    /// </summary>
    public enum HomeBuildingType
    {
        /// <summary>Nhà chính. Cấp của nó là điều kiện mở và nâng phần lớn nhà khác.</summary>
        TownHall = 0,

        /// <summary>Nhà lính. Nâng cấp lính và xem chỉ số lính.</summary>
        Barracks = 1,

        /// <summary>Tường thành. Trụ được tới cuối trận hay không là điều kiện thắng.</summary>
        Wall = 2
    }
}
