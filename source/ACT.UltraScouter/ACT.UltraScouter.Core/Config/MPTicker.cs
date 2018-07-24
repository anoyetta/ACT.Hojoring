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
        [XmlIgnore] private bool visible;
        [XmlIgnore] private bool counterVisible;
        [XmlIgnore] private ObservableCollection<JobAvailablity> targetJobs = new ObservableCollection<JobAvailablity>();
        [XmlIgnore] private double explationTimeForDisplay;
        [XmlIgnore] private bool useCircle;
        [XmlIgnore] private bool isCircleReverse;
        [XmlIgnore] private bool testMode;
        [XmlIgnore] private bool swapBarAndText = false;
        [XmlIgnore] private double barRowNumber = 2;
        [XmlIgnore] private double textRowNumber = 0;

        /// <summary>
        /// テストモード？
        /// </summary>
        [XmlIgnore]
        public bool TestMode
        {
            get => this.testMode;
            set => this.SetProperty(ref this.testMode, value);
        }

        /// <summary>
        /// 表示？
        /// </summary>
        [DataMember]
        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        /// <summary>
        /// カウンターを表示するか？
        /// </summary>
        [DataMember]
        public bool CounterVisible
        {
            get => this.counterVisible;
            set => this.SetProperty(ref this.counterVisible, value);
        }

        /// <summary>
        /// 対象とするジョブ
        /// </summary>
        [DataMember]
        public ObservableCollection<JobAvailablity> TargetJobs
        {
            get => this.targetJobs;
            set => this.SetProperty(ref this.targetJobs, value);
        }

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

        /// <summary>
        /// サークルの動作を反転させるか？
        /// </summary>
        [DataMember]
        public bool IsCircleReverse
        {
            get => this.isCircleReverse;
            set => this.SetProperty(ref this.isCircleReverse, value);
        }

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

        /// <summary>
        /// バーの行番号
        /// </summary>
        [XmlIgnore]
        public double BarRowNumber
        {
            get => this.barRowNumber;
            set => this.SetProperty(ref this.barRowNumber, value);
        }

        /// <summary>
        /// テキストの行番号
        /// </summary>
        [XmlIgnore]
        public double TextRowNumber
        {
            get => this.textRowNumber;
            set => this.SetProperty(ref this.textRowNumber, value);
        }
    }
}
