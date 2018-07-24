using System;

namespace FFXIV.Framework.Extensions
{
    public static class TimeSpanExtensions
    {
        public static string ToTLString(
            this TimeSpan ts)
            => ts.TotalSeconds >= 0 ?
            ts.ToString(@"mm\:ss") :
            ts.ToString(@"\-mm\:ss");

        public static string ToSecondString(
            this TimeSpan ts)
            => ts.TotalSeconds.ToString("N0");

        public static TimeSpan FromTLString(
            string timelineString)
        {
            var ts = TimeSpan.Zero;

            if (timelineString.Contains(":"))
            {
                var values = timelineString.Split(':');
                if (values.Length >= 2)
                {
                    int m, s;
                    if (int.TryParse(values[0], out m) &&
                        int.TryParse(values[1], out s))
                    {
                        ts = new TimeSpan(0, m, s);
                    }
                }
            }
            else
            {
                double s;
                if (double.TryParse(timelineString, out s))
                {
                    ts = TimeSpan.FromSeconds(s);
                }
            }

            return ts;
        }

        public static TimeSpan ToTimeSpan(
            this int i)
            => TimeSpan.FromSeconds(i);
    }
}
