using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class HistogramsModel :
        BindableBase
    {
        public HistogramsModel()
        {
            this.rankList.CollectionChanged += (x, y) => this.RaisePropertyChanged(nameof(this.IsExistsRanks));
        }

        private string specName = string.Empty;

        public string SpecName
        {
            get => this.specName;
            set => this.SetProperty(ref this.specName, value);
        }

        private double minRank = 0d;

        public double MinRank
        {
            get => this.minRank;
            set => this.SetProperty(ref this.minRank, value);
        }

        private double maxRank = 0d;

        public double MaxRank
        {
            get => this.maxRank;
            set => this.SetProperty(ref this.maxRank, value);
        }

        public double maxFrequencyPercent = 0d;

        public double MaxFrequencyPercent
        {
            get => this.maxFrequencyPercent;
            set => this.SetProperty(ref this.maxFrequencyPercent, value);
        }

        private readonly ObservableCollection<HistogramModel> rankList = new ObservableCollection<HistogramModel>();

        public IEnumerable<HistogramModel> Ranks
        {
            get => this.rankList;
            set
            {
                this.rankList.Clear();
                this.rankList.AddRange(value);
                this.RaisePropertyChanged();
            }
        }

        public bool IsExistsRanks => this.rankList.Count > 0;

        public void RaiseAllPropertiesChanged()
        {
            foreach (var pi in this.GetType().GetProperties())
            {
                this.RaisePropertyChanged(pi.Name);
            }
        }

        #region Designtime Model

        public static HistogramsModel DesigntimeModel => designtimeModel ?? (designtimeModel = CreateDesigntimeModel());

        private static HistogramsModel designtimeModel;

        private static HistogramsModel CreateDesigntimeModel()
        {
            var model = new HistogramsModel()
            {
                SpecName = "Black Mage",
                Ranks = new[]
                {
                    new HistogramModel() { SpecName = "Black Mage", Rank = 1800, RankFrom = 1800, RankPercentile =  0.000 , FrequencyPercent = 0.016 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 1900, RankFrom = 1900, RankPercentile =  0.016 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2000, RankFrom = 2000, RankPercentile =  0.016 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2100, RankFrom = 2100, RankPercentile =  0.016 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2200, RankFrom = 2200, RankPercentile =  0.016 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2300, RankFrom = 2300, RankPercentile =  0.016 , FrequencyPercent = 0.016 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2400, RankFrom = 2400, RankPercentile =  0.032 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2500, RankFrom = 2500, RankPercentile =  0.032 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2600, RankFrom = 2600, RankPercentile =  0.032 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2700, RankFrom = 2700, RankPercentile =  0.032 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2800, RankFrom = 2800, RankPercentile =  0.032 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 2900, RankFrom = 2900, RankPercentile =  0.032 , FrequencyPercent = 0.016 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3000, RankFrom = 3000, RankPercentile =  0.047 , FrequencyPercent = 0.047 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3100, RankFrom = 3100, RankPercentile =  0.095 , FrequencyPercent = 0.032 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3200, RankFrom = 3200, RankPercentile =  0.126 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3300, RankFrom = 3300, RankPercentile =  0.126 , FrequencyPercent = 0.032 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3400, RankFrom = 3400, RankPercentile =  0.158 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3500, RankFrom = 3500, RankPercentile =  0.158 , FrequencyPercent = 0.032 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3600, RankFrom = 3600, RankPercentile =  0.190 , FrequencyPercent = 0.047 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3700, RankFrom = 3700, RankPercentile =  0.237 , FrequencyPercent = 0.047 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3800, RankFrom = 3800, RankPercentile =  0.284 , FrequencyPercent = 0.047 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 3900, RankFrom = 3900, RankPercentile =  0.332 , FrequencyPercent = 0.095 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4000, RankFrom = 4000, RankPercentile =  0.427 , FrequencyPercent = 0.111 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4100, RankFrom = 4100, RankPercentile =  0.537 , FrequencyPercent = 0.111 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4200, RankFrom = 4200, RankPercentile =  0.648 , FrequencyPercent = 0.142 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4300, RankFrom = 4300, RankPercentile =  0.790 , FrequencyPercent = 0.253 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4400, RankFrom = 4400, RankPercentile =  1.043 , FrequencyPercent = 0.363 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4500, RankFrom = 4500, RankPercentile =  1.406 , FrequencyPercent = 0.300 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4600, RankFrom = 4600, RankPercentile =  1.706 , FrequencyPercent = 0.427 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4700, RankFrom = 4700, RankPercentile =  2.133 , FrequencyPercent = 0.237 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4800, RankFrom = 4800, RankPercentile =  2.370 , FrequencyPercent = 0.537 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 4900, RankFrom = 4900, RankPercentile =  2.907 , FrequencyPercent = 0.648 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5000, RankFrom = 5000, RankPercentile =  3.555 , FrequencyPercent = 1.059 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5100, RankFrom = 5100, RankPercentile =  4.614 , FrequencyPercent = 1.027 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5200, RankFrom = 5200, RankPercentile =  5.641 , FrequencyPercent = 1.201 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5300, RankFrom = 5300, RankPercentile =  6.842 , FrequencyPercent = 1.738 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5400, RankFrom = 5400, RankPercentile =  8.580 , FrequencyPercent = 1.659 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5500, RankFrom = 5500, RankPercentile = 10.239 , FrequencyPercent = 1.975 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5600, RankFrom = 5600, RankPercentile = 12.214 , FrequencyPercent = 2.212 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5700, RankFrom = 5700, RankPercentile = 14.426 , FrequencyPercent = 2.639 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5800, RankFrom = 5800, RankPercentile = 17.064 , FrequencyPercent = 3.429 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 5900, RankFrom = 5900, RankPercentile = 20.493 , FrequencyPercent = 3.492 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6000, RankFrom = 6000, RankPercentile = 23.985 , FrequencyPercent = 3.760 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6100, RankFrom = 6100, RankPercentile = 27.745 , FrequencyPercent = 4.282 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6200, RankFrom = 6200, RankPercentile = 32.027 , FrequencyPercent = 4.677 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6300, RankFrom = 6300, RankPercentile = 36.704 , FrequencyPercent = 5.498 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6400, RankFrom = 6400, RankPercentile = 42.203 , FrequencyPercent = 5.024 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6500, RankFrom = 6500, RankPercentile = 47.227 , FrequencyPercent = 5.309, IsCurrent = true },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6600, RankFrom = 6600, RankPercentile = 52.536 , FrequencyPercent = 5.894 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6700, RankFrom = 6700, RankPercentile = 58.429 , FrequencyPercent = 5.135 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6800, RankFrom = 6800, RankPercentile = 63.565 , FrequencyPercent = 4.661 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 6900, RankFrom = 6900, RankPercentile = 68.226 , FrequencyPercent = 4.076 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7000, RankFrom = 7000, RankPercentile = 72.302 , FrequencyPercent = 4.234 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7100, RankFrom = 7100, RankPercentile = 76.537 , FrequencyPercent = 3.666 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7200, RankFrom = 7200, RankPercentile = 80.202 , FrequencyPercent = 3.365 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7300, RankFrom = 7300, RankPercentile = 83.568 , FrequencyPercent = 2.781 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7400, RankFrom = 7400, RankPercentile = 86.349 , FrequencyPercent = 2.496 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7500, RankFrom = 7500, RankPercentile = 88.845 , FrequencyPercent = 2.465 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7600, RankFrom = 7600, RankPercentile = 91.310 , FrequencyPercent = 1.375 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7700, RankFrom = 7700, RankPercentile = 92.684 , FrequencyPercent = 1.422 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7800, RankFrom = 7800, RankPercentile = 94.106 , FrequencyPercent = 1.343 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 7900, RankFrom = 7900, RankPercentile = 95.450 , FrequencyPercent = 1.106 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8000, RankFrom = 8000, RankPercentile = 96.556 , FrequencyPercent = 1.027 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8100, RankFrom = 8100, RankPercentile = 97.583 , FrequencyPercent = 0.679 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8200, RankFrom = 8200, RankPercentile = 98.262 , FrequencyPercent = 0.521 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8300, RankFrom = 8300, RankPercentile = 98.783 , FrequencyPercent = 0.458 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8400, RankFrom = 8400, RankPercentile = 99.242 , FrequencyPercent = 0.205 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8500, RankFrom = 8500, RankPercentile = 99.447 , FrequencyPercent = 0.284 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8600, RankFrom = 8600, RankPercentile = 99.731 , FrequencyPercent = 0.126 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8700, RankFrom = 8700, RankPercentile = 99.858 , FrequencyPercent = 0.032 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8800, RankFrom = 8800, RankPercentile = 99.889 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 8900, RankFrom = 8900, RankPercentile = 99.889 , FrequencyPercent = 0.016 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 9000, RankFrom = 9000, RankPercentile = 99.905 , FrequencyPercent = 0.016 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 9100, RankFrom = 9100, RankPercentile = 99.921 , FrequencyPercent = 0.016 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 9200, RankFrom = 9200, RankPercentile = 99.937 , FrequencyPercent = 0.016 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 9300, RankFrom = 9300, RankPercentile = 99.953 , FrequencyPercent = 0.016 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 9400, RankFrom = 9400, RankPercentile = 99.968 , FrequencyPercent = 0.016 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 9500, RankFrom = 9500, RankPercentile = 99.984 , FrequencyPercent = 0.000 },
                    new HistogramModel() { SpecName = "Black Mage", Rank = 9600, RankFrom = 9600, RankPercentile = 99.984 , FrequencyPercent = 0.016 },
                },
            };

            var i = 1;
            foreach (var rank in model.Ranks)
            {
                rank.ID = i++;
            }

            model.MaxRank = model.Ranks.Max(x => x.Rank);
            model.MinRank = model.Ranks.Min(x => x.Rank);
            model.MaxFrequencyPercent = Math.Ceiling(model.Ranks.Max(x => x.FrequencyPercent));

            foreach (var rank in model.Ranks)
            {
                rank.FrequencyRatioToMaximum = rank.FrequencyPercent / model.MaxFrequencyPercent;
            }

            return model;
        }

        #endregion Designtime Model
    }
}
