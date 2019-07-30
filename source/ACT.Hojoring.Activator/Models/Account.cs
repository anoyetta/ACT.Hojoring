using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace ACT.Hojoring.Activator.Models
{
    internal class Account
    {
        [JsonProperty(PropertyName = "n")]
        internal string Name { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "s")]
        internal string Server { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "g")]
        internal string Guild { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "src")]
        internal string Source { get; set; } = string.Empty;

        internal bool IsMatch(
            string name,
            string server,
            string guild)
        {
            var result = false;

            if (string.IsNullOrEmpty(name) &&
                string.IsNullOrEmpty(server) &&
                string.IsNullOrEmpty(guild))
            {
                return result;
            }

            if (!string.IsNullOrEmpty(this.Guild))
            {
                result = GetMD5(guild) == this.Guild;
                return result;
            }

            if (!string.IsNullOrEmpty(this.Server))
            {
                result =
                    GetMD5(name) == this.Name &&
                    GetMD5(server) == this.Server;
            }
            else
            {
                if (!string.IsNullOrEmpty(this.Name))
                {
                    result = GetMD5(name) == this.Name;
                }
            }

            return result;
        }

        internal static string GetMD5(
            string t)
        {
            if (string.IsNullOrEmpty(t))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            using (var md5 = new MD5CryptoServiceProvider())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(t.ToLower()));

                foreach (var b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }
            }

            return sb.ToString();
        }
    }
}
