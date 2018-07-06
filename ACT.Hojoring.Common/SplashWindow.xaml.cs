using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ACT.Hojoring.Common
{
    /// <summary>
    /// SplashWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SplashWindow : Window
    {
        public static Version HojoringVersion => Assembly.GetExecutingAssembly()?.GetName()?.Version;

        private DoubleAnimation opacityAnimation = new DoubleAnimation(
            0,
            new Duration(TimeSpan.FromSeconds(1)));

        public SplashWindow()
        {
            this.InitializeComponent();

            this.ToNonActive();
            this.ToTransparent();

            var ver = HojoringVersion;
            if (ver != null)
            {
                this.VersionLabel.Content = $"v{ver.Major}.{ver.Minor}.{ver.Revision}";
            }

            Timeline.SetDesiredFrameRate(this.opacityAnimation, 30);

            this.Loaded += (x, y) =>
            {
                Task.Run(() =>
                {
                    Thread.Sleep(2000);

                    Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new Action(() =>
                            this.BeginAnimation(
                                Window.OpacityProperty,
                                this.opacityAnimation)));

                    Thread.Sleep(1010);

                    Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                            this.Close()));
                });
            };
        }

        /// <summary>
        /// アクティブにさせない
        /// </summary>
        /// <param name="x">Window</param>
        public void ToNonActive()
        {
            this.SourceInitialized += (s, e) =>
            {
                // Get this window's handle
                var hwnd = new WindowInteropHelper(this).Handle;

                // Change the extended window style to include WS_EX_TRANSPARENT
                var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                SetWindowLong(
                    hwnd,
                    GWL_EXSTYLE,
                    extendedStyle | WS_EX_NOACTIVATE);
            };
        }

        /// <summary>
        /// Click透過させる
        /// </summary>
        /// <param name="x">対象のWindow</param>
        public void ToTransparent()
        {
            // Get this window's handle
            var hwnd = new WindowInteropHelper(this).Handle;

            // Change the extended window style to include WS_EX_TRANSPARENT
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                extendedStyle | WS_EX_TRANSPARENT);
        }

        #region Win32 API

        public const int GWL_EXSTYLE = (-20);

        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        #endregion Win32 API
    }
}
