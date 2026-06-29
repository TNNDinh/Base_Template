using System;
using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Package.RpgStats
{
    [Serializable]
    public class CharacterStats
    {
        public RPGStatType statType;
        public EnumBase.StatModTypes valueType;
        public float value;
    }
}