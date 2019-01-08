using System.Data.Linq.Mapping;
using FFXIV.Framework.Extensions;
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

    [Table(Name = "rankings")]
    public class RankingModel
    {
        [JsonIgnore]
        public StatisticsDatabase Database { get; set; }

        [Column(Name = "ID", DbType = "INT", IsPrimaryKey = true)]
        public int ID { get; set; }

        [JsonProperty("encounterName")]
        [Column(Name = "encounter_name")]
        public string EncounterName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("serverName")]
        public string ServerName { get; set; }

        [JsonProperty("spec")]
        public int SpecID { get; set; }

        [JsonProperty("specName")]
        [Column(Name = "spec_name")]
        public string Spec { get; set; }

        [JsonIgnore]
        [Column(Name = "character_hash")]
        public string CharacterHash { get; set; }

        [JsonProperty("total")]
        [Column(Name = "total", DbType = "REAL")]
        public double Total { get; set; }

        [JsonProperty("regionName")]
        [Column(Name = "region")]
        public string Region { get; set; }

        public string CreateCharacterHash() => $"{this.Name}@{this.ServerName}-{this.Spec}".GetMD5();
    }
}
