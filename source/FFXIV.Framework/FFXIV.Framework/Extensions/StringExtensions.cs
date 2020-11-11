using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FFXIV.Framework.Extensions
{
    public static class StringExtensions
    {
        public static bool ContainsIgnoreCase(
            this string source,
            string value)
            => Contains(source, value, StringComparison.OrdinalIgnoreCase);

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

        public static string[] Split(
            this string self,
            int count)
        {
            var result = new List<string>();
            var length = (int)Math.Ceiling((double)self.Length / count);

            for (int i = 0; i < length; i++)
            {
                int start = count * i;
                if (self.Length <= start)
                {
                    break;
                }

                if (self.Length < start + count)
                {
                    result.Add(self.Substring(start));
                }
                else
                {
                    result.Add(self.Substring(start, count));
                }
            }

            return result.ToArray();
        }
    }
}
