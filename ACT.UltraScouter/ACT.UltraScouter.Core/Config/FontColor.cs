using System;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;
using FFXIV.Framework.Extensions;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// 表示テキストの設定
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "")]
    public class FontColor :
        BindableBase
    {
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
