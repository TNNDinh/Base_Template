using System;
using Ezg.Package.Factory;

public class WeeklyPassFactory : FactoryGeneric<WeeklyPassType, WeeklyPassBaseLogic>
{
}

[Serializable]
public enum WeeklyPassType
{
    None = 0,
    Silver = 1,
    Gold = 2
}