using System;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    [DataContract(Namespace = "")]
    public class Enmity :
        BindableBase
    {
        private bool isDesignMode;

        [XmlIgnore]
        public bool IsDesignMode
        {
            get => this.isDesignMode;
            set => this.SetProperty(ref this.isDesignMode, value);
        }

        [DataMember]
        public DisplayText DisplayText { get; set; } = new DisplayText();

        [DataMember]
        public Location Location { get; set; } = new Location();

        private bool visible;

        [DataMember]
        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        private bool hideInNotCombat = true;

        [DataMember]
        public bool HideInNotCombat
        {
            get => this.hideInNotCombat;
            set => this.SetProperty(ref this.hideInNotCombat, value);
        }

        private bool hideInSolo = true;

        [DataMember]
        public bool HideInSolo
        {
            get => this.hideInSolo;
            set => this.SetProperty(ref this.hideInSolo, value);
        }

        private bool isSelfDisplayYou = true;

        [DataMember]
        public bool IsSelfDisplayYou
        {
            get => this.isSelfDisplayYou;
            set => this.SetProperty(ref this.isSelfDisplayYou, value);
        }

        private bool isDenomi = false;

        [DataMember]
        public bool IsDenomi
        {
            get => this.isDenomi;
            set => this.SetProperty(ref this.isDenomi, value);
        }

        private bool isVisibleIcon = true;

        [DataMember]
        public bool IsVisibleIcon
        {
            get => this.isVisibleIcon;
            set
            {
                if (this.SetProperty(ref this.isVisibleIcon, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsVisibleIconLeft));
                    this.RaisePropertyChanged(nameof(this.IsVisibleIconRight));
                }
            }
        }

        private bool isIconRight;

        [DataMember]
        public bool IsIconRight
        {
            get => this.isIconRight;
            set
            {
                if (this.SetProperty(ref this.isIconRight, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsVisibleIconLeft));
                    this.RaisePropertyChanged(nameof(this.IsVisibleIconRight));
                }
            }
        }

        [XmlIgnore]
        public bool IsVisibleIconLeft => this.isVisibleIcon && !this.isIconRight;

        [XmlIgnore]
        public bool IsVisibleIconRight => this.isVisibleIcon && this.IsIconRight;

        private bool isVisibleName = true;

        [DataMember]
        public bool IsVisibleName
        {
            get => this.isVisibleName;
            set => this.SetProperty(ref this.isVisibleName, value);
        }

        private double iconScale = 1.0d;

        [DataMember]
        public double IconScale
        {
            get => this.iconScale;
            set => this.SetProperty(ref this.iconScale, value);
        }

        private double scale = 1.0d;

        [DataMember]
        public double Scale
        {
            get => this.scale;
            set => this.SetProperty(ref this.scale, value);
        }

        private double barWidth = 250d;

        [DataMember]
        public double BarWidth
        {
            get => this.barWidth;
            set => this.SetProperty(ref this.barWidth, value);
        }

        private double barHeight = 6d;

        [DataMember]
        public double BarHeight
        {
            get => this.barHeight;
            set => this.SetProperty(ref this.barHeight, value);
        }

        private VerticalAlignment barVerticalAlignment = VerticalAlignment.Center;

        [DataMember]
        public VerticalAlignment BarVerticalAlignment
        {
            get => this.barVerticalAlignment;
            set => this.SetProperty(ref this.barVerticalAlignment, value);
        }

        [DataMember]
        private HorizontalAlignment barHorizontalAlignment = HorizontalAlignment.Left;

        public HorizontalAlignment BarHorizontalAlignment
        {
            get => this.barHorizontalAlignment;
            set => this.SetProperty(ref this.barHorizontalAlignment, value);
        }

        private double scaningRate = 250;

        [DataMember]
        public double ScaningRate
        {
            get => this.scaningRate;
            set => this.SetProperty(ref this.scaningRate, value);
        }

        private int maxCountOfDisplay = 8;

        [DataMember]
        public int MaxCountOfDisplay
        {
            get => this.maxCountOfDisplay;
            set => this.SetProperty(ref this.maxCountOfDisplay, value);
        }

        private Color background = Colors.Transparent;

        [DataMember]
        public Color Background
        {
            get => this.background;
            set
            {
                if (this.SetProperty(ref this.background, value))
                {
                    this.RaisePropertyChanged(nameof(this.BackgroundOpacity));
                }
            }
        }

        [XmlIgnore]
        public double BackgroundOpacity => (double)this.background.A / 255d;

        private bool isDisplayDifference;

        [DataMember]
        public bool IsDisplayDifference
        {
            get => this.isDisplayDifference;
            set
            {
                if (this.SetProperty(ref this.isDisplayDifference, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsNotDisplayDifference));
                }
            }
        }

        [XmlIgnore]
        public bool IsNotDisplayDifference => !this.IsDisplayDifference;

        private string alterTextMeValue = "YOU";

        [DataMember]
        public string AlterTextMeValue
        {
            get => this.alterTextMeValue;
            set => this.SetProperty(ref this.alterTextMeValue, value);
        }

        private bool isNearIndicator;

        [DataMember]
        public bool IsNearIndicator
        {
            get => this.isNearIndicator;
            set => this.SetProperty(ref this.isNearIndicator, value);
        }

        [XmlIgnore]
        public static readonly Color DefaultNearColor = (Color)ColorConverter.ConvertFromString("#e60033");

        private Color nearColor = DefaultNearColor;

        [DataMember]
        public Color NearColor
        {
            get => this.nearColor;
            set => this.SetProperty(ref this.nearColor, value);
        }

        private double nearThresholdRate = 5.0;

        [DataMember]
        public double NearThresholdRate
        {
            get => this.nearThresholdRate;
            set => this.SetProperty(ref this.nearThresholdRate, value);
        }

        private bool isVisibleNearThreshold;

        [DataMember]
        public bool IsVisibleNearThreshold
        {
            get => this.isVisibleNearThreshold;
            set => this.SetProperty(ref this.isVisibleNearThreshold, value);
        }

        private string logDirectory = string.Empty;

        [DataMember]
        public string LogDirectory
        {
            get => this.logDirectory;
            set => this.SetProperty(ref this.logDirectory, value);
        }
    }
}
