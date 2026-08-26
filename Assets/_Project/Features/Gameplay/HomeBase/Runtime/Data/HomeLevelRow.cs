using System;
using Ezg.Core.Utils;

namespace Ezg.Feature.HomeBase
{
    /// <summary>Loại điều kiện. Thêm loại mới thì thêm ở đây rồi thêm nhánh trong bộ dựng điều kiện.</summary>
    public enum HomeRequirementKind
    {
        /// <summary>Không có điều kiện. Ô <c>require_kind</c> để trống rơi vào đây.</summary>
        None = 0,

        /// <summary>Đòi một cái nhà khác đạt tới cấp nào đó.</summary>
        BuildingLevel = 1
    }
}

/// <summary>
/// Một dòng cấp trong CSV, phần dùng chung của mọi thực thể: cấp mấy, tốn gì, cần gì.
/// <para>
/// Mỗi nhà và mỗi loại lính có file CSV riêng, và lớp con thêm vào các cột chỉ số của riêng nó.
/// Để ở namespace gốc cho khớp bộ nhập CSV — nó tra class theo đúng tên file, không có tiền tố namespace.
/// </para>
/// </summary>
[Serializable]
public class HomeLevelRow
{
    /// <summary>Cấp mà dòng này mô tả. Dòng cấp 1 chính là điều kiện và giá mở khoá.</summary>
    public int level;

    public EnumBase.MoneyTypes costResource;

    /// <summary>Giá để lên tới cấp này. Bỏ trống hoặc 0 là miễn phí.</summary>
    public long costAmount;

    /// <summary>Loại điều kiện. Bỏ trống là không có điều kiện gì.</summary>
    public Ezg.Feature.HomeBase.HomeRequirementKind requireKind;

    /// <summary>Đối tượng của điều kiện, ví dụ <c>TownHall</c> khi đòi cấp nhà chính.</summary>
    public string requireId;

    /// <summary>Ngưỡng của điều kiện, ví dụ cấp tối thiểu.</summary>
    public int requireValue;
}
