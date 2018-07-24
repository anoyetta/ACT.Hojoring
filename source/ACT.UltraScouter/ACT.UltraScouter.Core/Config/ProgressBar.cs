using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;
using FFXIV.Framework.Extensions;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// プログレスバーの設定
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "")]
    public class ProgressBar :
        BindableBase
    {
        [XmlIgnore] private double height;
        [XmlIgnore] private bool linkOutlineColor;
        [XmlIgnore] private Color outlineColor;
        [XmlIgnore] private double width;
        [XmlIgnore] private ObservableCollection<ProgressBarColorRange> colorRange = new ObservableCollection<ProgressBarColorRange>();

        /// <summary>
        /// カラー範囲
        /// </summary>
        [DataMember]
        public ObservableCollection<ProgressBarColorRange> ColorRange
        {
            get => this.colorRange;
            set
            {
                var old = this.colorRange;
                if (this.SetProperty(ref this.colorRange, value))
                {
                    old.CollectionChanged -= this.ColorRangeOnCollectionChanged;
                    this.colorRange.CollectionChanged += this.ColorRangeOnCollectionChanged;
                }
            }
        }

        private void ColorRangeOnCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ProgressBarColorRange item in e.NewItems)
                {
                    item.Parent = this;
                }
            }
        }

        /// <summary>
        /// 高さ
        /// </summary>
        [DataMember]
        public double Height
        {
            get => this.height;
            set => this.SetProperty(ref this.height, value);
        }

        /// <summary>
        /// アウトラインのカラーをメインカラー（Fillカラー）に連動させる
        /// </summary>
        [DataMember]
        public bool LinkOutlineColor
        {
            get => this.linkOutlineColor;
            set => this.SetProperty(ref this.linkOutlineColor, value);
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
        [DataMember(Name = "OutlineColor")]
        public string OutlineColorText
        {
            get => this.OutlineColor.ToString();
            set => this.OutlineColor = this.OutlineColor.FromString(value);
        }

        /// <summary>
        /// 幅
        /// </summary>
        [DataMember]
        public double Width
        {
            get => this.width;
            set => this.SetProperty(ref width, value);
        }

        /// <summary>
        /// 適用対象となるカラー
        /// </summary>
        /// <param name="value">
        /// 値</param>
        /// <returns>
        /// カラー</returns>
        public Color AvailableColor(
            double value)
        {
            var c = this.ColorRange
                .Where(x => x.IsApply(value))
                .OrderBy(x => x.Min)
                .FirstOrDefault()?
                .Color;

            return c ?? Colors.Transparent;
        }
    }
}
