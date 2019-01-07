using Newtonsoft.Json;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class ZonesModel
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("frozen")]
        public bool Frozen { get; set; }

        [JsonProperty("encounters")]
        public BasicEntryModel[] Enconters { get; set; }
    }
}
