using System;

public interface IUIFramework
{
    public void Show(string key, params object[] custom);
    public void Close(string key);

    public void Close(string key, Action onComplete);

    public void CloseLastUI();
}