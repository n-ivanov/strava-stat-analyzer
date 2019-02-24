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
}
    
