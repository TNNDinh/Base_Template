using System;
using Cysharp.Threading.Tasks;
using Ezg.Feature.Shared;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.System.Settings
{
    public static class SettingManager
    {
        private static bool _isStartCountTime;

        /// <summary>
        ///     Đếm tgian online của user
        /// </summary>
        /// <returns></returns>
        public static async UniTask CountOnlineTime()
        {
            if (_isStartCountTime) return;

            Start:
            var delayTime = new TimeSpan(1000);
            await UniTask.Delay(delayTime, DelayType.Realtime);
            PlayerDataManager.Settings.dataBase.OnlineTime++;
            goto Start;
        }
    }
}