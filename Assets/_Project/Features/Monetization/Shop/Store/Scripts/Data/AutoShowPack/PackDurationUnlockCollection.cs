using System;
using UnityEngine;

public class PackDurationUnlockCollection : ScriptableObject
{
    public PackDurationUnlockModel dataGroups;
}

[Serializable]
public class PackDurationUnlockModel
{
    public PackDurationType packType;
    public int levelUnlock;
}