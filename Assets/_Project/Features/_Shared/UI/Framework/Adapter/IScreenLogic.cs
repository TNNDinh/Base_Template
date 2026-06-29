using System;

public interface IScreenLogic
{
    public event Action<Action> OnClose;

    void SetData(object data);
}