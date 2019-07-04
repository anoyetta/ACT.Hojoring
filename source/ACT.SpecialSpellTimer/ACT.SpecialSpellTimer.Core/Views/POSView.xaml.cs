using System;
using System.Windows;
using System.Windows.Threading;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.WPF.Views;
using FFXIV.Framework.XIVHelper;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Views
{
    /// <summary>
    /// POSView.xaml の相互作用ロジック
    /// </summary>
    public partial class POSView :
        Window,
        IOverlay
    {
        private static POSView instance;

        public static POSView Instance => instance;

        public static void ShowPOS()
        {
            instance = new POSView()
            {
                OverlayVisible = Settings.Default.POSViewVisible,
            };

            instance.Show();
        }

        public static void ClosePOS()
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }
        }

        private DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);

        public POSView()
        {
            this.InitializeComponent();

            this.DataContext = new POSViewModel();

            this.Opacity = 0;
            this.ToNonActive();

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.Loaded += this.POSView_Loaded;
            this.Closed += this.POSView_Closed;
#if !DEBUG
            this.XText.Text = string.Empty;
            this.YText.Text = string.Empty;
            this.ZText.Text = string.Empty;
#endif
            this.BaseGrid.Visibility = Visibility.Hidden;
        }

        public POSViewModel ViewModel => this.DataContext as POSViewModel;

        private bool overlayVisible;
        private bool? clickTranceparent;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value, Settings.Default.OpacityToView);
        }

        public bool ClickTransparent
        {
            get => this.clickTranceparent ?? false;
            set
            {
                if (this.clickTranceparent != value)
                {
                    this.clickTranceparent = value;

                    if (this.clickTranceparent.Value)
                    {
                        this.ToTransparent();
                    }
                    else
                    {
                        this.ToNotTransparent();
                    }
                }
            }
        }

        private void POSView_Loaded(object sender, RoutedEventArgs e)
        {
            this.timer.Interval = TimeSpan.FromSeconds(1.0);
            this.timer.Tick += this.Timer_Tick;
            this.timer.Start();

            this.SubscribeZOrderCorrector();
        }

        private void POSView_Closed(object sender, EventArgs e)
        {
            this.timer.Stop();
            this.timer = null;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!PluginMainWorker.Instance.IsFFXIVActive &&
                Settings.Default.HideWhenNotActive)
            {
                this.BaseGrid.Visibility = Visibility.Hidden;
            }
            else
            {
                var player = CombatantsManager.Instance.Player;
                if (player == null)
                {
                    this.BaseGrid.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.XText.Text = player.PosXMap.ToString("N2");
                    this.YText.Text = player.PosYMap.ToString("N2");
                    this.ZText.Text = player.PosZMap.ToString("N2");

                    this.XRaw.Text = player.PosX.ToString("N2");
                    this.YRaw.Text = player.PosY.ToString("N2");
                    this.ZRaw.Text = player.PosZ.ToString("N2");
                    this.Head.Text = player.Heading.ToString("N2");

                    this.HeadDegree.Text = player.HeadingDegree.ToString("N0");
                    this.ViewModel.HeadDegree = player.HeadingDegree;

                    /*
                    CameraInfo.Instance.Refresh();
                    if (CameraInfo.Instance.IsAvailable)
                    {
                        this.CameraMode.Text = CameraInfo.Instance.Mode.ToString();
                        this.CameraHead.Text = CameraInfo.Instance.Heading.ToString("N2");
                        this.CameraHeadDegree.Text = CameraInfo.Instance.HeadingDegree.ToString("N0");
                        this.ViewModel.CameraDegree = CameraInfo.Instance.HeadingDegree;
                        this.CameraAlt.Text = CameraInfo.Instance.Elevation.ToString("N2");
                    }
                    else
                    {
                        this.CameraMode.Text = string.Empty;
                        this.CameraHead.Text = string.Empty;
                        this.CameraHeadDegree.Text = string.Empty;
                        this.ViewModel.CameraDegree = 0;
                        this.CameraAlt.Text = string.Empty;
                    }
                    */

                    this.BaseGrid.Visibility = Visibility.Visible;
                }
            }

            // ついでにクリック透過を切り替える
            this.ClickTransparent = Settings.Default.ClickThroughEnabled;
        }
    }

    public class POSViewModel :
        BindableBase
    {
        public Settings Config => Settings.Default;

        private double headDegree = 0;
        private double cameraDegree = 0;

        public double HeadDegree
        {
            get => this.headDegree;
            set => this.SetProperty(ref this.headDegree, value);
        }

        public double CameraDegree
        {
            get => this.cameraDegree;
            set => this.SetProperty(ref this.cameraDegree, value);
        }
    }
}
