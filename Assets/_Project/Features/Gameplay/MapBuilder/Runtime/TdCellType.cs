namespace Ezg.Feature.MapBuilder
{
    /// <summary>
    /// Loại ô trên lưới map thủ thành.
    /// Giá trị số được ghi thẳng xuống asset — KHÔNG đổi thứ tự khi thêm loại mới.
    /// </summary>
    public enum TdCellType
    {
        /// <summary>Ô chặn: quái không đi qua, người chơi không xây được (vực, nước, vách đá).</summary>
        Blocked = 0,

        /// <summary>Ô đất trống: đặt trụ/nhà được, quái KHÔNG đi qua.</summary>
        Buildable = 1,

        /// <summary>Ô đường: quái đi qua, KHÔNG đặt trụ được.</summary>
        Path = 2,
    }
}
