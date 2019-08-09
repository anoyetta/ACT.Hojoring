using System.ComponentModel;
using Newtonsoft.Json;

namespace ACT.Hojoring.Activator.Models
{
    public class Account
    {
        [JsonProperty(PropertyName = "n")]
        [DefaultValue("")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "s")]
        [DefaultValue("")]
        public string Server { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "g")]
        [DefaultValue("")]
        public string Guild { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "src")]
        [DefaultValue("")]
        public string Source { get; set; } = string.Empty;

        internal bool IsMatch(
            string name,
            string server,
            string guild,
            string salt = null)
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
                result = HCrypt.Verify(this.Guild, guild, salt);
                return result;
            }

            if (!string.IsNullOrEmpty(this.Server))
            {
                result =
                    HCrypt.Verify(this.Name, name, salt) &&
                    HCrypt.Verify(this.Server, server, salt);
            }
            else
            {
                if (!string.IsNullOrEmpty(this.Name))
                {
                    result = HCrypt.Verify(this.Name, name, salt);
                }
            }

            return result;
        }
    }
}
