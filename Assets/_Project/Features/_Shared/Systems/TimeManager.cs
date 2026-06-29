using System;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Ezg.Feature.Shared.Systems
{
    public static class TimeManager
    {
        /// <summary>
        ///     Ngày đầu tuần là thứ mấy
        /// </summary>
        private static readonly DayOfWeek DAY_OF_WEEK = DayOfWeek.Monday;

        private static UnityAction _nextDayAction;
        private static UnityAction _nextWeekAction;
        private static UnityAction _nextMonthAction;
        private static CancellationTokenSource _scheduleTokenSource;

        /// <summary>
        ///     Số giây được bonus thêm
        /// </summary>
        public static long BonusTimeNow;

        public static void Init()
        {
            _scheduleTokenSource?.Cancel();
            _scheduleTokenSource?.Dispose();
            _scheduleTokenSource = new CancellationTokenSource();

            SetupNextDay(_scheduleTokenSource.Token);
            SetupNextWeek(_scheduleTokenSource.Token);
            SetupNextMonth(_scheduleTokenSource.Token);
        }

        public static void ResetRuntimeState()
        {
            _nextDayAction = null;
            _nextWeekAction = null;
            _nextMonthAction = null;

            _scheduleTokenSource?.Cancel();
            _scheduleTokenSource?.Dispose();
            _scheduleTokenSource = null;

            BonusTimeNow = 0;
            OnlineTime = DateTime.UtcNow;
            StartOnlineLocalTime = 0;
            StartOnlineTime = 0;
            IsOnline = null;
        }

        private static async void SetupNextDay(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var remainingTime = (long)GetRemainingTimeToNextDay().ToMiliseconds();
                if (remainingTime is > int.MaxValue or <= 0) return;

                try
                {
                    await UniTask.Delay((int)remainingTime, true, cancellationToken: cancellationToken);
                    await UniTask.Delay(2.ToMiliseconds(), true, cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                _nextDayAction?.Invoke();
                EventManager.EmitEvent(nameof(EventName.NextDayEvent));
            }
        }

        private static async void SetupNextWeek(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var remainingTime = (long)GetRemainingTimeToNextWeek().ToMiliseconds();
                if (remainingTime is > int.MaxValue or <= 0) return;

                try
                {
                    await UniTask.Delay((int)remainingTime, true, cancellationToken: cancellationToken);
                    await UniTask.Delay(2.ToMiliseconds(), true, cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                _nextWeekAction?.Invoke();
                EventManager.EmitEvent(nameof(EventName.NextWeekEvent));
            }
        }

        private static async void SetupNextMonth(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var remainingTime = (long)GetRemainingTimeToNextMonth().ToMiliseconds();
                if (remainingTime is > int.MaxValue or <= 0) return;

                try
                {
                    await UniTask.Delay((int)remainingTime, true, cancellationToken: cancellationToken);
                    await UniTask.Delay(2.ToMiliseconds(), true, cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                _nextMonthAction?.Invoke();
                EventManager.EmitEvent(nameof(EventName.NextMonthEvent));
            }
        }

        public static void RegEventNextDay(UnityAction action)
        {
            _nextDayAction -= action;
            _nextDayAction += action;
        }

        public static void RegEventNextWeek(UnityAction action)
        {
            _nextWeekAction -= action;
            _nextWeekAction += action;
        }

        public static void RegEventMonthWeek(UnityAction action)
        {
            _nextMonthAction -= action;
            _nextMonthAction += action;
        }

        /// <summary>
        ///     Check thời gian hiện tại đã qua ngày so với last time checkin chưa
        /// </summary>
        /// <param name="lastTimeChecking"></param>
        /// <returns></returns>
        public static bool IsNextDay(long lastTimeChecking)
        {
            return DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime.Date >
                   DateTimeOffset.FromUnixTimeSeconds(lastTimeChecking).UtcDateTime.Date;
        }

        public static int GetRangeDay(long lastTimeChecking)
        {
            return
                DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime.DayOfYear - DateTimeOffset
                    .FromUnixTimeSeconds(lastTimeChecking).UtcDateTime.DayOfYear;
        }

        /// <summary>
        ///     Check thời gian hiện tại đã qua tuần so với last time checkin chưa
        /// </summary>
        /// <param name="lastTimeChecking"></param>
        /// <returns></returns>
        public static bool IsNextWeek(long lastTimeChecking)
        {
            if (lastTimeChecking == 0) return true;

            DateTimeOffset lastCheckDate = DateTimeOffset.FromUnixTimeSeconds(lastTimeChecking).UtcDateTime;

            var lastCheckWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(lastCheckDate.DateTime,
                CalendarWeekRule.FirstFourDayWeek, DAY_OF_WEEK);
            var currentWeek =
                CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                    DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime, CalendarWeekRule.FirstFourDayWeek,
                    DAY_OF_WEEK);

            return currentWeek != lastCheckWeek;
        }

        /// <summary>
        ///     Check thời gian hiện tại đã qua tuần so với last time checkin chưa
        /// </summary>
        /// <param name="lastTimeChecking"></param>
        /// <returns></returns>
        public static bool IsNextMonth(long lastTimeChecking)
        {
            DateTimeOffset lastCheckDate = DateTimeOffset.FromUnixTimeSeconds(lastTimeChecking).UtcDateTime;

            var lastCheckMonth = lastCheckDate.Month;
            var lastCheckYear = lastCheckDate.Year;
            var currentMonth = DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime.Month;
            var currentYear = DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime.Year;

            if (currentYear == lastCheckYear) return currentMonth == lastCheckMonth + 1;

            if (currentYear > lastCheckYear) return currentMonth == 1 && lastCheckMonth == 12;

            return false;
        }

        /// <summary>
        ///     Lấy thời gian hiện tại
        /// </summary>
        /// <returns></returns>
        public static long GetNow()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + BonusTimeNow;
        }

        public static DateTimeOffset GetTimeFromUnix(long unixTimeSeconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
        }

        public static long GetMillisecondsNow()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + BonusTimeNow * 1000;
        }

        public static double GetNowDouble()
        {
            return GetMillisecondsNow() / 1000d;
        }

        /// <summary>
        ///     Lấy thời điểm qua ngày
        /// </summary>
        /// <returns></returns>
        public static long GetNextDayTime()
        {
            var nextDay = DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime.Date.AddDays(1);
            var nextDayStartTime = nextDay.AddTicks(-1);
            return ((DateTimeOffset)nextDayStartTime).ToUnixTimeSeconds();

            //DateTimeOffset utcTime2 =
            //    DateTime.SpecifyKind(DateTimeOffset.FromUnixTimeSeconds(GetNow()).Date.AddDays(1).AddTicks(-1),
            //        DateTimeKind.Local);
            //return utcTime2.ToUnixTimeSeconds();
        }

        /// <summary>
        ///     Lấy thời điểm theo khung giờ, nếu quá giờ sẽ lấy thời điểm đó ngày hôm sau
        /// </summary>
        /// <param name="hour"></param>
        /// <returns></returns>
        public static long GetNextHour(int hour)
        {
            var now = DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime;
            var target = now.Date.AddHours(hour);

            if (target <= now) target = target.AddDays(1);

            return ((DateTimeOffset)target).ToUnixTimeSeconds() - ((DateTimeOffset)now).ToUnixTimeSeconds();
        }

        /// <summary>
        ///     Check xem đã qua thời điểm hay chưa
        /// </summary>
        /// <param name="hour"></param>
        /// <returns></returns>
        public static bool IsPassHour(int hour)
        {
            var now = DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime;
            var target = now.Date.AddHours(hour);

            return target <= now;
        }

        /// <summary>
        ///     Lấy thời điểm qua tuần
        /// </summary>
        /// <returns></returns>
        public static long GetNextWeekTime()
        {
            var nextWeek = DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime.Date.AddDays(
                7 - (int)DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime.Date.DayOfWeek + (int)DAY_OF_WEEK);
            var nextWeekStartTime = nextWeek.AddTicks(-1);
            return ((DateTimeOffset)nextWeekStartTime).ToUnixTimeSeconds();
        }

        /// <summary>
        ///     Lấy thời điểm qua tháng
        /// </summary>
        /// <returns></returns>
        public static long GetNextMonthTime()
        {
            var nextMonth = DateTimeOffset.FromUnixTimeSeconds(GetNow()).UtcDateTime.AddMonths(1);
            nextMonth = new DateTime(nextMonth.Year, nextMonth.Month, 1); // Đặt ngày đầu tiên của tháng tiếp theo

            return ((DateTimeOffset)nextMonth).ToUnixTimeSeconds();
        }

        /// <summary>
        ///     Lấy thời gian cho tới ngày mới
        /// </summary>
        /// <returns></returns>
        public static int GetRemainingTimeToNextDay()
        {
            return (int)(GetNextDayTime() - GetNow());
        }

        /// <summary>
        ///     Lấy thời gian cho tới tuần mới
        /// </summary>
        /// <returns></returns>
        public static int GetRemainingTimeToNextWeek()
        {
            return (int)(GetNextWeekTime() - GetNow());
        }

        /// <summary>
        ///     Lấy thời gian cho tới tháng mới
        /// </summary>
        /// <returns></returns>
        public static int GetRemainingTimeToNextMonth()
        {
            return (int)(GetNextMonthTime() - GetNow());
        }

        /// <summary>
        ///     Lấy thời gian đếm ngược
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static string GetRemainingTimeToString(long seconds)
        {
            return TimeSpan.FromSeconds(seconds)
                .ToString(seconds > 86400 ? @"dd\d\ hh\h\ mm\m" : seconds > 3600 ? @"hh\:mm\:ss" : @"mm\:ss");
        }

        /// <summary>
        ///     Lấy tổng số giây còn lại, time save sẽ được tính bằng thời điểm hiện tại + time config
        /// </summary>
        /// <param name="timeSave"></param>
        /// <returns></returns>
        public static long GetTotalSecondRemain(long timeSave)
        {
            return timeSave - GetNow();
        }

        #region Online

        public static DateTime OnlineTime = DateTime.UtcNow;

        public static long StartOnlineLocalTime;
        public static long StartOnlineTime;

        public static bool? IsOnline;

        //public static void GetOnlineTime(Action onSuccess, Action onFailed)
        //{
        //    GetUtcTimeAsync().GetOnlineTimeAction(onSuccess, onFailed);
        //}

        public static long GetOnlineNow()
        {
            return StartOnlineTime + (GetNow() - StartOnlineLocalTime);
        }

        public static DayOfWeek GetOnlineDayOfWeek()
        {
            return DateTimeOffset
                .FromUnixTimeSeconds(GetOnlineNow()).UtcDateTime.DayOfWeek;
        }

        public static int GetOnlineDayOfMonth()
        {
            return DateTimeOffset
                .FromUnixTimeSeconds(GetOnlineNow()).UtcDateTime.Day;
        }

        public static int GetOnlineDayOfYear()
        {
            return DateTimeOffset
                .FromUnixTimeSeconds(GetOnlineNow()).UtcDateTime.DayOfYear;
        }

        /// <summary>
        ///     Lấy thời gian cho tới ngày target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static long GetOnlineTimeToTargetDayOfWeek(DayOfWeek target, bool isGetEndDay, bool isSameDay = false)
        {
            var daysUntilTarget = ((int)target - (int)GetOnlineDayOfWeek() + 7 + (isSameDay ? -1 : 0)) % 7;

            var nextTarget = isGetEndDay
                ? DateTimeOffset.FromUnixTimeSeconds(GetOnlineNow()).UtcDateTime.Date
                    .AddDays(daysUntilTarget + 1 + (isSameDay ? 1 : 0)).AddTicks(-1)
                : DateTimeOffset.FromUnixTimeSeconds(GetOnlineNow()).UtcDateTime.Date
                    .AddDays(daysUntilTarget + (isSameDay ? 1 : 0));

            return new DateTimeOffset(nextTarget, TimeSpan.Zero).ToUnixTimeSeconds();
        }

        /// <summary>
        ///     Lấy thời gian cho tới ngày target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static long GetOnlineTimeToTargetDayOfMonth(int target, bool isGetEndDay)
        {
            var now = DateTimeOffset
                .FromUnixTimeSeconds(GetOnlineNow()).UtcDateTime;
            var targetDay = isGetEndDay
                ? now.Date.AddDays(target - now.Day).AddDays(1).AddTicks(-1)
                : now.Date.AddDays(target - now.Day);
            return new DateTimeOffset(targetDay, TimeSpan.Zero).ToUnixTimeSeconds();
        }

        /// <summary>
        ///     Lấy thời gian cho tới ngày target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static long GetOnlineTimeToTargetDayOfYear(int target, bool isGetEndDay)
        {
            var now = DateTimeOffset
                .FromUnixTimeSeconds(GetOnlineNow()).UtcDateTime;
            var targetDay = isGetEndDay
                ? now.Date.AddDays(target - now.Day).AddDays(1).AddTicks(-1)
                : now.Date.AddDays(target - now.Day);
            return new DateTimeOffset(targetDay, TimeSpan.Zero).ToUnixTimeSeconds();
        }

        private static async UniTask WrapErrors(this UniTask task)
        {
            await task;
        }

        private static async void GetOnlineTimeAction(this UniTask task, Action onSuccess, Action onFailed)
        {
            await task.ContinueWith(() =>
            {
                if (IsOnline == true)
                    onSuccess?.Invoke();
                else
                    onFailed?.Invoke();
            });
        }

        public static async UniTask GetOnlineTime(Action onSuccess = null, Action onFailed = null)
        {
            IsOnline = null;
            if (Application.internetReachability is NetworkReachability.NotReachable)
            {
                IsOnline = false;
                onFailed?.Invoke();
                return;
            }

            var cts = new CancellationTokenSource(10000); // 10s timeout cứng
            using var webRequest = UnityWebRequest.Get("https://m1.developer-a1f.workers.dev/api/server-time");
            webRequest.timeout = 5;

            try
            {
                await webRequest.SendWebRequest().WithCancellation(cts.Token);

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var time = webRequest.downloadHandler.text;
                    if (long.TryParse(time, out var result))
                    {
                        OnlineTime = DateTimeOffset.FromUnixTimeSeconds(result).UtcDateTime;
                        StartOnlineLocalTime = GetNow();
                        StartOnlineTime = new DateTimeOffset(OnlineTime, TimeSpan.Zero).ToUnixTimeSeconds();
                        IsOnline = true;
                        Debug.Log(OnlineTime);
                        onSuccess?.Invoke();
                    }
                }
            }
            catch
            {
                webRequest.Abort();
                IsOnline = false;
                onFailed?.Invoke();
            }
            finally
            {
                cts.Dispose();
            }
        }

        #endregion
    }
}