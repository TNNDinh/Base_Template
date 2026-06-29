public static partial class EventName
{
    public const string CameraOrthographicSizeChanged = "CameraOrthographicSizeChanged";

    public const string NextDayEvent = "NextDayEvent";

    public const string ForceSyncData = "ForceSyncData";

    public const string ChangeUIOverlays = "ChangeUIOverlays";
    public const string ChangeUIDefault = "ChangeUIDefault";

    public static bool OnChangeScene;
    public static bool OnChangedScene;
    public static bool OnShowFeature;
    public static bool OnCloseFeature;
    public static bool InitFirebaseSuccessfully;
    public static bool OnGameEventsLoaded;
    public static bool OnGameEventChangeStatus; //Thay đổi trạng thái đóng mở event
    public static bool OnLoadAccountSuccess;
    public static bool OnChangeLimitEnergy;
    public static bool CheatChanged;
    public static bool CheatLevelType;
    public static bool BattleCheatUpdateUI;
    public static bool OnEnableTouch;
    public static bool OnDisableTouch;
    public static bool NextWeekEvent;
    public static bool NextMonthEvent;
    public static bool LoadedBanner;
    public static bool HideBanner;
}