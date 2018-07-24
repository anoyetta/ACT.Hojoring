using System;

namespace FFXIV.Framework.Extensions
{
    public static class StringExtensions
    {
        public static bool ContainsIgnoreCase(
            this string source,
            string value)
            => Contains(source, value, StringComparison.InvariantCultureIgnoreCase);

        public static bool Contains(
            this string source,
            string value,
            StringComparison comprarison)
        {
            return source.IndexOf(value, comprarison) >= 0;
        }

        public static string EscapeDoubleQuotes(
            this string source)
        {
            return source.Replace(@"""", @"\""");
        }
    }
}
