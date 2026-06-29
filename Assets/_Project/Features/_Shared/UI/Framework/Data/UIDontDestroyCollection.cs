using System;
using BlackFace.Libraries.Modules.UIModule;
using UnityEngine;
using Ezg.Feature.Shared.Config;

public class UIDontDestroyCollection : ScriptableObject
{
    public UIDontDestroyModel[] dataGroups;
}

[Serializable]
public class UIDontDestroyModel
{
    public GameEnums.Features feature;
    public UIManager.UIGroupName group = UIManager.UIGroupName.Overlay_Container;
}