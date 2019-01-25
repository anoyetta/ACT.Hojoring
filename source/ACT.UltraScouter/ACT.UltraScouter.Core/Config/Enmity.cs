using System;
using System.Runtime.Serialization;
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
            set => this.SetProperty(ref this.background, value);
        }

        private bool isDesignMode;

        [XmlIgnore]
        public bool IsDesignMode
        {
            get => this.isDesignMode;
            set => this.SetProperty(ref this.isDesignMode, value);
        }
    }
}
