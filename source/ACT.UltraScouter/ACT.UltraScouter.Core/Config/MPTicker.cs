using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    [DataContract(Namespace = "")]
    public class MPTicker :
        BindableBase
    {
        private bool testMode;

        /// <summary>
        /// テストモード？
        /// </summary>
        [XmlIgnore]
        public bool TestMode
        {
            get => this.testMode;
            set => this.SetProperty(ref this.testMode, value);
        }

        private bool visible;

        /// <summary>
        /// 表示？
        /// </summary>
        [DataMember]
        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        private bool isSyncHoT = true;

        /// <summary>
        /// HoTに同期する
        /// </summary>
        public bool IsSyncHoT
        {
            get => this.isSyncHoT;
            set => this.SetProperty(ref this.isSyncHoT, value);
        }

        private bool isSyncDoT;

        /// <summary>
        /// DoTに同期する
        /// </summary>
        public bool IsSyncDoT
        {
            get => this.isSyncDoT;
            set => this.SetProperty(ref this.isSyncDoT, value);
        }

        private double resyncInterval = 30.0d;

        /// <summary>
        /// 再同期までの間隔（秒)
        /// </summary>
        public double ResyncInterval
        {
            get => this.resyncInterval;
            set => this.SetProperty(ref this.resyncInterval, value);
        }

        private bool counterVisible;

        /// <summary>
        /// カウンターを表示するか？
        /// </summary>
        [DataMember]
        public bool CounterVisible
        {
            get => this.counterVisible;
            set => this.SetProperty(ref this.counterVisible, value);
        }

        private ObservableCollection<JobAvailablity> targetJobs = new ObservableCollection<JobAvailablity>();

        /// <summary>
        /// 対象とするジョブ
        /// </summary>
        [DataMember]
        public ObservableCollection<JobAvailablity> TargetJobs
        {
            get => this.targetJobs;
            set => this.SetProperty(ref this.targetJobs, value);
        }

        private double explationTimeForDisplay;

        /// <summary>
        /// 表示の有効期限（秒）
        /// </summary>
        [DataMember]
        public double ExplationTimeForDisplay
        {
            get => this.explationTimeForDisplay;
            set => this.SetProperty(ref this.explationTimeForDisplay, value);
        }

        /// <summary>
        /// 場所
        /// </summary>
        [DataMember]
        public Location Location { get; set; } = new Location();

        /// <summary>
        /// 表示テキスト
        /// </summary>
        [DataMember]
        public DisplayText DisplayText { get; set; } = new DisplayText();

        /// <summary>
        /// プログレスバー
        /// </summary>
        [DataMember]
        public ProgressBar ProgressBar { get; set; } = new ProgressBar();

        private bool useCircle;

        /// <summary>
        /// Circleを使用する？
        /// </summary>
        [DataMember]
        public bool UseCircle
        {
            get => this.useCircle;
            set
            {
                if (this.SetProperty(ref this.useCircle, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsSquareStyle));
                    this.RaisePropertyChanged(nameof(this.IsCircleStyle));
                }
            }
        }

        public bool IsSquareStyle => !this.UseCircle;

        public bool IsCircleStyle => this.UseCircle;

        private bool isCircleReverse;

        /// <summary>
        /// サークルの動作を反転させるか？
        /// </summary>
        [DataMember]
        public bool IsCircleReverse
        {
            get => this.isCircleReverse;
            set => this.SetProperty(ref this.isCircleReverse, value);
        }

        private bool swapBarAndText = false;

        /// <summary>
        /// バーとテキストの位置を入れ替える
        /// </summary>
        [DataMember]
        public bool SwapBarAndText
        {
            get => this.swapBarAndText;
            set
            {
                if (this.SetProperty(ref this.swapBarAndText, value))
                {
                    if (!this.swapBarAndText)
                    {
                        this.TextRowNumber = 0;
                        this.BarRowNumber = 2;
                    }
                    else
                    {
                        this.TextRowNumber = 2;
                        this.BarRowNumber = 0;
                    }
                }
            }
        }

        private double barRowNumber = 2;

        /// <summary>
        /// バーの行番号
        /// </summary>
        [XmlIgnore]
        public double BarRowNumber
        {
            get => this.barRowNumber;
            set => this.SetProperty(ref this.barRowNumber, value);
        }

        private double textRowNumber = 0;

        /// <summary>
        /// テキストの行番号
        /// </summary>
        [XmlIgnore]
        public double TextRowNumber
        {
            get => this.textRowNumber;
            set => this.SetProperty(ref this.textRowNumber, value);
        }

        private double syncLabelRowNumber;

        public double SyncLabelRowNumber
        {
            get => this.syncLabelRowNumber;
            set => this.SetProperty(ref this.syncLabelRowNumber, value);
        }
    }
}
