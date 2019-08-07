using System;
using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace ACT.Hojoring.Activator.Models
{
    internal class Account
    {
        [JsonProperty(PropertyName = "n")]
        [DefaultValue("")]
        internal string Name { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "s")]
        [DefaultValue("")]
        internal string Server { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "g")]
        [DefaultValue("")]
        internal string Guild { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "src")]
        [DefaultValue("")]
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
                result = GetHash(guild) == this.Guild;
                return result;
            }

            if (!string.IsNullOrEmpty(this.Server))
            {
                result =
                    GetHash(name) == this.Name &&
                    GetHash(server) == this.Server;
            }
            else
            {
                if (!string.IsNullOrEmpty(this.Name))
                {
                    result = GetHash(name) == this.Name;
                }
            }

            return result;
        }

        private static readonly Lazy<dynamic> LazyCrypto = new Lazy<dynamic>(() =>
        {
            dynamic result = null;

            try
            {
                var asm = Assembly.Load("ACT.Hojoring.Activator.Encoder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=af33eb5282ed18a3");
                var type = asm.GetType("ACT.Hojoring.Activator.Encoder.Crypto");
                result = System.Activator.CreateInstance(type);

                Logger.Instance.Write("encoder loaded.");
            }
            catch (Exception)
            {
                Logger.Instance.Write("encoder nothing.");
                result = null;
            }

            return result;
        });

        internal static string GetHash(
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

            return LazyCrypto.Value?.GetHash(sb.ToString()) ?? sb.ToString();
        }
    }
}
