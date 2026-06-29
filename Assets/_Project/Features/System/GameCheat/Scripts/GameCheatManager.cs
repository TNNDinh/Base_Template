using TigerForge;

public class GameCheatManager
{
    public static bool isInfinityGenerator;

    public static bool isHideCheatUI;

    public static void Init()
    {
        ClearData();
    }

    private static void ClearData()
    {
        isInfinityGenerator = false;
    }

    public static void ShowCheatUI()
    {
        isHideCheatUI = false;
        EventManager.EmitEvent(EventName.CheatShowUIUA);
    }

    public static void HideCheatUI()
    {
        isHideCheatUI = true;
        EventManager.EmitEvent(EventName.CheatHideUIUA);
    }
}