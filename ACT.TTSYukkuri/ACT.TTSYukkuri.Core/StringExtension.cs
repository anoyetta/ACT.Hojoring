using System.Security.Cryptography;
using System.Text;

namespace ACT.TTSYukkuri
{
    public static class StringExtension
    {
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
