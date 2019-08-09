using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace ACT.Hojoring.Activator
{
    public static class HCrypt
    {
        public static IEnumerable<string> GetSalts(
            int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                yield return GetMD5(Guid.NewGuid().ToString());
            }
        }

        public static string GetHash(
            string source,
            string salt = null,
            int factor = 11)
        {
            var result = salt != null ?
                source + salt :
                source;

            var sw = Stopwatch.StartNew();

            try
            {
                result = BCrypt.Net.BCrypt.EnhancedHashPassword(
                    result,
                    factor,
                    HashType.SHA512);
            }
            finally
            {
                sw.Stop();
            }

            Debug.WriteLine($"GetHash duration={sw.ElapsedMilliseconds}ms");

            return result;
        }

        public static bool Verify(
            string hash,
            string source,
            string salt = null)
        {
            if (string.IsNullOrEmpty(source))
            {
                return true;
            }

            if (string.Equals(
                source,
                hash,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var target = salt != null ?
                source + salt :
                source;

            if (string.Equals(
                target,
                hash,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return BCrypt.Net.BCrypt.EnhancedVerify(
                target,
                hash,
                HashType.SHA512);
        }

        internal static string GetMD5(
            string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            using (var md5 = new MD5CryptoServiceProvider())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(source.ToLower()));

                foreach (var b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }
            }

            return sb.ToString();
        }
    }
}
