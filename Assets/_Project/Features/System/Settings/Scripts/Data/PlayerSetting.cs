using System;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Package.Factory;
using UnityEngine;
using Random = UnityEngine.Random;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;

public class PlayerSetting : DataPlayerBaseGeneric<PlayerSettingData>
{
    public int NumberChangeName
    {
        get => dataBase.NumberChangeName;
        set
        {
            dataBase.NumberChangeName = value;
            Save();
        }
    }

    protected override void AfterLoad()
    {
        base.AfterLoad();
        //TimeManager.BonusTimeNow = dataBase.BonusTimeCheat;
    }

    public PlayerSettingNotificationData GetNotiData()
    {
        return dataBase.NotificationData ?? (dataBase.NotificationData = new PlayerSettingNotificationData());
    }

    public float GetMusic()
    {
        return dataBase.MusicVolume;
    }

    public float GetSound()
    {
        return dataBase.SoundVolume;
    }

    public bool GetPerformance()
    {
        return dataBase.Performance;
    }

    public bool GetVibrate()
    {
        return dataBase.IsVibrate;
    }

    public bool GetNotification()
    {
        return dataBase.IsNotification;
    }

    public bool GetShowDmg()
    {
        return dataBase.IsShowDamage;
    }

    public SystemLanguage GetLanguage()
    {
        return dataBase.Language;
    }

    public void SetMusicVolumne(float value)
    {
        dataBase.MusicVolume = value;
    }

    public void SetSoundVolume(float value)
    {
        dataBase.SoundVolume = value;
    }

    public void SetOnOffSound(bool isOn)
    {
        dataBase.SoundVolume = isOn ? 1 : 0;
    }

    public void SetOnOffMusic(bool isOn)
    {
        dataBase.MusicVolume = isOn ? 1 : 0;
    }

    public void SetPerformance(bool isOn)
    {
        dataBase.Performance = isOn;
    }

    public void SetShowDmg(bool isOn)
    {
        dataBase.IsShowDamage = isOn;
    }

    public void SetVibrate(bool isOn)
    {
        dataBase.IsVibrate = isOn;
    }

    public void SetNotification(bool isOn)
    {
        dataBase.IsNotification = isOn;
    }

    public void SetLanguage(SystemLanguage languages)
    {
        dataBase.Language = languages;
    }

    public bool IsNewAccount()
    {
        return dataBase.NewAccount;
    }

    public void EnsureCreatedTime()
    {
        if (!string.IsNullOrEmpty(dataBase.CreatedTime)) return;

        dataBase.CreatedTime = GetCreatedDateString();
        Save();
    }

    private static string GetCreatedDateString()
    {
        if (TimeManager.IsOnline == true && TimeManager.StartOnlineTime > 0)
            return DateTimeOffset.FromUnixTimeSeconds(TimeManager.GetOnlineNow()).UtcDateTime.ToString("yyyy-MM-dd");

        return DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    public void SetPlayerName(string name)
    {
        dataBase.PlayerName = name;
        PlayerDataManager.Settings.Save();
        // RankingManager.UpdateRanking(GameModeManager.GameModeData.eventAdventure,
        //     PlayerDataManager.GameplayAdventure.dataBase.Score,(() =>
        //     {
        //         RankingManager.ResetCache();
        //     }));
        // RankingManager.UpdateRankingClassic(() =>
        // {
        //     RankingManager.ResetRankingClassicCache();
        // });
    }


    public void RemoveNewAccount()
    {
        dataBase.NewAccount = false;
        var textName = "Player" + Random.Range(1000, 99999);
        SetPlayerName(textName);
        DataPlayer.SaveAllData();
    }
}