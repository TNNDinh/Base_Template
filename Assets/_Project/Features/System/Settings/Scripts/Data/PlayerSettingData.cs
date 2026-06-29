using System;
using Ezg.Package.Factory;
using UnityEngine;

[Serializable]
public class PlayerSettingData : DataBase
{
    public float MusicVolume = 1f;

    public float SoundVolume = 1f;

    public bool Performance;

    public bool IsNotification = true;

    public bool IsVibrate = true;

    public bool IsShowDamage = true;

    public PlayerSettingNotificationData NotificationData = new();

    public SystemLanguage Language = SystemLanguage.English;

    public bool NewAccount = true;

    public bool ClaimDiscordReward;

    public long BonusTimeCheat;

    public string CreatedTime;

    public long OnlineTime;

    public bool IsRating;

    public long LastTimeSyncData;

    public string LastDevice;

    public string PlayerName;

    public int NumberChangeName;


    public int AvatarId;
    public int FrameId;
}

[Serializable]
public class PlayerSettingNotificationData
{
    public bool IsEnabled = true;
    public bool EnergyFull = true;
    public bool DailyReward = true;
    public bool CoinStreak = true;
    public bool DailyWeeklyQuests = true;
    public bool GuildEvent = true;
    public bool DungeonChallenge = true;
    public bool Adventure = true;
}