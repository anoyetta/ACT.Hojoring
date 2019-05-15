using System.Windows;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.ViewModels;
using FFXIV.Framework.WPF.Views;

namespace ACT.UltraScouter.Views
{
    /// <summary>
    /// MyMarkerView.xaml の相互作用ロジック
    /// </summary>
    public partial class MyMarkerView :
        Window,
        IOverlay
    {
        public MyMarkerView()
        {
            this.InitializeComponent();

            this.ToNonActive();
            this.Loaded += (s, e) => this.SubscribeZOrderCorrector();

            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            this.Opacity = 0;

            this.Loaded += (_, __) =>
            {
                this.ApplyMarkerType(this.ViewModel.Config.MarkerType);

                this.ViewModel.Config.PropertyChanged += (sender, e) =>
                {
                    var config = sender as MyMarker;

                    switch (e.PropertyName)
                    {
                        case nameof(MyMarker.MarkerType):
                            this.ApplyMarkerType(config.MarkerType);
                            break;
                    }
                };

                this.ViewModel.Config.GetOverlaySizeCallback = () =>
                    new Size(this.ActualWidth, this.ActualHeight);
            };
        }

        private void ApplyMarkerType(
            MyMarkerTypes markerType)
        {
            this.ArrowUpGrid.Visibility = Visibility.Hidden;
            this.ArrowDownGrid.Visibility = Visibility.Hidden;
            this.PlusGrid.Visibility = Visibility.Hidden;
            this.CrossGrid.Visibility = Visibility.Hidden;
            this.LineGrid.Visibility = Visibility.Hidden;
            this.DotGrid.Visibility = Visibility.Hidden;

            switch (markerType)
            {
                case MyMarkerTypes.Arrow:
                case MyMarkerTypes.ArrowUp:
                    this.ArrowUpGrid.Visibility = Visibility.Visible;
                    break;

                case MyMarkerTypes.ArrowDown:
                    this.ArrowDownGrid.Visibility = Visibility.Visible;
                    break;

                case MyMarkerTypes.Plus:
                    this.PlusGrid.Visibility = Visibility.Visible;
                    break;

                case MyMarkerTypes.Cross:
                    this.CrossGrid.Visibility = Visibility.Visible;
                    break;

                case MyMarkerTypes.Line:
                    this.LineGrid.Visibility = Visibility.Visible;
                    break;

                case MyMarkerTypes.Dot:
                    this.DotGrid.Visibility = Visibility.Visible;
                    break;
            }
        }

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, Settings.Instance.Opacity);
        }

        public MyMarkerViewModel ViewModel => (MyMarkerViewModel)this.DataContext;
    }
}
