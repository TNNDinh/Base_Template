using System;
using Ezg.Feature.Shared.Config;

[Serializable]
public class UnlockFeatureModel
{
    public GameEnums.Features feature;
    public FeatureTypes type;
    public UnlockFeatureType unlockType;
    public int unlockValue;
    public string tag;
    public bool showWhenOpen;
}