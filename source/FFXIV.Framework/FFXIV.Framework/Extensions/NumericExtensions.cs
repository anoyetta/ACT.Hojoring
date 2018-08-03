using System;

namespace FFXIV.Framework.Extensions
{
    public static class NumericExtensions
    {
        public static double CeilingEx(
            this double num,
            int decimals = 0) =>
            Math.Ceiling(num * Math.Pow(10, decimals)) / Math.Pow(10, decimals);

        public static float CeilingEx(
            this float num,
            int decimals = 0) =>
            (float)(Math.Ceiling(num * Math.Pow(10, decimals)) / Math.Pow(10, decimals));

        public static double TruncateEx(
            this double num,
            int decimals = 0) =>
            Math.Truncate(num * Math.Pow(10, decimals)) / Math.Pow(10, decimals);

        public static float TruncateEx(
            this float num,
            int decimals = 0) =>
            (float)(Math.Truncate(num * Math.Pow(10, decimals)) / Math.Pow(10, decimals));
    }
}
