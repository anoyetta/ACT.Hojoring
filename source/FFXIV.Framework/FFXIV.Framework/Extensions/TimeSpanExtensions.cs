using System;

namespace FFXIV.Framework.Extensions
{
    public static class TimeSpanExtensions
    {
        public static string ToTLString(
            this TimeSpan ts)
            => ts.TotalSeconds >= 0 ?
            ts.ToString(@"mm\:ss\.f") :
            ts.ToString(@"\-mm\:ss\.f");

        public static string ToSecondString(
            this TimeSpan ts)
            => ts.TotalSeconds.ToString("N1");

        public static TimeSpan FromTLString(
            string timelineString)
        {
            var ts = TimeSpan.Zero;

            if (timelineString.Contains(":"))
            {
                var values = timelineString.Split(':');
                if (values.Length >= 2)
                {
                    if (double.TryParse(values[0], out double m) &&
                        double.TryParse(values[1], out double s))
                    {
                        ts = TimeSpan.FromSeconds((m * 60) + s);
                    }
                }
            }
            else
            {
                if (double.TryParse(timelineString, out double s))
                {
                    ts = TimeSpan.FromSeconds(s);
                }
            }

            return ts;
        }

        public static TimeSpan ToTimeSpan(
            this double seconds)
            => TimeSpan.FromSeconds(seconds);
    }
}
