using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;
using Prism.Commands;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    public class TacticalRadar : BindableBase
    {
        private bool visible;

        [DataMember]
        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        private bool isDesignMode;

        [XmlIgnore]
        public bool IsDesignMode
        {
            get => this.isDesignMode;
            set => this.SetProperty(ref this.isDesignMode, value);
        }

        private double scale = 1.0d;

        [DataMember]
        public double Scale
        {
            get => this.scale;
            set => this.SetProperty(ref this.scale, value);
        }

        [DataMember]
        public Location Location { get; set; } = new Location();

        [DataMember]
        public DisplayText DisplayText { get; set; } = new DisplayText();

        private Color background = Colors.Transparent;

        [DataMember]
        public Color Background
        {
            get => this.background;
            set
            {
                if (this.SetProperty(ref this.background, value))
                {
                    var brush = new SolidColorBrush(this.background);
                    brush.Freeze();
                    this.BackgroundBrush = brush;
                }
            }
        }

        private SolidColorBrush backgroundBrush = new SolidColorBrush(Colors.Transparent);

        [XmlIgnore]
        public SolidColorBrush BackgroundBrush
        {
            get => this.backgroundBrush;
            private set => this.SetProperty(ref this.backgroundBrush, value);
        }

        private bool isHorizontalOrientation;

        [DataMember]
        public bool IsHorizontalOrientation
        {
            get => this.isHorizontalOrientation;
            set
            {
                if (this.SetProperty(ref this.isHorizontalOrientation, value))
                {
                    this.RaisePropertyChanged(nameof(this.Orientation));
                }
            }
        }

        [XmlIgnore]
        public Orientation Orientation => this.IsHorizontalOrientation ?
            Orientation.Horizontal :
            Orientation.Vertical;

        private DirectionOrigin directionOrigin = DirectionOrigin.Camera;

        [DataMember]
        public DirectionOrigin DirectionOrigin
        {
            get => this.directionOrigin;
            set => this.SetProperty(ref this.directionOrigin, value);
        }

        private readonly ObservableCollection<TacticalItem> tacticalItems = new ObservableCollection<TacticalItem>();

        [DataMember]
        [XmlArray("TacticalItems")]
        [XmlArrayItem("TacticalItem")]
        public ObservableCollection<TacticalItem> TacticalItems
        {
            get => this.tacticalItems;
            set
            {
                this.tacticalItems.Clear();
                foreach (var item in value)
                {
                    item.Parent = this;
                    this.tacticalItems.Add(item);
                }
            }
        }

        private DelegateCommand addTargetCommand;

        [XmlIgnore]
        public DelegateCommand AddTargetCommand =>
            this.addTargetCommand ?? (this.addTargetCommand = new DelegateCommand(this.ExecuteAddTargetCommand));

        private void ExecuteAddTargetCommand()
        {
            var target = new TacticalItem()
            {
                Parent = this
            };

            this.tacticalItems.Add(target);
        }
    }

    [Serializable]
    public class TacticalItem : BindableBase
    {
        [XmlIgnore]
        public TacticalRadar Parent
        {
            get;
            set;
        }

        private string targetName = string.Empty;

        [XmlAttribute("target")]
        public string TargetName
        {
            get => this.targetName;
            set => this.SetProperty(ref this.targetName, value);
        }

        private double detectRangeMinimum;

        [XmlAttribute("detect-range-min")]
        public double DetectRangeMinimum
        {
            get => this.detectRangeMinimum;
            set => this.SetProperty(ref this.detectRangeMinimum, value);
        }

        private double detectRangeMaximum = 999.9;

        [XmlAttribute("detect-range-max")]
        public double DetectRangeMaximum
        {
            get => this.detectRangeMaximum;
            set => this.SetProperty(ref this.detectRangeMaximum, value);
        }

        private bool isNoticeEnabled;

        [XmlAttribute("notice-enabled")]
        public bool IsNoticeEnabled
        {
            get => this.isNoticeEnabled;
            set => this.SetProperty(ref this.isNoticeEnabled, value);
        }

        private string tts = string.Empty;

        [XmlAttribute("tts")]
        public string TTS
        {
            get => this.tts;
            set => this.SetProperty(ref this.tts, value);
        }

        private bool isEnabled = true;

        [XmlAttribute("enabled")]
        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.SetProperty(ref this.isEnabled, value);
        }

        private DelegateCommand removeCommand;

        [XmlIgnore]
        public DelegateCommand RemoveCommand =>
            this.removeCommand ?? (this.removeCommand = new DelegateCommand(this.ExecuteRemoveCommand));

        private void ExecuteRemoveCommand()
        {
            if (this.Parent != null)
            {
                this.Parent.TacticalItems.Remove(this);
            }
        }
    }
}
