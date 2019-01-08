using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            }
        }

        public bool IsExistsRanks => this.rankList.Count > 0;
    }
}
