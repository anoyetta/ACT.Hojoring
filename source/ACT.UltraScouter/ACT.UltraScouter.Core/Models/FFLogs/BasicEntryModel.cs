using Newtonsoft.Json;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class BasicEntryModel
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
