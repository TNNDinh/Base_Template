using System.Collections.Generic;
using Ezg.Feature.HomeBase;
using Ezg.Package.Factory;

/// <summary>
/// Dữ liệu lưu của căn cứ nhà: cấp từng nhà và cấp từng loại lính.
/// <para>
/// Dùng Dictionary khoá bằng enum — Newtonsoft ghi ra TÊN enum chứ không phải số, nên sau này
/// chèn thêm mục vào giữa enum cũng không làm sai lệch file save cũ.
/// </para>
/// </summary>
public class PlayerHomeBaseData : DataBase
{
    public Dictionary<HomeBuildingType, int> buildingLevels = new Dictionary<HomeBuildingType, int>();

    public Dictionary<TroopType, int> troopLevels = new Dictionary<TroopType, int>();
}
