using System;
using Ezg.Core.Utils;
using Ezg.Package.Factory;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Social.Account
{
    public class PlayerLoginActivity : DataPlayerBaseGeneric<PlayerLoginActivityData>
    {
        public int ActiveDayCount => dataBase.activeDayCount;

        public long InstallTime => dataBase.installTime;

        public bool IsLoggedInToday()
        {
            if (dataBase.lastLoginTime <= 0) return false;

            var today = DateTimeOffset.FromUnixTimeSeconds(TimeManager.GetOnlineNow()).UtcDateTime.Date;
            var lastDay = DateTimeOffset.FromUnixTimeSeconds(dataBase.lastLoginTime).UtcDateTime.Date;
            return today == lastDay;
        }

        public long GetTodayLoginTime()
        {
            return IsLoggedInToday() ? dataBase.todayLoginTime : 0;
        }

        protected override void AfterLoad()
        {
            base.AfterLoad();
            EnsureInstallTime();
        }

        public void RecordDailyLogin()
        {
            var now = TimeManager.GetOnlineNow();
            EnsureInstallTime(now);

            if (!IsLoggedInToday())
            {
                dataBase.activeDayCount++;
                dataBase.todayLoginTime = now;
            }

            dataBase.lastLoginTime = now;
            Save();
        }

        private void EnsureInstallTime(long? now = null)
        {
            if (dataBase.installTime > 0) return;

            var createdTime = now ?? TimeManager.GetOnlineNow();
            dataBase.installTime = createdTime;
            Save();
        }
    }

    [Serializable]
    public class PlayerLoginActivityData : DataBase
    {
        public long installTime;
        public long lastLoginTime;
        public long todayLoginTime;
        public int activeDayCount = -1;
    }
}