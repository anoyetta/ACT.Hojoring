using System.Runtime.Serialization;
using System.Windows.Media;
using Newtonsoft.Json;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class ParseModel :
        BindableBase
    {
        [DataMember(Name = "encounterID")]
        [JsonProperty("encounterID")]
        public int EncounterID { get; set; }

        [DataMember(Name = "encounterName")]
        [JsonProperty("encounterName")]
        public string EncounterName { get; set; }

        [DataMember(Name = "class")]
        [JsonProperty("class")]
        public string DataClass { get; set; }

        [DataMember(Name = "spec")]
        [JsonProperty("spec")]
        public string Spec { get; set; }

        [DataMember(Name = "rank")]
        [JsonProperty("rank")]
        public int Rank { get; set; }

        [DataMember(Name = "outOf")]
        [JsonProperty("outOf")]
        public int OutOf { get; set; }

        [DataMember(Name = "duration")]
        [JsonProperty("duration")]
        public long Duration { get; set; }

        [DataMember(Name = "startTime")]
        [JsonProperty("startTime")]
        public long StartTime { get; set; }

        [DataMember(Name = "reportID")]
        [JsonProperty("reportID")]
        public string ReportID { get; set; }

        [DataMember(Name = "fightID")]
        [JsonProperty("fightID")]
        public int FightID { get; set; }

        [DataMember(Name = "difficulty")]
        [JsonProperty("difficulty")]
        public int Difficulty { get; set; }

        [DataMember(Name = "characterID")]
        [JsonProperty("characterID")]
        public int CharacterID { get; set; }

        [DataMember(Name = "characterName")]
        [JsonProperty("characterName")]
        public string CharacterName { get; set; }

        [DataMember(Name = "server")]
        [JsonProperty("server")]
        public string Server { get; set; }

        [DataMember(Name = "percentile")]
        [JsonProperty("percentile")]
        public float Percentile { get; set; }

        [DataMember(Name = "ilvlKeyOrPatch")]
        [JsonProperty("ilvlKeyOrPatch")]
        public float IlvlKeyOrPatch { get; set; }

        [DataMember(Name = "total")]
        [JsonProperty("total")]
        public float Total { get; set; }

        [DataMember(Name = "estimated")]
        [JsonProperty("estimated")]
        public bool Estimated { get; set; }

        [JsonIgnore]
        public string Category => ParseTotalModel.GetCategory(this.Percentile);

        [JsonIgnore]
        public Color CategoryColor => ParseTotalModel.GetCategoryColor(this.Percentile);

        [JsonIgnore]
        public SolidColorBrush CategoryBrush => ParseTotalModel.GetCategoryBrush(this.Percentile);
    }
}
