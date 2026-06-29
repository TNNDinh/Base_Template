using System;

public static class UIAdapter
{
    private static readonly IUIFramework _module = new UIBlackFace();

    public static void Show(string key, params object[] custom)
    {
        _module.Show(key, custom);
    }

    public static void Close(string key)
    {
        _module.Close(key);
    }

    public static void Close(string key, Action onComplete)
    {
        _module.Close(key, onComplete);
    }

    public static void CloseLastUI()
    {
        _module.CloseLastUI();
    }
}