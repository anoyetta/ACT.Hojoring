using System.Windows;
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
            this.Loaded += (_, __) => this.SubscribeZOrderCorrector();
            this.MouseLeftButtonDown += (_, __) =>
            {
                if (!this.Config.IsLock)
                {
                    this.DragMove();
                }
            };

            this.Opacity = 0;
        }

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, Settings.Instance.Opacity);
        }

        private MyStatus Config => Settings.Instance.MyHP;

        public MyMPViewModel ViewModel => this.DataContext as MyMPViewModel;
    }
}
