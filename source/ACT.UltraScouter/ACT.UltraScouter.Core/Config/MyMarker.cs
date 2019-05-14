using System;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    [DataContract(Namespace = "")]
    public class MyMarker :
        BindableBase
    {
        private bool visible;

        [DataMember]
        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        [DataMember]
        public Location Location { get; set; } = new Location();

        private double scale = 1.0;

        [DataMember]
        public double Scale
        {
            get => this.scale;
            set => this.SetProperty(ref this.scale, value);
        }

        [DataMember]
        public DisplayText DisplayText { get; set; } = new DisplayText();

        private MyMarkerTypes markerType = MyMarkerTypes.ArrowUp;

        [DataMember]
        public MyMarkerTypes MarkerType
        {
            get => this.markerType;
            set
            {
                if (value == MyMarkerTypes.Arrow)
                {
                    value = MyMarkerTypes.ArrowUp;
                }

                this.SetProperty(ref this.markerType, value);
            }
        }

        [XmlIgnore]
        public Func<Size> GetOverlaySizeCallback { get; set; }
    }

    public enum MyMarkerTypes
    {
        Arrow,
        ArrowUp,
        ArrowDown,
        Plus,
        Cross,
        Line,
        Dot,
    }
}
