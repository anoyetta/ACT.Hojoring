using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;
using ACT.UltraScouter.Models.FFLogs;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    [DataContract(Namespace = "")]
    public class FFLogs :
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

        private bool hideInCombat = true;

        [DataMember]
        public bool HideInCombat
        {
            get => this.hideInCombat;
            set => this.SetProperty(ref this.hideInCombat, value);
        }

        private double scale = 1.0d;

        [DataMember]
        public double Scale
        {
            get => this.scale;
            set => this.SetProperty(ref this.scale, value);
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

        private string apiKey;

        [DataMember]
        public string ApiKey
        {
            get => this.apiKey;
            set => this.SetProperty(ref this.apiKey, value);
        }

        private FFLogsRegions serverRegion;

        [DataMember]
        public FFLogsRegions ServerRegion
        {
            get => this.serverRegion;
            set => this.SetProperty(ref this.serverRegion, value);
        }

        private double refreshInterval = 8.0d;

        [DataMember]
        public double RefreshInterval
        {
            get => this.refreshInterval;
            set => this.SetProperty(ref this.refreshInterval, value);
        }

        private ColorSet[] categoryColors = DefaultCategoryColors;

        [DataMember]
        [XmlArray]
        [XmlArrayItem(ElementName = "color")]
        public ColorSet[] CategoryColors
        {
            get => this.categoryColors;
            set
            {
                if (this.SetProperty(ref this.categoryColors, value))
                {
                    this.categoryColorDictionary = value?.ToDictionary(x => x.ID);
                    this.RaisePropertyChanged(nameof(this.CategoryColorDictionary));
                }
            }
        }

        private Dictionary<string, ColorSet> categoryColorDictionary = DefaultCategoryColors.ToDictionary(x => x.ID);

        [XmlIgnore]
        public Dictionary<string, ColorSet> CategoryColorDictionary => this.categoryColorDictionary;

        public static readonly ColorSet[] DefaultCategoryColors = new[]
        {
            new ColorSet() { ID = "A", Fill = "#e5cc80", Stroke = "#ccf0f0f0" },
            new ColorSet() { ID = "B", Fill = "#ff8000", Stroke = "#ccf0f0f0" },
            new ColorSet() { ID = "C", Fill = "#a335ee", Stroke = "#ccf0f0f0" },
            new ColorSet() { ID = "D", Fill = "#0070ff", Stroke = "#ccf0f0f0" },
            new ColorSet() { ID = "E", Fill = "#1eff00", Stroke = "#ccf0f0f0" },
            new ColorSet() { ID = "F", Fill = "#666666", Stroke = "#ccf0f0f0" },
        };
    }

    [Serializable]
    public class ColorSet :
        BindableBase
    {
        private string id;

        [XmlAttribute(AttributeName = "id")]
        public string ID
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        private string fill;

        [XmlAttribute(AttributeName = "fill")]
        public string Fill
        {
            get => this.fill;
            set
            {
                if (this.SetProperty(ref this.fill, value))
                {
                    this.RaisePropertyChanged(nameof(this.FillColor));
                }
            }
        }

        [XmlIgnore]
        public Color FillColor => (Color)ColorConverter.ConvertFromString(this.fill);

        private string stroke;

        [XmlAttribute(AttributeName = "stroke")]
        public string Stroke
        {
            get => this.stroke;
            set
            {
                if (this.SetProperty(ref this.stroke, value))
                {
                    this.RaisePropertyChanged(nameof(this.StrokeColor));
                }
            }
        }

        [XmlIgnore]
        public Color StrokeColor => (Color)ColorConverter.ConvertFromString(this.stroke);
    }
}
