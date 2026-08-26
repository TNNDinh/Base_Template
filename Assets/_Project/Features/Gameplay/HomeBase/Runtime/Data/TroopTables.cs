using System;
using Ezg.Feature.HomeBase;
using UnityEngine;

/// <summary>
/// Một dòng cấp của lính. Mọi loại lính dùng chung đúng bộ cột này.
/// </summary>
[Serializable]
public class TroopRowModel : HomeLevelRow
{
    public int health;

    public int damage;

    /// <summary>Số đòn mỗi giây.</summary>
    public float attackSpeed;

    /// <summary>Tốc độ chạy, mét mỗi giây.</summary>
    public float moveSpeed;

    /// <summary>Tầm đánh, tính bằng mét.</summary>
    public float attackRange;
}

/// <summary>
/// Bảng cấp của một loại lính.
/// <para>
/// Mỗi loại lính có file CSV riêng, mà bộ nhập CSV tra class theo tên file — nên phải có một cặp
/// Model + Collection trùng tên mỗi file. Vì mọi loại lính dùng chung bộ cột, các cặp đó chỉ là
/// khai báo rỗng kế thừa lớp này; thêm loại lính mới tốn đúng hai dòng và một file CSV.
/// </para>
/// </summary>
public class TroopTableCollection : HomeLevelTableCollection
{
    public TroopRowModel[] dataGroups;

    public override int RowCount => dataGroups?.Length ?? 0;

    public override HomeLevelRow GetRow(int index) => dataGroups[index];

    /// <summary>Chỉ số của lính ở một cấp. Cấp ngoài bảng thì trả về bộ rỗng.</summary>
    public TroopStats StatsAt(int level)
    {
        if (!TryGetRow(level, out HomeLevelRow row) || row is not TroopRowModel troop) return default;

        return new TroopStats(troop.health, troop.damage, troop.attackSpeed, troop.moveSpeed, troop.attackRange);
    }
}

#region Bind từng file CSV — TroopSwordsman.csv, TroopArcher.csv, TroopKnight.csv

[Serializable] public class TroopSwordsmanModel : TroopRowModel { }

public class TroopSwordsmanCollection : TroopTableCollection { }

[Serializable] public class TroopArcherModel : TroopRowModel { }

public class TroopArcherCollection : TroopTableCollection { }

[Serializable] public class TroopKnightModel : TroopRowModel { }

public class TroopKnightCollection : TroopTableCollection { }

#endregion
