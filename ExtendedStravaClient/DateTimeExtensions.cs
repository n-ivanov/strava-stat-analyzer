using System;

namespace System.Extensions
{
    public static class DateTimeExtensions
    {
        private static DateTime epoch_ = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static int ToEpoch(this DateTime date)
        {
            DateTime origin = epoch_;
            TimeSpan diff = date.ToUniversalTime() - origin;
            return (int)Math.Floor(diff.TotalSeconds);
        }

        public static DateTime FromEpoch(this int epochTime)
        {
            DateTime origin = epoch_;
            return origin.AddSeconds(epochTime);
        }
    }

    public static class TimeExtensions
    {
        public static string ToTime(this int time)
        {
            var seconds = time % 60;
            var totalMinutes = time / 60;
            var hours = totalMinutes / 60;
            return $"{hours:00}:{totalMinutes%60:00}:{seconds:00}";
        }

        public static string ToTime(this double time)
        {
            return ((int)Math.Floor(time)).ToTime();
        }
    }
}