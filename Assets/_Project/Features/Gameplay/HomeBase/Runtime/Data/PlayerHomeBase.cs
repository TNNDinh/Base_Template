using System.Collections.Generic;
using Ezg.Feature.HomeBase;
using Ezg.Package.Factory;

/// <summary>
/// Module lưu của căn cứ nhà. Cài luôn hai cổng <see cref="IHomeBuildingState"/> và
/// <see cref="ITroopState"/> để hệ thống nhà ghi thẳng vào đây mà không cần biết
/// bên dưới là PlayerPrefs.
/// </summary>
public class PlayerHomeBase : DataPlayerBaseGeneric<PlayerHomeBaseData>, IHomeBuildingState, ITroopState
{
    #region Constants

    /// <summary>Cấp của thứ chưa được mở khoá.</summary>
    private const int LOCKED_LEVEL = 0;

    #endregion

    #region Public

    public int GetBuildingLevel(HomeBuildingType type) => Read(EnsureBuildings(), type);

    public void SetBuildingLevel(HomeBuildingType type, int level)
    {
        EnsureBuildings()[type] = level;
        Save();
    }

    public int GetTroopLevel(TroopType type) => Read(EnsureTroops(), type);

    public void SetTroopLevel(TroopType type, int level)
    {
        EnsureTroops()[type] = level;
        Save();
    }

    #endregion

    #region Private

    protected override void SetupDefaultData()
    {
        dataBase.buildingLevels = new Dictionary<HomeBuildingType, int>();
        dataBase.troopLevels = new Dictionary<TroopType, int>();
    }

    /// <summary>
    /// Save cũ được ghi trước khi có trường này thì Newtonsoft trả về null — dựng lại cho khỏi
    /// văng, thay vì bắt mọi chỗ gọi phải tự kiểm tra.
    /// </summary>
    private Dictionary<HomeBuildingType, int> EnsureBuildings()
    {
        return dataBase.buildingLevels ??= new Dictionary<HomeBuildingType, int>();
    }

    private Dictionary<TroopType, int> EnsureTroops()
    {
        return dataBase.troopLevels ??= new Dictionary<TroopType, int>();
    }

    private static int Read<TKey>(Dictionary<TKey, int> source, TKey key)
    {
        return source.TryGetValue(key, out int level) ? level : LOCKED_LEVEL;
    }

    #endregion
}
