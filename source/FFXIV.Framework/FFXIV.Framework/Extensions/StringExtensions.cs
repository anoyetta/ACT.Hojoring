using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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

        private static readonly DataTable dataTable = new DataTable();

        private static readonly HashSet<string> syntaxErrorStrings = new HashSet<string>();

        public static object Eval(
            this string text,
            params object[] args)
        {
            text = string.Format(text, args);

            try
            {
                if (syntaxErrorStrings.Contains(text))
                {
                    return null;
                }

                return dataTable.Compute(text, string.Empty);
            }
            catch (SyntaxErrorException)
            {
                syntaxErrorStrings.Add(text);
                throw;
            }
        }

        public static bool TryParse0xString2Int(
            this string text,
            out int i)
        {
            i = 0;

            if (!string.IsNullOrEmpty(text))
            {
                if (text.Length > 2 && text.StartsWith("0x"))
                {
                    if (int.TryParse(
                        text.Substring(2),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out i))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
