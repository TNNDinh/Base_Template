using System;
using System.Collections.Generic;
using System.Linq;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Utils;
using Ezg.Feature.LocalNotification;
using Ezg.Feature.Shared;
using TigerForge;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

public class PackDurationService
{
    public static List<PackDurationType> packDurationActive = new();

    private static bool _isLooping;
    public static PlayerPackDurationData PlayerData => PlayerDataManager.PlayerPackDurationData;

    public static void Init()
    {
        _isLooping = false;
        packDurationActive.Clear();
        RegisEvent();
        GetAllPackActive();
        AutoTimePackDuration();
    }

    private static void GetAllPackActive()
    {
        foreach (var packDuration in PlayerData.dataBase.packDurations)
        {
            AddPackDurationActive(packDuration.Key);
            ShowUI(packDuration.Key);
        }
    }

    private static void AddPackDurationActive(PackDurationType type)
    {
        if (packDurationActive.Contains(type)) return;
        packDurationActive.Add(type);
        EventManager.StartListening(EventName.PurchasePackDuration, PurchasePackDuration);
        EventManager.EmitEvent(EventName.ActivePackDuration + type);
        RegisterReminderByType(type);
        //ShowUI(type);
    }

    private static void RemovePackDurationActive(PackDurationType type)
    {
        if (packDurationActive.Contains(type))
        {
            packDurationActive.Remove(type);

            EventManager.EmitEvent(EventName.DeActivePackDuration + type);
            StopReminderByType(type);
        }

        if (packDurationActive.Count == 0)
            EventManager.StopListening(EventName.PurchasePackDuration, PurchasePackDuration);
    }


    private static void PurchasePackDuration()
    {
        var type = EventManager.GetData<PackDurationType>(EventName.PurchasePackDuration);
        RemovePackData(type);
        RemovePackDurationActive(type);
        AddPackBought(type);
    }

    private static void RegisterReminderByType(PackDurationType type)
    {
        switch (type)
        {
            case PackDurationType.StarterPack:
                LocalNotificationRules.RegisterStarterPackReminder();
                break;
            case PackDurationType.OpenningPack:
                LocalNotificationRules.RegisterSpecialOfferReminder();
                break;
        }
    }

    private static void StopReminderByType(PackDurationType type)
    {
        switch (type)
        {
            case PackDurationType.StarterPack:
                LocalNotificationRules.StopStarterPackReminder();
                break;
            case PackDurationType.OpenningPack:
                LocalNotificationRules.StopSpecialOfferReminder();
                break;
        }
    }

    public static void RegisEvent()
    {
        EventManager.StartListening(EventName.ShowPackDuration, ShowPack);
        EventManager.StartListening(EventName.UpLevelInData, CheckUnlockPack);
        EventManager.StartListening(EventName.ShowPopupPackDuration, ShowPopupForce);
    }

    public static void UnRegisEvent()
    {
        EventManager.StopListening(EventName.ShowPackDuration, ShowPack);
        EventManager.StopListening(EventName.ShowPopupPackDuration, ShowPopupForce);
        EventManager.StopListening(EventName.UpLevelInData, CheckUnlockPack);
    }

    private static void CheckUnlockPack()
    {
        foreach (PackDurationType pack in Enum.GetValues(typeof(PackDurationType)))
        {
            var types = GameEnums.Features.none;
            switch (pack)
            {
                case PackDurationType.StarterPack:
                {
                    types = GameEnums.Features.StarterPack;
                    break;
                }
            }

            if (types != GameEnums.Features.none && UnlockFeatureService.IsUnlocked(types) &&
                !PlayerData.dataBase.packShowByLevel.Contains(pack))
            {
                var properties = new PackDurationProperties
                {
                    type = pack,
                    isShowPopup = false
                };
                PlayerData.dataBase.packShowByLevel.Add(pack);
                PlayerData.Save();
                EventManager.EmitEventData(EventName.ShowPackDuration, properties);
            }
        }
    }

    public static void ShowPack()
    {
        var properties = EventManager.GetData<PackDurationProperties>(EventName.ShowPackDuration);
        HandlePackDurationType(properties);
    }

    private static void HandlePackDurationType(PackDurationProperties properties)
    {
        long duration = 0;
        var model = GetModelByType(properties.type);

        if (model == null) return;
        if (!IsPackActive(properties.type) && CanActivePack(properties.type))
        {
            duration = TimeManager.GetOnlineNow() + model.durationPack;
            SetDuration(properties.type, duration);
            SetIdScene(properties.type);
            AddPackDurationActive(properties.type);
            AutoTimePackDuration();
            if (properties.isShowPopup) ShowUI(properties.type);
        }
    }

    private static bool CanActivePack(PackDurationType type)
    {
        var model = DataManager.PackDurationBuyOnlyOne.dataGroups.FirstOrDefault(x => x.type == type);
        if (model != null)
            if (PlayerData.dataBase.packsBought.TryGetValue(model.type, out var pack))
                return false;

        return true;
    }

    private static void ShowUI(PackDurationType type)
    {
        if (!CanShowUI(type)) return;
        HandleShowUI(type);
        SetTimeCooldownUI(type);
    }

    private static void HandleShowUI(PackDurationType type)
    {
        switch (type)
        {
            case PackDurationType.StarterPack:
                UIManager.Instance.AddFeatureSequense(GameEnums.Scenes.HomeScene, GameEnums.Features.StarterPack);
                break;
            case PackDurationType.OpenningPack:
                UIManager.Instance.AddFeatureSequense(GameEnums.Scenes.HomeScene, GameEnums.Features.OpenningPack);
                break;
        }
    }

    private static void ShowPopupForce()
    {
        var type = EventManager.GetData<PackDurationType>(EventName.ShowPopupPackDuration);
        if (GetDurationPack(type) > 0) HandleShowUI(type);
    }

    private static bool CanShowUI(PackDurationType type)
    {
        if (PlayerData.dataBase.packDurationsToShowUI.TryGetValue(type, out var durationUI) &&
            PlayerData.dataBase.packDurations.TryGetValue(type, out var duration))
            return durationUI <= TimeManager.GetOnlineNow() && duration > TimeManager.GetOnlineNow();

        return true;
    }

    private static void SetTimeCooldownUI(PackDurationType type)
    {
        PlayerData.dataBase.packDurationsToShowUI[type] =
            TimeManager.GetOnlineNow() + GetModelByType(type).durationCooldownShowUi;
        PlayerData.Save();
    }

    private static void SetIdScene(PackDurationType type)
    {
        // removed: BuildUpGoalManager
        PlayerData.Save();
    }

    private static PackDurationModel GetModelByType(PackDurationType type)
    {
        // removed: StarterPackService, OpenningPackService
        return null; // gameplay removed
    }

    private static async void AutoTimePackDuration()
    {
        if (_isLooping) return;
        Start:
        _isLooping = true;
        var listTemp = packDurationActive.ToList();
        foreach (var type in listTemp) CheckPackExpired(type);

        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        if (packDurationActive.Count >= 0) goto Start;

        _isLooping = false;
    }

    private static void SetDuration(PackDurationType type, long duration)
    {
        PlayerData.dataBase.packDurations[type] = duration;
        PlayerData.Save();
    }

    private static void CheckPackExpired(PackDurationType type)
    {
        if (PlayerData.dataBase.packDurations.TryGetValue(type, out var duration))
            if (duration <= TimeManager.GetOnlineNow())
            {
                RemovePackData(type);
                RemovePackDurationActive(type);
                EventManager.EmitEventData(EventName.PackDurationExpired, type);
            }
    }

    private static void RemovePackData(PackDurationType type)
    {
        PlayerData.dataBase.packDurations.Remove(type);
        PlayerData.dataBase.packDurationsToShowUI.Remove(type);
        PlayerData.dataBase.packScenes.Remove(type);
        PlayerData.Save();
    }

    private static void AddPackBought(PackDurationType type)
    {
        if (PlayerData.dataBase.packsBought.TryGetValue(type, out var manyBuy))
            PlayerData.dataBase.packsBought[type] += 1;
        else
            PlayerData.dataBase.packsBought.Add(type, 1);
        PlayerData.Save();
    }

    public static bool IsPackActive(PackDurationType type)
    {
        if (PlayerData.dataBase.packDurations.TryGetValue(type, out var duration)) return true;

        return false;
    }

    public static long GetDurationPack(PackDurationType type)
    {
        return PlayerData.dataBase.packDurations.GetValueOrDefault(type, 0);
    }

    public static int GetScenePack(PackDurationType type)
    {
        return PlayerData.dataBase.packScenes.GetValueOrDefault(type, 0);
    }
}

[Serializable]
public enum PackDurationType
{
    None = 0,
    StarterPack = 1,
    OpenningPack = 2
}

public class PackDurationProperties
{
    public bool isShowPopup = true;
    public PackDurationType type;
}