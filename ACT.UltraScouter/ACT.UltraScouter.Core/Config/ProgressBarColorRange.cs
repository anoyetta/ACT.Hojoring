using System;
using System.Runtime.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using ACT.UltraScouter.Config.UI.ViewModels;
using ACT.UltraScouter.Workers;
using FFXIV.Framework.Extensions;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// プログレスバーのカラーレンジの設定
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "")]
    public class ProgressBarColorRange :
        BindableBase
    {
        [XmlIgnore] private Guid id = Guid.NewGuid();
        [XmlIgnore] private Color color;
        [XmlIgnore] private double max = 0;
        [XmlIgnore] private double min = 0;

        /// <summary>
        /// ID
        /// </summary>
        [XmlIgnore]
        public Guid ID
        {
            get => this.id;
            set
            {
                if (value == null)
                {
                    value = Guid.NewGuid();
                }

                this.SetProperty(ref this.id, value);
            }
        }

        /// <summary>
        /// カラー
        /// </summary>
        [XmlIgnore]
        public Color Color
        {
            get => this.color;
            set
            {
                if (this.SetProperty(ref this.color, value))
                {
                    this.RefreshViewModel();
                }
            }
        }

        /// <summary>
        /// カラー
        /// </summary>
        [XmlElement(ElementName = "Color")]
        [DataMember(Name = "Color")]
        public string ColorText
        {
            get => this.Color.ToString();
            set => this.Color = this.Color.FromString(value);
        }

        /// <summary>
        /// このカラーが適用される最大値
        /// </summary>
        [XmlElement]
        public double Max
        {
            get => this.max;
            set
            {
                if (this.SetProperty(ref this.max, value))
                {
                    this.RefreshViewModel();
                }
            }
        }

        /// <summary>
        /// このカラーが適用される最小値
        /// </summary>
        [XmlElement]
        public double Min
        {
            get => this.min;
            set
            {
                if (this.SetProperty(ref this.min, value))
                {
                    this.RefreshViewModel();
                }
            }
        }

        /// <summary>
        /// 値がこのカラーの適用対象か？
        /// </summary>
        /// <param name="value">
        /// 値</param>
        /// <returns>真偽</returns>
        public bool IsApply(
            double value)
        {
            if (this.Max == 0 &&
                this.Min == 0)
            {
                return true;
            }

            return
                this.Min <= value &&
                value <= this.Max;
        }

        private void RefreshViewModel() => MainWorker.Instance?.RefreshAllViewModels();

        [XmlIgnore]
        public ProgressBar Parent { get; set; }

        private ICommand changeColorCommand;
        private ICommand deleteColorCommand;

        [XmlIgnore]
        public ICommand ChangeColorCommand =>
            this.changeColorCommand ?? (this.changeColorCommand = new ChangeColorRangeCommand(this.RefreshViewModel));

        [XmlIgnore]
        public ICommand DeleteColorCommand =>
            this.deleteColorCommand ?? (this.deleteColorCommand = new DeleteColorRangeCommand(this.RefreshViewModel));
    }
}
