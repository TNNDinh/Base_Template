namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Một điều kiện phải thoả trước khi mở khoá hoặc nâng cấp nhà.
    /// <para>
    /// Điều kiện là thứ hay đẻ thêm nhất trong game xây nhà (cấp nhà chính, đủ tài nguyên,
    /// qua màn N, mua gói...). Tách ra thành giao diện để thêm loại mới chỉ việc viết một class,
    /// không phải mở lại code của từng cái nhà.
    /// </para>
    /// </summary>
    public interface IBuildingRequirement
    {
        /// <summary>Điều kiện đã thoả chưa.</summary>
        bool IsMet(IHomeBuildingRegistry buildings);

        /// <summary>Câu mô tả điều kiện để hiện lên UI hoặc log.</summary>
        string Describe(IHomeBuildingRegistry buildings);
    }
}
