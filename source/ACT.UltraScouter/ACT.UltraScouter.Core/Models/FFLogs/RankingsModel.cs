using Newtonsoft.Json;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class RankingsModel
    {
        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("hasMorePages")]
        public bool HasMorePages { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("rankings")]
        public RankingModel[] Rankings { get; set; }
    }

    public class RankingModel
    {
        [JsonIgnore]
        public string EncounterName { get; set; }

        [JsonProperty("spec")]
        public int SpecID { get; set; }

        [JsonIgnore]
        public string Spec { get; set; }

        [JsonProperty("total")]
        public float Total { get; set; }

        [JsonProperty("regionName")]
        public string Region { get; set; }
    }
}
