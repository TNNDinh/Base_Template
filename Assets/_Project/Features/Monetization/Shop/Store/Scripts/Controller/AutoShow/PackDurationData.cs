using System.Collections.Generic;
using Ezg.Package.Factory;

public class PackDurationData : DataBase
{
    public Dictionary<PackDurationType, long> packDurations = new();
    public Dictionary<PackDurationType, long> packDurationsToShowUI = new();
    public Dictionary<PackDurationType, int> packsBought = new();
    public Dictionary<PackDurationType, int> packScenes = new();
    public List<PackDurationType> packShowByLevel = new();
}