using System.Windows;
using System.Windows.Controls;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.WPF.Views;

namespace ACT.UltraScouter.Views
{
    /// <summary>
    /// MyHPView.xaml の相互作用ロジック
    /// </summary>
    public partial class MyMPView :
        Window,
        IOverlay
    {
        public MyMPView()
        {
            this.InitializeComponent();
            this.ToNonActive();

            this.Loaded += (_, __) =>
            {
                this.SubscribeZOrderCorrector();
                this.SwitchBarStyle();
            };

            this.Config.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(this.Config.BarStyle):
                        this.SwitchBarStyle();
                        break;
                }
            };

            this.MouseLeftButtonDown += (_, __) =>
            {
                if (!this.Config.IsLock)
                {
                    this.DragMove();
                }
            };

            this.Opacity = 0;
        }

        private MyStatus Config => Settings.Instance.MyMP;

        public MyMPViewModel ViewModel => this.DataContext as MyMPViewModel;

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, Settings.Instance.Opacity);
        }

        private void SwitchBarStyle()
        {
            var content = this.Config.BarStyle switch
            {
                StatusStyles.Horizontal => new MyStatusHorizontal() as UserControl,
                StatusStyles.Vertical => new MyStatusVertical() as UserControl,
                StatusStyles.Circle => new MyStatusCircle() as UserControl,
                _ => null,
            };

            if (content != null)
            {
                content.DataContext = this.DataContext;
            }

            this.BarPresenter.Content = content;
        }
    }
}
