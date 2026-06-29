using System.Collections.Generic;
using Ezg.Package.Factory;
using Ezg.Feature.Shared.Config;

public class PlayerUnlockFeatureData : DataBase
{
    public Dictionary<GameEnums.Features, bool> FeatureDict = new();

    public List<int> LevelUnlocked = new();

    public List<GameEnums.Features> ListFeatureQueue = new();
}