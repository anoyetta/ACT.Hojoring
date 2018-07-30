using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// ターゲットのアクション（詠唱バー）
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "")]
    public class TargetAction :
        BindableBase
    {
        [XmlIgnore] private bool visible;
        [XmlIgnore] private bool castingActionNameVisible;
        [XmlIgnore] private bool castingRemainVisible;
        [XmlIgnore] private bool castingRemainInInteger;
        [XmlIgnore] private bool castingRateVisible;
        [XmlIgnore] private bool waveSoundEnabled;
        [XmlIgnore] private string waveFile;
        [XmlIgnore] private bool ttsEnabled;
        [XmlIgnore] private bool useCircle;
        [XmlIgnore] private bool isCircleReverse;
        [XmlIgnore] private double circleBlurRadius = 14;

        /// <summary>
        /// 表示テキスト
        /// </summary>
        [DataMember]
        public DisplayText DisplayText { get; set; } = new DisplayText();

        /// <summary>
        /// 場所
        /// </summary>
        [DataMember]
        public Location Location { get; set; } = new Location();

        /// <summary>
        /// プログレスバー
        /// </summary>
        [DataMember]
        public ProgressBar ProgressBar { get; set; } = new ProgressBar();

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
        /// キャスト中のアクション名を表示するか？
        /// </summary>
        [DataMember]
        public bool CastingActionNameVisible
        {
            get => this.castingActionNameVisible;
            set => this.SetProperty(ref this.castingActionNameVisible, value);
        }

        /// <summary>
        /// キャストの残りを表示するか？
        /// </summary>
        [DataMember]
        public bool CastingRemainVisible
        {
            get => this.castingRemainVisible;
            set => this.SetProperty(ref this.castingRemainVisible, value);
        }

        /// <summary>
        /// キャストの残り時間を整数表示にするか？
        /// </summary>
        [DataMember]
        public bool CastingRemainInInteger
        {
            get => this.castingRemainInInteger;
            set => this.SetProperty(ref this.castingRemainInInteger, value);
        }

        /// <summary>
        /// キャストの進捗率を表示するか？
        /// </summary>
        [DataMember]
        public bool CastingRateVisible
        {
            get => this.castingRateVisible;
            set => this.SetProperty(ref this.castingRateVisible, value);
        }

        /// <summary>
        /// WAVEサウンドが有効か？
        /// </summary>
        [DataMember]
        public bool WaveSoundEnabled
        {
            get => this.waveSoundEnabled;
            set => this.SetProperty(ref this.waveSoundEnabled, value);
        }

        /// <summary>
        /// WAVEサウンドファイル
        /// </summary>
        [DataMember]
        public string WaveFile
        {
            get => this.waveFile;
            set => this.SetProperty(ref this.waveFile, value);
        }

        /// <summary>
        /// TTSが有効か？
        /// </summary>
        [DataMember]
        public bool TTSEnabled
        {
            get => this.ttsEnabled;
            set => this.SetProperty(ref this.ttsEnabled, value);
        }

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
        /// サークルモードのときのBlurラジアス
        /// </summary>
        [DataMember]
        public double CircleBlurRadius
        {
            get => this.circleBlurRadius;
            set => this.SetProperty(ref this.circleBlurRadius, value);
        }
    }
}
