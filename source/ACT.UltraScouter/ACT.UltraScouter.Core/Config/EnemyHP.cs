using System;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;
using FFXIV.Framework.Common;
using Prism.Mvvm;

namespace ACT.UltraScouter.Config
{
    /// <summary>
    /// 敵のHP
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "")]
    public class EnemyHP :
        BindableBase
    {
        private bool visible;

        public bool Visible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }

        private bool isDesignMode;

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

#if DEBUG
        private Color background = WPFHelper.IsDesignMode ? Colors.WhiteSmoke : Colors.Transparent;
#else
        private Color background = Colors.Transparent;
#endif

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

        public DisplayText DisplayText { get; set; } = new DisplayText();

        public ProgressBar ProgressBar { get; set; } = new ProgressBar();

        [XmlIgnore]
        public Action RefreshViewDelegate { get; set; }

        public void ExecuteRefreshViewCommand()
        {
            this.RaisePropertyChanged(nameof(this.Background));
            this.RefreshViewDelegate?.Invoke();
        }
    }
}
