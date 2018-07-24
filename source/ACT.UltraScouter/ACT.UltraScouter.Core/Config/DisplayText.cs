using System;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// 表示テキストの設定
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "")]
    public class DisplayText :
        BindableBase
    {
        [XmlIgnore] private FontInfo font = new FontInfo();
        [XmlIgnore] private Color color;
        [XmlIgnore] private Color outlineColor;

        /// <summary>
        /// カラー
        /// </summary>
        [XmlIgnore]
        public Color Color
        {
            get => this.color;
            set => this.SetProperty(ref this.color, value);
        }

        /// <summary>
        /// カラー
        /// </summary>
        [XmlElement(ElementName = "Color")]
        [DataMember(Name = "Color", Order = 2)]
        public string ColorText
        {
            get => this.Color.ToString();
            set => this.Color = this.Color.FromString(value);
        }

        /// <summary>
        /// フォント
        /// </summary>
        [DataMember(Order = 1)]
        public FontInfo Font
        {
            get => this.font;
            set
            {
                if (!Equals(this.font, value))
                {
                    this.font = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// アウトラインのカラー
        /// </summary>
        [XmlIgnore]
        public Color OutlineColor
        {
            get => this.outlineColor;
            set => this.SetProperty(ref this.outlineColor, value);
        }

        /// <summary>
        /// アウトラインのカラー
        /// </summary>
        [XmlElement(ElementName = "OutlineColor")]
        [DataMember(Name = "OutlineColor", Order = 3)]
        public string OutlineColorText
        {
            get => this.OutlineColor.ToString();
            set => this.OutlineColor = this.OutlineColor.FromString(value);
        }
    }
}
