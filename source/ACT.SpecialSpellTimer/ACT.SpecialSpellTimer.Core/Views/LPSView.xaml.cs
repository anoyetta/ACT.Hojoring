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
    /// LPSView.xaml の相互作用ロジック
    /// </summary>
    public partial class LPSView :
        Window,
        IOverlay
    {
        private static LPSView instance;

        public static LPSView Instance => instance;

        public static void ShowLPS()
        {
            instance = new LPSView()
            {
                OverlayVisible = Settings.Default.LPSViewVisible
            };

            instance.Show();
        }

        public static void CloseLPS()
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }
        }

        private DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);

        public LPSView()
        {
            this.InitializeComponent();

            this.DataContext = new LPSViewModel();

            this.Opacity = 0;
            this.ToNonActive();

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.Loaded += this.LPSView_Loaded;
            this.Closed += this.LPSView_Closed;
#if !DEBUG
            this.LPSTextBlock.Text = string.Empty;
#endif
        }

        public LPSViewModel ViewModel => this.DataContext as LPSViewModel;

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

        private void LPSView_Loaded(object sender, RoutedEventArgs e)
        {
            this.timer.Interval = TimeSpan.FromSeconds(3.1);
            this.timer.Tick += this.Timer_Tick;
            this.timer.Start();

            this.SubscribeZOrderCorrector();
        }

        private void LPSView_Closed(object sender, EventArgs e)
        {
            this.timer.Stop();
            this.timer = null;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            this.LPSTextBlock.Text = XIVPluginHelper.Instance.LPS.ToString("N0");

            // ついでにクリック透過を切り替える
            this.ClickTransparent = Settings.Default.ClickThroughEnabled;

            // アクティブによる表示切り替えは内側のグリッドで切り替える
            if (Settings.Default.HideWhenNotActive)
            {
                this.BaseGrid.Visibility = PluginMainWorker.Instance.IsFFXIVActive ?
                    Visibility.Visible :
                    Visibility.Collapsed;
            }
            else
            {
                this.BaseGrid.Visibility = Visibility.Visible;
            }
        }
    }

    public class LPSViewModel :
        BindableBase
    {
        public Settings Config => Settings.Default;
    }
}
