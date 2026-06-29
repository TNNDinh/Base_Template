using System;
using UnityEngine;

public class BaseScreenLogic<T> : MonoBehaviour, IScreenLogic
{
    public T property;

    public event Action<Action> OnClose;

    public virtual void SetData(object data)
    {
        property = (T)data;
    }

    protected virtual void Close()
    {
        OnClose?.Invoke(null);
    }
}

public class BaseScreenLogic : MonoBehaviour, IScreenLogic
{
    public event Action<Action> OnClose;

    public virtual void SetData(object data)
    {
    }

    protected virtual void Close()
    {
        OnClose?.Invoke(null);
    }
}