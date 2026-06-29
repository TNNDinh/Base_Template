using System;
using Ezg.Package.Factory;
using Exception = System.Exception;

namespace Ezg.Feature.Social.Account
{
    public class PlayerCampaign : DataPlayerBaseGeneric<PlayerCampaignData>
    {
        public int Level
        {
            get
            {
                try
                {
                    return dataBase.level;
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    return 99;
#endif
                    return 99;
                }
            }
            set
            {
                try
                {
                    dataBase.level = value;
                    dataBase.HighestLevel = Math.Max(value, dataBase.HighestLevel);
                    Save();
                }
                catch (Exception e)
                {
                    //
                }
            }
        }

        public int HighestLevel => dataBase.HighestLevel;
    }

    [Serializable]
    public class PlayerCampaignData : DataBase
    {
        public int level = 1;

        public int HighestLevel = 1;
    }
}