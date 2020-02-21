using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using FFXIV.Framework.Common;
using Prism.Commands;

namespace ACT.TTSYukkuri.Config.Views
{
    public static class CevioTrayManager
    {
        private static readonly object Locker = new object();

        private static CevioTrayWindow trayWindow;

        private static SasaraConfig Config => Settings.Default.SasaraSettings;

        private static readonly double Interval = 5 * 1000;
        private static readonly double IdleInterval = 15 * 1000;

        private static readonly ThreadWorker CevioSubscriber = new ThreadWorker(() =>
        {
            if (!Config.IsHideCevioWindow)
            {
                CevioSubscriber.Interval = IdleInterval;
                return;
            }

            if (windowHandle != IntPtr.Zero)
            {
                CevioSubscriber.Interval = IdleInterval;
                return;
            }

            lock (Locker)
            {
                var handle = GetCevioWindowHandle();
                if (handle != IntPtr.Zero)
                {
                    ToIcon();
                }
            }
        },
        Interval,
        "CevioSubscriber",
        ThreadPriority.Lowest);

        public static void Start()
        {
            if (!CevioSubscriber.IsRunning)
            {
                CevioSubscriber.Run();
            }

            if (!Config.IsHideCevioWindow)
            {
                return;
            }

            lock (Locker)
            {
                if (trayWindow == null)
                {
                    trayWindow = new CevioTrayWindow();
                    trayWindow.Show();

                    WPFHelper.CurrentApp.Exit += (_, __) =>
                    {
                        RestoreWindow();
                        End();
                    };

                    WPFHelper.CurrentApp.DispatcherUnhandledException += (_, __) =>
                    {
                        RestoreWindow();
                        End();
                    };
                }
            }
        }

        public static void End()
        {
            lock (Locker)
            {
                if (trayWindow != null)
                {
                    trayWindow.CevioIcon.Visibility = Visibility.Collapsed;
                    trayWindow.Close();
                    trayWindow = null;
                }
            }
        }

        private static IntPtr windowHandle;

        public static void ToIcon()
        {
            if (!Config.IsHideCevioWindow)
            {
                return;
            }

            lock (Locker)
            {
                var handle = GetCevioWindowHandle();
                if (handle != IntPtr.Zero &&
                    windowHandle != handle)
                {
                    windowHandle = handle;
                }

                if (windowHandle != IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(windowHandle, NativeMethods.SW_HIDE);
                }
            }
        }

        public static void RestoreWindow()
        {
            lock (Locker)
            {
                if (windowHandle != IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(windowHandle, NativeMethods.SW_SHOWNA);
                }
            }
        }

        private static IntPtr GetCevioWindowHandle()
        {
            return Task.Run(() =>
            {
                var ps = Process.GetProcessesByName("CeVIO Creative Studio");
                if (ps == null ||
                    ps.Length < 1)
                {
                    return IntPtr.Zero;
                }

                return ps[0].MainWindowHandle;
            }).Result;
        }

        public static class NativeMethods
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

            public const int SW_HIDE = 0;              // ウィンドウを非表示にし、他のウィンドウをアクティブにします。
            public const int SW_SHOWNORMAL = 1;        // ウィンドウをアクティブにして表示します。ウィンドウが最小化または最大化されていた場合は、その位置とサイズを元に戻します。
            public const int SW_SHOWMINIMIZED = 2;     // ウィンドウをアクティブにして、最小化します。
            public const int SW_SHOWMAXIMIZED = 3;     // ウィンドウをアクティブにして、最大化します。
            public const int SW_MAXIMIZE = 3;          // ウィンドウを最大化します。
            public const int SW_SHOWNOACTIVATE = 4;    // ウィンドウを直前の位置とサイズで表示します。
            public const int SW_SHOW = 5;              // ウィンドウをアクティブにして、現在の位置とサイズで表示します。
            public const int SW_MINIMIZE = 6;          // ウィンドウを最小化し、Z オーダーが次のトップレベルウィンドウをアクティブにします。
            public const int SW_SHOWMINNOACTIVE = 7;   // ウィンドウを最小化します。(アクティブにはしない)
            public const int SW_SHOWNA = 8;            // ウィンドウを現在のサイズと位置で表示します。(アクティブにはしない)
            public const int SW_RESTORE = 9;           // ウィンドウをアクティブにして表示します。最小化または最大化されていたウィンドウは、元の位置とサイズに戻ります。
            public const int SW_SHOWDEFAULT = 10;      // アプリケーションを起動したプログラムが 関数に渡した 構造体で指定された SW_ フラグに従って表示状態を設定します。
            public const int SW_FORCEMINIMIZE = 11;    // たとえウィンドウを所有するスレッドがハングしていても、ウィンドウを最小化します。このフラグは、ほかのスレッドのウィンドウを最小化する場合にだけ使用してください。

            public const int GWL_EXSTYLE = (-20);

            public const int WS_EX_NOACTIVATE = 0x08000000;
            public const int WS_EX_TRANSPARENT = 0x00000020;

            [DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hwnd, int index);

            [DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        }
    }

    /// <summary>
    /// CevioTrayWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CevioTrayWindow : Window
    {
        public CevioTrayWindow()
        {
            this.InitializeComponent();
            this.Opacity = 0;
            this.ShowInTaskbar = false;

            this.SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                var extendedStyle = CevioTrayManager.NativeMethods.GetWindowLong(hwnd, CevioTrayManager.NativeMethods.GWL_EXSTYLE);
                CevioTrayManager.NativeMethods.SetWindowLong(
                    hwnd,
                    CevioTrayManager.NativeMethods.GWL_EXSTYLE,
                    extendedStyle | CevioTrayManager.NativeMethods.WS_EX_NOACTIVATE);
            };
        }

        private static readonly System.Drawing.Icon CevioIconLegacy =
            new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/ACT.TTSYukkuri.Core;component/resources/images/CeVIO.ico")).Stream);

        private DelegateCommand showCommand;

        public DelegateCommand ShowCommand =>
            this.showCommand ?? (this.showCommand = new DelegateCommand(this.ExecuteShowCommand));

        public void ExecuteShowCommand()
        {
            CevioTrayManager.RestoreWindow();
        }

        private DelegateCommand hideCommand;

        public DelegateCommand HideCommand =>
            this.hideCommand ?? (this.hideCommand = new DelegateCommand(this.ExecuteHideCommand));

        public void ExecuteHideCommand()
        {
            CevioTrayManager.ToIcon();
        }
    }
}
