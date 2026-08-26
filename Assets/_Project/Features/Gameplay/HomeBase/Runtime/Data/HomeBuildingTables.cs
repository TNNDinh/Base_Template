using System;
using UnityEngine;

// Mỗi cái nhà có một file CSV riêng, nên phải có một cặp Model + Collection trùng đúng tên file —
// đó là cách bộ nhập CSV tra ra class. Gom cả ba nhà vào một file cho dễ đối chiếu.

#region Nhà chính — HomeTownHall.csv

/// <summary>Một dòng cấp của nhà chính. Nhà chính không có chỉ số riêng, chỉ có giá và điều kiện.</summary>
[Serializable]
public class HomeTownHallModel : HomeLevelRow
{
}

/// <summary>Bảng cấp nhà chính, sinh ra từ <c>HomeTownHall.csv</c>.</summary>
public class HomeTownHallCollection : HomeLevelTableCollection
{
    public HomeTownHallModel[] dataGroups;

    public override int RowCount => dataGroups?.Length ?? 0;

    public override HomeLevelRow GetRow(int index) => dataGroups[index];
}

#endregion

#region Nhà lính — HomeBarracks.csv

/// <summary>Một dòng cấp của nhà lính: sức chứa và trần cấp lính mà cấp này cho phép.</summary>
[Serializable]
public class HomeBarracksModel : HomeLevelRow
{
    /// <summary>Số lính mang ra trận được ở cấp này.</summary>
    public int troopCapacity;

    /// <summary>Cấp lính cao nhất mà cấp nhà này cho phép.</summary>
    public int maxTroopLevel;
}

/// <summary>Bảng cấp nhà lính, sinh ra từ <c>HomeBarracks.csv</c>.</summary>
public class HomeBarracksCollection : HomeLevelTableCollection
{
    public HomeBarracksModel[] dataGroups;

    public override int RowCount => dataGroups?.Length ?? 0;

    public override HomeLevelRow GetRow(int index) => dataGroups[index];

    /// <summary>Dòng nhà lính ở một cấp, kèm chỉ số riêng.</summary>
    public HomeBarracksModel Row(int level)
    {
        return TryGetRow(level, out HomeLevelRow row) ? row as HomeBarracksModel : null;
    }
}

#endregion

#region Tường thành — HomeWall.csv

/// <summary>Một dòng cấp của tường thành: máu tường chịu được.</summary>
[Serializable]
public class HomeWallModel : HomeLevelRow
{
    /// <summary>Máu của tường ở cấp này.</summary>
    public int health;
}

/// <summary>Bảng cấp tường thành, sinh ra từ <c>HomeWall.csv</c>.</summary>
public class HomeWallCollection : HomeLevelTableCollection
{
    public HomeWallModel[] dataGroups;

    public override int RowCount => dataGroups?.Length ?? 0;

    public override HomeLevelRow GetRow(int index) => dataGroups[index];

    /// <summary>Dòng tường ở một cấp, kèm chỉ số riêng.</summary>
    public HomeWallModel Row(int level)
    {
        return TryGetRow(level, out HomeLevelRow row) ? row as HomeWallModel : null;
    }
}

#endregion
