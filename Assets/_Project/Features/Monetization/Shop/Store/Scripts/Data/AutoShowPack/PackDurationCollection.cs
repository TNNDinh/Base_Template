using System;
using UnityEngine;

public class PackDurationCollection : ScriptableObject
{
    public PackDurationModel[] dataGroup;
}

[Serializable]
public class PackDurationModel : PackTemplateModel
{
    public long durationPack;
    public long durationCooldownShowUi;
    public PackDurationType type;
}