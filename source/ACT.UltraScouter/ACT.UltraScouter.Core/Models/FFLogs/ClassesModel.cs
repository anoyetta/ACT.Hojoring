using Newtonsoft.Json;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class ClassesModel
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("specs")]
        public BasicEntryModel[] Specs { get; set; }
    }
}
