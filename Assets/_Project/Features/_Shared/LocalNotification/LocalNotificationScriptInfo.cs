using System;
using System.Collections.Generic;
using System.IO;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using UnityEngine;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.LocalNotification
{
    public sealed class LocalNotificationScriptInfo
    {
        public string Action;
        public string ContentKey;
        public string Description;
        public string Id;
        public string TitleKey;
        public string TriggerCondition;
    }

    public static class LocalNotificationScriptInfoRepository
    {
        public const string EnergyFullId = "PN001";
        public const string NewDayId = "PN002";
        public const string LunchId = "PN003";
        public const string DinnerId = "PN004";
        public const string Recall3DaysId = "PN005";
        public const string Recall7DaysId = "PN006";
        public const string StarterPackReminderId = "PN007";
        public const string SpecialOfferReminderId = "PN008";
        public const string PiggyBankReminderId = "PN009";
        public const string RecipeCompleteId = "PN010";

        private static readonly Dictionary<string, LocalNotificationScriptInfo> Cache = new();

        public static LocalNotificationScriptInfo Get(string notificationId)
        {
            EnsureLoaded();
            return Cache.TryGetValue(notificationId, out var info) ? info : null;
        }

        private static void EnsureLoaded()
        {
            if (Cache.Count > 0) return;

            var raw = LoadRawCsv();
            foreach (var row in ParseCsv(raw))
            {
                if (row.Count < 5 || string.Equals(row[0], "ID", StringComparison.OrdinalIgnoreCase)) continue;

                var info = new LocalNotificationScriptInfo
                {
                    Id = row[0],
                    Description = row[1],
                    Action = row[2],
                    TriggerCondition = row[3]
                };

                ParseLocalizeField(row[4], info);
                Cache[info.Id] = info;
            }
        }

        private static string LoadRawCsv()
        {
            var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "_Project/Features/_Shared/LocalNotification/ScriptInfo.csv"));
            if (File.Exists(projectPath)) return File.ReadAllText(projectPath);

            return @"ID,Description,Action,Trigger Condition,Localize Key
PN001,Energy Full,Application Pause,tính toán thời gian hồi full energy và hẹn giờ bắn notification,""Title: noti_energy_title
Content: noti_energy_content""
PN002,New day,Application Pause,""Ngày mai, theo device time, bắn 1 PN lúc 9:30"",""Title: noti_new_day
Content: noti_new_day_content""
PN003,Lunch time,Application Pause,""Ngày mai, theo device time, bắn 1 PN lúc 12:30"",""Title: noti_lunch_title
Content: noti_lunch_content""
PN004,Dinner time,Application Pause,""Ngày mai, theo device time, bắn 1 PN lúc 18:30"",""Title: noti_dinner_title
Content: noti_dinner_content""
PN005,Recall 3 days,Application Pause,""Nếu người chơi không đăng nhập trong 3 ngày liên tiếp, bắn 1 cái lúc 9h30 ở thời điểm ngày thứ 3"",""Title: noti_recall3_title
Content: noti_recall3_content""
PN006,Recall 7 days,Application Pause,""Nếu người chơi không đăng nhập trong 3 ngày liên tiếp, bắn 1 cái lúc 9h30 ở thời điểm ngày thứ 7"",""Title: noti_recall7_title
Content: noti_recall7_content""
PN007,Starter Pack Reminder,""Đăng ký notification khi starter pack được kích hoạt, và huỷ đăng ký nếu starter pack được purchase"",""Nếu người chơi không mua starter pack và đang được offer starter pack, bắn 1 PN remind trước khi pack expire 15 phút"",""Title: noti_starter_pack_title
Content: noti_starter_pack_content""
PN008,Special Offer Reminder,""Đăng ký notification khi Special Offer được kích hoạt, và huỷ đăng ký nếu Special Offer được purchase"",""Nếu người chơi không mua Special Offer và đang được offer Special pack, bắn 1 PN remind trước khi pack expire 15 phút"",""Title: noti_special_pack_title
Content: noti_special_pack_content""
PN009,Piggy Bank Reminder,""Đăng ký notification khi Piggy Bank được kích hoạt, và huỷ đăng ký nếu Piggy Bank được purchase"",""Nếu người chơi không mua Piggy Bank pack và đang được offer Piggy Bank pack, bắn 1 PN remind trước khi pack expire 15 phút"",""Title: noti_piggy_bank_title
Content: noti_piggy_bank_content""
PN010,Repcipe Complete,Application Pause,""Nếu người chơi sử dụng tool tạo một item theo repcipe và tgian tạo > 15', hẹn giờ bắn 1 PN để nhắc khi tool hoàn thành proceed.(task này là gameplay)"",""Title: noti_tool_done_title
Content: noti_tool_done_content""";
        }

        private static void ParseLocalizeField(string rawField, LocalNotificationScriptInfo info)
        {
            var lines = rawField.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index].Trim();
                if (line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                    info.TitleKey = line.Substring("Title:".Length).Trim();
                else if (line.StartsWith("Content:", StringComparison.OrdinalIgnoreCase))
                    info.ContentKey = line.Substring("Content:".Length).Trim();
            }
        }

        private static List<List<string>> ParseCsv(string raw)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = string.Empty;
            var inQuotes = false;

            for (var i = 0; i < raw.Length; i++)
            {
                var ch = raw[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < raw.Length && raw[i + 1] == '"')
                    {
                        cell += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    row.Add(cell);
                    cell = string.Empty;
                    continue;
                }

                if ((ch == '\n' || ch == '\r') && !inQuotes)
                {
                    if (ch == '\r' && i + 1 < raw.Length && raw[i + 1] == '\n') i++;

                    row.Add(cell);
                    rows.Add(row);
                    row = new List<string>();
                    cell = string.Empty;
                    continue;
                }

                cell += ch;
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell);
                rows.Add(row);
            }

            return rows;
        }
    }

    public static class LocalNotificationRules
    {
        // Wire this project's pause-driven rules into the package's AppPaused hook at startup,
        // so LocalNotificationService never has to reference game-specific scheduling logic.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterHooks()
        {
            LocalNotificationService.AppPaused -= RegisterPauseNotifications;
            LocalNotificationService.AppPaused += RegisterPauseNotifications;
        }

        public static void RegisterPauseNotifications()
        {
            RegisterEnergyFull();
            RegisterFixedLocalTomorrow(LocalNotificationScriptInfoRepository.NewDayId, 9, 30);
            RegisterFixedLocalTomorrow(LocalNotificationScriptInfoRepository.LunchId, 12, 30);
            RegisterFixedLocalTomorrow(LocalNotificationScriptInfoRepository.DinnerId, 18, 30);
            RegisterRecallDay(LocalNotificationScriptInfoRepository.Recall3DaysId, 3, 9, 30);
            RegisterRecallDay(LocalNotificationScriptInfoRepository.Recall7DaysId, 7, 9, 30);
            RegisterRecipeComplete();
            RegisterPiggyBankReminder();
        }

        public static void RegisterStarterPackReminder()
        {
            // removed: StarterPackService (gameplay removed)
            var endTime = PackDurationService.GetDurationPack(PackDurationType.StarterPack);
            RegisterReminderBeforeEnd(LocalNotificationScriptInfoRepository.StarterPackReminderId, endTime, 15 * 60,
                () => PackDurationService.IsPackActive(PackDurationType.StarterPack));
        }

        public static void RegisterSpecialOfferReminder()
        {
            // removed: OpenningPackService (gameplay removed)
            var endTime = PackDurationService.GetDurationPack(PackDurationType.OpenningPack);
            RegisterReminderBeforeEnd(LocalNotificationScriptInfoRepository.SpecialOfferReminderId, endTime, 15 * 60,
                () => PackDurationService.IsPackActive(PackDurationType.OpenningPack));
        }

        public static void RegisterPiggyBankReminder()
        {
            // removed: PlayerDataManager.PiggyBank (gameplay removed)
        }

        public static void StopStarterPackReminder()
        {
            LocalNotificationService.Stop(LocalNotificationScriptInfoRepository.StarterPackReminderId);
        }

        public static void StopSpecialOfferReminder()
        {
            LocalNotificationService.Stop(LocalNotificationScriptInfoRepository.SpecialOfferReminderId);
        }

        public static void StopPiggyBankReminder()
        {
            LocalNotificationService.Stop(LocalNotificationScriptInfoRepository.PiggyBankReminderId);
        }

        public static NotificationContentTemplate CreateContentTemplate(string notificationId)
        {
            var info = LocalNotificationScriptInfoRepository.Get(notificationId);
            if (info == null) return new NotificationContentTemplate();

            return new NotificationContentTemplate()
                .WithTitleKey(info.TitleKey)
                .WithBodyKey(info.ContentKey);
        }

        private static void RegisterEnergyFull()
        {
            var energyToMax = PlayerResource.GetLimitEnergy() -
                              PlayerResource.GetCurrencyValue(EnumBase.MoneyTypes.Energy);
            if (energyToMax <= 0)
            {
                LocalNotificationService.Stop(LocalNotificationScriptInfoRepository.EnergyFullId);
                return;
            }

            var secondsPerEnergy = (long)Math.Ceiling(PlayerResource.GetTimeRestoreEnergy());
            LocalNotificationService.RegisterOrReplace(
                LocalNotificationScriptInfoRepository.EnergyFullId,
                new NotificationDefinition(LocalNotificationScriptInfoRepository.EnergyFullId)
                    .WithContent(CreateContentTemplate(LocalNotificationScriptInfoRepository.EnergyFullId))
                    .WithSchedule(() =>
                        NotificationScheduleRequest.CreateDelay(Math.Max(1L, energyToMax * secondsPerEnergy))));
        }

        private static void RegisterFixedLocalTomorrow(string notificationId, int hour, int minute)
        {
            var now = DateTime.Now;
            var tomorrow = now.Date.AddDays(1).AddHours(hour).AddMinutes(minute);
            LocalNotificationService.RegisterOrReplace(
                notificationId,
                new NotificationDefinition(notificationId)
                    .WithContent(CreateContentTemplate(notificationId))
                    .WithSchedule(() => NotificationScheduleRequest.CreateFireAtLocal(tomorrow)));
        }

        private static void RegisterRecallDay(string notificationId, int days, int hour, int minute)
        {
            var target = DateTime.Now.Date.AddDays(days).AddHours(hour).AddMinutes(minute);
            LocalNotificationService.RegisterOrReplace(
                notificationId,
                new NotificationDefinition(notificationId)
                    .WithContent(CreateContentTemplate(notificationId))
                    .WithSchedule(() => NotificationScheduleRequest.CreateFireAtLocal(target)));
        }

        private static void RegisterRecipeComplete()
        {
            // removed: ItemToolManager (gameplay removed) — no cooking tools to schedule against
            LocalNotificationService.Stop(LocalNotificationScriptInfoRepository.RecipeCompleteId);
        }

        private static void RegisterReminderBeforeEnd(string notificationId, long endUnixTime, long remindBeforeSeconds,
            Func<bool> enabledPredicate)
        {
            if (enabledPredicate != null && !enabledPredicate.Invoke())
            {
                LocalNotificationService.Stop(notificationId);
                return;
            }

            var fireAt = endUnixTime - remindBeforeSeconds;
            var delay = fireAt - TimeManager.GetNow();
            if (delay <= 0)
            {
                LocalNotificationService.Stop(notificationId);
                return;
            }

            LocalNotificationService.RegisterOrReplace(
                notificationId,
                new NotificationDefinition(notificationId)
                    .WithContent(CreateContentTemplate(notificationId))
                    .WithEnabledPredicate(enabledPredicate)
                    .WithSchedule(() => NotificationScheduleRequest.CreateDelay(delay)));
        }
    }
}