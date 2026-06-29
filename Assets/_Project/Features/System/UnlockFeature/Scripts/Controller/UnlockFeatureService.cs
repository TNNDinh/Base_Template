using System.Linq;
using Ezg.Core.Adapter;
using Ezg.Feature.Shared;
using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

public static class UnlockFeatureService
{
    public static void Init()
    {
        EventManager.StartListening(nameof(EventName.UnlockFeature), () => ShowUnlockFeature());

        //QueueUnlockFeature();
    }

    //public static void QueueUnlockFeature()
    //{
    //    try
    //    {
    //        foreach (var feature in DataManager.UnlockFeature.dataGroups.Where(x => x.showWhenOpen))
    //            if (IsUnlockFeature(feature.feature))
    //                PlayerDataManager.UnlockFeature.AddQueueFeature(feature.feature);
    //    }
    //    catch
    //    {
    //        // ignored
    //    }
    //}

    public static bool ShowUnlockFeature()
    {
        if (PlayerDataManager.UnlockFeature.dataBase.LevelUnlocked.Contains(PlayerDataManager.Campaign.Level))
            return false;

        var featureInThisLevel =
            DataManager.UnlockFeature.dataGroups.FirstOrDefault(x => x.unlockValue == PlayerDataManager.Campaign.Level);
        if (featureInThisLevel == null) return false;

        PlayerDataManager.UnlockFeature.dataBase.LevelUnlocked.Add(PlayerDataManager.Campaign.Level);
        PlayerDataManager.UnlockFeature.Save();

        //UIManager.Instance.Show(GameEnums.Features.UnlockFeature).Forget();

        return true;
    }

    //private static void ShowUnlockFeature()
    //{
    //    var features = PlayerDataManager.UnlockFeature.GetQueueFeature();
    //    //AnhNT disable -> cho phép đè lên UI khác
    //    //var modalChildCount = UIManager.Instance.GetChildCountGroup(UIManager.UIGroupName.Modal_Container);
    //    if (features.Count > 0)// && modalChildCount == 0)
    //    {
    //        UIManager.Instance.Show(GameEnums.Features.UnlockFeature,
    //            data: features.Select(x => new UnlockFeatureProperty(x)).ToList()).Forget();
    //        PlayerDataManager.UnlockFeature.ClearAllShowed();
    //        features.Clear();
    //        PlayerDataManager.UnlockFeature.Save();
    //    }
    //}

    public static bool IsUnlockFeature(GameEnums.Features feature)
    {
        var featureData = DataManager.UnlockFeature.dataGroups.FirstOrDefault(x => x.feature == feature);
        if (featureData == null) return false;

        var unlockValue = featureData?.unlockValue;
        var unlockType = DataManager.UnlockFeature.dataGroups.FirstOrDefault(x => x.feature == feature)?.unlockType;
        switch (unlockType)
        {
            case UnlockFeatureType.LevelAccount:
                // AccountService.GetLevel() đã bị gỡ -> fallback sang Campaign.Level (số liệu tiến trình còn lại).
                // Với unlock_value = 0 thì luôn mở sẵn từ đầu, giữ đúng ý đồ cũ (GetLevel() >= 0).
                return PlayerDataManager.Campaign.Level >= unlockValue;
            case UnlockFeatureType.Level:
                var stageId = PlayerDataManager.Campaign.Level;
                return stageId >= unlockValue;
            default:
                return false;
        }
    }

    public static bool IsUnlocked(GameEnums.Features feature)
    {
        var featureData = DataManager.UnlockFeature.dataGroups.FirstOrDefault(x => x.feature == feature);
        if (featureData == null) return false;

        return IsUnlockFeature(feature);
    }

    public static int GetUnlockValue(GameEnums.Features feature)
    {
        return DataManager.UnlockFeature.dataGroups.FirstOrDefault(x => x.feature == feature).unlockValue;
    }

    public static Sprite GetIconFeature(GameEnums.Features feature)
    {
        var key = $"icon_feature_{(int)feature}";
        return ResLoader.Load<Sprite>(PathUtils.PathPrefabIconFeature + key);
    }

    //public static Sprite GetIconFeature(this GameEnums.Features feature)
    //{
    //    return DataManager.UnlockFeatureView.GetConfigByType(feature).icon;
    //}
}