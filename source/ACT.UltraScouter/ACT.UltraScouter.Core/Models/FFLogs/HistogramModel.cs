using System.Data.Linq.Mapping;

namespace ACT.UltraScouter.Models.FFLogs
{
    [Table(Name = "histograms")]
    public class HistogramModel
    {
        [Column(Name = "histogram_id", DbType = "INT", IsPrimaryKey = true)]
        public int ID { get; set; }

        [Column(Name = "spec_name")]
        public string SpecName { get; set; }

        [Column(Name = "rank", DbType = "REAL")]
        public double Rank { get; set; } = 0;

        [Column(Name = "rank_from", DbType = "REAL")]
        public double RankFrom { get; set; } = 0;

        [Column(Name = "rank_percentile", DbType = "REAL")]
        public double RankPercentile { get; set; } = 0;

        [Column(Name = "frequency_percent", DbType = "REAL")]
        public double FrequencyPercent { get; set; } = 0;

        public double Frequency { get; set; } = 0;
    }
}
