using Pagpag.Core;

namespace Pagpag.TimeSystem
{
    public static class GameTime
    {
        public const int MinutesPerDay = 24 * 60;

        public static DayPeriod PeriodOf(int minuteOfDay)
        {
            var h = minuteOfDay / 60;
            if (h >= 6 && h < 12) return DayPeriod.Morning;
            if (h >= 12 && h < 18) return DayPeriod.Afternoon;
            if (h >= 18 && h < 22) return DayPeriod.Evening;
            return DayPeriod.LateNight;
        }

        public static string PeriodName(DayPeriod p) => p switch
        {
            DayPeriod.Morning => "Morning",
            DayPeriod.Afternoon => "Afternoon",
            DayPeriod.Evening => "Evening",
            _ => "Late Night"
        };

        /// <summary>"6:20 AM"</summary>
        public static string Clock(int minuteOfDay)
        {
            var h = (minuteOfDay / 60) % 24;
            var m = minuteOfDay % 60;
            return $"{(h % 12 == 0 ? 12 : h % 12)}:{m:00} {(h < 12 ? "AM" : "PM")}";
        }

        /// <summary>"7 AM", "12 PM", "Midnight"</summary>
        public static string Hour(int hour)
        {
            hour %= 24;
            if (hour == 0) return "Midnight";
            if (hour == 12) return "Noon";
            return $"{(hour % 12 == 0 ? 12 : hour % 12)} {(hour < 12 ? "AM" : "PM")}";
        }

        /// <summary>True if hour (0–23) is inside [from, to). Handles windows that wrap past midnight.</summary>
        public static bool InWindow(int hour, int from, int to)
        {
            if (from == to || (from == 0 && to == 24)) return true;
            return from < to ? hour >= from && hour < to : hour >= from || hour < to;
        }

        public static string Duration(int minutes)
        {
            if (minutes < 60) return $"{minutes} min";
            var h = minutes / 60;
            var m = minutes % 60;
            return m == 0 ? $"{h} hr" : $"{h} hr {m} min";
        }
    }
}
