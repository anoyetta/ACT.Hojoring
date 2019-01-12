using System;
using System.Security.Cryptography;
using System.Text;

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

        public static string GetMD5(
            this string t)
        {
            if (string.IsNullOrEmpty(t))
            {
                return string.Empty;
            }

            var textForReturn = new StringBuilder();

            using (var md5 = new MD5CryptoServiceProvider())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(t));

                foreach (var b in hash)
                {
                    textForReturn.Append(b.ToString("X2"));
                }
            }

            return textForReturn.ToString();
        }
    }
}
