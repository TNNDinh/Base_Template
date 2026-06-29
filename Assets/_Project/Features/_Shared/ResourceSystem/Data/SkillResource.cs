using System;
using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;

[Serializable]
public class SkillResource : Resource
{
    public EnumBase.SkillTypes SkillType;

    public override Resource Clone()
    {
        return (SkillResource)MemberwiseClone();
    }
}