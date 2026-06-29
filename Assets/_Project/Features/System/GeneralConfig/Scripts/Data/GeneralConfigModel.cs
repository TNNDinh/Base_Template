using System;

[Serializable]
public struct GeneralConfigModel
{
    /// <summary>
    ///     Năng lượng tiêu tốn cho mỗi màn chơi
    /// </summary>
    public int energyPerStage;

    /// <summary>
    ///     Năng lượng tối da
    /// </summary>
    public int maxEnergy;

    /// <summary>
    ///     Năng lượng hồi lại
    /// </summary>
    public int restoreEnergyValue;

    /// <summary>
    ///     Thời gian hồi năng lượng
    /// </summary>
    public float restoreEnergyTime;

    /// <summary>
    ///     Thời gian revive khi dùng gold
    /// </summary>
    public int reviveTimeByGold;

    /// <summary>
    ///     Thời gian revive khi xem reward ads
    /// </summary>
    public int reviveTimeByAds;

    /// <summary>
    ///     Thời gian recommend booster
    /// </summary>
    public float timeRecommendBooster;

    /// <summary>
    ///     Phần thưởng sau khi xem inter
    /// </summary>
    public Resource rewardInter;
}