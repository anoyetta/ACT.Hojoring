using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// ターゲットのHP
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "")]
    public class TargetHP :
        BindableBase
    {
        [XmlIgnore] private bool hpBarVisible;
        [XmlIgnore] private bool isHPValueOnHPBar;
        [XmlIgnore] private bool isHPValueCompact;
        [XmlIgnore] private bool visible;

        /// <summary>
        /// テキストの場所
        /// </summary>
        [DataMember]
        public Location TextLocation { get; set; } = new Location();

        /// <summary>
        /// バーの場所
        /// </summary>
        [DataMember]
        public Location BarLocation { get; set; } = new Location();

        /// <summary>
        /// 表示テキスト
        /// </summary>
        [DataMember]
        public DisplayText DisplayText { get; set; } = new DisplayText();

        /// <summary>
        /// HPバーを表示？
        /// </summary>
        [DataMember]
        public bool HPBarVisible
        {
            get => this.hpBarVisible;
            set => this.SetProperty(ref this.hpBarVisible, value);
        }

        /// <summary>
        /// HPの値をHPバーに重ねる？
        /// </summary>
        [DataMember]
        public bool IsHPValueOnHPBar
        {
            get => WPFHelper.IsDesignMode ? true : this.isHPValueOnHPBar;
            set => this.SetProperty(ref this.isHPValueOnHPBar, value);
        }

        /// <summary>
        /// HPの値をコンパクト表示にするか？
        /// </summary>
        [DataMember]
        public bool IsHPValueCompact
        {
            get => WPFHelper.IsDesignMode ? true : this.isHPValueCompact;
            set
            {
                if (this.SetProperty(ref this.isHPValueCompact, value))
                {
                    this.RaisePropertyChanged(nameof(IsHPValueNotCompact));
                }
            }
        }

        [XmlIgnore]
        public bool IsHPValueNotCompact =>
            WPFHelper.IsDesignMode ? true : !this.isHPValueCompact;

        /// <summary>
        /// フォントのメインカラーをバーのカラーに連動させる
        /// </summary>
        [DataMember]
        public bool LinkFontColorToBarColor { get; set; }

        /// <summary>
        /// フォントのアウトラインカラーをバーのカラーに連動させる
        /// </summary>
        [DataMember]
        public bool LinkFontOutlineColorToBarColor { get; set; }

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
    }
}
