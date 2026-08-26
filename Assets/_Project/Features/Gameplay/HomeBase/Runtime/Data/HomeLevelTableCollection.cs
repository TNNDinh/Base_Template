using System.Collections.Generic;
using Ezg.Feature.HomeBase;
using UnityEngine;

/// <summary>
/// Bảng cấp của một thực thể, sinh ra từ file CSV riêng của nó.
/// <para>
/// Unity không tuần tự hoá được mảng đa hình, nên mỗi lớp con phải tự khai <c>dataGroups</c> theo
/// đúng kiểu dòng của mình rồi chỉ cần mở hai lối <see cref="RowCount"/> và <see cref="GetRow"/>.
/// Toàn bộ phần tra cứu — cấp cao nhất, giá, điều kiện — nằm ở đây, viết một lần.
/// </para>
/// </summary>
public abstract class HomeLevelTableCollection : ScriptableObject
{
    #region Fields

    private Dictionary<int, HomeLevelRow> _byLevel;
    private Dictionary<int, IBuildingRequirement[]> _requirementsByLevel;
    private int _maxLevel = -1;

    #endregion

    #region Public - Properties

    /// <summary>Cấp cao nhất có trong bảng.</summary>
    public int MaxLevel
    {
        get
        {
            EnsureIndex();
            return _maxLevel;
        }
    }

    #endregion

    #region Public - Abstract

    /// <summary>Số dòng trong bảng.</summary>
    public abstract int RowCount { get; }

    /// <summary>Dòng thứ <paramref name="index"/>.</summary>
    public abstract HomeLevelRow GetRow(int index);

    #endregion

    #region Public

    /// <summary>Bộ nhập CSV gọi sau khi nạp xong. Bỏ chỉ mục cũ để lần tra sau dựng lại.</summary>
    public void Convert() => Invalidate();

    /// <summary>Xoá chỉ mục, buộc dựng lại. Gọi khi sửa bảng lúc chạy.</summary>
    public void Invalidate()
    {
        _byLevel = null;
        _requirementsByLevel = null;
        _maxLevel = -1;
    }

    /// <summary>Dòng của một cấp.</summary>
    /// <returns><c>false</c> nếu bảng không có cấp đó.</returns>
    public bool TryGetRow(int level, out HomeLevelRow row)
    {
        EnsureIndex();
        return _byLevel.TryGetValue(level, out row);
    }

    /// <summary>Giá để lên tới <paramref name="level"/>. Không có dòng thì coi như miễn phí.</summary>
    public UpgradeCost GetCost(int level)
    {
        return TryGetRow(level, out HomeLevelRow row)
            ? new UpgradeCost(row.costResource, row.costAmount)
            : default;
    }

    /// <summary>
    /// Điều kiện để lên tới <paramref name="level"/>. Dựng một lần rồi giữ lại — mỗi lần UI hỏi
    /// "nâng được chưa" mà đẻ ra object mới thì rác sinh liên tục.
    /// </summary>
    public IReadOnlyList<IBuildingRequirement> GetRequirements(int level)
    {
        EnsureIndex();
        return _requirementsByLevel.TryGetValue(level, out IBuildingRequirement[] found)
            ? found
            : System.Array.Empty<IBuildingRequirement>();
    }

    #endregion

    #region Private

    private void EnsureIndex()
    {
        if (_byLevel != null) return;

        _byLevel = new Dictionary<int, HomeLevelRow>();
        _requirementsByLevel = new Dictionary<int, IBuildingRequirement[]>();
        _maxLevel = 0;

        int count = RowCount;
        for (int i = 0; i < count; i++)
        {
            HomeLevelRow row = GetRow(i);
            if (row == null) continue;

            _byLevel[row.level] = row;
            if (row.level > _maxLevel) _maxLevel = row.level;

            IBuildingRequirement requirement = HomeRequirementFactory.Create(row, this);
            if (requirement != null) _requirementsByLevel[row.level] = new[] { requirement };
        }
    }

    #endregion
}
