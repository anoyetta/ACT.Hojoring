using System;

namespace FFXIV.Framework.Extensions
{
    public static class NumericExtensions
    {
        public static double CeilingEx(
            this double num,
            int decimals = 0) =>
            Math.Ceiling(num * (10 ^ decimals)) / (10 ^ decimals);

        public static float CeilingEx(
            this float num,
            int decimals = 0) =>
            (float)(Math.Ceiling(num * (10 ^ decimals)) / (10 ^ decimals));
    }
}
