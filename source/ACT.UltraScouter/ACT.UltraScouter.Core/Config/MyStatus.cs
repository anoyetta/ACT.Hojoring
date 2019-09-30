using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    [Serializable]
    public class MyStatus : BindableBase
    {
        private bool visible;

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

        private bool hideInNotCombat = true;

        public bool HideInNotCombat
        {
            get => this.hideInNotCombat;
            set => this.SetProperty(ref this.hideInNotCombat, value);
        }

        public Location Location { get; set; } = new Location();

        public BindableSize Size { get; set; } = new BindableSize();

        private bool isLock;

        public bool IsLock
        {
            get => this.isLock;
            set => this.SetProperty(ref this.isLock, value);
        }

        private double scale = 1.0d;

        public double Scale
        {
            get => this.scale;
            set => this.SetProperty(ref this.scale, value);
        }

        private bool visibleText = true;

        public bool VisibleText
        {
            get => this.visibleText;
            set => this.SetProperty(ref this.visibleText, value);
        }

        private bool visibleBar = true;

        public bool VisibleBar
        {
            get => this.visibleBar;
            set => this.SetProperty(ref this.visibleBar, value);
        }

        private bool linkFontColorToBarColor;

        public bool LinkFontColorToBarColor
        {
            get => this.linkFontColorToBarColor;
            set => this.SetProperty(ref this.linkFontColorToBarColor, value);
        }

        private bool linkFontOutlineColorToBarColor;

        public bool LinkFontOutlineColorToBarColor
        {
            get => this.linkFontOutlineColorToBarColor;
            set => this.SetProperty(ref this.linkFontOutlineColorToBarColor, value);
        }

        public Location TextLocation { get; set; } = new Location();

        private HorizontalAlignment textHorizontalAlignment = HorizontalAlignment.Right;

        public HorizontalAlignment TextHorizontalAlignment
        {
            get => this.textHorizontalAlignment;
            set => this.SetProperty(ref this.textHorizontalAlignment, value);
        }

        public Location BarLocation { get; set; } = new Location();

        public DisplayText DisplayText { get; set; } = new DisplayText();

        private StatusStyles barStyle = StatusStyles.Horizontal;

        public StatusStyles BarStyle
        {
            get => this.barStyle;
            set => this.SetProperty(ref this.barStyle, value);
        }

        public ProgressBar ProgressBar { get; set; } = new ProgressBar();

        [XmlIgnore]
        public Action RefreshViewDelegate { get; set; }

        public void ExecuteRefreshViewCommand()
            => this.RefreshViewDelegate?.Invoke();

        private static readonly ObservableCollection<JobAvailablity> DefaultTargetJobs = new ObservableCollection<JobAvailablity>(Settings.DefaultMPOverlayTargetJobs);

        private ObservableCollection<JobAvailablity> targetJobs = new ObservableCollection<JobAvailablity>();

        public ObservableCollection<JobAvailablity> TargetJobs
        {
            get => this.targetJobs;
            set => this.SetProperty(ref this.targetJobs, value);
        }

        public void InitTargetJobs()
        {
            var toAdd = DefaultTargetJobs
                .Where(x => !this.targetJobs.Any(y => x.Job == y.Job))
                .ToArray();

            if (toAdd.Length > 0)
            {
                var newEntries = this.targetJobs.Concat(toAdd).ToArray();
                this.targetJobs.Clear();
                this.targetJobs.AddRange(newEntries.OrderBy(x => x.Job));
            }
        }
    }

    public enum StatusStyles
    {
        Horizontal = 0,
        Vertical,
        Circle
    }
}
