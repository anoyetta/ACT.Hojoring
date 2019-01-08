using System.Data.Linq.Mapping;
using System.Windows.Media;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models.FFLogs
{
    [Table(Name = "histograms")]
    public class HistogramModel :
        BindableBase
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

        public SolidColorBrush RankPercentileFillBrush => ParseTotalModel.GetCategoryFillBrush((float)this.RankPercentile);

        public SolidColorBrush RankPercentileStrokeBrush => ParseTotalModel.GetCategoryStrokeBrush((float)this.RankPercentile);

        [Column(Name = "frequency_percent", DbType = "REAL")]
        public double FrequencyPercent { get; set; } = 0;

        public double Frequency { get; set; } = 0;
    }
}
