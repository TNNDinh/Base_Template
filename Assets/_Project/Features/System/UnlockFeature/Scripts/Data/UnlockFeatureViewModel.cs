using System;
using UnityEngine;
using Ezg.Feature.Shared.Config;

[Serializable]
public struct UnlockFeatureViewModel
{
    public GameEnums.Features feature;
    public Sprite icon;
}